using Primary.Common;
using System.Numerics;

namespace Primary.Rendering.Raw
{
    internal sealed class RenderTargetPool : IDisposable
    {
        private RHI.GraphicsDevice _device;
        private Dictionary<long, RHI.RenderTarget> _renderTargets;

        private bool _disposedValue;

        internal RenderTargetPool(RHI.GraphicsDevice device)
        {
            _device = device;
            _renderTargets = new Dictionary<long, RHI.RenderTarget>();
        }

        internal RHI.RenderTarget GetOrCreate(long id, Vector2 clientSize)
        {
            if (clientSize == Vector2.Zero)
                throw new NotImplementedException("placeholder error");

            if (!_renderTargets.TryGetValue(id, out RHI.RenderTarget? rt))
            {
                rt = _device.CreateRenderTarget(new RHI.RenderTargetDescription
                {
                    ColorFormat = RHI.RenderTargetFormat.RGB10A2un,
                    DepthFormat = RHI.DepthStencilFormat.D24unS8ui,
                    Dimensions = new Size((int)clientSize.X, (int)clientSize.Y),
                    ShaderVisibility = RHI.RenderTargetVisiblity.Color
                });

                rt.Name = id.ToString();

                _renderTargets.Add(id, rt);
            }
            else if (rt.Description.Dimensions.AsVector2() != clientSize)
            {
                rt.Dispose();
                _renderTargets.Remove(id);

                return GetOrCreate(id, clientSize);
            }

            return rt;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (var kvp in _renderTargets)
                    {
                        kvp.Value.Dispose();
                    }

                    _renderTargets.Clear();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
