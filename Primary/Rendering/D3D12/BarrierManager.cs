using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering.Assets;
using Primary.Rendering.Resources;
using Primary.RHI2;
using Primary.RHI2.Direct3D12;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_ACCESS;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_LAYOUT;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_SYNC;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_TEXTURE_BARRIER_FLAGS;

namespace Primary.Rendering.D3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe sealed class BarrierManager
    {
        private readonly NRDDevice _device;

        private List<D3D12_BUFFER_BARRIER> _bufferBarriers;
        private List<D3D12_TEXTURE_BARRIER> _textureBarriers;

        private Dictionary<nint, NRDResourceState> _resourceStates;

        internal BarrierManager(NRDDevice device)
        {
            _device = device;

            _bufferBarriers = new List<D3D12_BUFFER_BARRIER>();
            _textureBarriers = new List<D3D12_TEXTURE_BARRIER>();

            _resourceStates = new Dictionary<nint, NRDResourceState>();
        }

        internal void ClearInternal()
        {
            _resourceStates.Clear();

            ClearPreviousBarriers();
        }

        internal void ClearPreviousBarriers()
        {
            _bufferBarriers.Clear();
            _textureBarriers.Clear();
        }

        internal void TransitionAllToStandard()
        {
            foreach (var kvp in _resourceStates)
            {
                if (kvp.Value.Id == FGResourceId.Buffer)
                {
                    AddBufferBarrier((ID3D12Resource*)kvp.Key, D3D12_BARRIER_SYNC_ALL, D3D12_BARRIER_ACCESS_COMMON);
                }
                else
                {
                    AddTextureBarrier((ID3D12Resource*)kvp.Key, D3D12_BARRIER_SYNC_ALL, D3D12_BARRIER_ACCESS_COMMON, D3D12_BARRIER_LAYOUT_COMMON);
                }
            }
        }

        internal void FlushBarriers(ID3D12GraphicsCommandList10* cmdList, BarrierFlushTypes types)
        {
            if (_bufferBarriers.Count > 0 && FlagUtility.HasFlag(types, BarrierFlushTypes.Buffer))
            {
                for (int i = 0; i < _bufferBarriers.Count; i++)
                {
                    D3D12_BUFFER_BARRIER barrier = _bufferBarriers[i];
                    ref NRDResourceState state = ref CollectionsMarshal.GetValueRefOrNullRef(_resourceStates, (nint)barrier.pResource);

                    state.PreviousSync = barrier.SyncAfter;
                    state.PreviousAccess = barrier.AccessAfter;
                }

                D3D12_BARRIER_GROUP group = new D3D12_BARRIER_GROUP(
                    (uint)_bufferBarriers.Count,
                    (D3D12_BUFFER_BARRIER*)Unsafe.AsPointer(ref _bufferBarriers.AsSpan().DangerousGetReference()));

                cmdList->Barrier(1, &group);
                _bufferBarriers.Clear();
            }

            if (_textureBarriers.Count > 0 && FlagUtility.HasFlag(types, BarrierFlushTypes.Texture))
            {
                for (int i = 0; i < _textureBarriers.Count; i++)
                {
                    D3D12_TEXTURE_BARRIER barrier = _textureBarriers[i];
                    ref NRDResourceState state = ref CollectionsMarshal.GetValueRefOrNullRef(_resourceStates, (nint)barrier.pResource);

                    state.PreviousSync = barrier.SyncAfter;
                    state.PreviousAccess = barrier.AccessAfter;
                    state.PreviousLayout = barrier.LayoutAfter;
                }

                D3D12_BARRIER_GROUP group = new D3D12_BARRIER_GROUP(
                    (uint)_textureBarriers.Count,
                    (D3D12_TEXTURE_BARRIER*)Unsafe.AsPointer(ref _textureBarriers.AsSpan().DangerousGetReference()));

                cmdList->Barrier(1, &group);
                _textureBarriers.Clear();
            }
        }

        internal void AddBufferBarrier(NRDResource buffer, D3D12_BARRIER_SYNC sync, D3D12_BARRIER_ACCESS access)
        {
            Debug.Assert(buffer.Id == NRDResourceId.Buffer);
            ID3D12Resource* resource = (ID3D12Resource*)buffer.GetNativeResource(_device.ResourceManager);

            AddBufferBarrier(resource, sync, access);
        }

        internal void AddBufferBarrier(ID3D12Resource* resource, D3D12_BARRIER_SYNC sync, D3D12_BARRIER_ACCESS access)
        {
            ref NRDResourceState state = ref CollectionsMarshal.GetValueRefOrNullRef(_resourceStates, (nint)resource);

            if (Unsafe.IsNullRef(ref state))
            {
                _bufferBarriers.Add(new D3D12_BUFFER_BARRIER
                {
                    SyncBefore = D3D12_BARRIER_SYNC_ALL,
                    SyncAfter = sync,

                    AccessBefore = D3D12_BARRIER_ACCESS_COMMON,
                    AccessAfter = access,

                    pResource = (ID3D12Resource*)resource,
                    Offset = 0,
                    Size = ulong.MaxValue
                });

                _resourceStates[(nint)resource] = new NRDResourceState(FGResourceId.Buffer, D3D12_BARRIER_SYNC_ALL, D3D12_BARRIER_ACCESS_COMMON);
            }
            else
            {
                if (state.PreviousSync == sync && state.PreviousAccess == access)
                    return;

                _bufferBarriers.Add(new D3D12_BUFFER_BARRIER
                {
                    SyncBefore = state.PreviousSync,
                    SyncAfter = sync,

                    AccessBefore = state.PreviousAccess,
                    AccessAfter = access,

                    pResource = (ID3D12Resource*)resource,
                    Offset = 0,
                    Size = ulong.MaxValue
                });

                //state.PreviousSync = sync;
                //state.PreviousAccess = access;
            }
        }

        internal void AddTextureBarrier(NRDResource texture, D3D12_BARRIER_SYNC sync, D3D12_BARRIER_ACCESS access, D3D12_BARRIER_LAYOUT layout, D3D12_BARRIER_SUBRESOURCE_RANGE? range = null)
        {
            Debug.Assert(texture.Id == NRDResourceId.Texture);
            ID3D12Resource* resource = (ID3D12Resource*)texture.GetNativeResource(_device.ResourceManager);

#if DEBUG
            D3D12_RESOURCE_DESC1 desc1 = ((ID3D12Resource2*)resource)->GetDesc1();
            if (sync == D3D12_BARRIER_SYNC_DEPTH_STENCIL)
                Debug.Assert(FlagUtility.HasFlag(desc1.Flags, D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL));
            else if (sync == D3D12_BARRIER_SYNC_RENDER_TARGET)
                Debug.Assert(FlagUtility.HasFlag(desc1.Flags, D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET));
#endif

            AddTextureBarrier(resource, sync, access, layout, range);
        }

        internal void AddTextureBarrier(ID3D12Resource* resource, D3D12_BARRIER_SYNC sync, D3D12_BARRIER_ACCESS access, D3D12_BARRIER_LAYOUT layout, D3D12_BARRIER_SUBRESOURCE_RANGE? range = null)
        {
            ref NRDResourceState state = ref CollectionsMarshal.GetValueRefOrNullRef(_resourceStates, (nint)resource);

            if (Unsafe.IsNullRef(ref state))
            {
                _textureBarriers.Add(new D3D12_TEXTURE_BARRIER
                {
                    SyncBefore = D3D12_BARRIER_SYNC_ALL,
                    SyncAfter = sync,

                    AccessBefore = D3D12_BARRIER_ACCESS_NO_ACCESS,
                    AccessAfter = access,

                    LayoutBefore = D3D12_BARRIER_LAYOUT_UNDEFINED,
                    LayoutAfter = layout,

                    pResource = (ID3D12Resource*)resource,
                    Subresources = range.GetValueOrDefault(new D3D12_BARRIER_SUBRESOURCE_RANGE(0xffffffff)),
                    Flags = D3D12_TEXTURE_BARRIER_FLAG_NONE,
                });

                _resourceStates[(nint)resource] = new NRDResourceState(FGResourceId.Texture, D3D12_BARRIER_SYNC_ALL, D3D12_BARRIER_ACCESS_NO_ACCESS, D3D12_BARRIER_LAYOUT_UNDEFINED);
            }
            else
            {
                if (state.PreviousSync == sync &&
                    state.PreviousAccess == access &&
                    state.PreviousLayout == layout)
                    return;

                _textureBarriers.Add(new D3D12_TEXTURE_BARRIER
                {
                    SyncBefore = state.PreviousSync,
                    SyncAfter = sync,

                    AccessBefore = state.PreviousAccess,
                    AccessAfter = access,

                    LayoutBefore = state.PreviousLayout,
                    LayoutAfter = layout,

                    pResource = (ID3D12Resource*)resource,
                    Subresources = range.GetValueOrDefault(new D3D12_BARRIER_SUBRESOURCE_RANGE(0xffffffff)),
                    Flags = D3D12_TEXTURE_BARRIER_FLAG_NONE,
                });

                //state.PreviousSync = sync;
                //state.PreviousAccess = access;
                //state.PreviousLayout = layout;
            }
        }

        internal void SetResourceState(ID3D12Resource* resource, NRDResourceState state) => _resourceStates[(nint)resource] = state;
        internal ref readonly NRDResourceState GetResourceState(ID3D12Resource* resource) => ref CollectionsMarshal.GetValueRefOrNullRef(_resourceStates, (nint)resource);

        [Conditional("DEBUG")]
        internal void DbgEnsureState(NRDResource buffer, ID3D12GraphicsCommandList10* cmdList, D3D12_BARRIER_SYNC sync, D3D12_BARRIER_ACCESS access)
        {
            ID3D12Resource* resource = (ID3D12Resource*)buffer.GetNativeResource(_device.ResourceManager);
            if (resource == null)
                return;

            ref NRDResourceState state = ref CollectionsMarshal.GetValueRefOrNullRef(_resourceStates, (nint)resource);

            Debug.Assert(state.PreviousSync == sync && state.PreviousAccess == access);

            //if (cmdList != null)
            //{
            //    ID3D12DebugCommandList3* debugCmdList = null;
            //    HRESULT hr = cmdList->QueryInterface(UuidOf.Get<ID3D12DebugCommandList3>(), (void**)&debugCmdList);
            //    if (hr.SUCCEEDED)
            //    {
            //        debugCmdList->AssertResourceAccess(resource, 0xffffffff, access);
            //    }
            //}
        }

        [Conditional("DEBUG")]
        internal void DbgEnsureState(NRDResource texture, ID3D12GraphicsCommandList10* cmdList, D3D12_BARRIER_SYNC sync, D3D12_BARRIER_ACCESS access, D3D12_BARRIER_LAYOUT layout)
        {
            ID3D12Resource* resource = (ID3D12Resource*)texture.GetNativeResource(_device.ResourceManager);
            if (resource == null)
                return;

            ref NRDResourceState state = ref CollectionsMarshal.GetValueRefOrNullRef(_resourceStates, (nint)resource);

            Debug.Assert(state.PreviousSync == sync && state.PreviousAccess == access && state.PreviousLayout == layout);

            //if (cmdList != null)
            //{
            //    ID3D12DebugCommandList3* debugCmdList = null;
            //    HRESULT hr = cmdList->QueryInterface(UuidOf.Get<ID3D12DebugCommandList3>(), (void**)&debugCmdList);
            //    if (hr.SUCCEEDED)
            //    {
            //        debugCmdList->AssertResourceAccess(resource, 0xffffffff, access);
            //        debugCmdList->AssertTextureLayout(resource, 0xffffffff, layout);
            //    }
            //}
        }

        private record struct BarrierGroupBundle(D3D12_BARRIER_GROUP G0, D3D12_BARRIER_GROUP G1);

        internal static void GetShaderBufferBarriers(NRDResource buffer, ResourceManager resources, ShPropertyStages stages, out D3D12_BARRIER_SYNC sync, out D3D12_BARRIER_ACCESS access)
        {
            sync = stages switch
            {
                ShPropertyStages.VertexShader => D3D12_BARRIER_SYNC_VERTEX_SHADING,
                ShPropertyStages.PixelShader => D3D12_BARRIER_SYNC_PIXEL_SHADING,
                ShPropertyStages.ComputeShader => D3D12_BARRIER_SYNC_COMPUTE_SHADING,
                ShPropertyStages.GenericShading => D3D12_BARRIER_SYNC_NON_PIXEL_SHADING,
                ShPropertyStages.AllShading => D3D12_BARRIER_SYNC_ALL_SHADING,
                _ => throw new NotImplementedException(),
            };

            if (buffer.IsExternal)
            {
                D3D12RHIBufferNative* rhi = (D3D12RHIBufferNative*)buffer.Native;
                RHIBufferDescription desc = rhi->Base.Description;

                if (FlagUtility.HasFlag(desc.Usage, RHIResourceUsage.ConstantBuffer))
                    access = D3D12_BARRIER_ACCESS_CONSTANT_BUFFER;
                else if (FlagUtility.HasFlag(desc.Usage, RHIResourceUsage.VertexInput))
                    access = D3D12_BARRIER_ACCESS_VERTEX_BUFFER;
                else if (FlagUtility.HasFlag(desc.Usage, RHIResourceUsage.IndexInput))
                    access = D3D12_BARRIER_ACCESS_INDEX_BUFFER;
                else if (FlagUtility.HasEither(desc.Usage, RHIResourceUsage.ShaderResource))
                    access = D3D12_BARRIER_ACCESS_SHADER_RESOURCE;
                else
                    throw new NotImplementedException();
            }
            else
            {
                FrameGraphBuffer fg = resources.FindFGBuffer(buffer);
                ref readonly FrameGraphBufferDesc desc = ref fg.Description;

                if (FlagUtility.HasFlag(desc.Usage, FGBufferUsage.ConstantBuffer))
                    access = D3D12_BARRIER_ACCESS_CONSTANT_BUFFER;
                else if (FlagUtility.HasEither(desc.Usage, FGBufferUsage.Structured | FGBufferUsage.Raw))
                    access = D3D12_BARRIER_ACCESS_SHADER_RESOURCE;
                else
                    throw new NotImplementedException();
            }
        }

        internal static void GetShaderTextureBarriers(NRDResource texture, ResourceManager resources, ShPropertyStages stages, out D3D12_BARRIER_SYNC sync, out D3D12_BARRIER_ACCESS access, out D3D12_BARRIER_LAYOUT layout)
        {
            sync = stages switch
            {
                ShPropertyStages.VertexShader => D3D12_BARRIER_SYNC_VERTEX_SHADING,
                ShPropertyStages.PixelShader => D3D12_BARRIER_SYNC_PIXEL_SHADING,
                ShPropertyStages.ComputeShader => D3D12_BARRIER_SYNC_COMPUTE_SHADING,
                ShPropertyStages.GenericShading => D3D12_BARRIER_SYNC_NON_PIXEL_SHADING,
                ShPropertyStages.AllShading => D3D12_BARRIER_SYNC_ALL_SHADING,
                _ => throw new NotImplementedException(),
            };

            if (texture.IsExternal)
            {
                D3D12RHITextureNative* rhi = (D3D12RHITextureNative*)texture.Native;
                RHITextureDescription desc = rhi->Base.Description;

                if (FlagUtility.HasFlag(desc.Usage, RHIResourceUsage.ShaderResource))
                {
                    access = D3D12_BARRIER_ACCESS_SHADER_RESOURCE;
                    layout = D3D12_BARRIER_LAYOUT_SHADER_RESOURCE;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                FrameGraphTexture fg = resources.FindFGTexture(texture);
                ref readonly FrameGraphTextureDesc desc = ref fg.Description;

                if (FlagUtility.HasFlag(desc.Usage, FGTextureUsage.ShaderResource))
                {
                    access = D3D12_BARRIER_ACCESS_SHADER_RESOURCE;
                    layout = D3D12_BARRIER_LAYOUT_SHADER_RESOURCE;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        internal static D3D12_BARRIER_SYNC GetSync(ShPropertyStages stages) => stages switch
        {
            ShPropertyStages.VertexShader => D3D12_BARRIER_SYNC_VERTEX_SHADING,
            ShPropertyStages.PixelShader => D3D12_BARRIER_SYNC_PIXEL_SHADING,
            ShPropertyStages.ComputeShader => D3D12_BARRIER_SYNC_COMPUTE_SHADING,
            ShPropertyStages.GenericShading => D3D12_BARRIER_SYNC_NON_PIXEL_SHADING,
            ShPropertyStages.AllShading => D3D12_BARRIER_SYNC_ALL_SHADING,
            _ => throw new NotImplementedException(),
        };
    }

    internal enum BarrierFlushTypes : byte
    {
        Global = 1 << 0,
        Texture = 1 << 1,
        Buffer = 1 << 2,
    }

    internal struct NRDResourceState(FGResourceId Id, D3D12_BARRIER_SYNC StartSync, D3D12_BARRIER_ACCESS StartAccess, D3D12_BARRIER_LAYOUT StartLayout = D3D12_BARRIER_LAYOUT_COMMON)
    {
        public readonly FGResourceId Id = Id;

        public D3D12_BARRIER_SYNC PreviousSync = StartSync;
        public D3D12_BARRIER_ACCESS PreviousAccess = StartAccess;
        public D3D12_BARRIER_LAYOUT PreviousLayout = StartLayout;
    }
}
