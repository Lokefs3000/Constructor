using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Assets;
using Primary.Collections.ReadOnly;
using Primary.Pooling;
using Primary.Profiling;
using Primary.Rendering.Assets;
using Primary.RHI;
using Primary.Utility;

namespace Primary.Rendering.Passes
{
    public sealed class RenderPassManager : IDisposable
    {
        private readonly FrameGraph _frameGraph;

        private readonly List<IRenderPass> _scheduledPasses;

        private readonly ObjectPool<RenderPassIndex> _indexPool;

        private readonly List<RenderPassIndex> _renderPasses;
        private readonly List<RenderPassIndex> _builtPasses;

        private readonly List<CompiledRenderPass> _renderPassPool;
        private int _renderPassPoolHead;
        private int _renderPassIndex;

        private bool _disposedValue;

        internal RenderPassManager(FrameGraph frameGraph)
        {
            _frameGraph = frameGraph;

            _scheduledPasses = new List<IRenderPass>();

            _indexPool = new ObjectPool<RenderPassIndex>(RenderPassIndex.GenericPoolPolicy.Default);

            _renderPasses = new List<RenderPassIndex>();
            _builtPasses = new List<RenderPassIndex>();

            _renderPassPool = new List<CompiledRenderPass>();
            _renderPassPoolHead = 0;
            _renderPassIndex = 0;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (CompiledRenderPass renderPass in _renderPassPool)
                    {
                        renderPass.Dispose();
                    }
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void CleanupForEndOfFrame()
        {
            _scheduledPasses.Clear();

            _renderPasses.Clear();
            _builtPasses.Clear();

            for (int i = 0; i < _renderPassPoolHead; ++i)
            {
                _renderPassPool[i].ClearPassForPooling();
            }

            _renderPassPoolHead = 0;
            _renderPassIndex = 0;
        }

        internal void AddPassesToPlan(CompiledRenderPlan plan)
        {
            using (new ProfilingScope("AddPasses"))
            {
                foreach (RenderPassIndex renderPass in _renderPasses)
                {
                    if (_renderPassPool.Count == _renderPassPoolHead)
                    {
                        _renderPassPool.Add(new CompiledRenderPass(_frameGraph));
                    }

                    CompiledRenderPass compiledPass = _renderPassPool[_renderPassPoolHead++];
                    compiledPass.SetupPassForUse(renderPass);

                    plan.AddRenderPass(compiledPass);
                }
            }
        }

        internal RenderPassIndex EnqueuePass(IRenderPass renderPass, RenderPassType passType, string name, RenderContextContainer contextContainer)
        {
            RenderPassIndex index = _indexPool.Get();
            index.ResetForNewPass(renderPass, _renderPassIndex++, passType, name, contextContainer);

            _renderPasses.Add(index);
            return index;
        }

        internal void FinishPassBuild(RenderPassIndex renderPass)
        {
            foreach (var (_, globalData) in ShaderGlobalsManager.Instance.GlobalProperties)
            {
                if (!globalData.FGResource.IsNull)
                {
                    _frameGraph.ResourceManager.AddRead(renderPass, globalData.FGResource);
                }
                else if (globalData.ObjResource is RHIResource resource)
                {
                    if (resource is RHIBuffer buffer)
                        _frameGraph.ResourceManager.AddRead(renderPass, _frameGraph.ImportBufferAsPure(buffer));
                    else
                        _frameGraph.ResourceManager.AddRead(renderPass, _frameGraph.ImportTextureAsPure(Unsafe.As<RHITexture>(resource)));
                }
                else if (globalData.ObjResource is TextureAsset texture)
                {
                    if (texture.IsLoaded && texture.RawRHITexture != null)
                    {
                        _frameGraph.ResourceManager.AddRead(renderPass, _frameGraph.ImportTextureAsPure(texture.RawRHITexture));
                    }
                }
            }

            _builtPasses.AddUnique(renderPass);
        }

        internal ROList<RenderPassIndex> ActivePasses => _builtPasses;
    }
}
