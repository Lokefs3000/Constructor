using System.Runtime.CompilerServices;

namespace Editor.Interop.Compressonator
{
    public partial struct AMD_CMD_SET
    {
        [NativeTypeName("CMP_CHAR[32]")]
        public _strCommand_e__FixedBuffer strCommand;

        [NativeTypeName("CMP_CHAR[16]")]
        public _strParameter_e__FixedBuffer strParameter;

        [InlineArray(32)]
        public partial struct _strCommand_e__FixedBuffer
        {
            public sbyte e0;
        }

        [InlineArray(16)]
        public partial struct _strParameter_e__FixedBuffer
        {
            public sbyte e0;
        }
    }
}
