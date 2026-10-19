using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Primary.Common;
using Primary.Mathematics;
using Primary.Rendering.Datatypes;
using Primary.Rendering.Playback;
using Primary.Rendering.Resources;
using Primary.RHI;

namespace Primary.Rendering.Memory
{
    internal sealed class ReadbackState : IReadbackCallback
    {
        private readonly ArrayPool<byte> _memoryPool;

        private ResourceHandle _resource;
        private VirtualAllocation _allocation;

        private IPlaybackReadback? _readback;

        private FGResourceDesc _resourceDesc;

        private FGRange? _range;
        private FGBox? _rect;
        private uint _slice;

        private byte[] _readData;
        private int _readByteLength;

        private bool _hasDataAvailable;

        internal ReadbackState(ArrayPool<byte> memoryPool)
        {
            _memoryPool = memoryPool;

            _resource = ResourceHandle.Null;
            _allocation = default;

            _readback = null;

            _resourceDesc = default;

            _range = null;
            _rect = null;
            _slice = 0;

            _readData = [];
            _readByteLength = 0;
        }

        internal void InitializeBuffer(VirtualAllocation allocation, BufferHandle buffer, FGRange? range)
        {
            _resource = buffer;
            _allocation = allocation;
        }

        void IReadbackCallback.OnReadbackFinished()
        {
            Debug.Assert(_readback != null);

            if (_resource.ResourceType == FGResourceType.Buffer)
            {
                FGRange readRange = _range.GetValueOrDefault();
                if (!_range.HasValue)
                {
                    readRange = new FGRange(_resourceDesc.BufferDesc.ElementCount * _resourceDesc.BufferDesc.Stride);
                }

                _readData = _memoryPool.Rent(readRange.Length);
                _readByteLength = readRange.Length;

                if (!_readback.ReadFromResource(_readData.AsSpan(_readByteLength)))
                {
                    EngLog.Render.Error("Readback failed");
                }
            }
            else
            {
                FGBox readBox = _rect.GetValueOrDefault();
                if (!_rect.HasValue)
                {
                    Int3 mipSize = new Int3(_resourceDesc.TextureDesc.Width, _resourceDesc.TextureDesc.Height, _resourceDesc.TextureDesc.DepthOrArraySize) >> (int)_slice;
                    if (_resourceDesc.TextureDesc.Dimension == FGTextureDimension.Tex2D)
                        mipSize.Z = 1;

                    readBox = new FGBox(Int3.Zero, mipSize);
                }

                Int3 boxSize = readBox.Size;
                RHIFormatInfo formatInfo = RHIFormatInfo.Query(_resourceDesc.TextureDesc.Format);

                _readData = _memoryPool.Rent((int)formatInfo.CalculateSize(boxSize.X, boxSize.Y, boxSize.Z));
                _readByteLength = (int)formatInfo.CalculateSize(boxSize.X, boxSize.Y, boxSize.Z);

                if (!_readback.ReadFromResource(_readData.AsSpan(_readByteLength)))
                {
                    EngLog.Render.Error("Readback failed");
                }
            }

            _readback.Dispose();
            _readback = null;

            _hasDataAvailable = true;
        }

        public bool IsDataAvailable => _hasDataAvailable;
        public ReadOnlySpan<byte> ReadData => _readData.AsSpan(_readByteLength);
    }
}
