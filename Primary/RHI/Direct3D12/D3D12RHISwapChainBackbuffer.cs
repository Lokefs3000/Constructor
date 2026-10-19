using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Rendering;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Primary.RHI.Direct3D12
{
    public unsafe sealed class D3D12RHISwapChainBackbuffer : RHISwapChainBackbuffer
    {
        private readonly D3D12RHIDevice _device;
        private readonly D3D12RHISwapChain _swapChain;

        private readonly int _bufferIndex;
        private readonly D3D12RHISwapChainBuffer* _bufferData;

        private readonly RHIAlignmentInfo _alignmentInfo;

        private D3D12RHITextureNative* _nativeRep;

        internal D3D12RHISwapChainBackbuffer(D3D12RHIDevice device, D3D12RHISwapChain swapChain, int bufferIndex, D3D12RHISwapChainBuffer* bufferData)
        {
            _device = device;
            _swapChain = swapChain;

            _bufferIndex = bufferIndex;
            _bufferData = bufferData;

            _alignmentInfo = default;
            _barrierState = new RHIBarrierState(ResourceBarrierSync.Draw, ResourceBarrierAccess.Common, BarrierQueueSpec.None);

            ref readonly RHISwapChainDescription swapChainDesc = ref swapChain.Description;
            _description = new RHITextureDescription
            {
                Width = (int)swapChainDesc.WindowSize.X,
                Height = (int)swapChainDesc.WindowSize.Y,
                MipLevels = 1,
                Usage = RHIResourceUsage.RenderTarget,
                Dimension = RHIDimension.Texture2D,
                Format = swapChainDesc.BackBufferFormat
            };

            {

                _nativeRep = (D3D12RHITextureNative*)NativeMemory.Alloc((nuint)Unsafe.SizeOf<D3D12RHITextureNative>());
                _nativeRep->Base = new RHITextureNative
                {
                    Description = _description
                };
                _nativeRep->Resource = *_bufferData->Resource.GetAddressOf();
                _nativeRep->Memory = null;
                _nativeRep->BarrierSync = BarrierSync.AllShading;
                _nativeRep->BarrierAccess = BarrierAccess.Common;
                _nativeRep->BarrierLayout = BarrierLayout.Present;
                _nativeRep->IsInitialized = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _device.AddResourceFreeNextFrame(() =>
                {
                    if (_nativeRep != null)
                        NativeMemory.Free(_nativeRep);
                    _nativeRep = null;
                });

                _disposedValue = true;
            }
        }

        protected override void SetDebugName(string? debugName)
        {
            if (!Unsafe.IsNullRef(in _bufferData->Resource.Get()))
            {
                ResourceHelper.SetResourceName(ref _bufferData->Resource.Get(), debugName);
            }
        }

        public override string ToString()
        {
            return $"RHISwapChainBackbuffer{{{_debugName}}}";
        }

        internal void ResizeBuffer(int newWidth, int newHeight)
        {
            _description.Width = newWidth;
            _description.Height = newHeight;

            _nativeRep->Base.Description = _description;
        }

        public override unsafe RHITextureNative* GetAsNative() => (RHITextureNative*)_nativeRep;
        public override unsafe RHIResourceNative* GetBaseAsNative() => (RHIResourceNative*)_nativeRep;

        public override RHIResourceType Type => RHIResourceType.Texture;

        public override ref readonly RHIAlignmentInfo AlignmentInfo => ref _alignmentInfo;

        public override RHISwapChain SwapChain => _swapChain;
        public override int BufferIndex => _bufferIndex;

        public ComPtr<ID3D12Resource2> Resource => _bufferData->Resource;
    }
}
