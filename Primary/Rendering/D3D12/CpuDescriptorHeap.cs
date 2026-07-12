using Arch.LowLevel;
using Primary.Common;
using Primary.Rendering.Resources;
using Primary.RHI;
using Primary.RHI.Direct3D12;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace Primary.Rendering.D3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe sealed class CpuDescriptorHeap : IDisposable
    {
        private readonly NRDDevice _device;

        private readonly int _individualHeapSize;
        private readonly int _incrementSize;
        private readonly DescriptorHeapType _heapType;

        private readonly int _maxDescriptorOffset;

        private List<HeapData> _heaps;

        private int _heapIndex;
        private int _heapDescriptorOffset;

        private readonly CpuDescriptorHandle _nullDescriptor;

        private Dictionary<NRDResource, CpuDescriptorHandle> _allocatedDescriptors;

        private bool _disposedValue;

        internal CpuDescriptorHeap(NRDDevice device, int individualHeapSize, DescriptorHeapType type)
        {
            _device = device;

            _individualHeapSize = individualHeapSize;
            _incrementSize = (int)device.Device->GetDescriptorHandleIncrementSize(type);
            _heapType = type;

            _maxDescriptorOffset = individualHeapSize * _incrementSize;

            _heaps = new List<HeapData>();

            _heapIndex = 0;
            _heapDescriptorOffset = 0;

            _allocatedDescriptors = new Dictionary<NRDResource, CpuDescriptorHandle>();

            AddNewHeapToList();

            {
                HeapData heap = _heaps[0];

                switch (type)
                {
                    case DescriptorHeapType.Rtv:
                        {
                            RenderTargetViewDesc desc = new RenderTargetViewDesc
                            {
                                Format = Format.FormatR8G8B8A8Unorm,
                                ViewDimension = RtvDimension.Texture2D,
                                Texture2D = new Tex2DRtv
                                {
                                    MipSlice = 0,
                                    PlaneSlice = 0
                                }
                            };

                            _device.Device->CreateRenderTargetView(null, &desc, heap.StartHandle);
                            break;
                        }
                    case DescriptorHeapType.Dsv:
                        {
                            DepthStencilViewDesc desc = new DepthStencilViewDesc
                            {
                                Format = Format.FormatD32Float,
                                ViewDimension = DsvDimension.Texture2D,
                                Texture2D = new Tex2DDsv
                                {
                                    MipSlice = 0,
                                }
                            };

                            _device.Device->CreateDepthStencilView(null, &desc, heap.StartHandle);
                            break;
                        }
                }

                _heapDescriptorOffset = _incrementSize;
                _nullDescriptor = heap.StartHandle;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                foreach (HeapData heap in _heaps)
                {
                    heap.Heap.Dispose();
                }
                _heaps.Clear();

                _disposedValue = true;
            }
        }

        ~CpuDescriptorHeap()
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
            _heapIndex = 0;
            _heapDescriptorOffset = 0;

            _allocatedDescriptors.Clear();

            if (_heaps.Count == 0)
                AddNewHeapToList();
        }

        internal CpuDescriptorHandle GetDescriptorHandle(NRDResource resource)
        {
            if (resource.IsNull)
                return _nullDescriptor;

            if (_allocatedDescriptors.TryGetValue(resource, out CpuDescriptorHandle handle))
                return handle;

            if (_heapDescriptorOffset >= _maxDescriptorOffset)
            {
                _heapIndex++;
                _heapDescriptorOffset = 0;

                AddNewHeapToList();
            }

            HeapData data = _heaps[_heapIndex];
            handle = _heapDescriptorOffset > 0 ? new CpuDescriptorHandle((nuint)(data.StartHandle.Ptr + (ulong)_heapDescriptorOffset)) : data.StartHandle;

            _heapDescriptorOffset += _incrementSize;

            if (resource.IsExternal)
            {
                ResourceManager resources = _device.ResourceManager;
                switch (_heapType)
                {
                    case DescriptorHeapType.Rtv:
                        {
                            D3D12RHITextureNative* native = (D3D12RHITextureNative*)resource.Native;

                            Debug.Assert(Flags.HasFlag(native->Base.Description.Usage, RHIResourceUsage.RenderTarget));

                            RenderTargetViewDesc desc = new RenderTargetViewDesc
                            {
                                Format = native->Base.Description.Format.ToRenderTargetFormat(),
                                ViewDimension = RtvDimension.Texture2D,
                                Texture2D = new Tex2DRtv
                                {
                                    MipSlice = 0,
                                    PlaneSlice = 0
                                }
                            };

                            _device.Device->CreateRenderTargetView((ID3D12Resource*)resources.GetResource(resource), &desc, handle);
                            break;
                        }
                    case DescriptorHeapType.Dsv:
                        {
                            D3D12RHITextureNative* native = (D3D12RHITextureNative*)resource.Native;

                            Debug.Assert(Flags.HasFlag(native->Base.Description.Usage, RHIResourceUsage.RenderTarget));

                            DepthStencilViewDesc desc = new DepthStencilViewDesc
                            {
                                Format = native->Base.Description.Format.ToDepthStencilFormat(),
                                ViewDimension = DsvDimension.Texture2D,
                                Texture2D = new Tex2DDsv
                                {
                                    MipSlice = 0,
                                }
                            };

                            _device.Device->CreateDepthStencilView((ID3D12Resource*)resources.GetResource(resource), &desc, handle);
                            break;
                        }
                }
            }
            else
            {
                ResourceManager resources = _device.ResourceManager;
                switch (_heapType)
                {
                    case DescriptorHeapType.Rtv:
                        {
                            FrameGraphTexture texture = resources.FindFGTexture(resource);

                            Debug.Assert(texture.Index >= 0);
                            Debug.Assert(Flags.HasFlag(texture.Description.Usage, FGTextureUsage.RenderTarget));

                            RenderTargetViewDesc desc = new RenderTargetViewDesc
                            {
                                Format = texture.Description.Format.ToRenderTargetFormat(),
                                ViewDimension = RtvDimension.Texture2D,
                                Texture2D = new Tex2DRtv
                                {
                                    MipSlice = 0,
                                    PlaneSlice = 0
                                }
                            };

                            _device.Device->CreateRenderTargetView((ID3D12Resource*)resources.GetResource(resource), &desc, handle);
                            break;
                        }
                    case DescriptorHeapType.Dsv:
                        {
                            FrameGraphTexture texture = resources.FindFGTexture(resource);

                            Debug.Assert(texture.Index >= 0);
                            Debug.Assert(Flags.HasFlag(texture.Description.Usage, FGTextureUsage.DepthStencil));

                            DepthStencilViewDesc desc = new DepthStencilViewDesc
                            {
                                Format = texture.Description.Format.ToDepthStencilFormat(),
                                ViewDimension = DsvDimension.Texture2D,
                                Texture2D = new Tex2DDsv
                                {
                                    MipSlice = 0,
                                }
                            };

                            _device.Device->CreateDepthStencilView((ID3D12Resource*)resources.GetResource(resource), &desc, handle);
                            break;
                        }
                }
            }

            _allocatedDescriptors[resource] = handle;
            return handle;
        }

        internal CpuDescriptorHandle GetDescriptorHandleForSwapChain(ref D3D12RHISwapChainNative native, ref D3D12RHISwapChainBuffer currentBuffer)
        {
            NRDResource localResource = new NRDResource { Native = Unsafe.AsPointer(ref currentBuffer) };
            if (_allocatedDescriptors.TryGetValue(localResource, out CpuDescriptorHandle handle))
                return handle;

            if (_heapDescriptorOffset >= _maxDescriptorOffset)
            {
                _heapIndex++;
                _heapDescriptorOffset = 0;

                AddNewHeapToList();
            }

            HeapData data = _heaps[_heapIndex];
            handle = _heapDescriptorOffset > 0 ? new CpuDescriptorHandle((nuint)(data.StartHandle.Ptr + (ulong)_heapDescriptorOffset)) : data.StartHandle;

            _heapDescriptorOffset += _incrementSize;

            RenderTargetViewDesc desc = new RenderTargetViewDesc
            {
                Format = native.Base.Description.BackBufferFormat.ToTextureFormat(),
                ViewDimension = RtvDimension.Texture2D,
                Texture2D = new Tex2DRtv
                {
                    MipSlice = 0,
                    PlaneSlice = 0
                }
            };

            _device.Device->CreateRenderTargetView(currentBuffer.Resource, &desc, handle);

            _allocatedDescriptors[localResource] = handle;
            return handle;
        }

        private void AddNewHeapToList()
        {
            DescriptorHeapDesc desc = new DescriptorHeapDesc
            {
                Type = _heapType,
                NumDescriptors = (uint)_individualHeapSize,
                Flags = DescriptorHeapFlags.None,
                NodeMask = 0
            };

            ComPtr<ID3D12DescriptorHeap> descriptorHeap = null;
            HResult ret = _device.Device->CreateDescriptorHeap(&desc, out descriptorHeap);

            //TODO: proper error messages and handling
            if (ret.IsFailure)
            {
                _device.RHIDevice.FlushPendingMessages();
                throw new NotImplementedException("No error handling yet");
            }

            _heaps.Add(new HeapData(descriptorHeap, descriptorHeap.GetCPUDescriptorHandleForHeapStart()));
        }

        internal CpuDescriptorHandle NullDescriptor => _nullDescriptor;

        private readonly record struct HeapData(ComPtr<ID3D12DescriptorHeap> Heap, CpuDescriptorHandle StartHandle);
    }
}
