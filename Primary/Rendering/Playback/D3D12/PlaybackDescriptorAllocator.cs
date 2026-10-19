using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Memory;
using Primary.Profiling;
using Primary.Rendering.Commands;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.RHI;
using Primary.RHI.Direct3D12;
using Primary.Threading;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using static Primary.Rendering.Resources.FGResourceDesc;
using DescriptorHeapType = Silk.NET.Direct3D12.DescriptorHeapType;

namespace Primary.Rendering.Playback.D3D12
{
    internal unsafe sealed class PlaybackDescriptorAllocator : IDisposable, IJob
    {
        private readonly PlaybackDevice _device;
        private readonly PlaybackExecutor _executor;

        private DescriptorHeap[] _renderTargetHeaps;
        private DescriptorHeap[] _depthStencilHeaps;
        private DescriptorHeap[] _cpuResourceViewHeaps;
        private DescriptorHeap[] _gpuResourceViewHeaps;
        private DescriptorHeap[] _samplerHeaps;

        private readonly uint _rtvHeapIncrementSize;
        private readonly uint _dsvHeapIncrementSize;
        private readonly uint _cbvSrvUavHeapIncrementSize;
        private readonly uint _samplerHeapIncrementSize;

        private DescriptorHandle[] _resourceDescriptors;
        private uint[] _samplerDescriptors;

        private CompiledRenderPlan? _renderPlan;
        private DescriptorAllocator? _descriptorAllocator;

        private bool _disposedValue;

        internal PlaybackDescriptorAllocator(PlaybackDevice device, PlaybackExecutor executor)
        {
            _device = device;
            _executor = executor;

            _renderTargetHeaps = [];
            _depthStencilHeaps = [];
            _cpuResourceViewHeaps = [];
            _gpuResourceViewHeaps = [];
            _samplerHeaps = [];

            _rtvHeapIncrementSize = device.NativeDevice.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv);
            _dsvHeapIncrementSize = device.NativeDevice.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Dsv);
            _cbvSrvUavHeapIncrementSize = device.NativeDevice.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);
            _samplerHeapIncrementSize = device.NativeDevice.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Sampler);

            _resourceDescriptors = [];
            _samplerDescriptors = [];

            _renderPlan = null;
            _descriptorAllocator = null;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                foreach (DescriptorHeap descriptorHeap in _renderTargetHeaps)
                {
                    if (!descriptorHeap.Ptr.IsNull)
                    {
                        descriptorHeap.Ptr.Pointer->Release();
                    }
                }

                foreach (DescriptorHeap descriptorHeap in _depthStencilHeaps)
                {
                    if (!descriptorHeap.Ptr.IsNull)
                    {
                        descriptorHeap.Ptr.Pointer->Release();
                    }
                }

                foreach (DescriptorHeap descriptorHeap in _cpuResourceViewHeaps)
                {
                    if (!descriptorHeap.Ptr.IsNull)
                    {
                        descriptorHeap.Ptr.Pointer->Release();
                    }
                }

                foreach (DescriptorHeap descriptorHeap in _gpuResourceViewHeaps)
                {
                    if (!descriptorHeap.Ptr.IsNull)
                    {
                        descriptorHeap.Ptr.Pointer->Release();
                    }
                }

                foreach (DescriptorHeap descriptorHeap in _samplerHeaps)
                {
                    if (!descriptorHeap.Ptr.IsNull)
                    {
                        descriptorHeap.Ptr.Pointer->Release();
                    }
                }

                Array.Clear(_resourceDescriptors);
                Array.Clear(_samplerDescriptors);

                _disposedValue = true;
            }
        }

        ~PlaybackDescriptorAllocator()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void SetJobArguments(CompiledRenderPlan renderPlan, DescriptorAllocator descriptorAllocator)
        {
            _renderPlan = renderPlan;
            _descriptorAllocator = descriptorAllocator;
        }

        void IJob.Execute()
        {
            using (new ProfilingScope("AllocDescriptors"))
            {
                Debug.Assert(_renderPlan != null);
                Debug.Assert(_descriptorAllocator != null);

                ref readonly TransientHeapData renderTargetHeap = ref _descriptorAllocator.RenderTargetHeap;
                ref readonly TransientHeapData depthStencilHeap = ref _descriptorAllocator.DepthStencilHeap;
                ref readonly TransientHeapData cpuResourceViewHeap = ref _descriptorAllocator.CPUResourceViewHeap;
                ref readonly TransientHeapData gpuResourceViewHeap = ref _descriptorAllocator.GPUResourceViewHeap;
                ref readonly TransientHeapData samplerHeap = ref _descriptorAllocator.SamplerHeap;

                using (new ProfilingScope("SetupHeaps"))
                {
                    EnsureHeapsExist(DescriptorHeapType.Rtv, DescriptorHeapFlags.None, in renderTargetHeap, ref _renderTargetHeaps);
                    EnsureHeapsExist(DescriptorHeapType.Dsv, DescriptorHeapFlags.None, in depthStencilHeap, ref _depthStencilHeaps);
                    EnsureHeapsExist(DescriptorHeapType.CbvSrvUav, DescriptorHeapFlags.None, in cpuResourceViewHeap, ref _cpuResourceViewHeaps);
                    EnsureHeapsExist(DescriptorHeapType.CbvSrvUav, DescriptorHeapFlags.ShaderVisible, in gpuResourceViewHeap, ref _gpuResourceViewHeaps);
                    EnsureHeapsExist(DescriptorHeapType.Sampler, DescriptorHeapFlags.ShaderVisible, in samplerHeap, ref _samplerHeaps);
                }

                ID3D12Device14* device = (ID3D12Device14*)Unsafe.AsPointer(ref _device.NativeDevice.Device.Get());

                using (new ProfilingScope("Resources"))
                {
                    ROList<DescriptorAllocationInfo> resourceDescriptors = _descriptorAllocator.ResourceDescriptors;
                    if (_resourceDescriptors.Length < resourceDescriptors.Count)
                    {
                        _resourceDescriptors = new DescriptorHandle[MemoryHelper.AlignUp(resourceDescriptors.Count, 64)];
                    }

                    for (int i = 0; i < resourceDescriptors.Count; ++i)
                    {
                        DescriptorAllocationInfo allocationInfo = resourceDescriptors[i];
                        ref DescriptorHandle handle = ref _resourceDescriptors[i];

                        if (allocationInfo.Resource.IsNull)
                        {
                            switch (allocationInfo.HeapType)
                            {
                                case Recording.DescriptorHeapType.RenderTarget:
                                    {
                                        DescriptorHeap heap = _renderTargetHeaps[allocationInfo.HeapIndex];
                                        CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _rtvHeapIncrementSize);

                                        RenderTargetViewDesc rtvDesc = new RenderTargetViewDesc
                                        {
                                            ViewDimension = RtvDimension.Texture2D,
                                            Format = Format.FormatR8G8B8A8Unorm,
                                            Anonymous = new RenderTargetViewDescUnion
                                            {
                                                Texture2D = new Tex2DRtv
                                                {
                                                    MipSlice = 0,
                                                    PlaneSlice = 0
                                                }
                                            }
                                        };

                                        device->CreateRenderTargetView(null, &rtvDesc, cpu);
                                        handle = new DescriptorHandle((uint)allocationInfo.DescriptorIndex, cpu);
                                        break;
                                    }
                                case Recording.DescriptorHeapType.DepthStencil:
                                    {
                                        DescriptorHeap heap = _depthStencilHeaps[allocationInfo.HeapIndex];
                                        CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _dsvHeapIncrementSize);

                                        DepthStencilViewDesc dsvDesc = new DepthStencilViewDesc
                                        {
                                            ViewDimension = DsvDimension.Texture2D,
                                            Format = Format.FormatD24UnormS8Uint,
                                            Flags = DsvFlags.None,
                                            Anonymous = new DepthStencilViewDescUnion
                                            {
                                                Texture2D = new Tex2DDsv
                                                {
                                                    MipSlice = 0
                                                }
                                            }
                                        };

                                        device->CreateDepthStencilView(null, &dsvDesc, cpu);
                                        handle = new DescriptorHandle((uint)allocationInfo.DescriptorIndex, cpu);
                                        break;
                                    }
                                case Recording.DescriptorHeapType.ConstantBufferView:
                                    {
                                        ConstantBufferViewDesc cbvDesc = new ConstantBufferViewDesc
                                        {
                                            BufferLocation = 0,
                                            SizeInBytes = 0
                                        };

                                        if (allocationInfo.IsGPUHeap)
                                        {
                                            DescriptorHeap heap = _gpuResourceViewHeaps[allocationInfo.HeapIndex];
                                            CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);
                                            GpuDescriptorHandle gpu = new GpuDescriptorHandle(heap.StartOffsetGpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);

                                            device->CreateConstantBufferView(&cbvDesc, cpu);
                                            handle = new DescriptorHandle((uint)allocationInfo.DescriptorIndex, gpu);
                                        }
                                        else
                                        {
                                            DescriptorHeap heap = _cpuResourceViewHeaps[allocationInfo.HeapIndex];
                                            CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);

                                            device->CreateConstantBufferView(&cbvDesc, cpu);
                                            handle = new DescriptorHandle((uint)allocationInfo.DescriptorIndex, cpu);
                                        }

                                        break;
                                    }
                                case Recording.DescriptorHeapType.ShaderResourceView:
                                    {
                                        ShaderResourceViewDesc srvDesc = new ShaderResourceViewDesc
                                        {
                                            ViewDimension = SrvDimension.Buffer,
                                            Format = Format.FormatUnknown,
                                            Shader4ComponentMapping = ResourceHelper.DefaultShader4ComponentMapping,
                                            Anonymous = new ShaderResourceViewDescUnion
                                            {
                                                Buffer = new BufferSrv
                                                {
                                                    FirstElement = 0,
                                                    NumElements = 0,
                                                    StructureByteStride = 0,
                                                    Flags = BufferSrvFlags.None
                                                }
                                            }
                                        };

                                        if (allocationInfo.IsGPUHeap)
                                        {
                                            DescriptorHeap heap = _gpuResourceViewHeaps[allocationInfo.HeapIndex];
                                            CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);
                                            GpuDescriptorHandle gpu = new GpuDescriptorHandle(heap.StartOffsetGpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);

                                            device->CreateShaderResourceView(null, &srvDesc, cpu);
                                            handle = new DescriptorHandle((uint)allocationInfo.DescriptorIndex, gpu);
                                        }
                                        else
                                        {
                                            DescriptorHeap heap = _cpuResourceViewHeaps[allocationInfo.HeapIndex];
                                            CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);

                                            device->CreateShaderResourceView(null, &srvDesc, cpu);
                                            handle = new DescriptorHandle((uint)allocationInfo.DescriptorIndex, cpu);
                                        }

                                        break;
                                    }
                                case Recording.DescriptorHeapType.UnorderedAccessView:
                                    {
                                        UnorderedAccessViewDesc uavDesc = new UnorderedAccessViewDesc
                                        {
                                            ViewDimension = UavDimension.Buffer,
                                            Format = Format.FormatUnknown,
                                            Anonymous = new UnorderedAccessViewDescUnion
                                            {
                                                Buffer = new BufferUav
                                                {
                                                    FirstElement = 0,
                                                    NumElements = 0,
                                                    StructureByteStride = 0,
                                                    CounterOffsetInBytes = 0,
                                                    Flags = BufferUavFlags.None
                                                }
                                            }
                                        };

                                        if (allocationInfo.IsGPUHeap)
                                        {
                                            DescriptorHeap heap = _gpuResourceViewHeaps[allocationInfo.HeapIndex];
                                            CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);
                                            GpuDescriptorHandle gpu = new GpuDescriptorHandle(heap.StartOffsetGpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);

                                            device->CreateUnorderedAccessView(null, null, &uavDesc, cpu);
                                            handle = new DescriptorHandle((uint)allocationInfo.DescriptorIndex, gpu);
                                        }
                                        else
                                        {
                                            DescriptorHeap heap = _cpuResourceViewHeaps[allocationInfo.HeapIndex];
                                            CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);

                                            device->CreateUnorderedAccessView(null, null, &uavDesc, cpu);
                                            handle = new DescriptorHandle((uint)allocationInfo.DescriptorIndex, cpu);
                                        }

                                        break;
                                    }
                            }

                            continue;
                        }

                        RenderResourceInfo resourceInfo = _renderPlan.Resources[_renderPlan.ResourceIndices[allocationInfo.Resource.Index]];
                        ID3D12Resource2* resourcePtr = _executor.ResourceManager.GetResourcePointer(resourceInfo.ResourceIndex);

                        switch (allocationInfo.HeapType)
                        {
                            case Recording.DescriptorHeapType.RenderTarget:
                                {
                                    Debug.Assert(!allocationInfo.IsGPUHeap);

                                    RenderTargetViewDesc rtvDesc;
                                    if (resourceInfo.ImportedResource != null)
                                    {
                                        RHITexture texture = (RHITexture)resourceInfo.ImportedResource;
                                        ref readonly RHITextureDescription textureDesc = ref texture.Description;

                                        switch (textureDesc.Dimension)
                                        {
                                            case RHIDimension.Texture1D:
                                                {
                                                    rtvDesc = new RenderTargetViewDesc
                                                    {
                                                        ViewDimension = RtvDimension.Texture1D,
                                                        Format = textureDesc.Format.ToRenderTargetFormat(),
                                                        Anonymous = new RenderTargetViewDescUnion
                                                        {
                                                            Texture1D = new Tex1DRtv
                                                            {
                                                                MipSlice = 0
                                                            }
                                                        }
                                                    };

                                                    break;
                                                }
                                            case RHIDimension.Texture2D:
                                                {
                                                    rtvDesc = new RenderTargetViewDesc
                                                    {
                                                        ViewDimension = RtvDimension.Texture2D,
                                                        Format = textureDesc.Format.ToRenderTargetFormat(),
                                                        Anonymous = new RenderTargetViewDescUnion
                                                        {
                                                            Texture2D = new Tex2DRtv
                                                            {
                                                                MipSlice = 0,
                                                                PlaneSlice = 0
                                                            }
                                                        }
                                                    };

                                                    break;
                                                }
                                            case RHIDimension.Texture3D:
                                                {
                                                    rtvDesc = new RenderTargetViewDesc
                                                    {
                                                        ViewDimension = RtvDimension.Texture3D,
                                                        Format = textureDesc.Format.ToRenderTargetFormat(),
                                                        Anonymous = new RenderTargetViewDescUnion
                                                        {
                                                            Texture3D = new Tex3DRtv
                                                            {
                                                                MipSlice = 0,
                                                                FirstWSlice = 0,
                                                                WSize = 1
                                                            }
                                                        }
                                                    };

                                                    break;
                                                }
                                        }
                                    }
                                    else
                                    {
                                        FGResourceDesc.Texture textureDesc = _renderPlan.ResourceDescs[resourceInfo.DescriptionIndex].Resource.TextureDesc;

                                        switch (textureDesc.Dimension)
                                        {
                                            case FGTextureDimension.Tex2D:
                                                {
                                                    rtvDesc = new RenderTargetViewDesc
                                                    {
                                                        ViewDimension = RtvDimension.Texture2D,
                                                        Format = textureDesc.Format.ToRenderTargetFormat(),
                                                        Anonymous = new RenderTargetViewDescUnion
                                                        {
                                                            Texture2D = new Tex2DRtv
                                                            {
                                                                MipSlice = 0,
                                                                PlaneSlice = 0
                                                            }
                                                        }
                                                    };

                                                    break;
                                                }
                                            case FGTextureDimension.Tex2DArray:
                                                {
                                                    rtvDesc = new RenderTargetViewDesc
                                                    {
                                                        ViewDimension = RtvDimension.Texture2Darray,
                                                        Format = textureDesc.Format.ToRenderTargetFormat(),
                                                        Anonymous = new RenderTargetViewDescUnion
                                                        {
                                                            Texture2DArray = new Tex2DArrayRtv
                                                            {
                                                                MipSlice = 0,
                                                                PlaneSlice = 0,
                                                                ArraySize = 1,
                                                                FirstArraySlice = 0
                                                            }
                                                        }
                                                    };

                                                    break;
                                                }
                                            case FGTextureDimension.Tex3D:
                                                {
                                                    rtvDesc = new RenderTargetViewDesc
                                                    {
                                                        ViewDimension = RtvDimension.Texture3D,
                                                        Format = textureDesc.Format.ToRenderTargetFormat(),
                                                        Anonymous = new RenderTargetViewDescUnion
                                                        {
                                                            Texture3D = new Tex3DRtv
                                                            {
                                                                MipSlice = 0,
                                                                WSize = 1,
                                                                FirstWSlice = 0
                                                            }
                                                        }
                                                    };

                                                    break;
                                                }
                                        }
                                    }

                                    DescriptorHeap heap = _renderTargetHeaps[allocationInfo.HeapIndex];
                                    CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _rtvHeapIncrementSize);

                                    device->CreateRenderTargetView((ID3D12Resource*)resourcePtr, &rtvDesc, cpu);
                                    handle = new DescriptorHandle((uint)allocationInfo.DescriptorIndex, cpu);
                                    break;
                                }
                            case Recording.DescriptorHeapType.DepthStencil:
                                {
                                    Debug.Assert(!allocationInfo.IsGPUHeap);

                                    DepthStencilViewDesc dsvDesc;
                                    if (resourceInfo.ImportedResource != null)
                                    {
                                        RHITexture texture = (RHITexture)resourceInfo.ImportedResource;
                                        ref readonly RHITextureDescription textureDesc = ref texture.Description;

                                        switch (textureDesc.Dimension)
                                        {
                                            case RHIDimension.Texture1D:
                                                {
                                                    dsvDesc = new DepthStencilViewDesc
                                                    {
                                                        ViewDimension = DsvDimension.Texture1D,
                                                        Format = textureDesc.Format.ToRenderTargetFormat(),
                                                        Anonymous = new DepthStencilViewDescUnion
                                                        {
                                                            Texture1D = new Tex1DDsv
                                                            {
                                                                MipSlice = 0
                                                            }
                                                        }
                                                    };

                                                    break;
                                                }
                                            case RHIDimension.Texture2D:
                                                {
                                                    dsvDesc = new DepthStencilViewDesc
                                                    {
                                                        ViewDimension = DsvDimension.Texture2D,
                                                        Format = textureDesc.Format.ToRenderTargetFormat(),
                                                        Anonymous = new DepthStencilViewDescUnion
                                                        {
                                                            Texture2D = new Tex2DDsv
                                                            {
                                                                MipSlice = 0
                                                            }
                                                        }
                                                    };

                                                    break;
                                                }
                                        }
                                    }
                                    else
                                    {
                                        FGResourceDesc.Texture textureDesc = _renderPlan.ResourceDescs[resourceInfo.DescriptionIndex].Resource.TextureDesc;

                                        switch (textureDesc.Dimension)
                                        {
                                            case FGTextureDimension.Tex2D:
                                                {
                                                    dsvDesc = new DepthStencilViewDesc
                                                    {
                                                        ViewDimension = DsvDimension.Texture2D,
                                                        Format = textureDesc.Format.ToRenderTargetFormat(),
                                                        Anonymous = new DepthStencilViewDescUnion
                                                        {
                                                            Texture2D = new Tex2DDsv
                                                            {
                                                                MipSlice = 0
                                                            }
                                                        }
                                                    };

                                                    break;
                                                }
                                            case FGTextureDimension.Tex2DArray:
                                                {
                                                    dsvDesc = new DepthStencilViewDesc
                                                    {
                                                        ViewDimension = DsvDimension.Texture2Darray,
                                                        Format = textureDesc.Format.ToRenderTargetFormat(),
                                                        Anonymous = new DepthStencilViewDescUnion
                                                        {
                                                            Texture2DArray = new Tex2DArrayDsv
                                                            {
                                                                MipSlice = 0,
                                                                ArraySize = 1,
                                                                FirstArraySlice = 0
                                                            }
                                                        }
                                                    };

                                                    break;
                                                }
                                        }
                                    }

                                    DescriptorHeap heap = _depthStencilHeaps[allocationInfo.HeapIndex];
                                    CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _dsvHeapIncrementSize);

                                    device->CreateDepthStencilView((ID3D12Resource*)resourcePtr, &dsvDesc, cpu);
                                    handle = new DescriptorHandle((uint)allocationInfo.DescriptorIndex, cpu);
                                    break;
                                }
                            case Recording.DescriptorHeapType.ConstantBufferView:
                                {
                                    ConstantBufferViewDesc cbvDesc;
                                    if (resourceInfo.ImportedResource != null)
                                    {
                                        RHIBuffer buffer = (RHIBuffer)resourceInfo.ImportedResource;
                                        ref readonly RHIBufferDescription bufferDesc = ref buffer.Description;

                                        cbvDesc = new ConstantBufferViewDesc
                                        {
                                            BufferLocation = resourcePtr->GetGPUVirtualAddress(),
                                            SizeInBytes = (uint)resourceInfo.SizeInBytes
                                        };
                                    }
                                    else
                                    {
                                        FGResourceDesc.Buffer bufferDesc = _renderPlan.ResourceDescs[resourceInfo.DescriptionIndex].Resource.BufferDesc;

                                        cbvDesc = new ConstantBufferViewDesc
                                        {
                                            BufferLocation = resourcePtr->GetGPUVirtualAddress(),
                                            SizeInBytes = (uint)resourceInfo.SizeInBytes
                                        };
                                    }

                                    if (allocationInfo.IsGPUHeap)
                                    {
                                        DescriptorHeap heap = _gpuResourceViewHeaps[allocationInfo.HeapIndex];
                                        CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);
                                        GpuDescriptorHandle gpu = new GpuDescriptorHandle(heap.StartOffsetGpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);

                                        device->CreateConstantBufferView(&cbvDesc, cpu);
                                        handle = new DescriptorHandle((uint)allocationInfo.DescriptorIndex, gpu);
                                    }
                                    else
                                    {
                                        DescriptorHeap heap = _cpuResourceViewHeaps[allocationInfo.HeapIndex];
                                        CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);

                                        device->CreateConstantBufferView(&cbvDesc, cpu);
                                        handle = new DescriptorHandle((uint)allocationInfo.DescriptorIndex, cpu);
                                    }

                                    break;
                                }
                            case Recording.DescriptorHeapType.ShaderResourceView:
                                {
                                    ShaderResourceViewDesc srvDesc;
                                    if (resourceInfo.ImportedResource != null)
                                    {
                                        if (resourceInfo.ImportedResource is RHIBuffer buffer)
                                        {
                                            ref readonly RHIBufferDescription bufferDesc = ref buffer.Description;

                                            srvDesc = new ShaderResourceViewDesc
                                            {
                                                ViewDimension = SrvDimension.Buffer,
                                                Shader4ComponentMapping = ResourceHelper.DefaultShader4ComponentMapping
                                            };

                                            if (bufferDesc.Mode == RHIBufferMode.Default)
                                            {
                                                srvDesc.Format = Format.FormatUnknown;
                                                srvDesc.Buffer = new BufferSrv
                                                {
                                                    FirstElement = 0,
                                                    NumElements = (uint)bufferDesc.ElementCount,
                                                    StructureByteStride = (uint)bufferDesc.Stride,
                                                    Flags = BufferSrvFlags.None
                                                };
                                            }
                                            else
                                            {
                                                srvDesc.Format = Format.FormatR32Typeless;
                                                srvDesc.Buffer = new BufferSrv
                                                {
                                                    FirstElement = 0,
                                                    NumElements = bufferDesc.Width / sizeof(uint),
                                                    StructureByteStride = 0,
                                                    Flags = BufferSrvFlags.Raw
                                                };
                                            }
                                        }
                                        else
                                        {
                                            RHITexture texture = (RHITexture)resourceInfo.ImportedResource;
                                            ref readonly RHITextureDescription textureDesc = ref texture.Description;

                                            uint componentMapping = ResourceHelper.EncodeShader4ComponentMapping(
                                                (uint)textureDesc.Swizzle.R,
                                                (uint)textureDesc.Swizzle.G,
                                                (uint)textureDesc.Swizzle.B,
                                                (uint)textureDesc.Swizzle.A);

                                            switch (textureDesc.Dimension)
                                            {
                                                case RHIDimension.Texture1D:
                                                    {
                                                        srvDesc = new ShaderResourceViewDesc
                                                        {
                                                            ViewDimension = SrvDimension.Texture1D,
                                                            Format = textureDesc.Format.ToResourceViewFormat(),
                                                            Shader4ComponentMapping = componentMapping,
                                                            Anonymous = new ShaderResourceViewDescUnion
                                                            {
                                                                Texture1D = new Tex1DSrv
                                                                {
                                                                    MipLevels = (uint)textureDesc.MipLevels,
                                                                    MostDetailedMip = 0,
                                                                    ResourceMinLODClamp = 0.0f
                                                                }
                                                            }
                                                        };

                                                        break;
                                                    }
                                                case RHIDimension.Texture2D:
                                                    {
                                                        srvDesc = new ShaderResourceViewDesc
                                                        {
                                                            ViewDimension = SrvDimension.Texture2D,
                                                            Format = textureDesc.Format.ToResourceViewFormat(),
                                                            Shader4ComponentMapping = componentMapping,
                                                            Anonymous = new ShaderResourceViewDescUnion
                                                            {
                                                                Texture2D = new Tex2DSrv
                                                                {
                                                                    MipLevels = (uint)textureDesc.MipLevels,
                                                                    MostDetailedMip = 0,
                                                                    ResourceMinLODClamp = 0.0f,
                                                                    PlaneSlice = 0,
                                                                }
                                                            }
                                                        };

                                                        break;
                                                    }
                                                case RHIDimension.Texture3D:
                                                    {
                                                        srvDesc = new ShaderResourceViewDesc
                                                        {
                                                            ViewDimension = SrvDimension.Texture3D,
                                                            Format = textureDesc.Format.ToResourceViewFormat(),
                                                            Shader4ComponentMapping = componentMapping,
                                                            Anonymous = new ShaderResourceViewDescUnion
                                                            {
                                                                Texture3D = new Tex3DSrv
                                                                {
                                                                    MipLevels = (uint)textureDesc.MipLevels,
                                                                    MostDetailedMip = 0,
                                                                    ResourceMinLODClamp = 0.0f
                                                                }
                                                            }
                                                        };

                                                        break;
                                                    }
                                                case RHIDimension.TextureCube:
                                                    {
                                                        srvDesc = new ShaderResourceViewDesc
                                                        {
                                                            ViewDimension = SrvDimension.Texturecube,
                                                            Format = textureDesc.Format.ToResourceViewFormat(),
                                                            Shader4ComponentMapping = componentMapping,
                                                            Anonymous = new ShaderResourceViewDescUnion
                                                            {
                                                                TextureCube = new TexcubeSrv
                                                                {
                                                                    MipLevels = (uint)textureDesc.MipLevels,
                                                                    MostDetailedMip = 0,
                                                                    ResourceMinLODClamp = 0.0f
                                                                }
                                                            }
                                                        };

                                                        break;
                                                    }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        GenericResourceDesc resourceDesc = _renderPlan.ResourceDescs[resourceInfo.DescriptionIndex];
                                        if (resourceDesc.Type == FGResourceType.Buffer)
                                        {
                                            FGResourceDesc.Buffer bufferDesc = resourceDesc.Resource.BufferDesc;

                                            srvDesc = new ShaderResourceViewDesc
                                            {
                                                ViewDimension = SrvDimension.Buffer,
                                                Shader4ComponentMapping = ResourceHelper.DefaultShader4ComponentMapping,
                                            };

                                            if (bufferDesc.Target.HasFlags(FGBufferTarget.Raw))
                                            {
                                                srvDesc.Format = Format.FormatR32Typeless;
                                                srvDesc.Buffer = new BufferSrv
                                                {
                                                    FirstElement = 0,
                                                    NumElements = (uint)bufferDesc.ElementCount,
                                                    StructureByteStride = 0,
                                                    Flags = BufferSrvFlags.Raw
                                                };
                                            }
                                            else
                                            {
                                                srvDesc.Format = Format.FormatUnknown;
                                                srvDesc.Buffer = new BufferSrv
                                                {
                                                    FirstElement = 0,
                                                    NumElements = (uint)bufferDesc.ElementCount,
                                                    StructureByteStride = (uint)bufferDesc.Stride,
                                                    Flags = BufferSrvFlags.None
                                                };
                                            }
                                        }
                                        else
                                        {
                                            FGResourceDesc.Texture textureDesc = resourceDesc.Resource.TextureDesc;

                                            switch (textureDesc.Dimension)
                                            {
                                                case FGTextureDimension.Tex2D:
                                                    {
                                                        srvDesc = new ShaderResourceViewDesc
                                                        {
                                                            ViewDimension = SrvDimension.Texture2D,
                                                            Format = textureDesc.Format.ToResourceViewFormat(),
                                                            Shader4ComponentMapping = ResourceHelper.DefaultShader4ComponentMapping,
                                                            Anonymous = new ShaderResourceViewDescUnion
                                                            {
                                                                Texture2D = new Tex2DSrv
                                                                {
                                                                    MipLevels = (uint)textureDesc.MipCount,
                                                                    MostDetailedMip = 0,
                                                                    ResourceMinLODClamp = 0.0f,
                                                                    PlaneSlice = 0,
                                                                }
                                                            }
                                                        };

                                                        break;
                                                    }
                                                case FGTextureDimension.Tex2DArray:
                                                    {
                                                        srvDesc = new ShaderResourceViewDesc
                                                        {
                                                            ViewDimension = SrvDimension.Texture2Darray,
                                                            Format = textureDesc.Format.ToResourceViewFormat(),
                                                            Shader4ComponentMapping = ResourceHelper.DefaultShader4ComponentMapping,
                                                            Anonymous = new ShaderResourceViewDescUnion
                                                            {
                                                                Texture2DArray = new Tex2DArraySrv
                                                                {
                                                                    MipLevels = (uint)textureDesc.MipCount,
                                                                    MostDetailedMip = 0,
                                                                    ResourceMinLODClamp = 0.0f,
                                                                    PlaneSlice = 0,
                                                                    FirstArraySlice = 0,
                                                                    ArraySize = (uint)textureDesc.DepthOrArraySize
                                                                }
                                                            }
                                                        };

                                                        break;
                                                    }
                                                case FGTextureDimension.Tex3D:
                                                    {
                                                        srvDesc = new ShaderResourceViewDesc
                                                        {
                                                            ViewDimension = SrvDimension.Texture3D,
                                                            Format = textureDesc.Format.ToResourceViewFormat(),
                                                            Shader4ComponentMapping = ResourceHelper.DefaultShader4ComponentMapping,
                                                            Anonymous = new ShaderResourceViewDescUnion
                                                            {
                                                                Texture3D = new Tex3DSrv
                                                                {
                                                                    MipLevels = (uint)textureDesc.MipCount,
                                                                    MostDetailedMip = 0,
                                                                    ResourceMinLODClamp = 0.0f
                                                                }
                                                            }
                                                        };

                                                        break;
                                                    }
                                                case FGTextureDimension.Cube:
                                                    {
                                                        srvDesc = new ShaderResourceViewDesc
                                                        {
                                                            ViewDimension = SrvDimension.Texturecube,
                                                            Format = textureDesc.Format.ToResourceViewFormat(),
                                                            Shader4ComponentMapping = ResourceHelper.DefaultShader4ComponentMapping,
                                                            Anonymous = new ShaderResourceViewDescUnion
                                                            {
                                                                TextureCube = new TexcubeSrv
                                                                {
                                                                    MipLevels = (uint)textureDesc.MipCount,
                                                                    MostDetailedMip = 0,
                                                                    ResourceMinLODClamp = 0.0f
                                                                }
                                                            }
                                                        };

                                                        break;
                                                    }
                                                case FGTextureDimension.CubeArray:
                                                    {
                                                        srvDesc = new ShaderResourceViewDesc
                                                        {
                                                            ViewDimension = SrvDimension.Texturecubearray,
                                                            Format = textureDesc.Format.ToResourceViewFormat(),
                                                            Shader4ComponentMapping = ResourceHelper.DefaultShader4ComponentMapping,
                                                            Anonymous = new ShaderResourceViewDescUnion
                                                            {
                                                                TextureCubeArray = new TexcubeArraySrv
                                                                {
                                                                    MipLevels = (uint)textureDesc.MipCount,
                                                                    MostDetailedMip = 0,
                                                                    ResourceMinLODClamp = 0.0f,
                                                                    First2DArrayFace = 0,
                                                                    NumCubes = (uint)textureDesc.DepthOrArraySize
                                                                }
                                                            }
                                                        };

                                                        break;
                                                    }
                                            }
                                        }
                                    }

                                    if (allocationInfo.IsGPUHeap)
                                    {
                                        DescriptorHeap heap = _gpuResourceViewHeaps[allocationInfo.HeapIndex];
                                        CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);
                                        GpuDescriptorHandle gpu = new GpuDescriptorHandle(heap.StartOffsetGpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);

                                        device->CreateShaderResourceView((ID3D12Resource*)resourcePtr, &srvDesc, cpu);
                                        handle = new DescriptorHandle((uint)allocationInfo.DescriptorIndex, gpu);
                                    }
                                    else
                                    {
                                        DescriptorHeap heap = _cpuResourceViewHeaps[allocationInfo.HeapIndex];
                                        CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);

                                        device->CreateShaderResourceView((ID3D12Resource*)resourcePtr, &srvDesc, cpu);
                                        handle = new DescriptorHandle((uint)allocationInfo.DescriptorIndex, cpu);
                                    }

                                    break;
                                }
                            case Recording.DescriptorHeapType.UnorderedAccessView:
                                {
                                    UnorderedAccessViewDesc uavDesc;
                                    if (resourceInfo.ImportedResource != null)
                                    {
                                        if (resourceInfo.ImportedResource is RHIBuffer buffer)
                                        {
                                            ref readonly RHIBufferDescription bufferDesc = ref buffer.Description;

                                            uavDesc = new UnorderedAccessViewDesc
                                            {
                                                ViewDimension = UavDimension.Buffer,
                                            };

                                            if (bufferDesc.Mode == RHIBufferMode.Default)
                                            {
                                                uavDesc.Format = Format.FormatUnknown;
                                                uavDesc.Buffer = new BufferUav
                                                {
                                                    FirstElement = 0,
                                                    NumElements = (uint)bufferDesc.ElementCount,
                                                    StructureByteStride = (uint)bufferDesc.Stride,
                                                    CounterOffsetInBytes = 0,
                                                    Flags = BufferUavFlags.None
                                                };
                                            }
                                            else
                                            {
                                                uavDesc.Format = Format.FormatR32Typeless;
                                                uavDesc.Buffer = new BufferUav
                                                {
                                                    FirstElement = 0,
                                                    NumElements = bufferDesc.Width / sizeof(uint),
                                                    StructureByteStride = 0,
                                                    CounterOffsetInBytes = 0,
                                                    Flags = BufferUavFlags.Raw
                                                };
                                            }
                                        }
                                        else
                                        {
                                            RHITexture texture = (RHITexture)resourceInfo.ImportedResource;
                                            ref readonly RHITextureDescription textureDesc = ref texture.Description;

                                            switch (textureDesc.Dimension)
                                            {
                                                case RHIDimension.Texture1D:
                                                    {
                                                        uavDesc = new UnorderedAccessViewDesc
                                                        {
                                                            ViewDimension = UavDimension.Texture1D,
                                                            Format = textureDesc.Format.ToResourceViewFormat(),
                                                            Anonymous = new UnorderedAccessViewDescUnion
                                                            {
                                                                Texture1D = new Tex1DUav
                                                                {
                                                                    MipSlice = 0
                                                                }
                                                            }
                                                        };

                                                        break;
                                                    }
                                                case RHIDimension.Texture2D:
                                                    {
                                                        uavDesc = new UnorderedAccessViewDesc
                                                        {
                                                            ViewDimension = UavDimension.Texture2D,
                                                            Format = textureDesc.Format.ToResourceViewFormat(),
                                                            Anonymous = new UnorderedAccessViewDescUnion
                                                            {
                                                                Texture2D = new Tex2DUav
                                                                {
                                                                    MipSlice = 0,
                                                                    PlaneSlice = 0
                                                                }
                                                            }
                                                        };

                                                        break;
                                                    }
                                                case RHIDimension.Texture3D:
                                                    {
                                                        uavDesc = new UnorderedAccessViewDesc
                                                        {
                                                            ViewDimension = UavDimension.Texture3D,
                                                            Format = textureDesc.Format.ToResourceViewFormat(),
                                                            Anonymous = new UnorderedAccessViewDescUnion
                                                            {
                                                                Texture3D = new Tex3DUav
                                                                {
                                                                    MipSlice = 0,
                                                                    FirstWSlice = 0,
                                                                    WSize = (uint)textureDesc.DepthOrArraySize
                                                                }
                                                            }
                                                        };

                                                        break;
                                                    }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        GenericResourceDesc resourceDesc = _renderPlan.ResourceDescs[resourceInfo.DescriptionIndex];
                                        if (resourceDesc.Type == FGResourceType.Buffer)
                                        {
                                            FGResourceDesc.Buffer bufferDesc = resourceDesc.Resource.BufferDesc;

                                            uavDesc = new UnorderedAccessViewDesc
                                            {
                                                ViewDimension = UavDimension.Buffer,
                                            };

                                            if (bufferDesc.Target.HasFlags(FGBufferTarget.Raw))
                                            {
                                                uavDesc.Format = Format.FormatR32Typeless;
                                                uavDesc.Buffer = new BufferUav
                                                {
                                                    FirstElement = 0,
                                                    NumElements = (uint)bufferDesc.ElementCount,
                                                    StructureByteStride = 0,
                                                    CounterOffsetInBytes = 0,
                                                    Flags = BufferUavFlags.Raw
                                                };
                                            }
                                            else
                                            {
                                                uavDesc.Format = Format.FormatUnknown;
                                                uavDesc.Buffer = new BufferUav
                                                {
                                                    FirstElement = 0,
                                                    NumElements = (uint)bufferDesc.ElementCount,
                                                    StructureByteStride = (uint)bufferDesc.Stride,
                                                    CounterOffsetInBytes = 0,
                                                    Flags = BufferUavFlags.Raw
                                                };
                                            }
                                        }
                                        else
                                        {
                                            FGResourceDesc.Texture textureDesc = resourceDesc.Resource.TextureDesc;

                                            switch (textureDesc.Dimension)
                                            {
                                                case FGTextureDimension.Tex2D:
                                                    {
                                                        uavDesc = new UnorderedAccessViewDesc
                                                        {
                                                            ViewDimension = UavDimension.Texture2D,
                                                            Format = textureDesc.Format.ToResourceViewFormat(),
                                                            Anonymous = new UnorderedAccessViewDescUnion
                                                            {
                                                                Texture2D = new Tex2DUav
                                                                {
                                                                    MipSlice = 0,
                                                                    PlaneSlice = 0
                                                                }
                                                            }
                                                        };

                                                        break;
                                                    }
                                                case FGTextureDimension.Tex2DArray:
                                                    {
                                                        uavDesc = new UnorderedAccessViewDesc
                                                        {
                                                            ViewDimension = UavDimension.Texture2Darray,
                                                            Format = textureDesc.Format.ToResourceViewFormat(),
                                                            Anonymous = new UnorderedAccessViewDescUnion
                                                            {
                                                                Texture2DArray = new Tex2DArrayUav
                                                                {
                                                                    MipSlice = 0,
                                                                    PlaneSlice = 0,
                                                                    FirstArraySlice = 0,
                                                                    ArraySize = (uint)textureDesc.DepthOrArraySize
                                                                }
                                                            }
                                                        };

                                                        break;
                                                    }
                                                case FGTextureDimension.Tex3D:
                                                    {
                                                        uavDesc = new UnorderedAccessViewDesc
                                                        {
                                                            ViewDimension = UavDimension.Texture3D,
                                                            Format = textureDesc.Format.ToResourceViewFormat(),
                                                            Anonymous = new UnorderedAccessViewDescUnion
                                                            {
                                                                Texture3D = new Tex3DUav
                                                                {
                                                                    MipSlice = 0,
                                                                    FirstWSlice = 0,
                                                                    WSize = (uint)textureDesc.DepthOrArraySize
                                                                }
                                                            }
                                                        };

                                                        break;
                                                    }
                                            }
                                        }
                                    }

                                    if (allocationInfo.IsGPUHeap)
                                    {
                                        DescriptorHeap heap = _gpuResourceViewHeaps[allocationInfo.HeapIndex];
                                        CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);
                                        GpuDescriptorHandle gpu = new GpuDescriptorHandle(heap.StartOffsetGpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);

                                        device->CreateUnorderedAccessView((ID3D12Resource*)resourcePtr, null, &uavDesc, cpu);
                                        handle = new DescriptorHandle((uint)allocationInfo.DescriptorIndex, gpu);
                                    }
                                    else
                                    {
                                        DescriptorHeap heap = _cpuResourceViewHeaps[allocationInfo.HeapIndex];
                                        CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _cbvSrvUavHeapIncrementSize);

                                        device->CreateUnorderedAccessView((ID3D12Resource*)resourcePtr, null, &uavDesc, cpu);
                                        handle = new DescriptorHandle((uint)allocationInfo.DescriptorIndex, cpu);
                                    }

                                    break;
                                }
                        }
                    }
                }

                using (new ProfilingScope("Samplers"))
                {
                    ROList<SamplerDescriptorAllocationInfo> samplerDescriptors = _descriptorAllocator.SamplerDescriptors;
                    if (_samplerDescriptors.Length < samplerDescriptors.Count)
                    {
                        _samplerDescriptors = new uint[MemoryHelper.AlignUp(samplerDescriptors.Count, 64)];
                    }

                    for (int i = 0; i < samplerDescriptors.Count; ++i)
                    {
                        SamplerDescriptorAllocationInfo allocationInfo = samplerDescriptors[i];
                        ref uint handle = ref _samplerDescriptors[i];

                        ref readonly RHISamplerDescription rhiDesc = ref allocationInfo.Sampler.Description;

                        SamplerDesc2 samplerDesc = new SamplerDesc2
                        {
                            Filter = rhiDesc.MaxAnisotropy > 1 ?
                                ResourceHelper.EncodeAnisotropicFilter(rhiDesc.Reduction) :
                                ResourceHelper.EncodeBasicFilter(rhiDesc.Min, rhiDesc.Mag, rhiDesc.Mip, rhiDesc.Reduction),
                            AddressU = ConversionHelper.ToTextureAddressMode(rhiDesc.AddressModeU),
                            AddressV = ConversionHelper.ToTextureAddressMode(rhiDesc.AddressModeV),
                            AddressW = ConversionHelper.ToTextureAddressMode(rhiDesc.AddressModeW),
                            MipLODBias = rhiDesc.MipLODBias,
                            MaxAnisotropy = rhiDesc.MaxAnisotropy,
                            ComparisonFunc = ConversionHelper.ToComparisonFunc(rhiDesc.ComparisonFunction),
                            MinLOD = rhiDesc.MinLOD,
                            MaxLOD = rhiDesc.MaxLOD,
                            Flags = SamplerFlags.None
                        };

                        if (rhiDesc.BorderColorAsUInt)
                            samplerDesc.Flags |= SamplerFlags.UintBorderColor;

                        // UInt borders are just bitcasted color fields anyway
                        Color color = rhiDesc.BorderColor;
                        NativeMemory.Copy(&color, samplerDesc.Anonymous.FloatBorderColor, 4 * sizeof(float));

                        DescriptorHeap heap = _samplerHeaps[allocationInfo.HeapIndex];
                        CpuDescriptorHandle cpu = new CpuDescriptorHandle(heap.StartOffsetCpu.Ptr + (nuint)allocationInfo.DescriptorIndex * _samplerHeapIncrementSize);

                        device->CreateSampler2(&samplerDesc, cpu);
                        handle = (uint)allocationInfo.DescriptorIndex;
                    }
                }
            }
        }

        private void EnsureHeapsExist(DescriptorHeapType heapType, DescriptorHeapFlags heapFlags, ref readonly TransientHeapData heapData, ref DescriptorHeap[] heaps)
        {
            int heapCount = heapData.HeapIndex;
            if (heapData.DescriptorIndex > 0)
                ++heapCount;

            heapCount = Math.Max(heapCount, 1);

            if (heapCount == 0)
                return;

            if (heaps.Length < heapCount)
            {
                DescriptorHeap[] newArray = new DescriptorHeap[MemoryHelper.AlignUp(heapCount, 2)];
                Array.Copy(heaps, newArray, heaps.Length);

                heaps = newArray;
            }

            ID3D12Device14* device = (ID3D12Device14*)Unsafe.AsPointer(ref _device.NativeDevice.Device.Get());

            for (int i = 0; i < heapCount; ++i)
            {
                ref DescriptorHeap heapPtr = ref heaps[i];
                if (heapPtr.Ptr.IsNull)
                {
                    DescriptorHeapDesc heapDesc = new DescriptorHeapDesc
                    {
                        Type = heapType,
                        NumDescriptors = (uint)heapData.HeapSizeLimit,
                        Flags = heapFlags
                    };

                    ID3D12DescriptorHeap* descriptorHeap = null;
                    HResult hr = device->CreateDescriptorHeap(&heapDesc, SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(), (void**)&descriptorHeap);

                    if (hr.IsFailure)
                    {
                        EngLog.Render.Fatal("Failed to create descriptor heap");
                        EngLog.Render.Fatal("    Heap desc: {");
                        EngLog.Render.Fatal("       Type: {v}", heapDesc.Type);
                        EngLog.Render.Fatal("       NumDescriptors: {v}", heapDesc.NumDescriptors);
                        EngLog.Render.Fatal("       Flags: {v}", heapDesc.Flags);
                        EngLog.Render.Fatal("       NodeMask: {v}", heapDesc.NodeMask);
                        EngLog.Render.Fatal("    }");
                        EngLog.Render.Fatal("    HResult value: {v:x8}", hr.Value);

                        _device.NativeDevice.FlushPendingMessages();
                        EngLog.ThrowFatal();
                    }

                    heapPtr = new DescriptorHeap(descriptorHeap,
                        descriptorHeap->GetCPUDescriptorHandleForHeapStart(),
                        heapFlags.HasFlags(DescriptorHeapFlags.ShaderVisible) ? descriptorHeap->GetGPUDescriptorHandleForHeapStart() : default);
                }
            }
        }

        internal uint GetDescriptorIndex(CPUDescriptorCmdData cmdData) => cmdData.IsNull ? throw new NullReferenceException() : _resourceDescriptors[cmdData.IndexIntoDescriptorArray].Index;
        internal uint GetDescriptorIndex(GPUDescriptorCmdData cmdData) => cmdData.IsNull ? throw new NullReferenceException() : _resourceDescriptors[cmdData.IndexIntoDescriptorArray].Index;

        internal uint GetSamplerDescriptorIndex(CPUDescriptorCmdData cmdData) => cmdData.IsNull ? throw new NullReferenceException() : _samplerDescriptors[cmdData.IndexIntoDescriptorArray];

        internal CpuDescriptorHandle GetCpuDescriptorHandle(CPUDescriptorCmdData cmdData) => cmdData.IsNull ? throw new NullReferenceException() : _resourceDescriptors[cmdData.IndexIntoDescriptorArray].CPU;
        internal GpuDescriptorHandle GetGpuDescriptorHandle(GPUDescriptorCmdData cmdData) => cmdData.IsNull ? throw new NullReferenceException() : _resourceDescriptors[cmdData.IndexIntoDescriptorArray].GPU;

        internal ID3D12DescriptorHeap* GetGpuHeapPointer(int heapIndex) => _gpuResourceViewHeaps[heapIndex].Ptr;
        internal ID3D12DescriptorHeap* GetSamplerHeapPointer(int heapIndex) => _samplerHeaps[heapIndex].Ptr;

        private readonly record struct DescriptorHeap(Ptr<ID3D12DescriptorHeap> Ptr, CpuDescriptorHandle StartOffsetCpu, GpuDescriptorHandle StartOffsetGpu);

        [StructLayout(LayoutKind.Explicit)]
        private readonly record struct DescriptorHandle
        {
            [FieldOffset(0)] public readonly uint Index;
            [FieldOffset(4)] public readonly CpuDescriptorHandle CPU;
            [FieldOffset(4)] public readonly GpuDescriptorHandle GPU;

            public DescriptorHandle(uint index, CpuDescriptorHandle cpu)
            {
                Index = index;
                CPU = cpu;
            }

            public DescriptorHandle(uint index, GpuDescriptorHandle gpu)
            {
                Index = index;
                GPU = gpu;
            }
        }
    }
}
