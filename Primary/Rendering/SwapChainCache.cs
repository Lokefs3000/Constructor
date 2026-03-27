using Primary.Mathematics;
using Primary.RHI2;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Primary.Rendering
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
        public RHISwapChain? GetForWindow(Window window, bool createIfNull = true)
        {
            if (_swapChains.TryGetValue(window.WindowId, out SwapChainData data))
                return data.SwapChain;

            if (!createIfNull)
                return null;

            RHISwapChain swapChain = _manager.GraphicsDevice.CreateSwapChain(new RHISwapChainDescription
            {
                WindowHandle = window.NativeWindowHandle,
                WindowSize = window.ClientSize.AsVector2(),

                BackBufferFormat = RHIFormat.RGB10A2_UNorm,
                BackBufferCount = 2
            }) ?? throw new NullReferenceException();

            Action<Int2> resizeEvent = (x) => swapChain.Resize(x.AsVector2());

            data = new SwapChainData(window, swapChain, resizeEvent);
            _swapChains[window.WindowId] = data;

            window.WindowResized += resizeEvent;

            return swapChain;
        }

        private readonly record struct SwapChainData(Window Window, RHISwapChain SwapChain, Action<Int2> ResizeEvent);
    }
}
