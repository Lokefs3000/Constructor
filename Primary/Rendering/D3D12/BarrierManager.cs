using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering.Assets;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.RHI2;
using Primary.RHI2.Direct3D12;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using System.Collections.Frozen;

using static TerraFX.Interop.DirectX.D3D12_BARRIER_ACCESS;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_LAYOUT;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_SYNC;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_TEXTURE_BARRIER_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_COMMAND_LIST_TYPE;

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

        internal void TransitionToCompatible(ID3D12GraphicsCommandList10* cmdList, D3D12_COMMAND_LIST_TYPE listType, CommandRecorder recorder)
        {
            BarrierFlushTypes flushTypes = 0;

            foreach (FrameGraphResource fg in recorder.UsedResources)
            {
                NRDResource nrd = ResourceUtility.AsNRDResource(fg);
                ID3D12Resource* resource = (ID3D12Resource*)_device.ResourceManager.GetResource(nrd);
                ref readonly NRDResourceState state = ref GetResourceState(resource);

                if (Unsafe.IsNullRef(in state))
                    continue;

                //TODO: Switch to next required state instead of a default one
                switch (nrd.Id)
                {
                    case NRDResourceId.Buffer:
                        {
                            if (!IsStateFlagsCompatible(listType, in state, NRDResourceId.Buffer, out IncompatibleBarrierFlags flags))
                            {
                                GetCompatibleBarrierLayout(in state, flags, out D3D12_BARRIER_SYNC newSync, out D3D12_BARRIER_ACCESS newAccess);
                                AddBufferBarrier(resource, newSync, newAccess);

                                flushTypes |= BarrierFlushTypes.Buffer;
                            }

                            break;
                        }
                    case NRDResourceId.Texture:
                        {
                            if (!IsStateFlagsCompatible(listType, in state, NRDResourceId.Texture, out IncompatibleBarrierFlags flags))
                            {
                                GetCompatibleBarrierLayout(in state, flags, out D3D12_BARRIER_SYNC newSync, out D3D12_BARRIER_ACCESS newAccess, out D3D12_BARRIER_LAYOUT newLayout);
                                AddTextureBarrier(resource, newSync, newAccess, newLayout);

                                flushTypes |= BarrierFlushTypes.Texture;
                            }

                            break;
                        }
                }
            }

            if (flushTypes > 0)
                FlushBarriers(cmdList, flushTypes);
        }

        internal void FlushBarriers(ID3D12GraphicsCommandList10* cmdList, BarrierFlushTypes types)
        {
            if (types == 0)
                return;

            if (_bufferBarriers.Count > 0 && Flags.HasFlag(types, BarrierFlushTypes.Buffer))
            {
                for (int i = 0; i < _bufferBarriers.Count; i++)
                {
                    D3D12_BUFFER_BARRIER barrier = _bufferBarriers[i];
                    ref NRDResourceState state = ref CollectionsMarshal.GetValueRefOrNullRef(_resourceStates, (nint)barrier.pResource);

                    state.PreviousSync = barrier.SyncAfter;
                    state.PreviousAccess = barrier.AccessAfter;

                    if (!state.Native.IsNull)
                    {
                        D3D12RHIBufferNative* native = (D3D12RHIBufferNative*)state.Native.Pointer;
                        native->BarrierSync = barrier.SyncAfter;
                        native->BarrierAccess = barrier.AccessAfter;
                    }
                }

                fixed (D3D12_BUFFER_BARRIER* ptr = _bufferBarriers.AsSpan())
                {
                    D3D12_BARRIER_GROUP group = new D3D12_BARRIER_GROUP(
                        (uint)_bufferBarriers.Count,
                        ptr);

                    cmdList->Barrier(1, &group);
                }
                _bufferBarriers.Clear();
            }

            if (_textureBarriers.Count > 0 && Flags.HasFlag(types, BarrierFlushTypes.Texture))
            {
                for (int i = 0; i < _textureBarriers.Count; i++)
                {
                    D3D12_TEXTURE_BARRIER barrier = _textureBarriers[i];
                    ref NRDResourceState state = ref CollectionsMarshal.GetValueRefOrNullRef(_resourceStates, (nint)barrier.pResource);

                    state.PreviousSync = barrier.SyncAfter;
                    state.PreviousAccess = barrier.AccessAfter;
                    state.PreviousLayout = barrier.LayoutAfter;

                    if (!state.Native.IsNull)
                    {
                        D3D12RHITextureNative* native = (D3D12RHITextureNative*)state.Native.Pointer;
                        native->BarrierSync = barrier.SyncAfter;
                        native->BarrierAccess = barrier.AccessAfter;
                        native->BarrierLayout = barrier.LayoutAfter;
                    }
                }

                fixed (D3D12_TEXTURE_BARRIER* ptr = _textureBarriers.AsSpan())
                {
                    D3D12_BARRIER_GROUP group = new D3D12_BARRIER_GROUP(
                    (uint)_textureBarriers.Count,
                    ptr);

                    cmdList->Barrier(1, &group);
                }
                _textureBarriers.Clear();
            }
        }

        internal void AddBufferBarrier(NRDResource buffer, D3D12_BARRIER_SYNC sync, D3D12_BARRIER_ACCESS access)
        {
            if (buffer.IsNull)
                return;

            Debug.Assert(buffer.Id == NRDResourceId.Buffer);
            ID3D12Resource* resource = (ID3D12Resource*)buffer.GetNativeResource(_device.ResourceManager);

            AddBufferBarrier(resource, sync, access, buffer.GetRHINative());
        }

        internal void AddBufferBarrier(ID3D12Resource* resource, D3D12_BARRIER_SYNC sync, D3D12_BARRIER_ACCESS access, RHIResourceNative* native = null)
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

                if (native != null)
                {
                    D3D12RHIBufferNative* bufferNative = (D3D12RHIBufferNative*)native;
                    _resourceStates[(nint)resource] = new NRDResourceState(FGResourceId.Buffer, native, bufferNative->BarrierSync, bufferNative->BarrierAccess);
                }
                else
                {
                    _resourceStates[(nint)resource] = new NRDResourceState(FGResourceId.Buffer, null, D3D12_BARRIER_SYNC_ALL, D3D12_BARRIER_ACCESS_COMMON);
                }
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
            if (texture.IsNull)
                return;

            Debug.Assert(texture.Id == NRDResourceId.Texture);
            ID3D12Resource* resource = (ID3D12Resource*)texture.GetNativeResource(_device.ResourceManager);

#if DEBUG
            D3D12_RESOURCE_DESC1 desc1 = ((ID3D12Resource2*)resource)->GetDesc1();
            if (sync == D3D12_BARRIER_SYNC_DEPTH_STENCIL)
                Debug.Assert(Flags.HasFlag(desc1.Flags, D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL));
            else if (sync == D3D12_BARRIER_SYNC_RENDER_TARGET)
                Debug.Assert(Flags.HasFlag(desc1.Flags, D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET));
#endif

            AddTextureBarrier(resource, sync, access, layout, range, texture.GetRHINative());
        }

        internal void AddTextureBarrier(ID3D12Resource* resource, D3D12_BARRIER_SYNC sync, D3D12_BARRIER_ACCESS access, D3D12_BARRIER_LAYOUT layout, D3D12_BARRIER_SUBRESOURCE_RANGE? range = null, RHIResourceNative* native = null)
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

                if (native != null)
                {
                    D3D12RHITextureNative* textureNative = (D3D12RHITextureNative*)native;
                    _resourceStates[(nint)resource] = new NRDResourceState(FGResourceId.Texture, native, textureNative->BarrierSync, textureNative->BarrierAccess, textureNative->BarrierLayout);
                }
                else
                {
                    _resourceStates[(nint)resource] = new NRDResourceState(FGResourceId.Texture, null, D3D12_BARRIER_SYNC_ALL, D3D12_BARRIER_ACCESS_COMMON, D3D12_BARRIER_LAYOUT_UNDEFINED);
                }
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

        internal static void GetShaderBufferBarriers(NRDResource buffer, ResourceManager resources, ShPropertyStages stages, ShPropertyFlags flags, out D3D12_BARRIER_SYNC sync, out D3D12_BARRIER_ACCESS access)
        {
            sync = stages switch
            {
                ShPropertyStages.VertexShading => D3D12_BARRIER_SYNC_VERTEX_SHADING,
                ShPropertyStages.PixelShading => D3D12_BARRIER_SYNC_PIXEL_SHADING,
                ShPropertyStages.ComputeShading => D3D12_BARRIER_SYNC_COMPUTE_SHADING,
                ShPropertyStages.AllShading => D3D12_BARRIER_SYNC_ALL_SHADING,
                _ => throw new NotImplementedException(),
            };

            if (buffer.IsExternal)
            {
                D3D12RHIBufferNative* rhi = (D3D12RHIBufferNative*)buffer.Native;
                RHIBufferDescription desc = rhi->Base.Description;

                if (Flags.HasFlag(desc.Usage, RHIResourceUsage.ConstantBuffer))
                    access = D3D12_BARRIER_ACCESS_CONSTANT_BUFFER;
                else if (Flags.HasFlag(desc.Usage, RHIResourceUsage.VertexInput))
                    access = D3D12_BARRIER_ACCESS_VERTEX_BUFFER;
                else if (Flags.HasFlag(desc.Usage, RHIResourceUsage.IndexInput))
                    access = D3D12_BARRIER_ACCESS_INDEX_BUFFER;
                else if (Flags.HasFlag(desc.Usage, RHIResourceUsage.UnorderedAccess) && Flags.HasFlag(flags, ShPropertyFlags.ReadWrite))
                    access = D3D12_BARRIER_ACCESS_UNORDERED_ACCESS;
                else if (Flags.HasEither(desc.Usage, RHIResourceUsage.ShaderResource))
                    access = D3D12_BARRIER_ACCESS_SHADER_RESOURCE;
                else
                    throw new NotImplementedException();
            }
            else
            {
                FrameGraphBuffer fg = resources.FindFGBuffer(buffer);
                ref readonly FrameGraphBufferDesc desc = ref fg.Description;

                if (Flags.HasFlag(desc.Usage, FGBufferUsage.ConstantBuffer))
                    access = D3D12_BARRIER_ACCESS_CONSTANT_BUFFER;
                else if (Flags.HasFlag(desc.Usage, FGBufferUsage.UnorderedAccess) && Flags.HasFlag(flags, ShPropertyFlags.ReadWrite))
                    access = D3D12_BARRIER_ACCESS_UNORDERED_ACCESS;
                else if (Flags.HasEither(desc.Usage, FGBufferUsage.Structured | FGBufferUsage.Raw))
                    access = D3D12_BARRIER_ACCESS_SHADER_RESOURCE;
                else
                    throw new NotImplementedException();
            }
        }

        internal static void GetShaderTextureBarriers(NRDResource texture, ResourceManager resources, ShPropertyStages stages, ShPropertyFlags flags, out D3D12_BARRIER_SYNC sync, out D3D12_BARRIER_ACCESS access, out D3D12_BARRIER_LAYOUT layout)
        {
            sync = stages switch
            {
                ShPropertyStages.VertexShading => D3D12_BARRIER_SYNC_VERTEX_SHADING,
                ShPropertyStages.PixelShading => D3D12_BARRIER_SYNC_PIXEL_SHADING,
                ShPropertyStages.ComputeShading => D3D12_BARRIER_SYNC_COMPUTE_SHADING,
                ShPropertyStages.AllShading => D3D12_BARRIER_SYNC_ALL_SHADING,
                _ => throw new NotImplementedException(),
            };

            if (texture.IsExternal)
            {
                D3D12RHITextureNative* rhi = (D3D12RHITextureNative*)texture.Native;
                RHITextureDescription desc = rhi->Base.Description;

                if (Flags.HasFlag(desc.Usage, RHIResourceUsage.UnorderedAccess) && Flags.HasFlag(flags, ShPropertyFlags.ReadWrite))
                {
                    access = D3D12_BARRIER_ACCESS_UNORDERED_ACCESS;
                    layout = D3D12_BARRIER_LAYOUT_UNORDERED_ACCESS;
                }
                else if (Flags.HasFlag(desc.Usage, RHIResourceUsage.ShaderResource))
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

                if (Flags.HasFlag(desc.Usage, FGTextureUsage.UnorderedAccess) && Flags.HasFlag(flags, ShPropertyFlags.ReadWrite))
                {
                    access = D3D12_BARRIER_ACCESS_UNORDERED_ACCESS;
                    layout = D3D12_BARRIER_LAYOUT_UNORDERED_ACCESS;
                }
                else if (Flags.HasFlag(desc.Usage, FGTextureUsage.ShaderResource))
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
            ShPropertyStages.VertexShading => D3D12_BARRIER_SYNC_VERTEX_SHADING,
            ShPropertyStages.PixelShading => D3D12_BARRIER_SYNC_PIXEL_SHADING,
            ShPropertyStages.ComputeShading => D3D12_BARRIER_SYNC_COMPUTE_SHADING,
            ShPropertyStages.AllShading => D3D12_BARRIER_SYNC_ALL_SHADING,
            _ => throw new NotImplementedException(),
        };

        private static bool IsStateFlagsCompatible(D3D12_COMMAND_LIST_TYPE listType, ref readonly NRDResourceState state, NRDResourceId resType, out IncompatibleBarrierFlags flags)
        {
            if (resType == NRDResourceId.Buffer)
            {
                flags = IncompatibleBarrierFlags.None;

                if (!s_syncCompatibility[listType].Contains(state.PreviousSync))
                    flags |= IncompatibleBarrierFlags.Sync;
                if (!s_accessCompatibility[listType].Contains(state.PreviousAccess))
                    flags |= IncompatibleBarrierFlags.Access;

                return flags == 0;
            }
            else if (resType == NRDResourceId.Texture)
            {
                flags = IncompatibleBarrierFlags.None;

                if (!s_syncCompatibility[listType].Contains(state.PreviousSync))
                    flags |= IncompatibleBarrierFlags.Sync;
                if (!s_accessCompatibility[listType].Contains(state.PreviousAccess))
                    flags |= IncompatibleBarrierFlags.Access;
                if (!s_layoutCompatibility[listType].Contains(state.PreviousLayout))
                    flags |= IncompatibleBarrierFlags.Layout;

                return flags == 0;
            }
            else
                throw new NotSupportedException();
        }

        //TODO: improve functions to use more effecient barriers
        private static void GetCompatibleBarrierLayout(ref readonly NRDResourceState state, IncompatibleBarrierFlags flags, out D3D12_BARRIER_SYNC sync, out D3D12_BARRIER_ACCESS access)
        {
            if (Flags.HasFlag(flags, IncompatibleBarrierFlags.Sync))
                sync = D3D12_BARRIER_SYNC_ALL;
            else
                sync = state.PreviousSync;

            if (Flags.HasFlag(flags, IncompatibleBarrierFlags.Access))
                access = D3D12_BARRIER_ACCESS_COMMON;
            else
                access = state.PreviousAccess;
        }

        private static void GetCompatibleBarrierLayout(ref readonly NRDResourceState state, IncompatibleBarrierFlags flags, out D3D12_BARRIER_SYNC sync, out D3D12_BARRIER_ACCESS access, out D3D12_BARRIER_LAYOUT layout)
        {
            if (Flags.HasFlag(flags, IncompatibleBarrierFlags.Sync))
                sync = D3D12_BARRIER_SYNC_ALL;
            else
                sync = state.PreviousSync;

            if (Flags.HasFlag(flags, IncompatibleBarrierFlags.Access))
                access = D3D12_BARRIER_ACCESS_COMMON;
            else
                access = state.PreviousAccess;

            if (Flags.HasFlag(flags, IncompatibleBarrierFlags.Layout))
                layout = D3D12_BARRIER_LAYOUT_COMMON;
            else
                layout = state.PreviousLayout;
        }

        private static FrozenDictionary<D3D12_COMMAND_LIST_TYPE, FrozenSet<D3D12_BARRIER_SYNC>> s_syncCompatibility = new Dictionary<D3D12_COMMAND_LIST_TYPE, FrozenSet<D3D12_BARRIER_SYNC>>
        {
            { D3D12_COMMAND_LIST_TYPE_DIRECT, [
                D3D12_BARRIER_SYNC_ALL,
                D3D12_BARRIER_SYNC_DRAW,
                D3D12_BARRIER_SYNC_INDEX_INPUT,
                D3D12_BARRIER_SYNC_VERTEX_SHADING,
                D3D12_BARRIER_SYNC_PIXEL_SHADING,
                D3D12_BARRIER_SYNC_DEPTH_STENCIL,
                D3D12_BARRIER_SYNC_RENDER_TARGET,
                D3D12_BARRIER_SYNC_COMPUTE_SHADING,
                D3D12_BARRIER_SYNC_RAYTRACING,
                D3D12_BARRIER_SYNC_COPY,
                D3D12_BARRIER_SYNC_RESOLVE,
                D3D12_BARRIER_SYNC_EXECUTE_INDIRECT,
                D3D12_BARRIER_SYNC_PREDICATION,
                D3D12_BARRIER_SYNC_ALL_SHADING,
                D3D12_BARRIER_SYNC_NON_PIXEL_SHADING,
                D3D12_BARRIER_SYNC_BUILD_RAYTRACING_ACCELERATION_STRUCTURE,
                D3D12_BARRIER_SYNC_COPY_RAYTRACING_ACCELERATION_STRUCTURE,
                D3D12_BARRIER_SYNC_EMIT_RAYTRACING_ACCELERATION_STRUCTURE_POSTBUILD_INFO,
                D3D12_BARRIER_SYNC_CLEAR_UNORDERED_ACCESS_VIEW,
                D3D12_BARRIER_SYNC_SPLIT
                ] },
            { D3D12_COMMAND_LIST_TYPE_COMPUTE, [
                D3D12_BARRIER_SYNC_ALL,
                D3D12_BARRIER_SYNC_COMPUTE_SHADING,
                D3D12_BARRIER_SYNC_RAYTRACING,
                D3D12_BARRIER_SYNC_COPY,
                D3D12_BARRIER_SYNC_EXECUTE_INDIRECT,
                D3D12_BARRIER_SYNC_ALL_SHADING,
                D3D12_BARRIER_SYNC_NON_PIXEL_SHADING,
                D3D12_BARRIER_SYNC_BUILD_RAYTRACING_ACCELERATION_STRUCTURE,
                D3D12_BARRIER_SYNC_COPY_RAYTRACING_ACCELERATION_STRUCTURE,
                D3D12_BARRIER_SYNC_EMIT_RAYTRACING_ACCELERATION_STRUCTURE_POSTBUILD_INFO,
                D3D12_BARRIER_SYNC_CLEAR_UNORDERED_ACCESS_VIEW,
                D3D12_BARRIER_SYNC_SPLIT
                ] }
        }.ToFrozenDictionary();
        private static FrozenDictionary<D3D12_COMMAND_LIST_TYPE, FrozenSet<D3D12_BARRIER_ACCESS>> s_accessCompatibility = new Dictionary<D3D12_COMMAND_LIST_TYPE, FrozenSet<D3D12_BARRIER_ACCESS>>
        {
            { D3D12_COMMAND_LIST_TYPE_DIRECT, [
                D3D12_BARRIER_ACCESS_VERTEX_BUFFER,
                D3D12_BARRIER_ACCESS_CONSTANT_BUFFER,
                D3D12_BARRIER_ACCESS_INDEX_BUFFER,
                D3D12_BARRIER_ACCESS_RENDER_TARGET,
                D3D12_BARRIER_ACCESS_UNORDERED_ACCESS,
                D3D12_BARRIER_ACCESS_DEPTH_STENCIL_WRITE,
                D3D12_BARRIER_ACCESS_DEPTH_STENCIL_READ,
                D3D12_BARRIER_ACCESS_SHADER_RESOURCE,
                D3D12_BARRIER_ACCESS_STREAM_OUTPUT,
                D3D12_BARRIER_ACCESS_INDIRECT_ARGUMENT,
                D3D12_BARRIER_ACCESS_COPY_DEST,
                D3D12_BARRIER_ACCESS_COPY_SOURCE,
                D3D12_BARRIER_ACCESS_RESOLVE_DEST,
                D3D12_BARRIER_ACCESS_RESOLVE_SOURCE,
                D3D12_BARRIER_ACCESS_RAYTRACING_ACCELERATION_STRUCTURE_READ,
                D3D12_BARRIER_ACCESS_RAYTRACING_ACCELERATION_STRUCTURE_WRITE,
                D3D12_BARRIER_ACCESS_SHADING_RATE_SOURCE,
                D3D12_BARRIER_ACCESS_PREDICATION
                ] },
            { D3D12_COMMAND_LIST_TYPE_COMPUTE, [
                D3D12_BARRIER_ACCESS_VERTEX_BUFFER,
                D3D12_BARRIER_ACCESS_CONSTANT_BUFFER,
                D3D12_BARRIER_ACCESS_UNORDERED_ACCESS,
                D3D12_BARRIER_ACCESS_SHADER_RESOURCE,
                D3D12_BARRIER_ACCESS_INDIRECT_ARGUMENT,
                D3D12_BARRIER_ACCESS_COPY_DEST,
                D3D12_BARRIER_ACCESS_COPY_SOURCE,
                D3D12_BARRIER_ACCESS_RAYTRACING_ACCELERATION_STRUCTURE_READ,
                D3D12_BARRIER_ACCESS_RAYTRACING_ACCELERATION_STRUCTURE_WRITE,
                D3D12_BARRIER_ACCESS_PREDICATION
                ] }
        }.ToFrozenDictionary();
        private static FrozenDictionary<D3D12_COMMAND_LIST_TYPE, FrozenSet<D3D12_BARRIER_LAYOUT>> s_layoutCompatibility = new Dictionary<D3D12_COMMAND_LIST_TYPE, FrozenSet<D3D12_BARRIER_LAYOUT>>
        {
            { D3D12_COMMAND_LIST_TYPE_DIRECT, [
                D3D12_BARRIER_LAYOUT_COMMON,
                D3D12_BARRIER_LAYOUT_GENERIC_READ,
                D3D12_BARRIER_LAYOUT_RENDER_TARGET,
                D3D12_BARRIER_LAYOUT_UNORDERED_ACCESS,
                D3D12_BARRIER_LAYOUT_DEPTH_STENCIL_WRITE,
                D3D12_BARRIER_LAYOUT_DEPTH_STENCIL_READ,
                D3D12_BARRIER_LAYOUT_SHADER_RESOURCE,
                D3D12_BARRIER_LAYOUT_COPY_SOURCE,
                D3D12_BARRIER_LAYOUT_COPY_DEST,
                D3D12_BARRIER_LAYOUT_RESOLVE_SOURCE,
                D3D12_BARRIER_LAYOUT_RESOLVE_DEST,
                D3D12_BARRIER_LAYOUT_SHADING_RATE_SOURCE,
                D3D12_BARRIER_LAYOUT_DIRECT_QUEUE_GENERIC_READ,
                D3D12_BARRIER_LAYOUT_DIRECT_QUEUE_UNORDERED_ACCESS,
                D3D12_BARRIER_LAYOUT_DIRECT_QUEUE_SHADER_RESOURCE,
                D3D12_BARRIER_LAYOUT_DIRECT_QUEUE_COPY_SOURCE,
                D3D12_BARRIER_LAYOUT_DIRECT_QUEUE_COPY_DEST,
                D3D12_BARRIER_LAYOUT_DIRECT_QUEUE_GENERIC_READ_COMPUTE_QUEUE_ACCESSIBLE
                ] },
            { D3D12_COMMAND_LIST_TYPE_COMPUTE, [
                D3D12_BARRIER_LAYOUT_COMMON,
                D3D12_BARRIER_LAYOUT_GENERIC_READ,
                D3D12_BARRIER_LAYOUT_UNORDERED_ACCESS,
                D3D12_BARRIER_LAYOUT_SHADER_RESOURCE,
                D3D12_BARRIER_LAYOUT_COPY_SOURCE,
                D3D12_BARRIER_LAYOUT_COPY_DEST,
                D3D12_BARRIER_LAYOUT_DIRECT_QUEUE_GENERIC_READ_COMPUTE_QUEUE_ACCESSIBLE
                ] }
        }.ToFrozenDictionary();
    
        private enum IncompatibleBarrierFlags : byte
        {
            None = 0,

            Sync = 1 << 0,
            Access = 1 << 1,
            Layout = 1 << 2
        }
    }

    internal enum BarrierFlushTypes : byte
    {
        Global = 1 << 0,
        Texture = 1 << 1,
        Buffer = 1 << 2,
    }

    internal struct NRDResourceState(FGResourceId Id, Ptr<RHIResourceNative> Native, D3D12_BARRIER_SYNC StartSync, D3D12_BARRIER_ACCESS StartAccess, D3D12_BARRIER_LAYOUT StartLayout = D3D12_BARRIER_LAYOUT_COMMON)
    {
        public readonly FGResourceId Id = Id;
        public readonly Ptr<RHIResourceNative> Native = Native;

        public D3D12_BARRIER_SYNC PreviousSync = StartSync;
        public D3D12_BARRIER_ACCESS PreviousAccess = StartAccess;
        public D3D12_BARRIER_LAYOUT PreviousLayout = StartLayout;
    }
}
