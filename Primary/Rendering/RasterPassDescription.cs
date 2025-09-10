using Primary.Rendering.Data;
using Primary.Rendering.Pooling;
using Primary.Rendering.Raw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering
{
    public sealed class RasterPassDescription : IPassDescription, IDisposable
    {
        private RenderPass? _renderPass;

        private RenderPassThreadingPolicy _threadingPolicy;
        private int _threadingSplitCount;

        private Action<RasterCommandBuffer, RenderPassData>? _function;

        internal RasterPassDescription()
        {

        }

        ///<summary>
        ///Invokes <seealso cref="Submit"/>
        ///<para/>
        ///<inheritdoc cref="Submit" />
        ///</summary>
        public void Dispose() => Submit();

        /// <summary>Not thread-safe</summary>
        internal void Reset(RenderPass? renderPass)
        {
            _renderPass = renderPass;

            _threadingPolicy = RenderPassThreadingPolicy.None;
            _threadingSplitCount = 0;

            _function = null;
        }

        /// <summary>Not thread-safe</summary>
        public void Submit()
        {
            _renderPass!.SubmitPassForExecution(this);
        }

        /// <summary>Not thread-safe</summary>
        public void SetThreadingPolicy(RenderPassThreadingPolicy threadingPolicy)
        {
            _threadingPolicy = threadingPolicy;
        }

        /// <summary>Not thread-safe</summary>
        public void SetThreadingSplitCount(int splitCount)
        {
            _threadingSplitCount = Math.Max(splitCount, 0);
        }

        /// <summary>Not thread-safe</summary>
        public void SetFunction(Action<RasterCommandBuffer, RenderPassData> function)
        {
            _function = function;
        }

        void IPassDescription.ExecuteInternal(RenderPassData passData)
        {
            RasterCommandBuffer commandBuffer = CommandBufferPool.Get();

            _function!.Invoke(commandBuffer, passData);

            CommandBufferPool.Return(commandBuffer);
        }
    }
}
