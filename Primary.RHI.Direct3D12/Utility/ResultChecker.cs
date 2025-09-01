using SharpGen.Runtime;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Primary.RHI.Direct3D12.Utility
{
    internal static class ResultChecker
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public static void ThrowIfUnhandled(Result result, GraphicsDeviceImpl? device = null)
        {
            if (result.Failure)
                throw new RHIException(GetErrorString(result), device);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PrintIfUnhandled(Result result, GraphicsDeviceImpl? device = null, bool includeStacktrace = true)
        {
            bool failure = result.Failure;
            if (failure)
                TryFailFast(result, device);
            return !failure;

#if !DEBUG
            [DebuggerHidden]
#endif
            static void TryFailFast(Result result, GraphicsDeviceImpl? device = null)
            {
                try
                {
                    throw new RHIException("An api returned an invalid value.", device);
                }
                catch (RHIException ex)
                {
                    GraphicsDeviceImpl.Logger.Error(ex, GetErrorString(result));
                }
            }
        }

        public static string GetErrorString(Result result)
        {
            return result.Code switch
            {
                D3D12ErrorAdapterNotFound => "Adapter not found",
                D3D12ErrorDriverVersionMismatch => "Driver version mismatch",
                EFail => "Unspecified failure",
                EInvalidArg => "Invalid argument",
                EOutOfMemory => "Out of memory",
                ENotImpl => "Not implemented",
                DXGIAccessDenied => "Access denied",
                DXGIAccessLost => "Access lost",
                DXGIAlreadyExists => "Already exists",
                DXGICannotProtectContent => "Cannot protect content",
                DXGIDeviceHung => "Device hung",
                DXGIDeviceRemoved => "Device removed",
                DXGIDeviceReset => "Device reset",
                DXGIDriverInternalError => "Driver internal error",
                DXGIFrameStatisticsDisjoint => "Frame statistics disjoint",
                DXGIGraphicsVIDPNSourceInUse => "Graphics VIDPN source in use",
                DXGIInvalidCall => "Invalid call",
                DXGIMoreData => "More data",
                DXGINameAlreadyExists => "Name already exists",
                DXGINonExclusive => "Non exclusive",
                DXGINotCurrentlyAvailable => "Not currently available",
                DXGINotFound => "Not found",
                DXGIRestrictOutputStale => "Restrict output stale",
                DXGISDKComponentMissing => "SDK component missing",
                DXGIUnsupported => "Unsupported",
                DXGIWaitTimeout => "Wait timeout",
                DXGIWasStillDrawing => "Was still drawing",
                _ => result.Description
            };
        }

        public const int D3D12ErrorAdapterNotFound = unchecked((int)0x887E0001);
        public const int D3D12ErrorDriverVersionMismatch = unchecked((int)0x887E0002);

        public const int EFail = unchecked((int)0x80004005);
        public const int EInvalidArg = unchecked((int)0x80070057);
        public const int EOutOfMemory = unchecked((int)0x8007000E);
        public const int ENotImpl = unchecked((int)0x80004001);

        public const int DXGIAccessDenied = unchecked((int)0x887A002B);
        public const int DXGIAccessLost = unchecked((int)0x887A0026);
        public const int DXGIAlreadyExists = unchecked((int)0x887A0036L);
        public const int DXGICannotProtectContent = unchecked((int)0x887A002A);
        public const int DXGIDeviceHung = unchecked((int)0x887A0006);
        public const int DXGIDeviceRemoved = unchecked((int)0x887A0005);
        public const int DXGIDeviceReset = unchecked((int)0x887A0007);
        public const int DXGIDriverInternalError = unchecked((int)0x887A0020);
        public const int DXGIFrameStatisticsDisjoint = unchecked((int)0x887A000B);
        public const int DXGIGraphicsVIDPNSourceInUse = unchecked((int)0x887A000C);
        public const int DXGIInvalidCall = unchecked((int)0x887A0001);
        public const int DXGIMoreData = unchecked((int)0x887A0003);
        public const int DXGINameAlreadyExists = unchecked((int)0x887A002C);
        public const int DXGINonExclusive = unchecked((int)0x887A0021);
        public const int DXGINotCurrentlyAvailable = unchecked((int)0x887A0022);
        public const int DXGINotFound = unchecked((int)0x887A0002);
        public const int DXGIRestrictOutputStale = unchecked((int)0x887A0029);
        public const int DXGISDKComponentMissing = unchecked((int)0x887A002D);
        public const int DXGIUnsupported = unchecked((int)0x887A0004);
        public const int DXGIWaitTimeout = unchecked((int)0x887A0027);
        public const int DXGIWasStillDrawing = unchecked((int)0x887A000A);
    }

    internal sealed class RHIException : Exception
    {
        internal RHIException(string message, GraphicsDeviceImpl? device) : base(message)
        {
            device?.DumpMessageQueue();
        }
    }
}
