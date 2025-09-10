namespace Editor.Interop.Compressonator
{
    public partial struct KernelPerformanceStats
    {
        [NativeTypeName("CMP_FLOAT")]
        public float m_computeShaderElapsedMS;

        [NativeTypeName("CMP_INT")]
        public int m_num_blocks;

        [NativeTypeName("CMP_FLOAT")]
        public float m_CmpMTxPerSec;
    }
}
