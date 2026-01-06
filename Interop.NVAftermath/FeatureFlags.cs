using System;
using System.Collections.Generic;
using System.Text;

namespace Interop.NVAftermath
{
    public enum AftermathFeatureFlags
    {
        /// <summary>
        /// The minimal flag only allows use of the 'GFSDK_Aftermath_GetDeviceStatus'
        /// entry point and GPU crash dump generation with basic information about the
        /// GPU fault.
        /// </summary>
        Minimum = 0x00000000,

        /// <summary>
        /// This flag enables support for DX Aftermath event markers, including both
        /// the support for user markers that are explicitly added by the application
        /// via 'GFSDK_Aftermath_SetEventMarker' and automatic call stack markers
        /// controlled by 'GFSDK_Aftermath_FeatureFlags_CallStackCapturing'.
        ///
        /// For Vulkan, the event marker (checkpoints) feature is enabled through the
        /// 'VK_NV_device_diagnostic_checkpoints' extension.
        ///
        /// NOTE: Using event markers should be considered carefully as they can cause
        /// very high CPU overhead when used in high frequency code paths. Due to the
        /// inherent overhead, event markers should be used only for debugging purposes on
        /// development or QA systems. Therefore, on some driver versions, Aftermath
        /// event marker tracking on DX11 and DX12 is only available if the Nsight
        /// Aftermath GPU Crash Dump Monitor is running on the system. This requirement
        /// applies to the following driver versions:
        /// - DX12: R495 to R530 (inclusive).
        /// - DX12 32-bit (x86): R495 to R590 (inclusive).
        /// - DX11: R495 and later.
        /// No Aftermath configuration needs to be made in the Monitor. It serves
        /// only as a dongle to ensure Aftermath event markers do not impact application
        /// performance on end user systems. That means this flag will be ignored if the
        /// monitor process is not detected.
        /// </summary>
        EnableMarkers = 0x00000001,

        /// <summary>
        /// With this flag set, live and recently destroyed resources are tracked by the
        /// display driver. In case of a page fault that information will be used to
        /// identify possible candidates of deleted resources that correspond to the fault
        /// address. Information about the most likely resource related to the fault will
        /// be included in the page fault data, including, for example, information about
        /// the size of the resource, its format, and the epoch time stamp when it was
        /// deleted.
        ///
        /// The corresponding feature configuration flag for Vulkan is
        /// 'VK_DEVICE_DIAGNOSTICS_CONFIG_ENABLE_RESOURCE_TRACKING_BIT_NV'.
        ///
        /// NOTE: Enabling this feature will incur memory overhead due to the additional
        /// tracking data managed by the display driver as well as CPU overhead for each
        /// resource creation and destruction.
        /// </summary>
        EnableResourceTracking = 0x00000002,

        /// <summary>
        /// With this flag set, event markers are automatically set for all draw calls,
        /// compute dispatches and copy operations to capture the CPU call stack for the
        /// corresponding API call as the event marker payload.
        ///
        /// The corresponding feature configuration flag for Vulkan is
        /// 'VK_DEVICE_DIAGNOSTICS_CONFIG_ENABLE_AUTOMATIC_CHECKPOINTS_BIT_NV'.
        ///
        /// NOTE: Requires also 'GFSDK_Aftermath_FeatureFlags_EnableMarkers' to be set.
        ///
        /// NOTE: Enabling this feature will cause very high CPU overhead during command
        /// list recording. Due to the inherent overhead, call stack capturing should only
        /// be used for debugging purposes on development or QA systems and should not be
        /// enabled in applications shipped to customers. Therefore, on R495+ drivers,
        /// call stack capturing on DX11 and DX12 is only available if the Nsight Aftermath
        /// GPU Crash Dump Monitor is running on the system. No Aftermath configuration
        /// needs to be made in the Monitor. It serves only as a dongle to ensure call
        /// stack capturing does not impact application performance on end user systems.
        /// That means this flag will be ignored if the monitor process is not detected.
        ///
        /// NOTE: When enabling this feature, Aftermath GPU crash dumps will include file
        /// paths to the crashing application's executable as well as all DLLs it has loaded.
        /// </summary>
        CallStackCapturing = 0x40000000,

        /// <summary>
        /// With this flag set, debug information (line tables for mapping from the shader
        /// IL passed to the driver to the shader microcode) for all shaders is generated
        /// by the display driver.
        ///
        /// The corresponding feature configuration flag for Vulkan is
        /// 'VK_DEVICE_DIAGNOSTICS_CONFIG_ENABLE_SHADER_DEBUG_INFO_BIT_NV'.
        ///
        /// NOTE: Using this feature should be considered carefully. It may cause
        /// considerable shader compilation overhead and additional overhead for handling
        /// the corresponding shader debug information callbacks (if provided to
        /// 'GFSDK_Aftermath_EnableGpuCrashDumps').
        ///
        /// NOTE: shader debug information is only supported for DX12 applications using
        /// shaders compiled as DXIL. This flag has no effect on DX11 applications.
        /// </summary>
        GenerateShaderDebugInfo = 0x00000008,

        /// <summary>
        /// If this flag is set, the GPU will run in a mode that allows to capture runtime
        /// errors in shaders that are not caught with default driver settings. This may
        /// provide additional information for debugging GPU hangs, GPU crashes or other
        /// unexpected behavior related to shader execution.
        ///
        /// The corresponding feature configuration flag for Vulkan is
        /// 'VK_DEVICE_DIAGNOSTICS_CONFIG_ENABLE_SHADER_ERROR_REPORTING_BIT_NV'.
        ///
        /// NOTE: Enabling this feature does not cause any performance overhead, but it
        /// may result in additional crash dumps being generated to report issues in
        /// shaders that exhibit undefined behavior or have hidden bugs, which so far went
        /// unnoticed, because with default driver settings the HW silently ignores them.
        ///
        /// NOTE: This feature is only supported on R515 or later drivers. The feature
        /// flag will be ignored on earlier driver versions.
        ///
        /// Examples for problems that are caught when this feature is enabled:
        ///
        /// o) Accessing memory using misaligned addresses, such as reading or
        ///    writing a byte address that is not a multiple of the access size.
        ///
        /// o) Accessing memory out-of-bounds, such as reading or writing beyond the
        ///    declared bounds of (group) shared or thread local memory or reading from an
        ///    out-of-bounds constant buffer address.
        ///
        /// o) Hitting call stack limits.
        /// </summary>
        EnableShaderErrorReporting = 0x00000010,
    }
}
