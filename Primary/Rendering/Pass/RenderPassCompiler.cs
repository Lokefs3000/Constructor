using Primary.Common;
using Primary.Pooling;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using System.Diagnostics;

namespace Primary.Rendering.Pass
{
    internal sealed class RenderPassCompiler
    {
        private readonly bool _noPassCulling;

        private ObjectPool<RenderPassContainer> _passContainerPool;

        private HashSet<int> _dependencySet;
        private Dictionary<int, IndexRange> _dependencyDict;

        private int[] _pooledDependencyArray;

        private Dictionary<FrameGraphResource, IndexRange> _resourceLifetimeDict;

        internal RenderPassCompiler()
        {
            _noPassCulling = AppArguments.HasArgument("--fg-nocull");

            _passContainerPool = new ObjectPool<RenderPassContainer>(new RenderPassContainer.PoolingPolicy());

            _dependencySet = new HashSet<int>();
            _dependencyDict = new Dictionary<int, IndexRange>();

            _pooledDependencyArray = new int[16];

            _resourceLifetimeDict = new Dictionary<FrameGraphResource, IndexRange>();
        }

        internal void Compile(FrameGraphTexture finalTexture, ReadOnlySpan<RenderPassDescription> passes, FrameGraphTimeline timeline, FrameGraphResources resources, FrameGraphState stateManager)
        {
            timeline.ClearTimeline();
            resources.ClearResources();

            if (passes.IsEmpty)
                return;

            _dependencySet.Clear();
            _dependencyDict.Clear();
            _resourceLifetimeDict.Clear();

            using RentedArray<RenderPassContainer> containers = RentedArray<RenderPassContainer>.Rent(passes.Length, true);
            for (int j = 0; j < passes.Length; j++)
            {
                ref readonly RenderPassDescription currentPass = ref passes[j];

                RenderPassContainer container = _passContainerPool.Get();
                container.Initialize(in currentPass);

                containers[j] = container;
            }

            int i = passes.Length - 1;
            int dependencyIndex = 0;

            while (i >= 0)
            {
                ref readonly RenderPassDescription currentPassDesc = ref passes[i];
                RenderPassContainer currentPass = containers[i];

                //TODO: cull passes with only state setting commands
                if (!_noPassCulling && currentPassDesc.AllowCulling && i == passes.Length - 1 && (!currentPass.HasOutput(finalTexture) || currentPassDesc.Type != RenderPassType.Graphics))
                {
                    i--;
                    continue;
                }

                int startDependencyIndex = dependencyIndex;
                for (int j = i - 1; j >= 0; j--)
                {
                    ref readonly RenderPassDescription previousPassDesc = ref passes[j];
                    RenderPassContainer previousPass = containers[j];

                    if (DoesPassModifyRequiredState(currentPass, previousPass))
                    {
                        _dependencySet.Add((i << 16) | j);
                        _pooledDependencyArray[dependencyIndex++] = j;

                        if (_pooledDependencyArray.Length < dependencyIndex)
                        {
                            Array.Resize(ref _pooledDependencyArray, _pooledDependencyArray.Length * 2);
                        }
                    }
                }

                if (dependencyIndex != startDependencyIndex)
                    _dependencyDict[i] = new IndexRange(startDependencyIndex, dependencyIndex);

                i--;
            }

            CreateTimelineUntilBarrier(timeline, passes, containers.Span, 0, passes[0].Type);
            AddResourceEventsToTimeline(resources);

            _resourceLifetimeDict.Clear();
            foreach (RenderPassContainer pass in containers)
                _passContainerPool.Return(pass);
        }

        private void CreateTimelineUntilBarrier(FrameGraphTimeline timeline, ReadOnlySpan<RenderPassDescription> descriptions, ReadOnlySpan<RenderPassContainer> containers, int startPassIndex, RenderPassType type)
        {
            int rasterIndex = 0;
            int computeIndex = 0;

            int index = 0;

            for (int i = startPassIndex; i < descriptions.Length; i++)
            {
                ref readonly RenderPassDescription desc = ref descriptions[i];
                RenderPassContainer pass = containers[i];

                if (!timeline.IsEmpty && _dependencyDict.TryGetValue(i, out IndexRange range))
                {
                    ReadOnlySpan<int> rangeSpan = _pooledDependencyArray.AsSpan(range);
                    for (int j = 0; j < rangeSpan.Length; j++)
                    {
                        ref readonly RenderPassDescription dependencyDesc = ref descriptions[i];
                        if (dependencyDesc.Type != type)
                        {
                            timeline.AddFenceEvent(GetQueueFromType(desc.Type), GetQueueFromType(dependencyDesc.Type));
                            //break;
                        }
                    }
                }

                WeakRef<int> lifetimeIndex = desc.Type switch
                {
                    RenderPassType.Graphics => new WeakRef<int>(ref rasterIndex),
                    RenderPassType.Compute => new WeakRef<int>(ref computeIndex),
                    _ => throw new NotSupportedException()
                };

                lifetimeIndex = new WeakRef<int>(ref index);

                foreach (var kvp in pass.Resources)
                {
                    if (kvp.Key.IsExternal)
                        continue;

                    if (_resourceLifetimeDict.TryGetValue(kvp.Key, out range))
                    {
                        if (lifetimeIndex.Ref < range.Start)
                        {
                            Debug.Assert(range.End >= lifetimeIndex.Ref);
                            _resourceLifetimeDict[kvp.Key] = new IndexRange(lifetimeIndex.Ref, range.End);
                        }
                        else if (range.End < lifetimeIndex.Ref)
                        {
                            Debug.Assert(range.Start <= lifetimeIndex.Ref);
                            _resourceLifetimeDict[kvp.Key] = new IndexRange(range.Start, lifetimeIndex.Ref);
                        }
                    }
                    else
                    {
                        _resourceLifetimeDict.Add(kvp.Key, new IndexRange(lifetimeIndex.Ref, lifetimeIndex.Ref));
                    }
                }

                switch (desc.Type)
                {
                    case RenderPassType.Graphics: timeline.AddRasterEvent(i); break;
                    case RenderPassType.Compute: timeline.AddComputeEvent(i); break;
                }

                ++lifetimeIndex.Ref;
            }

            static TimelineFenceQueue GetQueueFromType(RenderPassType type) => type switch
            {
                RenderPassType.Graphics => TimelineFenceQueue.Graphics,
                RenderPassType.Compute => TimelineFenceQueue.Compute,
                _ => throw new NotSupportedException()
            };
        }

        private void AddResourceEventsToTimeline(FrameGraphResources resources)
        {
            foreach (var kvp in _resourceLifetimeDict)
            {
                resources.AddResourceWithLifetime(kvp.Key, kvp.Value);
            }

            resources.SortAndFinish();
        }

        private static bool DoesPassModifyRequiredState(RenderPassContainer currentPass, RenderPassContainer passToCheck)
        {
            foreach (var kvp in currentPass.Resources)
            {
                if (!FlagUtility.HasFlag(kvp.Value, FGResourceUsage.Read))
                {
                    if (passToCheck.HasResourceWithUsage(kvp.Key, FGResourceUsage.Write))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private class RenderPassContainer
        {
            private HashSet<FrameGraphTexture> _outputs;
            private Dictionary<FrameGraphResource, FGResourceUsage> _resources;

            internal RenderPassContainer()
            {
                _outputs = new HashSet<FrameGraphTexture>();
                _resources = new Dictionary<FrameGraphResource, FGResourceUsage>();
            }

            internal void Initialize(ref readonly RenderPassDescription desc)
            {
                foreach (ref readonly UsedResourceData resourceData in desc.Resources.Span)
                {
                    _resources[resourceData.Resource] = resourceData.Usage;
                }

                foreach (ref readonly UsedRenderTargetData renderTargetData in desc.RenderTargets.Span)
                {
                    _outputs.Add(renderTargetData.Target);

                    if (_resources.TryGetValue(renderTargetData.Target, out FGResourceUsage usage))
                        _resources[renderTargetData.Target] = usage | FGResourceUsage.Write;
                    else
                        _resources[renderTargetData.Target] = FGResourceUsage.Write;
                }
            }

            internal void Clear()
            {
                _outputs.Clear();
                _resources.Clear();
            }

            internal bool HasOutput(FrameGraphTexture texture) => _outputs.Contains(texture);
            internal bool HasResource(FrameGraphResource resource) => _resources.ContainsKey(resource);

            internal bool HasResourceWithUsage(FrameGraphResource resource, FGResourceUsage usage)
            {
                if (_resources.TryGetValue(resource, out FGResourceUsage resUsage))
                    return FlagUtility.HasEither(resUsage, usage);

                return false;
            }

            internal IReadOnlyDictionary<FrameGraphResource, FGResourceUsage> Resources => _resources;

            internal readonly record struct PoolingPolicy : IObjectPoolPolicy<RenderPassContainer>
            {
                RenderPassContainer IObjectPoolPolicy<RenderPassContainer>.Create() => new RenderPassContainer();

                bool IObjectPoolPolicy<RenderPassContainer>.Return(ref RenderPassContainer obj)
                {
                    obj.Clear();
                    return true;
                }
            }
        }
    }
}
