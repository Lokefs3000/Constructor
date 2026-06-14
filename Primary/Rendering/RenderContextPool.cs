using Primary.Profiling;
using Primary.Timing;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Primary.Rendering
{
    public sealed class RenderContextPool
    {
        private ContextCollection _cameraCollection;
        private ContextCollection _windowCollection;

        internal RenderContextPool()
        {
            _cameraCollection = new ContextCollection();
            _windowCollection = new ContextCollection();
        }

        internal void ResetPoolForNewRender()
        {
            using (new ProfilingScope("ResetContextPool"))
            {
                _cameraCollection.ClearAndAnalyze();
                _windowCollection.ClearAndAnalyze();
            }
        }

        internal RenderContextContainer GetCameraContext() => _cameraCollection.Get();
        internal RenderContextContainer GetWindowContext() => _windowCollection.Get();

        private record struct ContextCollection
        {
            public readonly AverageAnalyser<int> Analyser;

            public RenderContextContainer?[] Contexts;
            public int ContextIndex;

            public int MaxContextIndex;

            public ContextCollection()
            {
                Contexts = Array.Empty<RenderContextContainer?>();
                Analyser = new AverageAnalyser<int>(8, 0.5f);

                ContextIndex = 0;

                MaxContextIndex = 0;
            }

            public void ClearAndAnalyze()
            {
                MaxContextIndex = Math.Max(MaxContextIndex, ContextIndex);

                if (Analyser.Sample(MaxContextIndex, Time.DeltaTime))
                    MaxContextIndex = 0;

                if (Analyser.IsValid)
                {
                    int max = (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(Analyser.Max(), MaxContextIndex));
                    if (max == 0)
                    {
                        EngLog.Render.Debug("Setting render context array to empty array");
                        Contexts = Array.Empty<RenderContextContainer>();
                    }
                    else if (max < Contexts.Length)
                    {
                        EngLog.Render.Debug("Culling excess render context entries: {fr} -> {to}", Contexts.Length, max);
                        Array.Resize(ref Contexts, max);
                    }
                }

                ContextIndex = 0;
            }

            public RenderContextContainer Get()
            {
                if (ContextIndex == Contexts.Length)
                    Array.Resize(ref Contexts, Math.Max(Contexts.Length * 2, 1));

                ref RenderContextContainer? context = ref Contexts[ContextIndex++];
                context ??= new RenderContextContainer();

                return context;
            }
        }
    }
}
