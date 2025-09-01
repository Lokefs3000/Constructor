using Primary.RHI.Direct3D12.Utility;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace Primary.RHI.Direct3D12
{
    internal class FenceImpl : Fence
    {
        private readonly GraphicsDeviceImpl _device;

        private ID3D12Fence _fence;
        private AutoResetEvent _event;

        private bool _disposedValue;

        internal FenceImpl(GraphicsDeviceImpl device, ulong initialValue)
        {
            _device = device;

            ResultChecker.ThrowIfUnhandled(device.D3D12Device.CreateFence(initialValue, FenceFlags.None, out _fence!));
            _event = new AutoResetEvent(false);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _device.EnqueueDataFree(() =>
                {
                    _event?.Dispose();
                    _fence?.Dispose();
                });

                _disposedValue = true;
            }
        }

        ~FenceImpl()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Signal(ulong value)
        {
            _fence.Signal(value);
        }

        public override void Wait(ulong value, FenceCondition condition, int timeout)
        {
            bool shouldWait = condition switch
            {
                FenceCondition.Always => true,
                FenceCondition.Equals => value == CompletedValue,
                FenceCondition.LessThan => value < CompletedValue,
                FenceCondition.LessThanOrEquals => value <= CompletedValue,
                FenceCondition.GreaterThan => value > CompletedValue,
                FenceCondition.GreaterThanOrEquals => value >= CompletedValue,
                _ => false
            };

            if (shouldWait)
            {
                if (ResultChecker.PrintIfUnhandled(_fence.SetEventOnCompletion(value, _event), _device))
                {
                    _event.WaitOne(timeout == -1 ? 2000 : timeout); //D3D12 auto times the GPU out if it is unresponsive for 2000ms i believe.
                }
            }
        }

        public override string Name { set => _fence.Name = value; }

        public override ulong CompletedValue => _fence.CompletedValue;
    }
}
