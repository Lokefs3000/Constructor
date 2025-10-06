using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.RHI
{
    public abstract class Descriptor : IDisposable
    {
        public abstract void Dispose();

        public abstract ref readonly DescriptorDescription Description { get; }
        public abstract Resource Owner { get; }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct DescriptorDescription
    {
        [FieldOffset(0)]
        public BufferCBDescriptorDescription BufferCB;

        [FieldOffset(0)]
        public BufferSRDescriptorDescription BufferSR;

        [FieldOffset(0)]
        public TextureSRDescriptorDescription TextureSR;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct TextureDescriptorDescription
    {
        [FieldOffset(0)]
        public BufferCBDescriptorDescription BufferCB;

        [FieldOffset(0)]
        public BufferSRDescriptorDescription BufferSR;

        [FieldOffset(0)]
        public TextureSRDescriptorDescription TextureSR;
    }

    public struct BufferCBDescriptorDescription
    {
        public uint ByteOffset;
        public uint SizeInBytes;
        public DescriptorFlags Flags;
    }

    public struct BufferSRDescriptorDescription
    {
        public uint FirstElement;
        public uint NumberOfElements;
        public uint StructureByteStride;
        public DescriptorFlags Flags;
    }

    public struct TextureSRDescriptorDescription
    {
        public uint MostDetailedMip;
        public uint MipLevels;
        public uint PlaneSlice;
        public float ResourceMinLODClamp;
        public DescriptorFlags Flags;
    }

    public enum DescriptorFlags : byte
    {
        None = 0,

        Dynamic = 1 << 0
    }
}
