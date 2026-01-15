namespace Primary.Rendering
{
    internal sealed class RenderPassErrorReporter
    {
        internal void ReportError(RPErrorSource source, RPErrorType type, string? resourceName)
        {
            EngLog.Render.Error("[{src}]: {type} - {res}", source, type, resourceName);
            throw new Exception();
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

        SetRenderFunction,

        ClearRenderTarget,
        ClearDepthStencil,

        SetVertexBuffer,
        SetIndexBuffer,

        MapBuffer,
        MapTexture,

        UploadBuffer,
        UploadTexture,

        CopyBuffer,
        CopyTexture,

        SetConstants,

        PresentOnWindow,

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
        NoResourceAccess,
        OutOfRange,
        ResourceTooSmall,
        InvalidResourceType,
        InvalidSubresource,
        NotEnoughDataSupplied,
        PassDataTypeMismatch
    }
}
