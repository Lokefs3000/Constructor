using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.RHI2
{
    public unsafe abstract class RHITexture : IDisposable, AsNativeObject<RHITextureNative>
    {
        protected RHITextureDescription _description;
        protected string? _debugName;

        protected bool _disposedValue;

        protected abstract void Dispose(bool disposing);
        protected abstract void SetDebugName(string? debugName);

        ~RHITexture()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public ref readonly RHITextureDescription Description => ref _description;

        public string? DebugName { get => _debugName; set { _debugName = value; SetDebugName(_debugName); } }

        public abstract RHITextureNative* GetAsNative();
    }

    public struct RHITextureNative
    {
        public RHITextureDescription Description;
    }

    public struct RHITextureDescription
    {
        public int Width;
        public int Height;
        public int DepthOrArraySize;

        public int MipLevels;

        public RHIResourceUsage Usage;
        public RHIFormat Format;

        public RHITextureDescription()
        {
            Width = 0;
            Height = 0;
            DepthOrArraySize = 0;

            MipLevels = 0;

            Usage = RHIResourceUsage.None;
            Format = RHIFormat.Unknown;
        }

        public RHITextureDescription(in RHITextureDescription other)
        {
            Width = other.Width;
            Height = other.Height;
            DepthOrArraySize = other.DepthOrArraySize;

            MipLevels = other.MipLevels;

            Usage = other.Usage;
            Format = other.Format;
        }

        public int Depth { get => DepthOrArraySize; set => DepthOrArraySize = value; }
        public int ArraySize { get => DepthOrArraySize; set => DepthOrArraySize = value; }
    }
}
