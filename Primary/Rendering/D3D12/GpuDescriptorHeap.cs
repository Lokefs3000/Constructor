using Primary.Common;
using Primary.Rendering.Resources;
using Primary.RHI2;
using Primary.RHI2.Direct3D12;
using Primary.Utility;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.DirectX.D3D12_BUFFER_SRV_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_DESCRIPTOR_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_SRV_DIMENSION;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;
using static TerraFX.Interop.DirectX.D3D12_UAV_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_BUFFER_UAV_FLAGS;
using System.Diagnostics;

namespace Primary.Rendering.D3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe sealed class GpuDescriptorHeap : IDisposable
    {
        private readonly NRDDevice _device;

        private readonly int _descriptorHandleSize;
        private readonly int _descriptorHeapSize;

        private readonly D3D12_DESCRIPTOR_HEAP_TYPE _heapType;

        private AverageAnalyser<int> _averageDescriptorUse;
        private int _descriptorsUsedThisFrame;

        private List<HeapData> _activeHeaps;
        private int _activeHeapIndex;
        private int _activeHeapOffset;

        private Dictionary<ResourceData, uint> _activeDescriptors;

        private bool _disposedValue;

        internal GpuDescriptorHeap(NRDDevice device, int heapSize, D3D12_DESCRIPTOR_HEAP_TYPE heapType)
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
                    heap.Heap.Pointer->Release();
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

        internal uint GetDescriptorIndex(NRDResource resource, bool bindAsUnorderedAccess, out bool changedActiveHeap)
        {
            changedActiveHeap = false;

            if (resource.IsNull)
                return ushort.MaxValue;

            ResourceData resData = new ResourceData(resource, bindAsUnorderedAccess);
            if (_activeDescriptors.TryGetValue(resData, out uint index))
                return index;

            if (_activeHeapIndex >= _activeHeaps.Count || _activeHeapOffset >= _descriptorHeapSize)
            {
                if (_activeHeapIndex >= _activeHeaps.Count)
                    AddNewHeapToList();
                else
                    _activeHeapIndex++;

                _activeDescriptors.Clear();
                _activeHeapOffset = 0;

                changedActiveHeap = true;
            }

            HeapData heap = _activeHeaps[_activeHeapIndex];

            index = (uint)_activeDescriptors.Count;
            _activeDescriptors[resData] = index;

            ID3D12Resource* res = (ID3D12Resource*)_device.ResourceManager.GetResource(resource);

            D3D12_CPU_DESCRIPTOR_HANDLE dstDescriptor = new D3D12_CPU_DESCRIPTOR_HANDLE(heap.StartHandle, _activeHeapOffset);

            switch (resource.Id)
            {
                case NRDResourceId.Texture:
                    {
                        if (bindAsUnorderedAccess)
                        {
                            D3D12_UNORDERED_ACCESS_VIEW_DESC desc;

                            if (resource.IsExternal)
                            {
                                RHITextureDescription texDesc = ((D3D12RHITextureNative*)resource.Native)->Base.Description;

                                desc = new D3D12_UNORDERED_ACCESS_VIEW_DESC
                                {
                                    ViewDimension = texDesc.Dimension switch
                                    {
                                        RHIDimension.Texture1D => D3D12_UAV_DIMENSION_TEXTURE1D,
                                        RHIDimension.Texture2D => D3D12_UAV_DIMENSION_TEXTURE2D,
                                        RHIDimension.Texture3D => D3D12_UAV_DIMENSION_TEXTURE3D,
                                        _ => throw new NotImplementedException(),
                                    },
                                    Format = texDesc.Format.ToResourceViewFormat(),
                                };
                            }
                            else
                            {
                                FrameGraphTexture fg = _device.ResourceManager.FindFGTexture(resource);
                                ref readonly FrameGraphTextureDesc texDesc = ref fg.Description;

                                desc = new D3D12_UNORDERED_ACCESS_VIEW_DESC
                                {
                                    ViewDimension = texDesc.Dimension switch
                                    {
                                        FGTextureDimension._1D => D3D12_UAV_DIMENSION_TEXTURE1D,
                                        FGTextureDimension._2D => D3D12_UAV_DIMENSION_TEXTURE2D,
                                        FGTextureDimension._3D => D3D12_UAV_DIMENSION_TEXTURE3D,
                                        _ => throw new NotImplementedException(),
                                    },
                                    Format = texDesc.Format.ToResourceViewFormat(),
                                };
                            }

                            switch (desc.ViewDimension)
                            {
                                case D3D12_UAV_DIMENSION_TEXTURE1D:
                                    {
                                        desc.Texture1D = new D3D12_TEX1D_UAV
                                        {
                                            MipSlice = 0
                                        };
                                        break;
                                    }
                                case D3D12_UAV_DIMENSION_TEXTURE2D:
                                    {
                                        desc.Texture2D = new D3D12_TEX2D_UAV
                                        {
                                            MipSlice = 0,
                                            PlaneSlice = 0,
                                        };
                                        break;
                                    }
                                case D3D12_UAV_DIMENSION_TEXTURE3D:
                                    {
                                        desc.Texture3D = new D3D12_TEX3D_UAV
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
                            D3D12_SHADER_RESOURCE_VIEW_DESC desc;

                            if (resource.IsExternal)
                            {
                                RHITextureDescription texDesc = ((D3D12RHITextureNative*)resource.Native)->Base.Description;

                                desc = new D3D12_SHADER_RESOURCE_VIEW_DESC
                                {
                                    ViewDimension = texDesc.Dimension switch
                                    {
                                        RHIDimension.Texture1D => D3D12_SRV_DIMENSION_TEXTURE1D,
                                        RHIDimension.Texture2D => D3D12_SRV_DIMENSION_TEXTURE2D,
                                        RHIDimension.Texture3D => D3D12_SRV_DIMENSION_TEXTURE3D,
                                        RHIDimension.TextureCube => D3D12_SRV_DIMENSION_TEXTURECUBE,
                                        _ => throw new NotImplementedException(),
                                    },
                                    Format = texDesc.Format.ToResourceViewFormat(),
                                    Shader4ComponentMapping = ResourceHelper.EncodeShader4ComponentMapping((uint)texDesc.Swizzle.R, (uint)texDesc.Swizzle.G, (uint)texDesc.Swizzle.B, (uint)texDesc.Swizzle.A),
                                };
                            }
                            else
                            {
                                FrameGraphTexture fg = _device.ResourceManager.FindFGTexture(resource);
                                ref readonly FrameGraphTextureDesc texDesc = ref fg.Description;

                                desc = new D3D12_SHADER_RESOURCE_VIEW_DESC
                                {
                                    ViewDimension = texDesc.Dimension switch
                                    {
                                        FGTextureDimension._1D => D3D12_SRV_DIMENSION_TEXTURE1D,
                                        FGTextureDimension._2D => D3D12_SRV_DIMENSION_TEXTURE2D,
                                        FGTextureDimension._3D => D3D12_SRV_DIMENSION_TEXTURE3D,
                                        FGTextureDimension.Cube => D3D12_SRV_DIMENSION_TEXTURECUBE,
                                        _ => throw new NotImplementedException(),
                                    },
                                    Format = texDesc.Format.ToResourceViewFormat(),
                                    Shader4ComponentMapping = DefaultShader4ComponentMapping,
                                };
                            }

                            switch (desc.ViewDimension)
                            {
                                case D3D12_SRV_DIMENSION_TEXTURE1D:
                                    {
                                        desc.Texture1D = new D3D12_TEX1D_SRV
                                        {
                                            MostDetailedMip = 0,
                                            MipLevels = 0xffffffff,
                                            ResourceMinLODClamp = 0.0f
                                        };
                                        break;
                                    }
                                case D3D12_SRV_DIMENSION_TEXTURE2D:
                                    {
                                        desc.Texture2D = new D3D12_TEX2D_SRV
                                        {
                                            MostDetailedMip = 0,
                                            PlaneSlice = 0,
                                            MipLevels = 0xffffffff,
                                            ResourceMinLODClamp = 0.0f,
                                        };
                                        break;
                                    }
                                case D3D12_SRV_DIMENSION_TEXTURE3D:
                                    {
                                        desc.Texture3D = new D3D12_TEX3D_SRV
                                        {
                                            MostDetailedMip = 0,
                                            MipLevels = 0xffffffff,
                                            ResourceMinLODClamp = 0.0f,
                                        };
                                        break;
                                    }
                                case D3D12_SRV_DIMENSION_TEXTURECUBE:
                                    {
                                        desc.TextureCube = new D3D12_TEXCUBE_SRV
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

                            if (FlagUtility.HasFlag(bufDesc.Usage, RHIResourceUsage.ConstantBuffer))
                            {
                                Debug.Assert(!bindAsUnorderedAccess);

                                D3D12_CONSTANT_BUFFER_VIEW_DESC desc = new D3D12_CONSTANT_BUFFER_VIEW_DESC
                                {
                                    BufferLocation = res->GetGPUVirtualAddress(),
                                    SizeInBytes = bufDesc.Width
                                };

                                _device.Device->CreateConstantBufferView(&desc, dstDescriptor);
                            }
                            else if (FlagUtility.HasFlag(bufDesc.Usage, RHIResourceUsage.ShaderResource))
                            {
                                if (bindAsUnorderedAccess)
                                {
                                    D3D12_UNORDERED_ACCESS_VIEW_DESC desc = new D3D12_UNORDERED_ACCESS_VIEW_DESC
                                    {
                                        ViewDimension = D3D12_UAV_DIMENSION_BUFFER,
                                        Format = DXGI_FORMAT_UNKNOWN,
                                    };

                                    if (bufDesc.Mode == RHIBufferMode.Raw)
                                    {
                                        desc.Buffer = new D3D12_BUFFER_UAV
                                        {
                                            FirstElement = bufDesc.FirstElement,
                                            NumElements = bufDesc.ElementCount > 0 ? (uint)bufDesc.ElementCount : bufDesc.Width,
                                            StructureByteStride = 1,
                                            Flags = D3D12_BUFFER_UAV_FLAG_RAW
                                        };
                                    }
                                    else
                                    {
                                        desc.Buffer = new D3D12_BUFFER_UAV
                                        {
                                            FirstElement = bufDesc.FirstElement,
                                            NumElements = (uint)(bufDesc.Width / bufDesc.Stride),
                                            StructureByteStride = (uint)bufDesc.Stride,
                                            Flags = D3D12_BUFFER_UAV_FLAG_NONE
                                        };
                                    }

                                    _device.Device->CreateUnorderedAccessView(res, null, &desc, dstDescriptor);
                                }
                                else
                                {
                                    D3D12_SHADER_RESOURCE_VIEW_DESC desc = new D3D12_SHADER_RESOURCE_VIEW_DESC
                                    {
                                        ViewDimension = D3D12_SRV_DIMENSION_BUFFER,
                                        Format = DXGI_FORMAT_UNKNOWN,
                                        Shader4ComponentMapping = DefaultShader4ComponentMapping,
                                    };

                                    if (bufDesc.Mode == RHIBufferMode.Raw)
                                    {
                                        desc.Buffer = new D3D12_BUFFER_SRV
                                        {
                                            FirstElement = bufDesc.FirstElement,
                                            NumElements = bufDesc.ElementCount > 0 ? (uint)bufDesc.ElementCount : bufDesc.Width,
                                            StructureByteStride = 1,
                                            Flags = D3D12_BUFFER_SRV_FLAG_RAW
                                        };
                                    }
                                    else
                                    {
                                        desc.Buffer = new D3D12_BUFFER_SRV
                                        {
                                            FirstElement = bufDesc.FirstElement,
                                            NumElements = (uint)(bufDesc.Width / bufDesc.Stride),
                                            StructureByteStride = (uint)bufDesc.Stride,
                                            Flags = D3D12_BUFFER_SRV_FLAG_NONE
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

                            if (FlagUtility.HasFlag(bufDesc.Usage, FGBufferUsage.ConstantBuffer))
                            {
                                Debug.Assert(!bindAsUnorderedAccess);

                                D3D12_CONSTANT_BUFFER_VIEW_DESC desc = new D3D12_CONSTANT_BUFFER_VIEW_DESC
                                {
                                    BufferLocation = res->GetGPUVirtualAddress(),
                                    SizeInBytes = bufDesc.Width
                                };

                                _device.Device->CreateConstantBufferView(&desc, dstDescriptor);
                            }
                            else if (FlagUtility.HasFlag(bufDesc.Usage, FGBufferUsage.Structured))
                            {
                                if (bindAsUnorderedAccess)
                                {
                                    D3D12_UNORDERED_ACCESS_VIEW_DESC desc = new D3D12_UNORDERED_ACCESS_VIEW_DESC
                                    {
                                        ViewDimension = D3D12_UAV_DIMENSION_BUFFER,
                                        Format = DXGI_FORMAT_UNKNOWN,
                                    };

                                    desc.Buffer = new D3D12_BUFFER_UAV
                                    {
                                        FirstElement = 0,
                                        NumElements = (uint)(bufDesc.Width / bufDesc.Stride),
                                        StructureByteStride = (uint)bufDesc.Stride,
                                        Flags = D3D12_BUFFER_UAV_FLAG_NONE
                                    };

                                    _device.Device->CreateUnorderedAccessView(res, null, &desc, dstDescriptor);
                                }
                                else
                                {
                                    D3D12_SHADER_RESOURCE_VIEW_DESC desc = new D3D12_SHADER_RESOURCE_VIEW_DESC
                                    {
                                        ViewDimension = D3D12_SRV_DIMENSION_BUFFER,
                                        Format = DXGI_FORMAT_UNKNOWN,
                                        Shader4ComponentMapping = DefaultShader4ComponentMapping,
                                    };

                                    desc.Buffer = new D3D12_BUFFER_SRV
                                    {
                                        FirstElement = 0,
                                        NumElements = (uint)(bufDesc.Width / bufDesc.Stride),
                                        StructureByteStride = (uint)bufDesc.Stride,
                                        Flags = D3D12_BUFFER_SRV_FLAG_NONE
                                    };

                                    _device.Device->CreateShaderResourceView(res, &desc, dstDescriptor);
                                }
                            }
                            else if (FlagUtility.HasFlag(bufDesc.Usage, FGBufferUsage.Raw))
                            {
                                if (bindAsUnorderedAccess)
                                {
                                    D3D12_UNORDERED_ACCESS_VIEW_DESC desc = new D3D12_UNORDERED_ACCESS_VIEW_DESC
                                    {
                                        ViewDimension = D3D12_UAV_DIMENSION_BUFFER,
                                        Format = DXGI_FORMAT_R32_TYPELESS,
                                    };

                                    desc.Buffer = new D3D12_BUFFER_UAV
                                    {
                                        FirstElement = 0,
                                        NumElements = bufDesc.Width / sizeof(uint),
                                        StructureByteStride = 0,
                                        Flags = D3D12_BUFFER_UAV_FLAG_RAW
                                    };

                                    _device.Device->CreateUnorderedAccessView(res, null, &desc, dstDescriptor);
                                }
                                else
                                {
                                    D3D12_SHADER_RESOURCE_VIEW_DESC desc = new D3D12_SHADER_RESOURCE_VIEW_DESC
                                    {
                                        ViewDimension = D3D12_SRV_DIMENSION_BUFFER,
                                        Format = DXGI_FORMAT_R32_TYPELESS,
                                        Shader4ComponentMapping = DefaultShader4ComponentMapping,
                                    };

                                    desc.Buffer = new D3D12_BUFFER_SRV
                                    {
                                        FirstElement = 0,
                                        NumElements = bufDesc.Width / sizeof(uint),
                                        StructureByteStride = 0,
                                        Flags = D3D12_BUFFER_SRV_FLAG_RAW
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
            D3D12_DESCRIPTOR_HEAP_DESC desc = new D3D12_DESCRIPTOR_HEAP_DESC
            {
                Type = _heapType,
                NumDescriptors = (uint)_descriptorHeapSize,
                Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE,
                NodeMask = 0
            };

            ID3D12DescriptorHeap* temp = null;
            HRESULT hr = _device.Device->CreateDescriptorHeap(&desc, UuidOf.Get<ID3D12DescriptorHeap>(), (void**)&temp);

            if (hr.FAILED)
            {
                _device.RHIDevice.FlushPendingMessages();
                throw new NotImplementedException("Add error message");
            }

            _activeHeaps.Add(new HeapData(temp, temp->GetCPUDescriptorHandleForHeapStart()));
        }

        internal ID3D12DescriptorHeap* GetActiveHeapOrCreateNew()
        {
            if (_activeHeapIndex < _activeHeaps.Count)
                return _activeHeaps[_activeHeapIndex].Heap.Pointer;

            AddNewHeapToList();
            return _activeHeaps[_activeHeapIndex].Heap.Pointer;
        }

        internal ID3D12DescriptorHeap* CurrentActiveHeap => _activeHeapIndex < _activeHeaps.Count ? _activeHeaps[_activeHeapIndex].Heap.Pointer : null;

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

        internal readonly record struct HeapData(Ptr<ID3D12DescriptorHeap> Heap, D3D12_CPU_DESCRIPTOR_HANDLE StartHandle);
        private readonly record struct ResourceData(NRDResource Resource, bool IsUnorderedAccess)
        {
            public override int GetHashCode() => HashCode.Combine(Resource, IsUnorderedAccess);
        }
    }
}
