using Primary.Rendering;
using Primary.RHI2;
using System.Numerics;

namespace Primary.Rendering2
{
    public sealed class SwapChainCache : IDisposable
    {
        private readonly RenderingManager _manager;

        private Dictionary<uint, SwapChainData> _swapChains;

        private bool _disposedValue;

        internal SwapChainCache(RenderingManager manager)
        {
            _manager = manager;

            _swapChains = new Dictionary<uint, SwapChainData>();

            Engine.GlobalSingleton.WindowManager.WindowDestroyed += WindowDestroyedEvent;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (var kvp in _swapChains)
                    {
                        kvp.Value.Window.WindowResized -= kvp.Value.ResizeEvent;
                        kvp.Value.SwapChain.Dispose();
                    }

                    _swapChains.Clear();

                    Engine.GlobalSingleton.WindowManager.WindowDestroyed -= WindowDestroyedEvent;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void WindowDestroyedEvent(Window window)
        {
            if (_swapChains.TryGetValue(window.WindowId, out SwapChainData data))
            {
                data.SwapChain.Dispose();
                _swapChains.Remove(window.WindowId);
            }
        }

        /// <summary>Not thread-safe</summary>
        public RHISwapChain GetForWindow(Window window, bool createIfNull = true)
        {
            if (_swapChains.TryGetValue(window.WindowId, out SwapChainData data))
                return data.SwapChain;

            RHISwapChain swapChain = _manager.GraphicsDevice.CreateSwapChain(new RHISwapChainDescription
            {
                WindowHandle = window.InternalWindowInterop,
                WindowSize = window.ClientSize,

                BackBufferFormat = RHIFormat.RGBA8_UNorm,
                BackBufferCount = 2
            }) ?? throw new NullReferenceException();

            Action<Vector2> resizeEvent = (x) => swapChain.Resize(x);

            data = new SwapChainData(window, swapChain, resizeEvent);
            _swapChains[window.WindowId] = data;

            window.WindowResized += resizeEvent;

            return swapChain;
        }

        private readonly record struct SwapChainData(Window Window, RHISwapChain SwapChain, Action<Vector2> ResizeEvent);
    }
}
