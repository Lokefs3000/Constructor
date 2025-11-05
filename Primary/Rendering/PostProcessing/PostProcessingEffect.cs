using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering.PostProcessing
{
    public abstract class PostProcessingEffect<T> : IGenericPostProcessingEffect where T : class, IPostProcessingData
    {
        public PostProcessingEffect()
        {

        }

        public abstract void SetupPass(RenderPass renderPass, T data);
        public abstract void Dispose();

        void IGenericPostProcessingEffect.ExecuteGeneric(RenderPass renderPass, IPostProcessingData data)
        {
            Debug.Assert(data is T);
            SetupPass(renderPass, Unsafe.As<T>(data));
        }
    }

    internal interface IGenericPostProcessingEffect : IDisposable
{
        internal void ExecuteGeneric(RenderPass renderPass, IPostProcessingData data);
    }
}
