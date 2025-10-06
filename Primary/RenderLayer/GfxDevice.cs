using Primary.Common;
using Primary.Rendering;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Primary.RenderLayer
{
    public record struct GfxDevice : IDisposable
    {
        private RHI.GraphicsDevice? _internal;

        public GfxDevice() => throw new NotSupportedException();
        internal GfxDevice(RHI.GraphicsDevice? device) => _internal = device;

        public void Dispose() => _internal?.Dispose();

        #region Base

        public void SynchronizeDevice(RHI.SynchronizeDeviceTargets targets) => NullableUtility.ThrowIfNull(_internal).SynchronizeDevice(targets);

        public void BeginFrame() => NullableUtility.ThrowIfNull(_internal).BeginFrame();
        public void FinishFrame() => NullableUtility.ThrowIfNull(_internal).FinishFrame();

        public void Submit(GfxCommandBuffer commandBuffer) => NullableUtility.ThrowIfNull(_internal).Submit(NullableUtility.ThrowIfNull(commandBuffer.RHICommandBuffer));

        public GfxSwapChain CreateSwapChain(Vector2 clientSize, nint windowHandle) => NullableUtility.ThrowIfNull(_internal).CreateSwapChain(clientSize, windowHandle);
        public GfxBuffer CreateBuffer(RHI.BufferDescription description, nint bufferData) => NullableUtility.ThrowIfNull(_internal).CreateBuffer(description, bufferData);
        public GfxTexture CreateTexture(RHI.TextureDescription description, Span<nint> textureData) => NullableUtility.ThrowIfNull(_internal).CreateTexture(description, textureData);
        public GfxPipelineLibrary CreatePipelineLibrary(Span<byte> initialData) => NullableUtility.ThrowIfNull(_internal).CreatePipelineLibrary(initialData);
        public GfxGraphicsPipeline CreateGraphicsPipeline(RHI.GraphicsPipelineDescription description, RHI.GraphicsPipelineBytecode bytecode) => NullableUtility.ThrowIfNull(_internal).CreateGraphicsPipeline(description, bytecode);
        public GfxCopyCommandBuffer CreateCopyCommandBuffer() => NullableUtility.ThrowIfNull(_internal).CreateCopyCommandBuffer();
        public GfxGraphicsCommandBuffer CreateGraphicsCommandBuffer() => NullableUtility.ThrowIfNull(_internal).CreateGraphicsCommandBuffer();
        public GfxRenderTarget CreateRenderTarget(RHI.RenderTargetDescription description) => NullableUtility.ThrowIfNull(_internal).CreateRenderTarget(description);
        public GfxFence CreateFence(ulong initialValue = 0) => NullableUtility.ThrowIfNull(_internal).CreateFence(initialValue);

        public bool IsSupported(RHI.RenderTargetFormat format) => NullableUtility.ThrowIfNull(_internal).IsSupported(format);
        public bool IsSupported(RHI.DepthStencilFormat format) => NullableUtility.ThrowIfNull(_internal).IsSupported(format);
        public bool IsSupported(RHI.TextureFormat format, RHI.TextureDimension dimension) => NullableUtility.ThrowIfNull(_internal).IsSupported(format, dimension);

        #endregion

        #region Extensions

        public GfxBuffer CreateBuffer(RHI.BufferDescription description) => NullableUtility.ThrowIfNull(_internal).CreateBuffer(description, nint.Zero);
        public unsafe GfxBuffer CreateBuffer<T>(RHI.BufferDescription description, Span<T> data) where T : unmanaged
        {
            fixed (T* ptr = data)
            {
                return NullableUtility.ThrowIfNull(_internal).CreateBuffer(description, (nint)ptr);
            }
        }
        public unsafe GfxBuffer CreateBuffer<T>(RHI.BufferDescription description, T data) where T : unmanaged => NullableUtility.ThrowIfNull(_internal).CreateBuffer(description, (nint)(&data));
        public unsafe GfxBuffer CreateBuffer<T>(RHI.BufferDescription description, ref T data) where T : unmanaged => NullableUtility.ThrowIfNull(_internal).CreateBuffer(description, (nint)Unsafe.AsPointer(ref data));

        public GfxTexture CreateTexture(RHI.TextureDescription description) => NullableUtility.ThrowIfNull(_internal).CreateTexture(description, Span<nint>.Empty);
        public unsafe GfxTexture CreateTexture<T>(RHI.TextureDescription description, Span<T> subresource0) where T : unmanaged
        {
            fixed (T* ptr = subresource0)
            {
                nint ptrV = (nint)ptr;
                return NullableUtility.ThrowIfNull(_internal).CreateTexture(description, new Span<nint>(ref ptrV));
            }
        }

        #endregion

        public RHI.GraphicsAPI API => NullableUtility.ThrowIfNull(_internal).API;
        public string Name => NullableUtility.ThrowIfNull(_internal).Name;

        public ulong TotalVideoMemory => NullableUtility.ThrowIfNull(_internal).TotalVideoMemory;

        public bool IsNull => _internal == null;
        public RHI.GraphicsDevice? RHIDevice => _internal;

        public static GfxDevice Current => RenderingManager.Device;

        public static GfxDevice Null = new GfxDevice(null);

        public static explicit operator RHI.GraphicsDevice?(GfxDevice device) => device._internal;
        public static implicit operator GfxDevice(RHI.GraphicsDevice? device) => new GfxDevice(device);
    }
}
