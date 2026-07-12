using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Collections;
using Primary.Common;
using Primary.Rendering.Assets;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.RHI;
using Primary.RHI.Direct3D12;
using Silk.NET.Direct3D12;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Primary.Rendering.D3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe sealed class BarrierManager
    {
        private readonly NRDDevice _device;

        private HashSet<nint> _pendingStates;
        private List<BufferBarrier> _bufferBarriers;
        private List<TextureBarrier> _textureBarriers;

        private Dictionary<nint, NRDResourceState> _resourceStates;

        internal BarrierManager(NRDDevice device)
        {
            _device = device;

            _pendingStates = new HashSet<nint>();
            _bufferBarriers = new List<BufferBarrier>();
            _textureBarriers = new List<TextureBarrier>();

            _resourceStates = new Dictionary<nint, NRDResourceState>();
        }

        internal void ClearInternal()
        {
            _resourceStates.Clear();

            ClearPreviousBarriers();
        }

        internal void ClearPreviousBarriers()
        {
            _pendingStates.Clear();
            _bufferBarriers.Clear();
            _textureBarriers.Clear();
        }

        internal void TransitionAllToStandard()
        {
            foreach (var kvp in _resourceStates)
            {
                if (kvp.Value.Id == FGResourceId.Buffer)
                {
                    AddBufferBarrier((ID3D12Resource*)kvp.Key, BarrierSync.All, BarrierAccess.Common);
                }
                else
                {
                    AddTextureBarrier((ID3D12Resource*)kvp.Key, BarrierSync.All, BarrierAccess.Common, BarrierLayout.Common);
                }
            }
        }

        internal void TransitionToCompatible(ref ID3D12GraphicsCommandList10 cmdList, CommandListType listType, CommandRecorder recorder)
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
                                GetCompatibleBarrierLayout(in state, flags, out BarrierSync newSync, out BarrierAccess newAccess);
                                AddBufferBarrier(resource, newSync, newAccess);

                                flushTypes |= BarrierFlushTypes.Buffer;
                            }

                            break;
                        }
                    case NRDResourceId.Texture:
                        {
                            if (!IsStateFlagsCompatible(listType, in state, NRDResourceId.Texture, out IncompatibleBarrierFlags flags))
                            {
                                GetCompatibleBarrierLayout(in state, flags, out BarrierSync newSync, out BarrierAccess newAccess, out BarrierLayout newLayout);
                                AddTextureBarrier(resource, newSync, newAccess, newLayout);

                                flushTypes |= BarrierFlushTypes.Texture;
                            }

                            break;
                        }
                }
            }

            if (flushTypes > 0)
                FlushBarriers(ref cmdList, flushTypes);
        }

        internal void FlushBarriers(ref ID3D12GraphicsCommandList10 cmdList, BarrierFlushTypes types)
        {
            if (types == 0 || _pendingStates.Count == 0)
                return;

            using RentedList<nint> removalList = new RentedList<nint>();

            bool acceptBuffers = Flags.HasFlag(types, BarrierFlushTypes.Buffer);
            bool acceptTextures = Flags.HasFlag(types, BarrierFlushTypes.Texture);

            foreach (nint resourceId in _pendingStates)
            {
                ref NRDResourceState state = ref CollectionsMarshal.GetValueRefOrNullRef(_resourceStates, resourceId);

                switch (state.Id)
                {
                    case FGResourceId.Buffer:
                        {
                            if (acceptBuffers)
                            {
                                if (state.PreviousSync != state.RequestedSync && state.PreviousAccess != state.RequestedAccess)
                                {
                                    _bufferBarriers.Add(new BufferBarrier
                                    {
                                        SyncBefore = state.PreviousSync,
                                        SyncAfter = state.RequestedSync,

                                        AccessBefore = state.PreviousAccess,
                                        AccessAfter = state.RequestedAccess,

                                        PResource = (ID3D12Resource*)resourceId,
                                        Offset = 0,
                                        Size = ulong.MaxValue
                                    });

                                    state.PreviousSync = state.RequestedSync;
                                    state.PreviousAccess = state.RequestedAccess;

                                    if (!state.Native.IsNull)
                                    {
                                        D3D12RHIBufferNative* native = (D3D12RHIBufferNative*)state.Native.Pointer;
                                        native->BarrierSync = state.RequestedSync;
                                        native->BarrierAccess = state.RequestedAccess;
                                    }
                                }

                                removalList.Add(resourceId);
                            }

                            break;
                        }
                    case FGResourceId.Texture:
                        {
                            if (acceptTextures)
                            {
                                if (state.PreviousSync != state.RequestedSync && state.PreviousAccess != state.RequestedAccess && state.PreviousLayout != state.RequestedLayout)
                                {
                                    _textureBarriers.Add(new TextureBarrier
                                    {
                                        SyncBefore = state.PreviousSync,
                                        SyncAfter = state.RequestedSync,

                                        AccessBefore = state.PreviousAccess,
                                        AccessAfter = state.RequestedAccess,

                                        LayoutBefore = state.PreviousLayout,
                                        LayoutAfter = state.RequestedLayout,

                                        PResource = (ID3D12Resource*)resourceId,
                                        Subresources = new BarrierSubresourceRange(0xffffffff),
                                        Flags = TextureBarrierFlags.None
                                    });

                                    state.PreviousSync = state.RequestedSync;
                                    state.PreviousAccess = state.RequestedAccess;
                                    state.PreviousLayout = state.RequestedLayout;

                                    if (!state.Native.IsNull)
                                    {
                                        D3D12RHITextureNative* native = (D3D12RHITextureNative*)state.Native.Pointer;
                                        native->BarrierSync = state.RequestedSync;
                                        native->BarrierAccess = state.RequestedAccess;
                                        native->BarrierLayout = state.RequestedLayout;
                                    }
                                }

                                removalList.Add(resourceId);
                            }

                            break;
                        }
                }
            }

            if (!removalList.IsEmpty)
            {
                foreach (nint resourceId in removalList)
                    _pendingStates.Remove(resourceId);

                if (_bufferBarriers.Count > 0 || _textureBarriers.Count > 0)
                {
                    BarrierGroup* groups = stackalloc BarrierGroup[2];

                    int count = 0;
                    fixed (BufferBarrier* bufferBarriers = _bufferBarriers.AsSpan())
                    fixed (TextureBarrier* textureBarriers = _textureBarriers.AsSpan())
                    {
                        if (_bufferBarriers.Count > 0)
                            groups[count++] = new BarrierGroup(type: BarrierType.Buffer, numBarriers: (uint)_bufferBarriers.Count, pBufferBarriers: bufferBarriers);
                        if (_textureBarriers.Count > 0)
                            groups[count++] = new BarrierGroup(type: BarrierType.Texture, numBarriers: (uint)_textureBarriers.Count, pTextureBarriers: textureBarriers);

                        Debug.Assert(count > 0);
                        cmdList.Barrier((uint)count, groups);
                    }
                }

                _bufferBarriers.Clear();
                _textureBarriers.Clear();
            }
        }

        internal void AddBufferBarrier(NRDResource buffer, BarrierSync sync, BarrierAccess access)
        {
            if (buffer.IsNull)
                return;

            Debug.Assert(buffer.Id == NRDResourceId.Buffer);
            ID3D12Resource* resource = (ID3D12Resource*)buffer.GetNativeResource(_device.ResourceManager);

            AddBufferBarrier(resource, sync, access, buffer.GetRHINative());
        }

        internal void AddBufferBarrier(ref ID3D12Resource2 resource, BarrierSync sync, BarrierAccess access, RHIResourceNative* native = null)
        {
            AddBufferBarrier((ID3D12Resource*)Unsafe.AsPointer(ref resource), sync, access, native);
        }

        internal void AddBufferBarrier(ID3D12Resource* resource, BarrierSync sync, BarrierAccess access, RHIResourceNative* native = null)
        {
            ref NRDResourceState state = ref CollectionsMarshal.GetValueRefOrNullRef(_resourceStates, (nint)resource);

            if (Unsafe.IsNullRef(ref state))
            {
                if (native != null)
                {
                    D3D12RHIBufferNative* bufferNative = (D3D12RHIBufferNative*)native;
                    if (bufferNative->BarrierSync == sync && bufferNative->BarrierAccess == access)
                        return;

                    _resourceStates[(nint)resource] = new NRDResourceState(FGResourceId.Buffer, native, bufferNative->BarrierSync, bufferNative->BarrierAccess)
                    {
                        RequestedSync = sync,
                        RequestedAccess = access
                    };
                }
                else
                {
                    _resourceStates[(nint)resource] = new NRDResourceState(FGResourceId.Buffer, null, BarrierSync.All, BarrierAccess.NoAccess)
                    {
                        RequestedSync = sync,
                        RequestedAccess = access
                    };
                }

                _pendingStates.Add((nint)resource);
            }
            else
            {
                if (state.PreviousSync == sync && state.PreviousAccess == access)
                {
                    _pendingStates.Remove((nint)resource);
                    return;
                }

                state.RequestedSync = sync;
                state.RequestedAccess = access;

                _pendingStates.Add((nint)resource);
            }
        }

        internal void AddTextureBarrier(NRDResource texture, BarrierSync sync, BarrierAccess access, BarrierLayout layout, BarrierSubresourceRange? range = null)
        {
            if (texture.IsNull)
                return;

            Debug.Assert(texture.Id == NRDResourceId.Texture);
            ID3D12Resource* resource = (ID3D12Resource*)texture.GetNativeResource(_device.ResourceManager);

#if DEBUG
            ResourceDesc1 desc1 = ((ID3D12Resource2*)resource)->GetDesc1();
            if (sync == BarrierSync.DepthStencil)
                Debug.Assert(Flags.HasFlag(desc1.Flags, ResourceFlags.AllowDepthStencil));
            else if (sync == BarrierSync.RenderTarget)
                Debug.Assert(Flags.HasFlag(desc1.Flags, ResourceFlags.AllowRenderTarget));
#endif

            AddTextureBarrier(resource, sync, access, layout, range, texture.GetRHINative());
        }

        internal void AddTextureBarrier(ref ID3D12Resource2 resource, BarrierSync sync, BarrierAccess access, BarrierLayout layout, BarrierSubresourceRange? range = null, RHIResourceNative* native = null)
        {
            AddTextureBarrier((ID3D12Resource*)Unsafe.AsPointer(ref resource), sync, access, layout, range, native);
        }

        internal void AddTextureBarrier(ID3D12Resource* resource, BarrierSync sync, BarrierAccess access, BarrierLayout layout, BarrierSubresourceRange? range = null, RHIResourceNative* native = null)
        {
            ref NRDResourceState state = ref CollectionsMarshal.GetValueRefOrNullRef(_resourceStates, (nint)resource);

            if (Unsafe.IsNullRef(ref state))
            {
                if (native != null)
                {
                    D3D12RHITextureNative* textureNative = (D3D12RHITextureNative*)native;
                    if (textureNative->BarrierSync == sync && textureNative->BarrierAccess == access && textureNative->BarrierLayout == layout)
                        return;

                    _resourceStates[(nint)resource] = new NRDResourceState(FGResourceId.Texture, native, textureNative->BarrierSync, textureNative->BarrierAccess, textureNative->BarrierLayout)
                    {
                        RequestedSync = sync,
                        RequestedAccess = access,
                        RequestedLayout = layout
                    };
                }
                else
                {
                    _resourceStates[(nint)resource] = new NRDResourceState(FGResourceId.Texture, null, BarrierSync.All, BarrierAccess.NoAccess, BarrierLayout.Undefined)
                    {
                        RequestedSync = sync,
                        RequestedAccess = access,
                        RequestedLayout = layout
                    };
                }

                _pendingStates.Add((nint)resource);
            }
            else
            {
                if (state.PreviousSync == sync &&
                    state.PreviousAccess == access &&
                    state.PreviousLayout == layout)
                {
                    _pendingStates.Remove((nint)resource);
                    return;
                }

                state.RequestedSync = sync;
                state.RequestedAccess = access;
                state.RequestedLayout = layout;

                _pendingStates.Add((nint)resource);
            }
        }

        internal void SetResourceState(ref ID3D12Resource2 resource, NRDResourceState state) => _resourceStates[(nint)Unsafe.AsPointer(ref resource)] = state;
        internal void SetResourceState(ID3D12Resource* resource, NRDResourceState state) => _resourceStates[(nint)resource] = state;
        
        internal ref readonly NRDResourceState GetResourceState(ref ID3D12Resource2 resource) => ref CollectionsMarshal.GetValueRefOrNullRef(_resourceStates, (nint)Unsafe.AsPointer(ref resource));
        internal ref readonly NRDResourceState GetResourceState(ID3D12Resource* resource) => ref CollectionsMarshal.GetValueRefOrNullRef(_resourceStates, (nint)resource);
        
        internal bool HasResourceState(ref ID3D12Resource2 resource) => _resourceStates.ContainsKey((nint)Unsafe.AsPointer(ref resource));
        internal bool HasResourceState(ID3D12Resource* resource) => _resourceStates.ContainsKey((nint)resource);

        [Conditional("DEBUG")]
        internal void DbgEnsureState(NRDResource buffer, ref ID3D12GraphicsCommandList10 cmdList, BarrierSync sync, BarrierAccess access)
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
        internal void DbgEnsureState(NRDResource texture, ref ID3D12GraphicsCommandList10 cmdList, BarrierSync sync, BarrierAccess access, BarrierLayout layout)
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

        private record struct BarrierGroupBundle(BarrierGroup G0, BarrierGroup G1);

        internal static void GetShaderBufferBarriers(NRDResource buffer, ResourceManager resources, ShPropertyStages stages, ShPropertyFlags flags, out BarrierSync sync, out BarrierAccess access)
        {
            sync = stages switch
            {
                ShPropertyStages.VertexShading => BarrierSync.VertexShading,
                ShPropertyStages.PixelShading => BarrierSync.PixelShading,
                ShPropertyStages.ComputeShading => BarrierSync.ComputeShading,
                ShPropertyStages.AllShading => BarrierSync.AllShading,
                _ => throw new NotImplementedException(),
            };

            if (buffer.IsExternal)
            {
                D3D12RHIBufferNative* rhi = (D3D12RHIBufferNative*)buffer.Native;
                RHIBufferDescription desc = rhi->Base.Description;

                if (Flags.HasFlag(desc.Usage, RHIResourceUsage.ConstantBuffer))
                    access = BarrierAccess.ConstantBuffer;
                else if (Flags.HasFlag(desc.Usage, RHIResourceUsage.VertexInput))
                    access = BarrierAccess.VertexBuffer;
                else if (Flags.HasFlag(desc.Usage, RHIResourceUsage.IndexInput))
                    access = BarrierAccess.IndexBuffer;
                else if (Flags.HasFlag(desc.Usage, RHIResourceUsage.UnorderedAccess) && Flags.HasFlag(flags, ShPropertyFlags.ReadWrite))
                    access = BarrierAccess.UnorderedAccess;
                else if (Flags.HasEither(desc.Usage, RHIResourceUsage.ShaderResource))
                    access = BarrierAccess.ShaderResource;
                else
                    throw new NotImplementedException();
            }
            else
            {
                FrameGraphBuffer fg = resources.FindFGBuffer(buffer);
                ref readonly FrameGraphBufferDesc desc = ref fg.Description;

                if (Flags.HasFlag(desc.Usage, FGBufferUsage.ConstantBuffer))
                    access = BarrierAccess.ConstantBuffer;
                else if (Flags.HasFlag(desc.Usage, FGBufferUsage.UnorderedAccess) && Flags.HasFlag(flags, ShPropertyFlags.ReadWrite))
                    access = BarrierAccess.UnorderedAccess;
                else if (Flags.HasEither(desc.Usage, FGBufferUsage.Structured | FGBufferUsage.Raw))
                    access = BarrierAccess.ShaderResource;
                else
                    throw new NotImplementedException();
            }
        }

        internal static void GetShaderTextureBarriers(NRDResource texture, ResourceManager resources, ShPropertyStages stages, ShPropertyFlags flags, out BarrierSync sync, out BarrierAccess access, out BarrierLayout layout)
        {
            sync = stages switch
            {
                ShPropertyStages.VertexShading => BarrierSync.VertexShading,
                ShPropertyStages.PixelShading => BarrierSync.PixelShading,
                ShPropertyStages.ComputeShading => BarrierSync.ComputeShading,
                ShPropertyStages.AllShading => BarrierSync.AllShading,
                _ => throw new NotImplementedException(),
            };

            if (texture.IsExternal)
            {
                D3D12RHITextureNative* rhi = (D3D12RHITextureNative*)texture.Native;
                RHITextureDescription desc = rhi->Base.Description;

                if (Flags.HasFlag(desc.Usage, RHIResourceUsage.UnorderedAccess) && Flags.HasFlag(flags, ShPropertyFlags.ReadWrite))
                {
                    access = BarrierAccess.UnorderedAccess;
                    layout = BarrierLayout.UnorderedAccess;
                }
                else if (Flags.HasFlag(desc.Usage, RHIResourceUsage.ShaderResource))
                {
                    access = BarrierAccess.ShaderResource;
                    layout = BarrierLayout.ShaderResource;
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
                    access = BarrierAccess.UnorderedAccess;
                    layout = BarrierLayout.UnorderedAccess;
                }
                else if (Flags.HasFlag(desc.Usage, FGTextureUsage.ShaderResource))
                {
                    access = BarrierAccess.ShaderResource;
                    layout = BarrierLayout.ShaderResource;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        internal static BarrierSync GetSync(ShPropertyStages stages) => stages switch
        {
            ShPropertyStages.VertexShading => BarrierSync.VertexShading,
            ShPropertyStages.PixelShading => BarrierSync.PixelShading,
            ShPropertyStages.ComputeShading => BarrierSync.ComputeShading,
            ShPropertyStages.AllShading => BarrierSync.AllShading,
            _ => throw new NotImplementedException(),
        };

        private static bool IsStateFlagsCompatible(CommandListType listType, ref readonly NRDResourceState state, NRDResourceId resType, out IncompatibleBarrierFlags flags)
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
        private static void GetCompatibleBarrierLayout(ref readonly NRDResourceState state, IncompatibleBarrierFlags flags, out BarrierSync sync, out BarrierAccess access)
        {
            if (Flags.HasFlag(flags, IncompatibleBarrierFlags.Sync))
                sync = BarrierSync.All;
            else
                sync = state.PreviousSync;

            if (Flags.HasFlag(flags, IncompatibleBarrierFlags.Access))
                access = BarrierAccess.Common;
            else
                access = state.PreviousAccess;
        }

        private static void GetCompatibleBarrierLayout(ref readonly NRDResourceState state, IncompatibleBarrierFlags flags, out BarrierSync sync, out BarrierAccess access, out BarrierLayout layout)
        {
            if (Flags.HasFlag(flags, IncompatibleBarrierFlags.Sync))
                sync = BarrierSync.All;
            else
                sync = state.PreviousSync;

            if (Flags.HasFlag(flags, IncompatibleBarrierFlags.Access))
                access = BarrierAccess.Common;
            else
                access = state.PreviousAccess;

            if (Flags.HasFlag(flags, IncompatibleBarrierFlags.Layout))
                layout = BarrierLayout.Common;
            else
                layout = state.PreviousLayout;
        }

        private static FrozenDictionary<CommandListType, FrozenSet<BarrierSync>> s_syncCompatibility = new Dictionary<CommandListType, FrozenSet<BarrierSync>>
        {
            { CommandListType.Direct, [
                BarrierSync.All,
                BarrierSync.Draw,
                BarrierSync.IndexInput,
                BarrierSync.VertexShading,
                BarrierSync.PixelShading,
                BarrierSync.DepthStencil,
                BarrierSync.RenderTarget,
                BarrierSync.ComputeShading,
                BarrierSync.Raytracing,
                BarrierSync.Copy,
                BarrierSync.Resolve,
                BarrierSync.ExecuteIndirect,
                BarrierSync.Predication,
                BarrierSync.AllShading,
                BarrierSync.NonPixelShading,
                BarrierSync.BuildRaytracingAccelerationStructure,
                BarrierSync.CopyRaytracingAccelerationStructure,
                BarrierSync.EmitRaytracingAccelerationStructurePostbuildInfo,
                BarrierSync.ClearUnorderedAccessView,
                BarrierSync.Split
                ] },
            { CommandListType.Compute, [
                BarrierSync.All,
                BarrierSync.ComputeShading,
                BarrierSync.Raytracing,
                BarrierSync.Copy,
                BarrierSync.ExecuteIndirect,
                BarrierSync.AllShading,
                BarrierSync.NonPixelShading,
                BarrierSync.BuildRaytracingAccelerationStructure,
                BarrierSync.CopyRaytracingAccelerationStructure,
                BarrierSync.EmitRaytracingAccelerationStructurePostbuildInfo,
                BarrierSync.ClearUnorderedAccessView,
                BarrierSync.Split
                ] }
        }.ToFrozenDictionary();
        private static FrozenDictionary<CommandListType, FrozenSet<BarrierAccess>> s_accessCompatibility = new Dictionary<CommandListType, FrozenSet<BarrierAccess>>
        {
            { CommandListType.Direct, [
                BarrierAccess.VertexBuffer,
                BarrierAccess.ConstantBuffer,
                BarrierAccess.IndexBuffer,
                BarrierAccess.RenderTarget,
                BarrierAccess.UnorderedAccess,
                BarrierAccess.DepthStencilWrite,
                BarrierAccess.DepthStencilRead,
                BarrierAccess.ShaderResource,
                BarrierAccess.StreamOutput,
                BarrierAccess.IndirectArgument,
                BarrierAccess.CopyDest,
                BarrierAccess.CopySource,
                BarrierAccess.ResolveDest,
                BarrierAccess.ResolveSource,
                BarrierAccess.RaytracingAccelerationStructureRead,
                BarrierAccess.RaytracingAccelerationStructureWrite,
                BarrierAccess.ShadingRateSource,
                BarrierAccess.Predication
                ] },
            { CommandListType.Compute, [
                BarrierAccess.VertexBuffer,
                BarrierAccess.ConstantBuffer,
                BarrierAccess.UnorderedAccess,
                BarrierAccess.ShaderResource,
                BarrierAccess.IndirectArgument,
                BarrierAccess.CopyDest,
                BarrierAccess.CopySource,
                BarrierAccess.RaytracingAccelerationStructureRead,
                BarrierAccess.RaytracingAccelerationStructureWrite,
                BarrierAccess.Predication
                ] }
        }.ToFrozenDictionary();
        private static FrozenDictionary<CommandListType, FrozenSet<BarrierLayout>> s_layoutCompatibility = new Dictionary<CommandListType, FrozenSet<BarrierLayout>>
        {
            { CommandListType.Direct, [
                BarrierLayout.Common,
                BarrierLayout.GenericRead,
                BarrierLayout.RenderTarget,
                BarrierLayout.UnorderedAccess,
                BarrierLayout.DepthStencilWrite,
                BarrierLayout.DepthStencilRead,
                BarrierLayout.ShaderResource,
                BarrierLayout.CopySource,
                BarrierLayout.CopyDest,
                BarrierLayout.ResolveSource,
                BarrierLayout.ResolveDest,
                BarrierLayout.ShadingRateSource,
                BarrierLayout.DirectQueueGenericRead,
                BarrierLayout.DirectQueueUnorderedAccess,
                BarrierLayout.DirectQueueShaderResource,
                BarrierLayout.DirectQueueCopySource,
                BarrierLayout.DirectQueueCopyDest,
                BarrierLayout.DirectQueueGenericReadComputeQueueAccessible
                ] },
            { CommandListType.Compute, [
                BarrierLayout.Common,
                BarrierLayout.GenericRead,
                BarrierLayout.UnorderedAccess,
                BarrierLayout.ShaderResource,
                BarrierLayout.CopySource,
                BarrierLayout.CopyDest,
                BarrierLayout.DirectQueueGenericReadComputeQueueAccessible
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

    internal struct NRDResourceState(FGResourceId Id, Ptr<RHIResourceNative> Native, BarrierSync StartSync, BarrierAccess StartAccess, BarrierLayout StartLayout = BarrierLayout.Common)
    {
        public readonly FGResourceId Id = Id;
        public readonly Ptr<RHIResourceNative> Native = Native;

        public BarrierSync PreviousSync = StartSync;
        public BarrierAccess PreviousAccess = StartAccess;
        public BarrierLayout PreviousLayout = StartLayout;

        public BarrierSync RequestedSync = StartSync;
        public BarrierAccess RequestedAccess = StartAccess;
        public BarrierLayout RequestedLayout = StartLayout;
    }
}
