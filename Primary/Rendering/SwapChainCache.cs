using Primary.Mathematics;
using Primary.RHI;
using Primary.Windowing;
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
                        kvp.Value.Window.OnVisiblityChanged -= kvp.Value.VisibilityEvent;
                        kvp.Value.SwapChain?.Dispose();
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
                data.Window.WindowResized -= data.ResizeEvent;
                data.Window.OnVisiblityChanged -= data.VisibilityEvent;

                data.SwapChain?.Dispose();
                data.SwapChain = null;

                _swapChains.Remove(window.WindowId);
            }
        }

        private void HandleSwapChainVisibility(Window window, bool isVisible)
        {
            return;

            if (!isVisible && _swapChains.TryGetValue(window.WindowId, out SwapChainData data) && data.SwapChain != null)
            {
                EngLog.Render.Debug("Disposing swapchain for window {wnd} because it is no longer visible!", window);

                data.SwapChain.Dispose();
                data.SwapChain = null;
            }
        }

        /// <summary>Not thread-safe</summary>
        public RHISwapChain? GetForWindow(Window window, bool createIfNull = true)
        {
            if (_swapChains.TryGetValue(window.WindowId, out SwapChainData data))
            {
                if (data.SwapChain == null)
                {
                    RHISwapChainDescription desc = new RHISwapChainDescription
                    {
                        WindowHandle = window.NativeWindowHandle,
                        WindowSize = window.ClientSize.AsVector2(),

                        BackBufferFormat = RHIFormat.RGB10A2_UNorm,
                        BackBufferCount = 2,

                        EnableComposition = window.IsTransparent
                    };

                    try
                    {
                        data.SwapChain = _manager.GraphicsDevice.CreateSwapChain(desc) ?? throw new NullReferenceException();
                    }
                    catch (Exception)
                    {
                        if (desc.EnableComposition)
                        {
                            EngLog.Render.Error("Failed to create swap chain for composition on '{w}'", window);

                            desc.EnableComposition = false;
                            data.SwapChain = _manager.GraphicsDevice.CreateSwapChain(desc) ?? throw new NullReferenceException();
                        }
                        else
                        {
                            throw;
                        }
                    }

                    _swapChains[window.WindowId] = data;
                }

                return data.SwapChain;
            }

            if (!createIfNull)
                return null;

            RHISwapChain? swapChain = null;
            if (window.IsShown)
            {
                swapChain = _manager.GraphicsDevice.CreateSwapChain(new RHISwapChainDescription
                {
                    WindowHandle = window.NativeWindowHandle,
                    WindowSize = window.ClientSize.AsVector2(),

                    BackBufferFormat = RHIFormat.RGB10A2_UNorm,
                    BackBufferCount = 2
                }) ?? throw new NullReferenceException();
            }

            Action<Int2> resizeEvent = (x) => _swapChains[window.WindowId].SwapChain?.Resize(x.AsVector2());
            Action<bool> visibilityEvent = (x) => HandleSwapChainVisibility(window, x);

            data = new SwapChainData(window, swapChain, resizeEvent, visibilityEvent);
            _swapChains[window.WindowId] = data;

            window.WindowResized += resizeEvent;
            window.OnVisiblityChanged += visibilityEvent;

            return swapChain;
        }

        private record struct SwapChainData(Window Window, RHISwapChain? SwapChain, Action<Int2> ResizeEvent, Action<bool> VisibilityEvent);
    }
}
