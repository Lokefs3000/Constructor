using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2
{
    internal sealed class RenderPassErrorReporter
    {
        internal void ReportError(RPErrorSource source, RPErrorType type)
        {

        }
    }

    public enum RPErrorSource : byte
    {
        Undefined = 0,

        CreateTexture,
        CreateBuffer,

        UseResource,
        UseDepthStencil,
        UseRenderTarget,

        SetRenderTarget,
        SetDepthStencil,

        ClearRenderTarget,
        ClearDepthStencil,

        SetVertexBuffer,
        SetIndexBuffer,
    }

    public enum RPErrorType : byte
    {
        Undefined = 0,

        IncompatibleFormat,
        MissingUsageFlag,
        IncompatibleUsage,
        InvalidDimension,
        InvalidSize,
        StrideTooLarge,
        InvalidStride,
        NoShaderAccess,
        InvalidUsage,
        InvalidOutput,
        SlotOutOfRange,
        NoResourceAccess
    }
}
