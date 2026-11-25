using System;
using System.Collections.Generic;
using System.Text;
using TerraFX.Interop.Windows;

namespace Primary.RHI.Direct3D12
{
    public class D3D12RHIException : RHIException
    {
        public D3D12RHIException()
        {
        }

        public D3D12RHIException(string? message) : base(message)
        {
        }

        public D3D12RHIException(string? message, int code) : base($"{message}\r\n{GetNameForCode(code)} (0x{code:x8})")
        {
        }

        private static string? GetNameForCode(int code) => code switch
        {
            DXGI_ERROR_ACCESS_DENIED => "DXGI Error: Access denied",
            DXGI_ERROR_ACCESS_LOST => "DXGI Error: Access lost",
            DXGI_ERROR_ALREADY_EXISTS => "DXGI Error: Already exists",
            DXGI_ERROR_CANNOT_PROTECT_CONTENT => "DXGI Error: Cannot protect content",
            DXGI_ERROR_DEVICE_HUNG => "DXGI Error: Device hung",
            DXGI_ERROR_DEVICE_REMOVED => "DXGI Error: Device removed",
            DXGI_ERROR_DEVICE_RESET => "DXGI Error: Device reset",
            DXGI_ERROR_DRIVER_INTERNAL_ERROR => "DXGI Error: Driver internal error",
            DXGI_ERROR_FRAME_STATISTICS_DISJOINT => "DXGI Error: Frame statistics disjoint",
            DXGI_ERROR_GRAPHICS_VIDPN_SOURCE_IN_USE => "DXGI Error: Graphics VIDPN source in use",
            DXGI_ERROR_INVALID_CALL => "DXGI Error: Invalid call",
            DXGI_ERROR_MORE_DATA => "DXGI Error: More data",
            DXGI_ERROR_NAME_ALREADY_EXISTS => "DXGI Error: Name already exists",
            DXGI_ERROR_NONEXCLUSIVE => "DXGI Error: Nonexclusive",
            DXGI_ERROR_NOT_CURRENTLY_AVAILABLE => "DXGI Error: Not currently available",
            DXGI_ERROR_NOT_FOUND => "DXGI Error: Not found",
            DXGI_ERROR_RESTRICT_TO_OUTPUT_STALE => "DXGI Error: Restrict to output stale",
            DXGI_ERROR_SDK_COMPONENT_MISSING => "DXGI Error: SDK component missing",
            DXGI_ERROR_SESSION_DISCONNECTED => "DXGI Error: Session disconnected",
            DXGI_ERROR_UNSUPPORTED => "DXGI Error: Unsupported",
            DXGI_ERROR_WAIT_TIMEOUT => "DXGI Error: Wait timeout",
            DXGI_ERROR_WAS_STILL_DRAWING => "DXGI Error: Was still drawing",
            _ => null
        };

        private const int DXGI_ERROR_ACCESS_DENIED = unchecked((int)0x887A002B);
        private const int DXGI_ERROR_ACCESS_LOST = unchecked((int)0x887A0026);
        private const int DXGI_ERROR_ALREADY_EXISTS = unchecked((int)0x887A0036L);
        private const int DXGI_ERROR_CANNOT_PROTECT_CONTENT = unchecked((int)0x887A002A);
        private const int DXGI_ERROR_DEVICE_HUNG = unchecked((int)0x887A0006);
        private const int DXGI_ERROR_DEVICE_REMOVED = unchecked((int)0x887A0005);
        private const int DXGI_ERROR_DEVICE_RESET = unchecked((int)0x887A0007);
        private const int DXGI_ERROR_DRIVER_INTERNAL_ERROR = unchecked((int)0x887A0020);
        private const int DXGI_ERROR_FRAME_STATISTICS_DISJOINT = unchecked((int)0x887A000B);
        private const int DXGI_ERROR_GRAPHICS_VIDPN_SOURCE_IN_USE = unchecked((int)0x887A000C);
        private const int DXGI_ERROR_INVALID_CALL = unchecked((int)0x887A0001);
        private const int DXGI_ERROR_MORE_DATA = unchecked((int)0x887A0003);
        private const int DXGI_ERROR_NAME_ALREADY_EXISTS = unchecked((int)0x887A002C);
        private const int DXGI_ERROR_NONEXCLUSIVE = unchecked((int)0x887A0021);
        private const int DXGI_ERROR_NOT_CURRENTLY_AVAILABLE = unchecked((int)0x887A0022);
        private const int DXGI_ERROR_NOT_FOUND = unchecked((int)0x887A0002);
        private const int DXGI_ERROR_RESTRICT_TO_OUTPUT_STALE = unchecked((int)0x887A0029);
        private const int DXGI_ERROR_SDK_COMPONENT_MISSING = unchecked((int)0x887A002D);
        private const int DXGI_ERROR_SESSION_DISCONNECTED = unchecked((int)0x887A0028);
        private const int DXGI_ERROR_UNSUPPORTED = unchecked((int)0x887A0004);
        private const int DXGI_ERROR_WAIT_TIMEOUT = unchecked((int)0x887A0027);
        private const int DXGI_ERROR_WAS_STILL_DRAWING = unchecked((int)0x887A000A);

        private const int E_FAIL = unchecked((int)0x80004005);
        private const int E_INVALIDARG = unchecked((int)0x80070057);
        private const int E_OUTOFMEMORY = unchecked((int)0x8007000E);
        private const int E_NOTIMPL = unchecked((int)0x80004001);
    }
}
