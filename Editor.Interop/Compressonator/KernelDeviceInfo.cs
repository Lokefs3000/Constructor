using System.Runtime.CompilerServices;

namespace Editor.Interop.Compressonator
{
    public partial struct KernelDeviceInfo
    {
        [NativeTypeName("CMP_CHAR[256]")]
        public _m_deviceName_e__FixedBuffer m_deviceName;

        [NativeTypeName("CMP_CHAR[128]")]
        public _m_version_e__FixedBuffer m_version;

        [NativeTypeName("CMP_INT")]
        public int m_maxUCores;

        [InlineArray(256)]
        public partial struct _m_deviceName_e__FixedBuffer
        {
            public sbyte e0;
        }

        [InlineArray(128)]
        public partial struct _m_version_e__FixedBuffer
        {
            public sbyte e0;
        }
    }
}
