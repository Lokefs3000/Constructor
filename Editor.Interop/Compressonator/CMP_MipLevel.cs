using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Editor.Interop.Compressonator
{
    public unsafe partial struct CMP_MipLevel
    {
        [NativeTypeName("CMP_INT")]
        public int m_nWidth;

        [NativeTypeName("CMP_INT")]
        public int m_nHeight;

        [NativeTypeName("CMP_DWORD")]
        public uint m_dwLinearSize;

        [NativeTypeName("__AnonymousRecord_compressonator_L631_C5")]
        public _Anonymous_e__Union Anonymous;

        [UnscopedRef]
        public ref sbyte* m_psbData
        {
            get
            {
                return ref Anonymous.m_psbData;
            }
        }

        [UnscopedRef]
        public ref byte* m_pbData
        {
            get
            {
                return ref Anonymous.m_pbData;
            }
        }

        [UnscopedRef]
        public ref ushort* m_pwData
        {
            get
            {
                return ref Anonymous.m_pwData;
            }
        }

        [UnscopedRef]
        public ref CMP_COLOR* m_pcData
        {
            get
            {
                return ref Anonymous.m_pcData;
            }
        }

        [UnscopedRef]
        public ref float* m_pfData
        {
            get
            {
                return ref Anonymous.m_pfData;
            }
        }

        [UnscopedRef]
        public ref short* m_phfsData
        {
            get
            {
                return ref Anonymous.m_phfsData;
            }
        }

        [UnscopedRef]
        public ref uint* m_pdwData
        {
            get
            {
                return ref Anonymous.m_pdwData;
            }
        }

        //[UnscopedRef]
        //public ref vector<byte>* m_pvec8Data
        //{
        //    get
        //    {
        //        return ref Anonymous.m_pvec8Data;
        //    }
        //}

        [StructLayout(LayoutKind.Explicit)]
        public unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("CMP_SBYTE *")]
            public sbyte* m_psbData;

            [FieldOffset(0)]
            [NativeTypeName("CMP_BYTE *")]
            public byte* m_pbData;

            [FieldOffset(0)]
            [NativeTypeName("CMP_WORD *")]
            public ushort* m_pwData;

            [FieldOffset(0)]
            public CMP_COLOR* m_pcData;

            [FieldOffset(0)]
            [NativeTypeName("CMP_FLOAT *")]
            public float* m_pfData;

            [FieldOffset(0)]
            [NativeTypeName("CMP_HALFSHORT *")]
            public short* m_phfsData;

            [FieldOffset(0)]
            [NativeTypeName("CMP_DWORD *")]
            public uint* m_pdwData;

            //[FieldOffset(0)]
            //[NativeTypeName("CMP_VEC8 *")]
            //public vector<byte>* m_pvec8Data;
        }
    }
}
