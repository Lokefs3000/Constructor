using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Editor.Interop.Compressonator
{
    public partial struct CMP_COLOR
    {
        [NativeTypeName("__AnonymousRecord_compressonator_L552_C5")]
        public _Anonymous_e__Union Anonymous;

        [UnscopedRef]
        public Span<byte> rgba
        {
            get
            {
                return Anonymous.rgba;
            }
        }

        [UnscopedRef]
        public ref uint asDword
        {
            get
            {
                return ref Anonymous.asDword;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("CMP_BYTE[4]")]
            public _rgba_e__FixedBuffer rgba;

            [FieldOffset(0)]
            [NativeTypeName("CMP_DWORD")]
            public uint asDword;

            [InlineArray(4)]
            public partial struct _rgba_e__FixedBuffer
            {
                public byte e0;
            }
        }
    }
}
