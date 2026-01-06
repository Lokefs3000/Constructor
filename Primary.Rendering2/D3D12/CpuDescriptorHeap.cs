using Primary.Common;
using Primary.Rendering2.Resources;
using Primary.RHI;
using System.Diagnostics;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12_DESCRIPTOR_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_DESCRIPTOR_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12_DSV_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_RTV_DIMENSION;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;

namespace Primary.Rendering2.D3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe sealed class CpuDescriptorHeap : IDisposable
    {
        private readonly NRDDevice _device;

        private readonly int _individualHeapSize;
        private readonly int _incrementSize;
        private readonly D3D12_DESCRIPTOR_HEAP_TYPE _heapType;

        private readonly int _maxDescriptorOffset;

        private List<HeapData> _heaps;

        private int _heapIndex;
        private int _heapDescriptorOffset;

        private readonly D3D12_CPU_DESCRIPTOR_HANDLE _nullDescriptor;

        private Dictionary<NRDResource, D3D12_CPU_DESCRIPTOR_HANDLE> _allocatedDescriptors;

        private bool _disposedValue;

        internal CpuDescriptorHeap(NRDDevice device, int individualHeapSize, D3D12_DESCRIPTOR_HEAP_TYPE type)
        {
            _device = device;

            _individualHeapSize = individualHeapSize;
            _incrementSize = (int)device.Device->GetDescriptorHandleIncrementSize(type);
            _heapType = type;

            _maxDescriptorOffset = individualHeapSize * _incrementSize;

            _heaps = new List<HeapData>();

            _heapIndex = 0;
            _heapDescriptorOffset = 0;

            _allocatedDescriptors = new Dictionary<NRDResource, D3D12_CPU_DESCRIPTOR_HANDLE>();

            AddNewHeapToList();

            {
                HeapData heap = _heaps[0];

                switch (type)
                {
                    case D3D12_DESCRIPTOR_HEAP_TYPE_RTV:
                        {
                            D3D12_RENDER_TARGET_VIEW_DESC desc = new D3D12_RENDER_TARGET_VIEW_DESC
                            {
                                Format = DXGI_FORMAT_R8G8B8A8_UNORM,
                                ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D,
                                Texture2D = new D3D12_TEX2D_RTV
                                {
                                    MipSlice = 0,
                                    PlaneSlice = 0
                                }
                            };

                            _device.Device->CreateRenderTargetView(null, &desc, heap.StartHandle);
                            break;
                        }
                    case D3D12_DESCRIPTOR_HEAP_TYPE_DSV:
                        {
                            D3D12_DEPTH_STENCIL_VIEW_DESC desc = new D3D12_DEPTH_STENCIL_VIEW_DESC
                            {
                                Format = DXGI_FORMAT_D32_FLOAT,
                                ViewDimension = D3D12_DSV_DIMENSION_TEXTURE2D,
                                Texture2D = new D3D12_TEX2D_DSV
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
                    heap.Heap.Pointer->Release();
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

        internal D3D12_CPU_DESCRIPTOR_HANDLE GetDescriptorHandle(NRDResource resource)
        {
            if (resource.IsNull)
                return _nullDescriptor;

            if (_allocatedDescriptors.TryGetValue(resource, out D3D12_CPU_DESCRIPTOR_HANDLE handle))
                return handle;

            if (_heapDescriptorOffset >= _maxDescriptorOffset)
            {
                _heapIndex++;
                _heapDescriptorOffset = 0;

                AddNewHeapToList();
            }

            HeapData data = _heaps[_heapIndex];
            handle = _heapDescriptorOffset > 0 ? new D3D12_CPU_DESCRIPTOR_HANDLE(data.StartHandle, _heapDescriptorOffset) : data.StartHandle;

            _heapDescriptorOffset += _incrementSize;

            if (resource.IsExternal)
            {
                throw new NotImplementedException("external");
            }
            else
            {
                ResourceManager resources = _device.ResourceManager;
                switch (_heapType)
                {
                    case D3D12_DESCRIPTOR_HEAP_TYPE_RTV:
                        {
                            FrameGraphTexture texture = resources.FindFGTexture(resource);

                            Debug.Assert(texture.Index >= 0);
                            Debug.Assert(FlagUtility.HasFlag(texture.Description.Usage, FGTextureUsage.RenderTarget));

                            D3D12_RENDER_TARGET_VIEW_DESC desc = new D3D12_RENDER_TARGET_VIEW_DESC
                            {
                                Format = FormatConverter.ToDXGIFormat(texture.Description.Format),
                                ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D,
                                Texture2D = new D3D12_TEX2D_RTV
                                {
                                    MipSlice = 0,
                                    PlaneSlice = 0
                                }
                            };

                            _device.Device->CreateRenderTargetView((ID3D12Resource*)resources.GetResource(resource), &desc, handle);
                            break;
                        }
                    case D3D12_DESCRIPTOR_HEAP_TYPE_DSV:
                        {
                            FrameGraphTexture texture = resources.FindFGTexture(resource);

                            Debug.Assert(texture.Index >= 0);
                            Debug.Assert(FlagUtility.HasFlag(texture.Description.Usage, FGTextureUsage.DepthStencil));

                            D3D12_DEPTH_STENCIL_VIEW_DESC desc = new D3D12_DEPTH_STENCIL_VIEW_DESC
                            {
                                Format = FormatConverter.ToDXGIFormat(texture.Description.Format),
                                ViewDimension = D3D12_DSV_DIMENSION_TEXTURE2D,
                                Texture2D = new D3D12_TEX2D_DSV
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

        private void AddNewHeapToList()
        {
            D3D12_DESCRIPTOR_HEAP_DESC desc = new D3D12_DESCRIPTOR_HEAP_DESC
            {
                Type = _heapType,
                NumDescriptors = (uint)_individualHeapSize,
                Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE,
                NodeMask = 0
            };
            
            ID3D12DescriptorHeap* heap = null;
            HRESULT ret = _device.Device->CreateDescriptorHeap(&desc, UuidOf.Get<ID3D12DescriptorHeap>(), (void**)&heap);

            //TODO: proper error messages and handling
            if (ret.FAILED)
            {
                _device.RHIDevice.FlushPendingMessages();
                throw new NotImplementedException("No error handling yet");
            }

            _heaps.Add(new HeapData(heap, heap->GetCPUDescriptorHandleForHeapStart()));
        }

        internal D3D12_CPU_DESCRIPTOR_HANDLE NullDescriptor => _nullDescriptor;

        private readonly record struct HeapData(Ptr<ID3D12DescriptorHeap> Heap, D3D12_CPU_DESCRIPTOR_HANDLE StartHandle);
    }
}
