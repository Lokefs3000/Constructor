using Primary.Common;
using Primary.Rendering.Raw;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Primary.Rendering.Pooling
{
    public sealed class CommandBufferPool : IDisposable
    {
        private static CommandBufferPool? s_instance;

        private RHI.GraphicsDevice _device;

        private ConcurrentStack<RasterCommandBuffer> _commandBufferWrappers;
        private ConcurrentStack<RHI.GraphicsCommandBuffer>[] _commandBuffers;

        private int _activeStack = 0;

        private bool _disposedValue;

        internal CommandBufferPool(RHI.GraphicsDevice device)
        {
            _device = device;

            _commandBufferWrappers = new ConcurrentStack<RasterCommandBuffer>();
            _commandBuffers = new ConcurrentStack<RHI.GraphicsCommandBuffer>[2];

            _commandBuffers[0] = new ConcurrentStack<RHI.GraphicsCommandBuffer>();
            _commandBuffers[1] = new ConcurrentStack<RHI.GraphicsCommandBuffer>();

            s_instance = this;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    while (_commandBuffers[0].TryPop(out RHI.GraphicsCommandBuffer? commandBuffer))
                        commandBuffer.Dispose();
                    while (_commandBuffers[1].TryPop(out RHI.GraphicsCommandBuffer? commandBuffer))
                        commandBuffer.Dispose();

                    s_instance = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PrepareNewFrame()
        {
            //_activeStack = _activeStack == 1 ? 0 : 1;
        }

        public static RasterCommandBuffer Get(bool autoBegin = true)
        {
            CommandBufferPool pool = NullableUtility.ThrowIfNull(s_instance);
            if (!pool._commandBuffers[pool._activeStack].TryPop(out RHI.GraphicsCommandBuffer? commandBuffer))
            {
                commandBuffer = pool._device.CreateGraphicsCommandBuffer();
            }

            if (autoBegin)
                commandBuffer.Begin(); //TODO: error checking

            if (!pool._commandBufferWrappers.TryPop(out RasterCommandBuffer? wrapper))
            {
                wrapper = new RasterCommandBuffer();
            }

            wrapper.BindForUsage(commandBuffer);
            return wrapper;
        }

        public static void Return(RasterCommandBuffer commandBuffer, bool autoEndAndSubmit = true)
        {
            CommandBufferPool pool = NullableUtility.ThrowIfNull(s_instance);

            if (autoEndAndSubmit)
            {
                if (commandBuffer.Wrapped.IsOpen)
                    commandBuffer.Wrapped.End();
                if (!commandBuffer.Wrapped.IsReady)
                    pool._device.Submit(commandBuffer.Wrapped);
            }

            pool._commandBuffers[pool._activeStack].Push(commandBuffer.Wrapped);

            commandBuffer.UnbindUsage();
            pool._commandBufferWrappers.Push(commandBuffer);
        }
    }
}
