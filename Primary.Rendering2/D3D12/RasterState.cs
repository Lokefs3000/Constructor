using Primary.Common;
using Primary.Rendering2.Assets;
using Primary.Rendering2.Resources;
using Primary.Rendering2.Structures;
using Primary.RHI;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.DirectX.D3D12_BARRIER_ACCESS;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_LAYOUT;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_SYNC;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;

namespace Primary.Rendering2.D3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe sealed class RasterState
    {
        private readonly NRDDevice _device;

        private DirtyArray<FrameGraphTexture> _renderTargets;
        private DirtyValue<FrameGraphTexture> _depthStencil;

        private DirtyArray<FGViewport?> _viewports;
        private DirtyArray<FGRect?> _scissors;

        private DirtyValue<uint> _stencilRef;

        private DirtyValue<SetBufferData> _vertexBuffer;
        private DirtyValue<SetBufferData> _indexBuffer;

        private DirtyValue<RHI.GraphicsPipeline?> _pipeline;

        private nint _propertyRawData;
        private int _propertyDataSize;
        private int _activeDataSize;

        private PropertyResourceData[] _resourceData;
        private int _activeResourceCount;

        private bool _useBufferForHeader;
        private bool _hasHadPropertyChange;

        //// state \\\\

        private D3D12_CPU_DESCRIPTOR_HANDLE[] _rtvDescriptors;
        private int _rtvDescriptorLength;

        private D3D12_CPU_DESCRIPTOR_HANDLE _dsvDescriptor;

        private D3D12_VIEWPORT[] _rawViewports;
        private RECT[] _rawRects;

        private D3D12_VERTEX_BUFFER_VIEW _vertexBufferView;
        private D3D12_INDEX_BUFFER_VIEW? _indexBufferView;

        private RHI.Direct3D12.PipelineState _rasterPipelineState;

        private nint _propertyDataBuffer;
        private int _propertyDataBufferSize;

        internal RasterState(NRDDevice device)
        {
            _device = device;

            _renderTargets = new DirtyArray<FrameGraphTexture>(8, FrameGraphTexture.Invalid);
            _depthStencil = new DirtyValue<FrameGraphTexture>(FrameGraphTexture.Invalid);

            _viewports = new DirtyArray<FGViewport?>(8, null);
            _scissors = new DirtyArray<FGRect?>(8, null);

            _stencilRef = new DirtyValue<uint>(0);

            _vertexBuffer = new DirtyValue<SetBufferData>(new SetBufferData(FrameGraphBuffer.Invalid, -1));
            _indexBuffer = new DirtyValue<SetBufferData>(new SetBufferData(FrameGraphBuffer.Invalid, -1));

            _pipeline = new DirtyValue<RHI.GraphicsPipeline?>();

            _propertyRawData = nint.Zero;
            _propertyDataSize = 0;
            _activeDataSize = 0;

            _resourceData = Array.Empty<PropertyResourceData>();
            _activeResourceCount = 0;

            _useBufferForHeader = false;
            _hasHadPropertyChange = false;

            _rtvDescriptors = new D3D12_CPU_DESCRIPTOR_HANDLE[8];
            _dsvDescriptor = D3D12_CPU_DESCRIPTOR_HANDLE.DEFAULT;

            _rawViewports = new D3D12_VIEWPORT[8];
            _rawRects = new RECT[8];

            _vertexBufferView = default;
            _indexBufferView = null;

            _rasterPipelineState = new RHI.Direct3D12.PipelineState();

            _propertyDataBuffer = nint.Zero;
            _propertyDataBufferSize = 0;
        }

        internal void ResetInternal()
        {
            _renderTargets.Fill(FrameGraphTexture.Invalid);
            _depthStencil.Value = FrameGraphTexture.Invalid;

            _viewports.Fill(null);
            _scissors.Fill(null);

            _vertexBuffer.Value = new SetBufferData(FrameGraphBuffer.Invalid, -1);
            _indexBuffer.Value = new SetBufferData(FrameGraphBuffer.Invalid, -1);

            _pipeline.Value = null;

            ClearPropertyData(0, false);

            _hasHadPropertyChange = false;

            Array.Fill(_rtvDescriptors, _device.RTVDescriptorHeap.NullDescriptor);
            _rtvDescriptorLength = 0;

            _dsvDescriptor = _device.DSVDescriptorHeap.NullDescriptor;

            Array.Clear(_rawViewports);
            Array.Clear(_rawRects);

            _vertexBufferView = default;
            _indexBufferView = null;

            _rasterPipelineState = new RHI.Direct3D12.PipelineState();
        }

        internal void SetRenderTarget(int index, FrameGraphTexture texture) => _renderTargets[index] = texture;
        internal void SetDepthStencil(FrameGraphTexture texture) => _depthStencil.Value = texture;

        internal void SetViewport(int index, FGViewport? rect) => _viewports[index] = rect;
        internal void SetScissor(int index, FGRect? rect) => _scissors[index] = rect;

        internal void SetStencilRef(uint value) => _stencilRef.Value = value;

        internal void SetVertexBuffer(FrameGraphBuffer buffer, int stride) => _vertexBuffer.Value = new SetBufferData(buffer, stride);
        internal void SetIndexBuffer(FrameGraphBuffer buffer, int stride) => _indexBuffer.Value = new SetBufferData(buffer, stride);

        internal void SetPipeline(RHI.GraphicsPipeline? pipeline) => _pipeline.Value = pipeline;

        internal void ClearPropertyData(int minimumResourceDataSize, bool useBufferForHeader)
        {
            _activeDataSize = 0;

            if (_resourceData.Length < minimumResourceDataSize)
                _resourceData = new PropertyResourceData[BitOperations.RoundUpToPowerOf2((uint)minimumResourceDataSize)];
            else
                Array.Clear(_resourceData);

            _activeResourceCount = minimumResourceDataSize;

            _useBufferForHeader = useBufferForHeader;
            _hasHadPropertyChange = true;
        }

        internal void SetPropertyRawData(nint ptr, int size)
        {
            if (_propertyDataSize < size)
            {
                if (_propertyRawData != nint.Zero)
                    NativeMemory.Free(_propertyRawData.ToPointer());

                _propertyDataSize = (int)BitOperations.RoundUpToPowerOf2((uint)size);
                _propertyRawData = (nint)NativeMemory.Alloc((uint)_propertyDataSize);
            }

            _activeDataSize = size;

            if (size > 0)
                NativeMemory.Copy(_propertyRawData.ToPointer(), ptr.ToPointer(), (nuint)size);
        }

        internal void SetPropertyResource(int index, FrameGraphBuffer buffer, ShPropertyStages stages)
        {
            _resourceData[index] = new PropertyResourceData(buffer, null, stages);
        }

        internal void SetPropertyResource(int index, FrameGraphTexture texture, ShPropertyStages stages)
        {
            _resourceData[index] = new PropertyResourceData(texture, null, stages);
        }

        internal void SetPropertyResource(int index, RHI.Sampler sampler, ShPropertyStages stages)
        {
            _resourceData[index] = new PropertyResourceData(null, sampler, stages);
        }

        internal bool FlushState(ID3D12GraphicsCommandList10* cmdList)
        {
            if (_pipeline.Value == null)
                return false;

            CommitFlags commit = CommitFlags.None;

            if (_renderTargets.IsAnyDirty)
            {
                int rtModLength = 0;

                for (int i = 0; i < 8; i++)
                {
                    if ((_renderTargets.DirtyMask & (1 << i)) > 0)
                    {
                        FrameGraphTexture texture = _renderTargets[i];
                        if (texture.IsValidAndRenderGraph || texture.IsExternal)
                        {
                            _device.BarrierManager.AddTextureBarrier(texture,
                                D3D12_BARRIER_SYNC_RENDER_TARGET,
                                D3D12_BARRIER_ACCESS_RENDER_TARGET,
                                D3D12_BARRIER_LAYOUT_RENDER_TARGET);

                            _device.ResourceManager.EnsureInitialized(texture);

                            _rtvDescriptors[i] = _device.RTVDescriptorHeap.GetDescriptorHandle(texture);
                            _rasterPipelineState.RTVs[i] = FormatConverter.ToRTVFormat(texture.Description.Format);

                            if (!_viewports[i].HasValue || !_scissors[i].HasValue)
                            {
                                if (texture.IsValidAndRenderGraph)
                                {
                                    ref readonly FrameGraphTextureDesc desc = ref texture.Description;

                                    if (!_viewports[i].HasValue)
                                    {
                                        _rawViewports[i] = new D3D12_VIEWPORT(0.0f, 0.0f, desc.Width, desc.Height);
                                        commit |= CommitFlags.Viewports;
                                    }

                                    if (!_scissors[i].HasValue)
                                    {
                                        _rawRects[i] = new RECT(0, 0, desc.Width, desc.Height);
                                        commit |= CommitFlags.Scissors;
                                    }
                                }
                                else
                                {
                                    ref readonly RHI.TextureDescription desc = ref Unsafe.As<RHI.Texture>(texture.Resource!).Description;

                                    if (!_viewports[i].HasValue)
                                    {
                                        _rawViewports[i] = new D3D12_VIEWPORT(0.0f, 0.0f, desc.Width, desc.Height);
                                        commit |= CommitFlags.Viewports;
                                    }

                                    if (!_scissors[i].HasValue)
                                    {
                                        _rawRects[i] = new RECT(0, 0, (int)desc.Width, (int)desc.Height);
                                        commit |= CommitFlags.Scissors;
                                    }
                                }
                            }

                            rtModLength = i + 1;
                        }
                        else
                        {
                            if (_rtvDescriptors[i] != _device.RTVDescriptorHeap.NullDescriptor)
                                rtModLength = i + 1;

                            _rtvDescriptors[i] = _device.RTVDescriptorHeap.NullDescriptor;
                            _rasterPipelineState.RTVs[i] = RHI.RenderTargetFormat.Undefined;
                        }
                    }
                }

                _renderTargets.CleanAll();
                commit |= CommitFlags.RenderTargets;

                _rtvDescriptorLength = rtModLength;
            }

            if (_depthStencil.Dirty)
            {
                FrameGraphTexture texture = _depthStencil.GetAndClean();
                if (texture.IsValidAndRenderGraph || texture.IsExternal)
                {
                    _device.BarrierManager.AddTextureBarrier(texture,
                        D3D12_BARRIER_SYNC_DEPTH_STENCIL,
                        D3D12_BARRIER_ACCESS_DEPTH_STENCIL_WRITE,
                        D3D12_BARRIER_LAYOUT_DEPTH_STENCIL_WRITE);

                    _device.ResourceManager.EnsureInitialized(texture);

                    _dsvDescriptor = _device.DSVDescriptorHeap.GetDescriptorHandle(texture);
                    _rasterPipelineState.DSV = FormatConverter.ToDSVFormat(texture.Description.Format);
                }
                else
                {
                    _dsvDescriptor = _device.DSVDescriptorHeap.NullDescriptor;
                    _rasterPipelineState.DSV = RHI.DepthStencilFormat.Undefined;
                }

                commit |= CommitFlags.RenderTargets;
            }

            if (_viewports.IsAnyDirty)
            {
                int i = 0;
                foreach (ref readonly FGViewport? viewport in _viewports.Values)
                {
                    FrameGraphTexture texture = _renderTargets[i];

                    if (viewport.HasValue)
                    {
                        FGViewport value = viewport.Value;
                        _rawViewports[i] = new D3D12_VIEWPORT(value.TopLeftX, value.TopLeftY, value.Width, value.Height, value.MinDepth, value.MaxDepth);
                    }
                    else if (texture.IsValidAndRenderGraph)
                    {
                        ref readonly FrameGraphTextureDesc desc = ref texture.Description;
                        _rawViewports[i] = new D3D12_VIEWPORT(0.0f, 0.0f, desc.Width, desc.Height);
                    }
                    else if (texture.IsExternal)
                    {
                        ref readonly RHI.TextureDescription desc = ref Unsafe.As<RHI.Texture>(texture.Resource!).Description;
                        _rawViewports[i] = new D3D12_VIEWPORT(0.0f, 0.0f, desc.Width, desc.Height);
                    }
                    else
                        _rawViewports[i] = default;

                    i++;
                }

                _viewports.CleanAll();
                commit |= CommitFlags.Viewports;
            }

            if (_scissors.IsAnyDirty)
            {
                int i = 0;
                foreach (ref readonly FGRect? scissor in _scissors.Values)
                {
                    if (scissor.HasValue)
                    {
                        FGRect value = scissor.Value;
                        _rawRects[i] = new RECT(value.Left, value.Top, value.Right, value.Bottom);
                    }
                    else
                    {
                        ref readonly D3D12_VIEWPORT viewport = ref _rawViewports[i];
                        _rawRects[i] = new RECT((int)viewport.TopLeftX, (int)viewport.TopLeftY, (int)(viewport.TopLeftX + viewport.Width), (int)(viewport.TopLeftY + viewport.Height));
                    }

                    i++;
                }

                _scissors.CleanAll();
                commit |= CommitFlags.Scissors;
            }

            if (_stencilRef.Dirty)
            {
                cmdList->OMSetStencilRef(_stencilRef.GetAndClean());
            }

            if (_vertexBuffer.Dirty)
            {
                SetBufferData data = _vertexBuffer.GetAndClean();
                if (data.Buffer.IsExternal || data.Buffer.IsValidAndRenderGraph)
                {
                    ID3D12Resource2* resource = _device.ResourceManager.GetResource(data.Buffer);
                    _device.BarrierManager.AddBufferBarrier(data.Buffer, D3D12_BARRIER_SYNC_VERTEX_SHADING, D3D12_BARRIER_ACCESS_VERTEX_BUFFER);

                    int stride = data.Stride;
                    if (stride == 0)
                    {
                        if (data.Buffer.IsExternal)
                            stride = (int)Unsafe.As<RHI.Buffer>(data.Buffer.Resource!).Description.Stride;
                        else
                            stride = data.Buffer.Description.Stride;
                    }

                    _vertexBufferView = new D3D12_VERTEX_BUFFER_VIEW
                    {
                        BufferLocation = resource->GetGPUVirtualAddress(),
                        SizeInBytes = (uint)ResourceUtility.GetBufferSize(data.Buffer),
                        StrideInBytes = (uint)stride
                    };

                    commit |= CommitFlags.VertexBuffer;
                }
            }

            if (_indexBuffer.Dirty)
            {
                SetBufferData data = _indexBuffer.GetAndClean();
                if (data.Buffer.IsExternal || data.Buffer.IsValidAndRenderGraph)
                {
                    ID3D12Resource2* resource = _device.ResourceManager.GetResource(data.Buffer);
                    _device.BarrierManager.AddBufferBarrier(data.Buffer, D3D12_BARRIER_SYNC_INDEX_INPUT, D3D12_BARRIER_ACCESS_INDEX_BUFFER);

                    int stride = data.Stride;
                    if (stride == 0)
                    {
                        if (data.Buffer.IsExternal)
                            stride = (int)Unsafe.As<RHI.Buffer>(data.Buffer.Resource!).Description.Stride;
                        else
                            stride = data.Buffer.Description.Stride;
                    }

                    _indexBufferView = new D3D12_INDEX_BUFFER_VIEW
                    {
                        BufferLocation = resource->GetGPUVirtualAddress(),
                        SizeInBytes = (uint)ResourceUtility.GetBufferSize(data.Buffer),
                        Format = stride switch
                        {
                            2 => DXGI_FORMAT_R16_UINT,
                            4 => DXGI_FORMAT_R32_UINT,
                            _ => throw new NotImplementedException(),
                        }
                    };

                    commit |= CommitFlags.IndexBuffer;
                }
                else
                {
                    _indexBufferView = null;
                    commit |= CommitFlags.IndexBuffer;
                }
            }

            if (_pipeline.Dirty)
            {
                RHI.GraphicsPipeline pipeline = _pipeline.GetAndClean()!;
                RHI.Direct3D12.GraphicsPipelineInternal @internal = pipeline;

                ID3D12PipelineState* pipelineState = (ID3D12PipelineState*)(@internal.GetPipelineForState(_rasterPipelineState)?.NativePointer ?? nint.Zero);
                if (pipelineState == null)
                    return false;

                cmdList->SetGraphicsRootSignature((ID3D12RootSignature*)@internal.RootSignature.NativePointer);
                cmdList->SetPipelineState(pipelineState);
                cmdList->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
            }

            if (_hasHadPropertyChange)
            {
                int totalDataSize = _activeResourceCount * sizeof(uint) + _activeDataSize;

                if (_propertyDataBufferSize < totalDataSize)
                {
                    if (_propertyDataBuffer != nint.Zero)
                        NativeMemory.Free(_propertyDataBuffer.ToPointer());

                    uint newSize = BitOperations.RoundUpToPowerOf2((uint)totalDataSize);

                    _propertyDataBuffer = (nint)NativeMemory.Alloc(newSize);
                    _propertyDataBufferSize = (int)newSize;
                }

                nint dataPtr = _propertyDataBuffer;

                if (_activeDataSize > 0)
                {
                    NativeMemory.Copy(_propertyRawData.ToPointer(), dataPtr.ToPointer(), (nuint)_activeDataSize);
                    dataPtr += _activeDataSize;
                }

                if (_activeResourceCount > 0)
                {
                //pretty bad brute force system here
                //should prob do some kind of optimization but im yet to deduce a good one hence why it looks like this for now

                RetryDescriptorAlloc:
                    for (int i = 0; i < _activeResourceCount; i++)
                    {
                        ref readonly PropertyResourceData rd = ref _resourceData[i];

                        if (!(rd.FGResource.HasValue || rd.RHISampler != null))
                        {
                            EngLog.NRD.Error("[{idx}]: Null resource or sampler in resource data!", i);
                            return false;
                        }

                        if (rd.FGResource.HasValue)
                        {
                            FrameGraphResource resource = rd.FGResource.Value;
                            Debug.Assert(resource.IsExternal || resource.IsValidAndRenderGraph);

                            uint index = _device.GPUDescriptorHeap.GetDescriptorIndex(resource, out bool createdNewHeap);
                            if (createdNewHeap)
                            {
                                SetHeapBundle bundle = new SetHeapBundle(_device.GPUDescriptorHeap.CurrentActiveHeap, _device.SamplerDescriptorHeap.CurrentActiveHeap);
                                cmdList->SetDescriptorHeaps(1, (ID3D12DescriptorHeap**)&bundle);

                                goto RetryDescriptorAlloc;
                            }

                            Unsafe.WriteUnaligned(dataPtr.ToPointer(), index);
                            dataPtr += sizeof(uint);

                            if (resource.ResourceId == FGResourceId.Buffer)
                            {
                                BarrierManager.GetShaderBufferBarriers(resource.AsBuffer(), rd.Stages, out D3D12_BARRIER_SYNC sync, out D3D12_BARRIER_ACCESS access);
                                _device.BarrierManager.AddBufferBarrier(resource.AsBuffer(), sync, access);
                            }
                            else
                            {
                                BarrierManager.GetShaderTextureBarriers(resource.AsTexture(), rd.Stages, out D3D12_BARRIER_SYNC sync, out D3D12_BARRIER_ACCESS access, out D3D12_BARRIER_LAYOUT layout);
                                _device.BarrierManager.AddTextureBarrier(resource.AsTexture(), sync, access, layout);
                            }
                        }
                        else
                        {
                            RHI.Sampler sampler = rd.RHISampler!;

                            uint index = _device.SamplerDescriptorHeap.GetDescriptorIndex(sampler.Description, out bool createdNewHeap);
                            if (createdNewHeap)
                            {
                                SetHeapBundle bundle = new SetHeapBundle(_device.GPUDescriptorHeap.CurrentActiveHeap, _device.SamplerDescriptorHeap.CurrentActiveHeap);
                                cmdList->SetDescriptorHeaps(1, (ID3D12DescriptorHeap**)&bundle);

                                goto RetryDescriptorAlloc;
                            }

                            Unsafe.WriteUnaligned(dataPtr.ToPointer(), index);
                            dataPtr += sizeof(uint);

                        }
                    }
                }

                _device.BarrierManager.FlushBarriers(cmdList, BarrierFlushTypes.Buffer | BarrierFlushTypes.Texture);

                if (_useBufferForHeader)
                    throw new NotImplementedException();
                else
                {
#if DEBUG
                    for (int i = 0; i < _activeResourceCount; i++)
                    {
                        ref readonly PropertyResourceData rd = ref _resourceData[i];
                        if (rd.FGResource.HasValue)
                        {
                            FrameGraphResource resource = rd.FGResource.Value;

                            if (resource.ResourceId == FGResourceId.Buffer)
                            {
                                BarrierManager.GetShaderBufferBarriers(resource.AsBuffer(), rd.Stages, out D3D12_BARRIER_SYNC sync, out D3D12_BARRIER_ACCESS access);
                                _device.BarrierManager.DbgEnsureState(resource.AsBuffer(), cmdList, sync, access);
                            }
                            else
                            {
                                BarrierManager.GetShaderTextureBarriers(resource.AsTexture(), rd.Stages, out D3D12_BARRIER_SYNC sync, out D3D12_BARRIER_ACCESS access, out D3D12_BARRIER_LAYOUT layout);
                                _device.BarrierManager.DbgEnsureState(resource.AsTexture(), cmdList, sync, access, layout);
                            }
                        }
                    }
#endif

                    cmdList->SetGraphicsRoot32BitConstants(0, (uint)totalDataSize / sizeof(uint), _propertyDataBuffer.ToPointer(), 0);
                }
            }

            _device.BarrierManager.FlushBarriers(cmdList, BarrierFlushTypes.Buffer | BarrierFlushTypes.Texture);
            _device.ResourceManager.FlushPendingInits(cmdList);

            if (FlagUtility.HasFlag(commit, CommitFlags.RenderTargets))
            {
#if DEBUG
                for (int i = 0; i < 8; i++)
                {
                    if (_rtvDescriptors[i].ptr > 0 && _rtvDescriptors[i] != _device.RTVDescriptorHeap.NullDescriptor)
                    {
                        FrameGraphTexture texture = _renderTargets[i];
                        if (texture.IsValidAndRenderGraph || texture.IsExternal)
                        {
                            _device.BarrierManager.DbgEnsureState(texture, cmdList,
                                D3D12_BARRIER_SYNC_RENDER_TARGET,
                                D3D12_BARRIER_ACCESS_RENDER_TARGET,
                                D3D12_BARRIER_LAYOUT_RENDER_TARGET);
                        }
                    }
                }
#endif

                fixed (D3D12_CPU_DESCRIPTOR_HANDLE* ptr = _rtvDescriptors)
                {
                    fixed (D3D12_CPU_DESCRIPTOR_HANDLE* ptr2 = &_dsvDescriptor)
                    {
                        cmdList->OMSetRenderTargets((uint)_rtvDescriptorLength, _rtvDescriptorLength == 0 ? null : ptr, false, ptr2);
                    }
                }
            }

            if (FlagUtility.HasFlag(commit, CommitFlags.Viewports))
            {
#if DEBUG
                if (_depthStencil.Value.IsExternal || _depthStencil.Value.IsValidAndRenderGraph)
                {
                    _device.BarrierManager.DbgEnsureState(_depthStencil.Value, cmdList,
                            D3D12_BARRIER_SYNC_DEPTH_STENCIL,
                            D3D12_BARRIER_ACCESS_DEPTH_STENCIL_WRITE,
                            D3D12_BARRIER_LAYOUT_DEPTH_STENCIL_WRITE);
                }
#endif

                fixed (D3D12_VIEWPORT* ptr = _rawViewports)
                {
                    cmdList->RSSetViewports((uint)_rtvDescriptorLength, _rtvDescriptorLength == 0 ? null : ptr);
                }
            }

            if (FlagUtility.HasFlag(commit, CommitFlags.Scissors))
            {
                fixed (RECT* ptr = _rawRects)
                {
                    cmdList->RSSetScissorRects((uint)_rtvDescriptorLength, _rtvDescriptorLength == 0 ? null : ptr);
                }
            }

            if (FlagUtility.HasFlag(commit, CommitFlags.VertexBuffer))
            {
#if DEBUG
                _device.BarrierManager.DbgEnsureState(_vertexBuffer.Value.Buffer, cmdList,
                           D3D12_BARRIER_SYNC_VERTEX_SHADING,
                           D3D12_BARRIER_ACCESS_VERTEX_BUFFER);
#endif

                D3D12_VERTEX_BUFFER_VIEW view = _vertexBufferView;
                cmdList->IASetVertexBuffers(0, 1, &view);
            }

            if (FlagUtility.HasFlag(commit, CommitFlags.IndexBuffer))
            {
                if (_indexBufferView.HasValue)
                {
#if DEBUG
                    _device.BarrierManager.DbgEnsureState(_indexBuffer.Value.Buffer, cmdList,
                               D3D12_BARRIER_SYNC_INDEX_INPUT,
                               D3D12_BARRIER_ACCESS_INDEX_BUFFER);
#endif

                    D3D12_INDEX_BUFFER_VIEW view = _indexBufferView.Value;
                    cmdList->IASetIndexBuffer(&view);
                }
                else
                {
                    cmdList->IASetIndexBuffer(null);
                }
            }

            return true;
        }

        private enum CommitFlags : byte
        {
            None = 0,

            RenderTargets = 1 << 0,
            DepthStencil = 1 << 1,
            Viewports = 1 << 2,
            Scissors = 1 << 3,
            VertexBuffer = 1 << 4,
            IndexBuffer = 1 << 5,
        }
    }

    internal readonly record struct SetBufferData(FrameGraphBuffer Buffer, int Stride);
    internal readonly record struct PropertyResourceData(FrameGraphResource? FGResource, RHI.Sampler? RHISampler, ShPropertyStages Stages);
    internal readonly record struct SetHeapBundle(Ptr<ID3D12DescriptorHeap> GpuHeap, Ptr<ID3D12DescriptorHeap> SamplerHeap);

    internal struct DirtyValue<T>
    {
        private T _value;
        private bool _dirty;

        public DirtyValue(T value)
        {
            _value = value;
            _dirty = true;
        }

        public T GetAndClean()
        {
            _dirty = false;
            return _value;
        }

        public T Value { get => _value; set { _value = value; _dirty = true; } }
        public bool Dirty => _dirty;
    }

    internal struct DirtyArray<T>
    {
        private T[] _values;
        private int _dirtyMask;

        public DirtyArray(int length, T value)
        {
            Debug.Assert(length < 32);

            _values = new T[length];
            _dirtyMask = 0;

            Array.Fill(_values, value);
        }

        public bool IsDirty(int index) => (_dirtyMask & (1 << index)) > 0;
        public void CleanAll() => _dirtyMask = 0;

        public void Fill(T value)
        {
            Array.Fill(_values, value);

            _dirtyMask = 0;
            for (int i = 0; i < _values.Length; i++)
                _dirtyMask |= 1 << i;
        }

        public T this[int index]
        {
            get => _values[index];
            set { _values[index] = value; _dirtyMask |= 1 << index; }
        }

        public ReadOnlySpan<T> Values => _values.AsSpan();
        public int DirtyMask => _dirtyMask;

        public bool IsAnyDirty => _dirtyMask > 0;
    }
}
