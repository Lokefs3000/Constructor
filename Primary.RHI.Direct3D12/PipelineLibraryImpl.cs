using Primary.RHI.Direct3D12.Utility;
using SharpGen.Runtime;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace Primary.RHI.Direct3D12
{
    internal sealed unsafe class PipelineLibraryImpl : PipelineLibrary
    {
        private readonly GraphicsDeviceImpl _device;

        private ID3D12PipelineLibrary _library;

        private bool _disposedValue;

        internal PipelineLibraryImpl(GraphicsDeviceImpl device, Span<byte> initialData)
        {
            _device = device;

            ResultChecker.ThrowIfUnhandled(device.D3D12Device.CreatePipelineLibrary(initialData, out _library!));
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _device.EnqueueDataFree(() =>
                {
                    _library?.Dispose();
                });

                _disposedValue = true;
            }
        }

        ~PipelineLibraryImpl()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void GetPipelineLibraryData(Span<byte> data)
        {
            _library.Serialize((nint)Unsafe.AsPointer(ref data[0]), new PointerUSize((uint)data.Length));
        }

        internal ID3D12PipelineState? LoadGraphicsPipeline(string name, GraphicsPipelineStateDescription description)
        {
            try
            {
                return _library.LoadGraphicsPipeline(name, description);
            }
            catch (SharpGenException ex)
            {
                if (ex.ResultCode.Code == ResultChecker.EInvalidArg)
                    return null; //invalid
                ResultChecker.ThrowIfUnhandled(ex.ResultCode, _device);
            }

            return null;
        }

        public override long PipelineDataSize => _library.SerializedSize;
    }
}
