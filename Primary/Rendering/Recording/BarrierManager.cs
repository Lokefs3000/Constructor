using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Text;
using Primary.Collections.ReadOnly;
using Primary.Memory;
using Primary.Profiling;
using Primary.RHI;

namespace Primary.Rendering.Recording
{
    public sealed class BarrierManager
    {
        private readonly FrameGraph _frameGraph;

        private ResourceBarrierState[] _states;

        private readonly BarrierEventGroup _warmupEventGroup;
        private BarrierEventGroup[] _eventGroups;
        private int _eventGroupIndex;

        internal BarrierManager(FrameGraph frameGraph)
        {
            _frameGraph = frameGraph;

            _states = [];

            _warmupEventGroup = new BarrierEventGroup();
            _eventGroups = [];
            _eventGroupIndex = 0;
        }

        internal void ClenaupForEndOfFrame()
        {
            for (int i = 0; i < _eventGroupIndex; ++i)
            {
                _eventGroups[i].Clear();
            }

            Array.Clear(_states);

            _warmupEventGroup.Clear();
            _eventGroupIndex = 0;
        }

        internal void BatchAllBarriers(CompiledRenderPlan renderPlan)
        {
            using (new ProfilingScope("Barriers"))
            {
                if (renderPlan.Resources.Count > _states.Length)
                {
                    _states = new ResourceBarrierState[MemoryHelper.AlignUp(renderPlan.Resources.Count, 32)];
                }

                foreach (RenderResourceInfo resourceInfo in renderPlan.Resources)
                {
                    ResourceHandle resource = _frameGraph.ResourceManager.GetResourceHandle(resourceInfo.ResourceIndex);

                    ref ResourceBarrierState barrierState = ref _states[resource.Index];
                    barrierState.LastTransitionIndex = -1;
                    barrierState.LastBarrierIndex = -1;

                    if (resource.IsImported)
                    {
                        Debug.Assert(resourceInfo.ImportedResource != null);
                        RHIResource rhiResource = resourceInfo.ImportedResource;

                        RHIBarrierState rhiBarrierState = rhiResource.BarrierState;

                        barrierState.CurrentSync = rhiBarrierState.Sync;
                        barrierState.CurrentAccess = rhiBarrierState.Access;
                        barrierState.CurrentQueueSpec = rhiBarrierState.QueueSpec;
                    }
                    else
                    {
                        barrierState.CurrentSync = ResourceBarrierSync.None;
                        barrierState.CurrentAccess = ResourceBarrierAccess.None;
                        barrierState.CurrentQueueSpec = BarrierQueueSpec.None;
                    }
                }

                if (_eventGroups.Length < renderPlan.Events.Count)
                {
                    _eventGroups = new BarrierEventGroup[MemoryHelper.AlignUp(renderPlan.Events.Count, 32)];

                    for (int i = 0; i < _eventGroups.Length; i++)
                    {
                        _eventGroups[i] = new BarrierEventGroup();
                    }
                }
                else
                {
                    for (int i = 0; i < _eventGroupIndex; ++i)
                    {
                        _eventGroups[i].Clear();
                    }
                }

                _warmupEventGroup.Clear();
                _eventGroupIndex = renderPlan.Events.Count;

                using (new ProfilingScope("Batch"))
                {
                    BatchBarriers(renderPlan);
                }

                using (new ProfilingScope("Flush"))
                {
                    FlushBarriersToPlan(renderPlan);
                }

                using (new ProfilingScope("UpdateRHIState"))
                {
                    UpdateRHIState(renderPlan);
                }
            }
        }

        private void BatchBarriers(CompiledRenderPlan renderPlan)
        {
            int lastBarrierIndex = 999;

            ROList<RenderEventInfo> eventInfos = renderPlan.Events;
            for (int i = 0; i < eventInfos.Count; ++i)
            {
                RenderEventInfo eventInfo = eventInfos[i];
                if (eventInfo.EventType == RenderEventType.ExecuteDirect || eventInfo.EventType == RenderEventType.ExecuteCompute)
                {
                    CompiledRenderPass renderPass = eventInfo.RenderPass1!;

                    ROList<BarrierRequest> requests = renderPass.BarrierGroup.BarrierRequests;
                    for (int j = 0; j < requests.Count; ++j)
                    {
                        BarrierRequest currentRequest = requests[j];

                        ref ResourceBarrierState barrierState = ref _states[currentRequest.Resource.Index];
                        if (currentRequest.Sync != barrierState.CurrentSync || currentRequest.Access != barrierState.CurrentAccess)
                        {
                            BarrierEventGroup thisEventGroup = _eventGroups[i];

                            Debug.Assert(CanQueueTransitionTo(eventInfo.EventType, currentRequest.Sync, currentRequest.Access));

                            bool isSourceCompatible = CanQueueTransitionTo(eventInfo.EventType, barrierState.CurrentSync, barrierState.CurrentAccess);
                            bool didFindEarlierGroup = false;

                            for (int k = barrierState.LastTransitionIndex + 1; k <= i; ++k)
                            {
                                BarrierEventGroup eventGroup = _eventGroups[k];
                                if (eventGroup != thisEventGroup)
                                {
                                    RenderEventType eventTypeAtGroup = eventInfos[k].EventType;
                                    if (eventTypeAtGroup != RenderEventType.InsertBarrier && (eventTypeAtGroup == eventInfo.EventType || k < lastBarrierIndex))
                                    {
                                        if (!CanQueueTransitionTo(eventTypeAtGroup, barrierState.CurrentSync, barrierState.CurrentAccess))
                                            continue;

                                        ResourceBarrierSync nextSync = isSourceCompatible ? ResourceBarrierSync.Split : currentRequest.Sync;

                                        eventGroup.Add(new RenderBarrier(
                                            k, 0, currentRequest.Resource.Index,
                                            barrierState.CurrentSync, barrierState.CurrentAccess, barrierState.CurrentQueueSpec,
                                            nextSync, currentRequest.Access, BarrierQueueSpec.None));

                                        barrierState.CurrentSync = nextSync;
                                        didFindEarlierGroup = true;
                                        break;
                                    }
                                }
                            }

                            if (didFindEarlierGroup)
                            {
                                Debug.Assert(isSourceCompatible);

                                thisEventGroup.Add(new RenderBarrier(
                                            i, currentRequest.CommandOffset, currentRequest.Resource.Index,
                                            barrierState.CurrentSync, barrierState.CurrentAccess, barrierState.CurrentQueueSpec,
                                            currentRequest.Sync, currentRequest.Access, BarrierQueueSpec.None));
                            }
                            else
                            {
                                BarrierEventGroup eventGroup = isSourceCompatible ? thisEventGroup : (barrierState.LastBarrierIndex == -1 ? _warmupEventGroup : _eventGroups[barrierState.LastBarrierIndex]);
                                eventGroup.Add(new RenderBarrier(
                                            i, currentRequest.CommandOffset, currentRequest.Resource.Index,
                                            barrierState.CurrentSync, barrierState.CurrentAccess, barrierState.CurrentQueueSpec,
                                            currentRequest.Sync, currentRequest.Access, BarrierQueueSpec.None));
                            }

                            barrierState.CurrentSync = currentRequest.Sync;
                            barrierState.CurrentAccess = currentRequest.Access;

                            barrierState.LastTransitionIndex = i;
                            barrierState.CurrentQueueSpec = BarrierQueueSpec.None;
                        }
                        else
                        {
                            barrierState.LastTransitionIndex = i;
                        }

                        barrierState.CurrentQueueSpec |= (eventInfo.EventType == RenderEventType.ExecuteDirect ? BarrierQueueSpec.Direct : BarrierQueueSpec.Compute);
                    }
                }
                else if (eventInfo.EventType == RenderEventType.InsertBarrier)
                {
                    lastBarrierIndex = i;
                }
            }
        }

        private void FlushBarriersToPlan(CompiledRenderPlan renderPlan)
        {
            ROList<RenderEventInfo> eventInfos = renderPlan.Events;
            for (int i = 0; i < eventInfos.Count; ++i)
            {
                RenderEventInfo eventInfo = eventInfos[i];
                if (eventInfo.EventType == RenderEventType.ExecuteDirect || eventInfo.EventType == RenderEventType.ExecuteCompute)
                {
                    CompiledRenderPass renderPass = eventInfo.RenderPass1!;
                    BarrierEventGroup eventGroup = _eventGroups[i];

                    eventGroup.SortBarriers();
                    renderPass.AddBarriers(eventGroup.Barriers.AsSpan());
                }
            }

            if (_warmupEventGroup.Barriers.Count > 0)
            {
                renderPlan.AddWarmupBarrier(_warmupEventGroup.Barriers.AsSpan());
            }
        }

        private void UpdateRHIState(CompiledRenderPlan renderPlan)
        {
            foreach (RenderResourceInfo resourceInfo in renderPlan.Resources)
            {
                if (resourceInfo.ImportedResource != null)
                {
                    ref ResourceBarrierState barrierState = ref _states[resourceInfo.ResourceIndex];

                    RHIResource resource = resourceInfo.ImportedResource;
                    resource.BarrierState = new RHIBarrierState(barrierState.CurrentSync, barrierState.CurrentAccess, barrierState.CurrentQueueSpec);
                }
            }
        }

        private static bool CanQueueTransitionTo(RenderEventType eventType, ResourceBarrierSync sync, ResourceBarrierAccess access)
        {
            switch (eventType)
            {
                case RenderEventType.ExecuteDirect:
                    {
                        return true;
                    }
                case RenderEventType.ExecuteCompute:
                    {
                        switch (sync)
                        {
                            case ResourceBarrierSync.IndexInput:
                            case ResourceBarrierSync.DepthStencil:
                            case ResourceBarrierSync.RenderTarget:
                            case ResourceBarrierSync.PixelShading:
                            case ResourceBarrierSync.VertexShading:
                            case ResourceBarrierSync.Raytracing:
                            case ResourceBarrierSync.Resolve:
                            case ResourceBarrierSync.BuildRTAcceleration:
                            case ResourceBarrierSync.CopyRTAcceleration:
                            case ResourceBarrierSync.ClearUnorderedAccess: return false;
                        }

                        switch (access)
                        {
                            case ResourceBarrierAccess.VertexBuffer:
                            case ResourceBarrierAccess.IndexBuffer:
                            case ResourceBarrierAccess.RenderTarget:
                            case ResourceBarrierAccess.DepthStencilRead:
                            case ResourceBarrierAccess.DepthStencilWrite:
                            case ResourceBarrierAccess.ResolveSource:
                            case ResourceBarrierAccess.ResolveDestination:
                            case ResourceBarrierAccess.ShadingRateSource: return false;
                        }

                        return true;
                    }
                default:
                    {
                        return true;
                    }
            }
        }

        private record struct ResourceBarrierState(int LastTransitionIndex, int LastBarrierIndex, ResourceBarrierSync CurrentSync, ResourceBarrierAccess CurrentAccess, BarrierQueueSpec CurrentQueueSpec);
    
        private sealed class BarrierEventGroup
        {
            private readonly List<RenderBarrier> _barriers;

            internal BarrierEventGroup()
            {
                _barriers = new List<RenderBarrier>();
            }

            internal void Clear()
            {
                _barriers.Clear();
            }

            internal void Add(RenderBarrier barrier)
            {
                _barriers.Add(barrier);
            }

            internal void SortBarriers()
            {
                _barriers.Sort(static (x, y) => x.CommandOffset.CompareTo(y.CommandOffset));
            }

            internal ROList<RenderBarrier> Barriers => _barriers;
        }
    }

    internal readonly record struct RenderBarrier(
        int EventIndex, int CommandOffset, int ResourceIndex,
        ResourceBarrierSync OldSync, ResourceBarrierAccess OldAccess, BarrierQueueSpec OldQueueSpec,
        ResourceBarrierSync NewSync, ResourceBarrierAccess NewAccess, BarrierQueueSpec NewQueueSpec);
}
