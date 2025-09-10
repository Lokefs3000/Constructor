using System.Runtime.InteropServices;

namespace Editor.Interop.Compressonator
{
    public static unsafe partial class Compressonator
    {
        [NativeTypeName("const uint8_t[4]")]
        public static readonly byte[] BRLG_FILE_IDENTIFIER = new byte[4]
        {
            (byte)'B',
            (byte)'R',
            (byte)'L',
            (byte)'G',
        };

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("CMP_INT")]
        public static extern int CMP_MaxFacesOrSlices([NativeTypeName("const CMP_MipSet *")] CMP_MipSet* pMipSet, [NativeTypeName("CMP_INT")] int nMipLevel);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("BC_ERROR")]
        public static extern _BC_ERROR CMP_InitializeBCLibrary();

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("BC_ERROR")]
        public static extern _BC_ERROR CMP_ShutdownBCLibrary();

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("BC_ERROR")]
        public static extern _BC_ERROR CMP_CreateBC6HEncoder(CMP_BC6H_BLOCK_PARAMETERS user_settings, BC6HBlockEncoder** encoder);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("BC_ERROR")]
        public static extern _BC_ERROR CMP_CreateBC7Encoder(double quality, [NativeTypeName("CMP_BOOL")] byte restrictColour, [NativeTypeName("CMP_BOOL")] byte restrictAlpha, [NativeTypeName("CMP_DWORD")] uint modeMask, double performance, BC7BlockEncoder** encoder);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("BC_ERROR")]
        public static extern _BC_ERROR CMP_EncodeBC7Block(BC7BlockEncoder* encoder, [NativeTypeName("double[16][4]")] double** @in, [NativeTypeName("CMP_BYTE *")] byte* @out);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("BC_ERROR")]
        public static extern _BC_ERROR CMP_EncodeBC6HBlock(BC6HBlockEncoder* encoder, [NativeTypeName("CMP_FLOAT[16][4]")] float** @in, [NativeTypeName("CMP_BYTE *")] byte* @out);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("BC_ERROR")]
        public static extern _BC_ERROR CMP_DecodeBC6HBlock([NativeTypeName("CMP_BYTE *")] byte* @in, [NativeTypeName("CMP_FLOAT[16][4]")] float** @out);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("BC_ERROR")]
        public static extern _BC_ERROR CMP_DecodeBC7Block([NativeTypeName("CMP_BYTE *")] byte* @in, [NativeTypeName("double[16][4]")] double** @out);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("BC_ERROR")]
        public static extern _BC_ERROR CMP_DestroyBC6HEncoder(BC6HBlockEncoder* encoder);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("BC_ERROR")]
        public static extern _BC_ERROR CMP_DestroyBC7Encoder(BC7BlockEncoder* encoder);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("CMP_DWORD")]
        public static extern uint CMP_CalculateBufferSize([NativeTypeName("const CMP_Texture *")] CMP_Texture* pTexture);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_ConvertTexture(CMP_Texture* pSourceTexture, CMP_Texture* pDestTexture, [NativeTypeName("const CMP_CompressOptions *")] CMP_CompressOptions* pOptions, [NativeTypeName("CMP_Feedback_Proc")] delegate* unmanaged[Cdecl]<float, nuint, nuint, byte> pFeedbackProc);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("CMP_INT")]
        public static extern int CMP_CalcMaxMipLevel([NativeTypeName("CMP_INT")] int nHeight, [NativeTypeName("CMP_INT")] int nWidth, [NativeTypeName("CMP_BOOL")] byte bForGPU);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("CMP_INT")]
        public static extern int CMP_CalcMinMipSize([NativeTypeName("CMP_INT")] int nHeight, [NativeTypeName("CMP_INT")] int nWidth, [NativeTypeName("CMP_INT")] int MipsLevel);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("CMP_INT")]
        public static extern int CMP_GenerateMIPLevelsEx(CMP_MipSet* pMipSet, CMP_CFilterParams* pCFilterParams);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("CMP_INT")]
        public static extern int CMP_GenerateMIPLevels(CMP_MipSet* pMipSet, [NativeTypeName("CMP_INT")] int nMinSize);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_CreateCompressMipSet(CMP_MipSet* pMipSetCMP, CMP_MipSet* pMipSetSRC);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_CreateMipSet(CMP_MipSet* pMipSet, [NativeTypeName("CMP_INT")] int nWidth, [NativeTypeName("CMP_INT")] int nHeight, [NativeTypeName("CMP_INT")] int nDepth, [NativeTypeName("ChannelFormat")] CMP_ChannelFormat channelFormat, [NativeTypeName("TextureType")] CMP_TextureType textureType);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("CMP_UINT")]
        public static extern uint CMP_getFormat_nChannels(CMP_FORMAT format);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_MipSetAnlaysis(CMP_MipSet* src1, CMP_MipSet* src2, [NativeTypeName("CMP_INT")] int nMipLevel, [NativeTypeName("CMP_INT")] int nFaceOrSlice, CMP_AnalysisData* pAnalysisData);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_ConvertMipTexture(CMP_MipSet* p_MipSetIn, CMP_MipSet* p_MipSetOut, [NativeTypeName("const CMP_CompressOptions *")] CMP_CompressOptions* pOptions, [NativeTypeName("CMP_Feedback_Proc")] delegate* unmanaged[Cdecl]<float, nuint, nuint, byte> pFeedbackProc);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_LoadTexture([NativeTypeName("const char *")] sbyte* sourceFile, CMP_MipSet* pMipSet);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_SaveTexture([NativeTypeName("const char *")] sbyte* destFile, CMP_MipSet* pMipSet);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_ProcessTexture(CMP_MipSet* srcMipSet, CMP_MipSet* dstMipSet, KernelOptions kernelOptions, [NativeTypeName("CMP_Feedback_Proc")] delegate* unmanaged[Cdecl]<float, nuint, nuint, byte> pFeedbackProc);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_CompressTexture(KernelOptions* options, CMP_MipSet srcMipSet, CMP_MipSet dstMipSet, [NativeTypeName("CMP_Feedback_Proc")] delegate* unmanaged[Cdecl]<float, nuint, nuint, byte> pFeedback);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("CMP_VOID")]
        public static extern void CMP_Format2FourCC(CMP_FORMAT format, CMP_MipSet* pMipSet);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_FORMAT CMP_ParseFormat([NativeTypeName("char *")] sbyte* pFormat);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("CMP_INT")]
        public static extern int CMP_NumberOfProcessors();

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("CMP_VOID")]
        public static extern void CMP_FreeMipSet(CMP_MipSet* MipSetIn);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("CMP_VOID")]
        public static extern void CMP_GetMipLevel(CMP_MipLevel** data, [NativeTypeName("const CMP_MipSet *")] CMP_MipSet* pMipSet, [NativeTypeName("CMP_INT")] int nMipLevel, [NativeTypeName("CMP_INT")] int nFaceOrSlice);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_GetPerformanceStats(KernelPerformanceStats* pPerfStats);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_GetDeviceInfo(KernelDeviceInfo* pDeviceInfo);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("CMP_BOOL")]
        public static extern byte CMP_IsCompressedFormat(CMP_FORMAT format);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("CMP_BOOL")]
        public static extern byte CMP_IsFloatFormat(CMP_FORMAT InFormat);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_CreateComputeLibrary(CMP_MipSet* srcTexture, KernelOptions* kernelOptions, void* Reserved);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_DestroyComputeLibrary([NativeTypeName("CMP_BOOL")] byte forceClose);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_SetComputeOptions(ComputeOptions* options);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_CreateBlockEncoder(void** blockEncoder, CMP_EncoderSetting encodeSettings);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_CompressBlock(void** blockEncoder, void* srcBlock, [NativeTypeName("unsigned int")] uint sourceStride, void* dstBlock, [NativeTypeName("unsigned int")] uint dstStride);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern CMP_ERROR CMP_CompressBlockXY(void** blockEncoder, [NativeTypeName("unsigned int")] uint blockx, [NativeTypeName("unsigned int")] uint blocky, void* imgSrc, [NativeTypeName("unsigned int")] uint sourceStride, void* cmpDst, [NativeTypeName("unsigned int")] uint dstStride);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CMP_DestroyBlockEncoder(void** blockEncoder);

        [DllImport("Compressonator_MT_DLL.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CMP_InitFramework();
    }
}
