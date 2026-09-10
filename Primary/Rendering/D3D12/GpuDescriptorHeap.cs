using Primary.Common;
using Primary.Rendering.Resources;
using Primary.RHI;
using Primary.RHI.Direct3D12;
using Primary.Utility;
using System.Runtime.Versioning;
using System.Diagnostics;
using Primary.Rendering.Assets;
using Silk.NET.Direct3D12;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using System.Runtime.CompilerServices;

namespace Primary.Rendering.D3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe sealed class GpuDescriptorHeap : IDisposable
    {
        private readonly NRDDevice _device;

        private readonly int _descriptorHandleSize;
        private readonly int _descriptorHeapSize;

        private readonly DescriptorHeapType _heapType;

        private AverageAnalyser<int> _averageDescriptorUse;
        private int _descriptorsUsedThisFrame;

        private List<HeapData> _activeHeaps;
        private int _activeHeapIndex;
        private int _activeHeapOffset;

        private Dictionary<ResourceData, uint> _activeDescriptors;

        private bool _disposedValue;

        internal GpuDescriptorHeap(NRDDevice device, int heapSize, DescriptorHeapType heapType)
        {
            _device = device;

            _descriptorHandleSize = (int)device.Device->GetDescriptorHandleIncrementSize(heapType);
            _descriptorHeapSize = heapSize;

            _heapType = heapType;

            _averageDescriptorUse = new AverageAnalyser<int>(16, 0.0f);
            _descriptorsUsedThisFrame = 0;

            _activeHeaps = new List<HeapData>();
            _activeHeapIndex = 0;
            _activeHeapOffset = 0;

            _activeDescriptors = new Dictionary<ResourceData, uint>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                foreach (HeapData heap in _activeHeaps)
                    heap.Heap.Dispose();
                _activeHeaps.Clear();

                _disposedValue = true;
            }
        }

        ~GpuDescriptorHeap()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ResetForNewFrame()
        {
            _averageDescriptorUse.Sample(_descriptorsUsedThisFrame, 1.0f);
            _descriptorsUsedThisFrame = 0;

            _activeHeapIndex = 0;
            _activeHeapOffset = 0;

            _activeDescriptors.Clear();
        }

        internal uint GetDescriptorIndex(NRDResource resource, bool bindAsUnorderedAccess, PropertyBindIntent intent, out bool changedActiveHeap)
        {
            changedActiveHeap = false;

            if (resource.IsNull)
                return ushort.MaxValue;

            ResourceData resData = new ResourceData(resource, bindAsUnorderedAccess, intent);
            if (_activeDescriptors.TryGetValue(resData, out uint index))
                return index;

            if (_activeHeapIndex >= _activeHeaps.Count || _activeHeapOffset >= _descriptorHeapSize)
            {
                if (_activeHeapOffset >= _descriptorHeapSize)
                    ++_activeHeapIndex;

                if (_activeHeapIndex >= _activeHeaps.Count)
                    AddNewHeapToList();

                _activeDescriptors.Clear();
                _activeHeapOffset = 0;

                changedActiveHeap = true;
            }

            HeapData heap = _activeHeaps[_activeHeapIndex];

            index = (uint)_activeDescriptors.Count;
            _activeDescriptors[resData] = index;

            ID3D12Resource* res = (ID3D12Resource*)_device.ResourceManager.GetResource(resource);

            CpuDescriptorHandle dstDescriptor = new CpuDescriptorHandle((nuint)(heap.StartHandle.Ptr + (ulong)_activeHeapOffset));

            switch (resource.Id)
            {
                case NRDResourceId.Texture:
                    {
                        if (bindAsUnorderedAccess)
                        {
                            UnorderedAccessViewDesc desc;

                            if (resource.IsExternal)
                            {
                                RHITextureDescription texDesc = ((D3D12RHITextureNative*)resource.Native)->Base.Description;

                                desc = new UnorderedAccessViewDesc
                                {
                                    ViewDimension = texDesc.Dimension switch
                                    {
                                        RHIDimension.Texture1D => UavDimension.Texture1D,
                                        RHIDimension.Texture2D => UavDimension.Texture2D,
                                        RHIDimension.Texture3D => UavDimension.Texture3D,
                                        _ => throw new NotImplementedException(),
                                    },
                                    Format = texDesc.Format.ToResourceViewFormat(),
                                };
                            }
                            else
                            {
                                FrameGraphTexture fg = _device.ResourceManager.FindFGTexture(resource);
                                ref readonly FrameGraphTextureDesc texDesc = ref fg.Description;

                                desc = new UnorderedAccessViewDesc
                                {
                                    ViewDimension = texDesc.Dimension switch
                                    {
                                        FGTextureDimension._1D => UavDimension.Texture1D,
                                        FGTextureDimension._2D => UavDimension.Texture2D,
                                        FGTextureDimension._3D => UavDimension.Texture3D,
                                        _ => throw new NotImplementedException(),
                                    },
                                    Format = texDesc.Format.ToResourceViewFormat(),
                                };
                            }

                            switch (desc.ViewDimension)
                            {
                                case UavDimension.Texture1D:
                                    {
                                        desc.Texture1D = new Tex1DUav
                                        {
                                            MipSlice = 0
                                        };
                                        break;
                                    }
                                case UavDimension.Texture2D:
                                    {
                                        desc.Texture2D = new Tex2DUav
                                        {
                                            MipSlice = 0,
                                            PlaneSlice = 0,
                                        };
                                        break;
                                    }
                                case UavDimension.Texture3D:
                                    {
                                        desc.Texture3D = new Tex3DUav
                                        {
                                            MipSlice = 0,
                                            FirstWSlice = 0,
                                            WSize = 0xffffffff
                                        };
                                        break;
                                    }
                            }

                            _device.Device->CreateUnorderedAccessView(res, null, &desc, dstDescriptor);
                        }
                        else
                        {
                            ShaderResourceViewDesc desc;

                            if (resource.IsExternal)
                            {
                                RHITextureDescription texDesc = ((D3D12RHITextureNative*)resource.Native)->Base.Description;

                                RHIFormat format = texDesc.Format;
                                if (Flags.HasFlag(texDesc.Usage, RHIResourceUsage.DepthStencil))
                                {
                                    if (intent == PropertyBindIntent.AsStencil)
                                        format = format.ToStencilFormat();
                                    else
                                        format = format.ToDepthFormat();

                                    if (format == RHIFormat.Unknown)
                                        format = texDesc.Format;
                                }

                                desc = new ShaderResourceViewDesc
                                {
                                    ViewDimension = texDesc.Dimension switch
                                    {
                                        RHIDimension.Texture1D => SrvDimension.Texture1D,
                                        RHIDimension.Texture2D => SrvDimension.Texture2D,
                                        RHIDimension.Texture3D => SrvDimension.Texture3D,
                                        RHIDimension.TextureCube => SrvDimension.Texturecube,
                                        _ => throw new NotImplementedException(),
                                    },
                                    Format = format.ToTextureFormat(),
                                    Shader4ComponentMapping = ResourceHelper.EncodeShader4ComponentMapping((uint)texDesc.Swizzle.R, (uint)texDesc.Swizzle.G, (uint)texDesc.Swizzle.B, (uint)texDesc.Swizzle.A),
                                };
                            }
                            else
                            {
                                FrameGraphTexture fg = _device.ResourceManager.FindFGTexture(resource);
                                ref readonly FrameGraphTextureDesc texDesc = ref fg.Description;

                                RHIFormat format = texDesc.Format;
                                if (Flags.HasFlag(texDesc.Usage, FGTextureUsage.DepthStencil))
                                {
                                    if (intent == PropertyBindIntent.AsStencil)
                                        format = format.ToStencilFormat();
                                    else
                                        format = format.ToDepthFormat();

                                    if (format == RHIFormat.Unknown)
                                        format = texDesc.Format;
                                }

                                desc = new ShaderResourceViewDesc
                                {
                                    ViewDimension = texDesc.Dimension switch
                                    {
                                        FGTextureDimension._1D => SrvDimension.Texture1D,
                                        FGTextureDimension._2D => SrvDimension.Texture2D,
                                        FGTextureDimension._3D => SrvDimension.Texture3D,
                                        FGTextureDimension.Cube => SrvDimension.Texturecube,
                                        _ => throw new NotImplementedException(),
                                    },
                                    Format = format.ToTextureFormat(),
                                    Shader4ComponentMapping = DefaultShader4ComponentMapping,
                                };
                            }

                            switch (desc.ViewDimension)
                            {
                                case SrvDimension.Texture1D:
                                    {
                                        desc.Texture1D = new Tex1DSrv
                                        {
                                            MostDetailedMip = 0,
                                            MipLevels = 0xffffffff,
                                            ResourceMinLODClamp = 0.0f
                                        };
                                        break;
                                    }
                                case SrvDimension.Texture2D:
                                    {
                                        desc.Texture2D = new Tex2DSrv
                                        {
                                            MostDetailedMip = 0,
                                            PlaneSlice = intent == PropertyBindIntent.AsStencil ? 1u : 0,
                                            MipLevels = 0xffffffff,
                                            ResourceMinLODClamp = 0.0f,
                                        };
                                        break;
                                    }
                                case SrvDimension.Texture3D:
                                    {
                                        desc.Texture3D = new Tex3DSrv
                                        {
                                            MostDetailedMip = 0,
                                            MipLevels = 0xffffffff,
                                            ResourceMinLODClamp = 0.0f,
                                        };
                                        break;
                                    }
                                case SrvDimension.Texturecube:
                                    {
                                        desc.TextureCube = new TexcubeSrv
                                        {
                                            MostDetailedMip = 0,
                                            MipLevels = 0xffffffff,
                                            ResourceMinLODClamp = 0.0f,
                                        };
                                        break;
                                    }
                            }

                            _device.Device->CreateShaderResourceView(res, &desc, dstDescriptor);
                        }

                        break;
                    }
                case NRDResourceId.Buffer:
                    {
                        if (resource.IsExternal)
                        {
                            RHIBufferDescription bufDesc = ((D3D12RHIBufferNative*)resource.Native)->Base.Description;

                            if (Flags.HasFlag(bufDesc.Usage, RHIResourceUsage.ConstantBuffer))
                            {
                                Debug.Assert(!bindAsUnorderedAccess);

                                ConstantBufferViewDesc desc = new ConstantBufferViewDesc
                                {
                                    BufferLocation = res->GetGPUVirtualAddress(),
                                    SizeInBytes = bufDesc.Width
                                };

                                _device.Device->CreateConstantBufferView(&desc, dstDescriptor);
                            }
                            else if (Flags.HasFlag(bufDesc.Usage, RHIResourceUsage.ShaderResource))
                            {
                                if (bindAsUnorderedAccess)
                                {
                                    UnorderedAccessViewDesc desc = new UnorderedAccessViewDesc
                                    {
                                        ViewDimension = UavDimension.Buffer,
                                        Format = Format.FormatUnknown,
                                    };

                                    if (bufDesc.Mode == RHIBufferMode.Raw)
                                    {
                                        desc.Buffer = new BufferUav
                                        {
                                            FirstElement = bufDesc.FirstElement,
                                            NumElements = bufDesc.ElementCount > 0 ? (uint)bufDesc.ElementCount : bufDesc.Width,
                                            StructureByteStride = 1,
                                            Flags = BufferUavFlags.Raw
                                        };
                                    }
                                    else
                                    {
                                        desc.Buffer = new BufferUav
                                        {
                                            FirstElement = bufDesc.FirstElement,
                                            NumElements = (uint)(bufDesc.Width / bufDesc.Stride),
                                            StructureByteStride = (uint)bufDesc.Stride,
                                            Flags = BufferUavFlags.None
                                        };
                                    }

                                    _device.Device->CreateUnorderedAccessView(res, null, &desc, dstDescriptor);
                                }
                                else
                                {
                                    ShaderResourceViewDesc desc = new ShaderResourceViewDesc
                                    {
                                        ViewDimension = SrvDimension.Buffer,
                                        Format = Format.FormatUnknown,
                                        Shader4ComponentMapping = DefaultShader4ComponentMapping,
                                    };

                                    if (bufDesc.Mode == RHIBufferMode.Raw)
                                    {
                                        desc.Buffer = new BufferSrv
                                        {
                                            FirstElement = bufDesc.FirstElement,
                                            NumElements = bufDesc.ElementCount > 0 ? (uint)bufDesc.ElementCount : bufDesc.Width,
                                            StructureByteStride = 1,
                                            Flags = BufferSrvFlags.Raw
                                        };
                                    }
                                    else
                                    {
                                        desc.Buffer = new BufferSrv
                                        {
                                            FirstElement = bufDesc.FirstElement,
                                            NumElements = (uint)(bufDesc.Width / bufDesc.Stride),
                                            StructureByteStride = (uint)bufDesc.Stride,
                                            Flags = BufferSrvFlags.None
                                        };
                                    }

                                    _device.Device->CreateShaderResourceView(res, &desc, dstDescriptor);
                                }
                            }
                        }
                        else
                        {
                            FrameGraphBuffer fg = _device.ResourceManager.FindFGBuffer(resource);
                            ref readonly FrameGraphBufferDesc bufDesc = ref fg.Description;

                            if (Flags.HasFlag(bufDesc.Usage, FGBufferUsage.ConstantBuffer))
                            {
                                Debug.Assert(!bindAsUnorderedAccess);

                                ConstantBufferViewDesc desc = new ConstantBufferViewDesc
                                {
                                    BufferLocation = res->GetGPUVirtualAddress(),
                                    SizeInBytes = bufDesc.Width
                                };

                                _device.Device->CreateConstantBufferView(&desc, dstDescriptor);
                            }
                            else if (Flags.HasFlag(bufDesc.Usage, FGBufferUsage.Structured))
                            {
                                if (bindAsUnorderedAccess)
                                {
                                    UnorderedAccessViewDesc desc = new UnorderedAccessViewDesc
                                    {
                                        ViewDimension = UavDimension.Buffer,
                                        Format = Format.FormatUnknown,
                                    };

                                    desc.Buffer = new BufferUav
                                    {
                                        FirstElement = 0,
                                        NumElements = (uint)(bufDesc.Width / bufDesc.Stride),
                                        StructureByteStride = (uint)bufDesc.Stride,
                                        Flags = BufferUavFlags.None
                                    };

                                    _device.Device->CreateUnorderedAccessView(res, null, &desc, dstDescriptor);
                                }
                                else
                                {
                                    ShaderResourceViewDesc desc = new ShaderResourceViewDesc
                                    {
                                        ViewDimension = SrvDimension.Buffer,
                                        Format = Format.FormatUnknown,
                                        Shader4ComponentMapping = DefaultShader4ComponentMapping,
                                    };

                                    desc.Buffer = new BufferSrv
                                    {
                                        FirstElement = 0,
                                        NumElements = (uint)(bufDesc.Width / bufDesc.Stride),
                                        StructureByteStride = (uint)bufDesc.Stride,
                                        Flags = BufferSrvFlags.None
                                    };

                                    _device.Device->CreateShaderResourceView(res, &desc, dstDescriptor);
                                }
                            }
                            else if (Flags.HasFlag(bufDesc.Usage, FGBufferUsage.Raw))
                            {
                                if (bindAsUnorderedAccess)
                                {
                                    UnorderedAccessViewDesc desc = new UnorderedAccessViewDesc
                                    {
                                        ViewDimension = UavDimension.Buffer,
                                        Format = Format.FormatR32Typeless,
                                    };

                                    desc.Buffer = new BufferUav
                                    {
                                        FirstElement = 0,
                                        NumElements = bufDesc.Width / sizeof(uint),
                                        StructureByteStride = 0,
                                        Flags = BufferUavFlags.Raw
                                    };

                                    _device.Device->CreateUnorderedAccessView(res, null, &desc, dstDescriptor);
                                }
                                else
                                {
                                    ShaderResourceViewDesc desc = new ShaderResourceViewDesc
                                    {
                                        ViewDimension = SrvDimension.Buffer,
                                        Format = Format.FormatR32Typeless,
                                        Shader4ComponentMapping = DefaultShader4ComponentMapping,
                                    };

                                    desc.Buffer = new BufferSrv
                                    {
                                        FirstElement = 0,
                                        NumElements = bufDesc.Width / sizeof(uint),
                                        StructureByteStride = 0,
                                        Flags = BufferSrvFlags.Raw
                                    };

                                    _device.Device->CreateShaderResourceView(res, &desc, dstDescriptor);
                                }
                            }
                        }

                        break;
                    }
            }

            ++_descriptorsUsedThisFrame;
            _activeHeapOffset += _descriptorHandleSize;

            return index;
        }

        private void AddNewHeapToList()
        {
            DescriptorHeapDesc desc = new DescriptorHeapDesc
            {
                Type = _heapType,
                NumDescriptors = (uint)_descriptorHeapSize,
                Flags = DescriptorHeapFlags.ShaderVisible,
                NodeMask = 0
            };

            ComPtr<ID3D12DescriptorHeap> descriptorHeap = new ComPtr<ID3D12DescriptorHeap>();
            HResult hr = _device.Device->CreateDescriptorHeap(&desc, out descriptorHeap);

            if (hr.IsFailure)
            {
                _device.RHIDevice.FlushPendingMessages();
                throw new NotImplementedException("Add error message");
            }

            _activeHeaps.Add(new HeapData(descriptorHeap, descriptorHeap.GetCPUDescriptorHandleForHeapStart()));
        }

        internal ID3D12DescriptorHeap* GetActiveHeapOrCreateNew()
        {
            if (_activeHeapIndex < _activeHeaps.Count)
                return (ID3D12DescriptorHeap*)Unsafe.AsPointer(ref _activeHeaps[_activeHeapIndex].Heap.Get());

            AddNewHeapToList();
            return (ID3D12DescriptorHeap*)Unsafe.AsPointer(ref _activeHeaps[_activeHeapIndex].Heap.Get());
        }

        internal ref ID3D12DescriptorHeap CurrentActiveHeap
        { 
            get
            {
                if (_activeHeapIndex < _activeHeaps.Count)
                    return ref _activeHeaps[_activeHeapIndex].Heap.Get();
                else
                    return ref Unsafe.NullRef<ID3D12DescriptorHeap>(); ;
            }
        }

        //https://learn.microsoft.com/en-us/windows/win32/api/d3d12/ne-d3d12-d3d12_shader_component_mapping

        private const int ShaderComponentMappingMask = 0x7;
        private const int ShaderComponentMappingShift = 3;
        private const int ShaderComponentMappingAlwaysSetBitAvoidingZeroMemMistakes = 1 << (ShaderComponentMappingShift * 4);

        private static readonly uint DefaultShader4ComponentMapping = EncodeShader4ComponentMapping(0, 1, 2, 3);

        private static uint EncodeShader4ComponentMapping(uint src0, uint src1, uint src2, uint src3)
        {
            return ((((src0) & ShaderComponentMappingMask) |
                    (((src1) & ShaderComponentMappingMask) << ShaderComponentMappingShift) |
                    (((src2) & ShaderComponentMappingMask) << (ShaderComponentMappingShift * 2)) |
                    (((src3) & ShaderComponentMappingMask) << (ShaderComponentMappingShift * 3)) |
                    ShaderComponentMappingAlwaysSetBitAvoidingZeroMemMistakes));
        }

        internal readonly record struct HeapData(ComPtr<ID3D12DescriptorHeap> Heap, CpuDescriptorHandle StartHandle);
        private readonly record struct ResourceData(NRDResource Resource, bool IsUnorderedAccess, PropertyBindIntent Intent)
        {
            public override int GetHashCode() => HashCode.Combine(Resource, IsUnorderedAccess);
        }
    }
}
