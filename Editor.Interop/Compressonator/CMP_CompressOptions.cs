using System.Runtime.CompilerServices;

namespace Editor.Interop.Compressonator
{
    public unsafe partial struct CMP_CompressOptions
    {
        [NativeTypeName("CMP_DWORD")]
        public uint dwSize;

        [NativeTypeName("bool")]
        public byte doPreconditionBRLG;

        [NativeTypeName("bool")]
        public byte doDeltaEncodeBRLG;

        [NativeTypeName("bool")]
        public byte doSwizzleBRLG;

        [NativeTypeName("CMP_DWORD")]
        public uint dwPageSize;

        [NativeTypeName("CMP_BOOL")]
        public byte bUseRefinementSteps;

        [NativeTypeName("CMP_INT")]
        public int nRefinementSteps;

        [NativeTypeName("CMP_BOOL")]
        public byte bUseChannelWeighting;

        [NativeTypeName("CMP_FLOAT")]
        public float fWeightingRed;

        [NativeTypeName("CMP_FLOAT")]
        public float fWeightingGreen;

        [NativeTypeName("CMP_FLOAT")]
        public float fWeightingBlue;

        [NativeTypeName("CMP_BOOL")]
        public byte bUseAdaptiveWeighting;

        [NativeTypeName("CMP_BOOL")]
        public byte bDXT1UseAlpha;

        [NativeTypeName("CMP_BOOL")]
        public byte bUseGPUDecompress;

        [NativeTypeName("CMP_BOOL")]
        public byte bUseCGCompress;

        [NativeTypeName("CMP_BYTE")]
        public byte nAlphaThreshold;

        [NativeTypeName("CMP_BOOL")]
        public byte bDisableMultiThreading;

        public CMP_Speed nCompressionSpeed;

        public CMP_GPUDecode nGPUDecode;

        public CMP_Compute_type nEncodeWith;

        [NativeTypeName("CMP_DWORD")]
        public uint dwnumThreads;

        [NativeTypeName("CMP_FLOAT")]
        public float fquality;

        [NativeTypeName("CMP_BOOL")]
        public byte brestrictColour;

        [NativeTypeName("CMP_BOOL")]
        public byte brestrictAlpha;

        [NativeTypeName("CMP_DWORD")]
        public uint dwmodeMask;

        public int NumCmds;

        [NativeTypeName("AMD_CMD_SET[20]")]
        public _CmdSet_e__FixedBuffer CmdSet;

        [NativeTypeName("CMP_FLOAT")]
        public float fInputDefog;

        [NativeTypeName("CMP_FLOAT")]
        public float fInputExposure;

        [NativeTypeName("CMP_FLOAT")]
        public float fInputKneeLow;

        [NativeTypeName("CMP_FLOAT")]
        public float fInputKneeHigh;

        [NativeTypeName("CMP_FLOAT")]
        public float fInputGamma;

        [NativeTypeName("CMP_FLOAT")]
        public float fInputFilterGamma;

        [NativeTypeName("CMP_INT")]
        public int iCmpLevel;

        [NativeTypeName("CMP_INT")]
        public int iPosBits;

        [NativeTypeName("CMP_INT")]
        public int iTexCBits;

        [NativeTypeName("CMP_INT")]
        public int iNormalBits;

        [NativeTypeName("CMP_INT")]
        public int iGenericBits;

        [NativeTypeName("CMP_INT")]
        public int iVcacheSize;

        [NativeTypeName("CMP_INT")]
        public int iVcacheFIFOSize;

        [NativeTypeName("CMP_FLOAT")]
        public float fOverdrawACMR;

        [NativeTypeName("CMP_INT")]
        public int iSimplifyLOD;

        [NativeTypeName("bool")]
        public byte bVertexFetch;

        public CMP_FORMAT SourceFormat;

        public CMP_FORMAT DestFormat;

        [NativeTypeName("CMP_BOOL")]
        public byte format_support_hostEncoder;

        [NativeTypeName("CMP_PrintInfoStr")]
        public delegate* unmanaged[Cdecl]<sbyte*, void> m_PrintInfoStr;

        [NativeTypeName("CMP_BOOL")]
        public byte getPerfStats;

        public KernelPerformanceStats perfStats;

        [NativeTypeName("CMP_BOOL")]
        public byte getDeviceInfo;

        public KernelDeviceInfo deviceInfo;

        [NativeTypeName("CMP_BOOL")]
        public byte genGPUMipMaps;

        [NativeTypeName("CMP_BOOL")]
        public byte useSRGBFrames;

        [NativeTypeName("CMP_INT")]
        public int miplevels;

        [InlineArray(20)]
        public partial struct _CmdSet_e__FixedBuffer
        {
            public AMD_CMD_SET e0;
        }
    }
}
