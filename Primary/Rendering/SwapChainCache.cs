using Primary.RHI;
using SDL;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Primary.Rendering
{
    public class SwapChainCache : IDisposable
    {
        private GraphicsDevice _graphicsDevice;
        private Dictionary<SDL_WindowID, SwapChainData> _swapChains;

        private bool _disposedValue;

        internal SwapChainCache(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            _swapChains = new Dictionary<SDL_WindowID, SwapChainData>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (SwapChainData swapChainData in _swapChains.Values)
                    {
                        swapChainData.SwapChain.Dispose();
                        swapChainData.Window.WindowResized -= swapChainData.ResizedCallback;
                    }

                    _swapChains.Clear();
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
        public SwapChain? GetIfExists(Window window)
        {
            if (_swapChains.TryGetValue(window.ID, out SwapChainData swapChainData))
                return swapChainData.SwapChain;
            return null;
        }


        public SwapChain GetOrAddDefault(Window window)
        {
            if (!_swapChains.TryGetValue(window.ID, out SwapChainData swapChainData))
            {
                SwapChain sw = _graphicsDevice.CreateSwapChain(window.ClientSize, window.NativeWindowHandle);

                swapChainData = new SwapChainData(
                    sw,
                    window,
                    (x) => sw.Resize(x));

                window.WindowResized += swapChainData.ResizedCallback;

                _swapChains.TryAdd(window.ID, swapChainData);
            }

            return swapChainData.SwapChain;
        }

        public void DestroySwapChain(Window window)
        {
            if (_swapChains.TryGetValue(window.ID, out SwapChainData swapChainData))
            {
                swapChainData.SwapChain.Dispose();
                swapChainData.Window.WindowResized -= swapChainData.ResizedCallback;

                _swapChains.Remove(window.ID);
            }
        }

        private record struct SwapChainData(SwapChain SwapChain, Window Window, Action<Vector2> ResizedCallback);
    }
}
