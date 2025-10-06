using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Editor.Interop.Compressonator
{
    public unsafe partial struct KernelOptions
    {
        [NativeTypeName("CMP_ComputeExtensions")]
        public CMPComputeExtensions Extensions;

        [NativeTypeName("CMP_DWORD")]
        public uint height;

        [NativeTypeName("CMP_DWORD")]
        public uint width;

        [NativeTypeName("CMP_FLOAT")]
        public float fquality;

        public CMP_FORMAT format;

        public CMP_FORMAT srcformat;

        public CMP_Compute_type encodeWith;

        [NativeTypeName("CMP_INT")]
        public int threads;

        [NativeTypeName("CMP_BOOL")]
        public byte getPerfStats;

        public KernelPerformanceStats perfStats;

        [NativeTypeName("CMP_BOOL")]
        public byte getDeviceInfo;

        public KernelDeviceInfo deviceInfo;

        [NativeTypeName("CMP_BOOL")]
        public byte genGPUMipMaps;

        [NativeTypeName("CMP_INT")]
        public int miplevels;

        [NativeTypeName("CMP_BOOL")]
        public byte useSRGBFrames;

        [NativeTypeName("__AnonymousRecord_compressonator_L312_C5")]
        public _Anonymous_e__Union Anonymous;

        [NativeTypeName("CMP_UINT")]
        public uint size;

        public void* data;

        public void* dataSVM;

        [NativeTypeName("char *")]
        public sbyte* srcfile;

        [UnscopedRef]
        public Span<byte> encodeoptions
        {
            get
            {
                return Anonymous.encodeoptions;
            }
        }

        [UnscopedRef]
        public ref _Anonymous_e__Union._bc15_e__Struct bc15
        {
            get
            {
                return ref Anonymous.bc15;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("CMP_BYTE[32]")]
            public _encodeoptions_e__FixedBuffer encodeoptions;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_compressonator_L317_C9")]
            public _bc15_e__Struct bc15;

            public partial struct _bc15_e__Struct
            {
                [NativeTypeName("CMP_BOOL")]
                public byte useChannelWeights;

                [NativeTypeName("CMP_FLOAT[3]")]
                public _channelWeights_e__FixedBuffer channelWeights;

                [NativeTypeName("CMP_BOOL")]
                public byte useAdaptiveWeights;

                [NativeTypeName("CMP_BOOL")]
                public byte useAlphaThreshold;

                [NativeTypeName("CMP_INT")]
                public int alphaThreshold;

                [NativeTypeName("CMP_BOOL")]
                public byte useRefinementSteps;

                [NativeTypeName("CMP_INT")]
                public int refinementSteps;

                [InlineArray(3)]
                public partial struct _channelWeights_e__FixedBuffer
                {
                    public float e0;
                }
            }

            [InlineArray(32)]
            public partial struct _encodeoptions_e__FixedBuffer
            {
                public byte e0;
            }
        }
    }
}
