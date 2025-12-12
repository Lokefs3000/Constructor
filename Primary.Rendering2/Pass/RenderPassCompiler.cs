using Primary.Common;
using Primary.Profiling;
using Primary.Rendering2.Recording;
using Primary.Rendering2.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Pass
{
    internal sealed class RenderPassCompiler
    {
        private RPOverview[] _overviews;
        private HashSet<int> _referencedResources;

        private RenderPassType _currentPassType;
        private List<int> _currentPassList;

        internal RenderPassCompiler()
        {
            _overviews = Array.Empty<RPOverview>();
            _referencedResources = new HashSet<int>();

            _currentPassType = RenderPassType.Graphics;
            _currentPassList = new List<int>();
        }

        internal void Compile(FrameGraphTexture finalTexture, ReadOnlySpan<RenderPassDescription> passes, FrameGraphTimeline timeline, FrameGraphState stateManager)
        {
            timeline.ClearTimeline();

            if (_overviews.Length < passes.Length)
            {
                _overviews = new RPOverview[passes.Length];

                for (int i = 0; i < passes.Length; i++)
                {
                    _overviews[i] = new RPOverview(new Dictionary<int, ResourceState>(), new HashSet<int>(), new Dictionary<int, RPRelation>(), ResourceStateFlags.None);
                }
            }
            else
            {
                foreach (ref readonly RPOverview overview in _overviews.AsSpan())
                {
                    overview.Clear();
                }
            }

            using (new ProfilingScope("FindResources"))
            {
                int idx = 0;
                foreach (ref readonly RenderPassDescription desc in passes)
                {
                    ref RPOverview overview = ref _overviews[idx++];

                    foreach (ref readonly UsedResourceData resourceData in desc.Resources.Span)
                    {
                        _referencedResources.Add(resourceData.Resource.Index);

                        bool isExternal = resourceData.Resource.IsExternal;
                        overview.ResourceStates.Add(resourceData.Resource.Index, new ResourceState(resourceData.Usage, isExternal));

                        if (isExternal && FlagUtility.HasFlag(resourceData.Usage, FGResourceUsage.Write))
                            overview.Flags |= ResourceStateFlags.HasExternalWrite;
                    }

                    foreach (ref readonly UsedRenderTargetData renderTargetData in desc.RenderTargets.Span)
                    {
                        bool isExternal = ((FrameGraphResource)renderTargetData.Target).IsExternal;

                        overview.OutputResources.Add(renderTargetData.Target.Index);
                        overview.ResourceStates.Add(renderTargetData.Target.Index, new ResourceState(FGResourceUsage.Write, isExternal));

                        if (isExternal)
                            overview.Flags |= ResourceStateFlags.HasExternalWrite;
                    }
                }
            }

            using (new ProfilingScope("FindRelations"))
            {
                for (int i = passes.Length - 1; i >= 0; i--)
                {
                    ref readonly RPOverview overview = ref _overviews[i];
                    ref readonly RenderPassDescription overviewDesc = ref passes[i];

                    for (int j = 0; j < passes.Length; j++)
                    {
                        if (i == j)
                            continue;

                        ref readonly RPOverview pass = ref _overviews[j];
                        ref readonly RenderPassDescription passDesc = ref passes[j];

                        RPRelation relation = RPRelation.None;

                        if (overviewDesc.Type != passDesc.Type)
                        {
                            foreach (var kvp in overview.ResourceStates)
                            {
                                if (pass.ResourceStates.TryGetValue(kvp.Key, out ResourceState state) && FlagUtility.HasFlag(kvp.Value.Usage | state.Usage, FGResourceUsage.Write))
                                {
                                    relation |= RPRelation.WriteOnSeparateQueue;
                                    break;
                                }
                            }
                        }

                        foreach (int res in overview.OutputResources)
                        {
                            if (pass.OutputResources.Contains(res))
                            {
                                relation |= RPRelation.SharedOutput;
                                break;
                            }
                        }

                        overview.Relations[j] = relation;
                    }
                }
            }

            using (new ProfilingScope("Output"))
            {
                for (int i = 0; i < passes.Length; i++)
                {
                    ref readonly RenderPassDescription desc = ref passes[i];
                    ref readonly RPOverview overview = ref _overviews[i];

                    RenderPassStateData stateData = stateManager.GetStateData(i);

                    if (desc.Type != RenderPassType.Graphics)
                        throw new NotImplementedException();

                    foreach (ref readonly UsedRenderTargetData data in desc.RenderTargets.Span)
                    {
                        stateData.AddOutput(data.Target.Index, data.Type switch
                        {
                            FGRenderTargetType.RenderTarget => FGOutputType.RenderTarget,
                            FGRenderTargetType.DepthStencil => FGOutputType.DepthStencil,
                            _ => throw new NotImplementedException()
                        });
                    }

                    foreach (ref readonly UsedResourceData data in desc.Resources.Span)
                    {
                        stateData.AddResource(data.Resource.Index, new FGResourceStateData(data.Usage));
                    }

                    timeline.AddRasterEvent(i);
                }
            }
        }

        private readonly record struct ResourceState(FGResourceUsage Usage, bool IsExternal);
        private record struct RefResourceMeta(int Lifetime);

        private record struct RPOverview(
            Dictionary<int, ResourceState> ResourceStates,
            HashSet<int> OutputResources,
            Dictionary<int, RPRelation> Relations, 
            ResourceStateFlags Flags)
        {
            public void Clear()
            {
                ResourceStates.Clear();
                OutputResources.Clear();
                Relations.Clear();
                Flags = ResourceStateFlags.None;
            }
        }

        private enum ResourceStateFlags : byte
        {
            None = 0,

            HasExternalWrite = 1 << 0
        }

        private enum RPRelation : byte
        {
            None = 0,

            WriteOnSeparateQueue = 1 << 0,
            SharedOutput = 1 << 1
        }
    }
}
