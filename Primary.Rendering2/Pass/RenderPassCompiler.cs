using Primary.Common;
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

        internal void Compile(ReadOnlySpan<RenderPassDescription> passes/*, FrameGraphTimeline timeline*/)
        {
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

            FrameGraphTexture finalOutput;

            for (int i = passes.Length - 1; i >= 0; i--)
            {
                ref readonly RPOverview overview = ref _overviews[i];
                
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

            MustHappenBefore,
            MustHappenAfter,
        }
    }
}
