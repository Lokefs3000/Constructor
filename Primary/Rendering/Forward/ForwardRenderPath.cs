using Primary.Assets;
using Primary.Editor;
using Primary.Profiling;
using Primary.Rendering.Batching;
using Primary.Rendering.Data;
using Primary.Rendering.Forward;
using Primary.Rendering.Forward.Debugging;
using Primary.Rendering.Forward.Managers;
using Serilog;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering
{
    public sealed class ForwardRenderPath : IRenderPath
    {
        private bool _disposedValue;

        private LightManager _lightManager;
        private ShadowManager _shadowManager;

        private RHI.Buffer? _matrixBuffer;
        private int _matrixBufferSize;

        private RHI.Buffer _cbWorldData;
        private RHI.Buffer _cbObjectData;

        //TODO: find a better way to do this
        private RHI.CopyCommandBuffer _copyCommandBuffer;

        private ShaderBindGroup _buffersBindGroup;
        private ShaderBindGroup _lightingBindGroup;

        private ForwardOpaquePass _opaquePass;

        private ShadowPass _shadowPass;

        private FinalBlitPass _finalBlit;

        private SDebugOpaquePass _sDebugOpaquePass;

        internal ForwardRenderPath(RenderingManager manager)
        {
            _lightManager = new LightManager(manager);
            _shadowManager = new ShadowManager(manager);

            _matrixBuffer = null;
            _matrixBufferSize = 0;

            _cbWorldData = manager.GraphicsDevice.CreateBuffer(new RHI.BufferDescription
            {
                ByteWidth = (uint)Unsafe.SizeOf<cbWorldDataStruct>(),
                CpuAccessFlags = RHI.CPUAccessFlags.Write,
                Memory = RHI.MemoryUsage.Dynamic,
                Mode = RHI.BufferMode.None,
                Stride = (uint)Unsafe.SizeOf<cbWorldDataStruct>(),
                Usage = RHI.BufferUsage.ConstantBuffer
            }, nint.Zero);
            _cbObjectData = manager.GraphicsDevice.CreateBuffer(new RHI.BufferDescription
            {
                ByteWidth = (uint)Unsafe.SizeOf<cbObjectDataStruct>(),
                CpuAccessFlags = RHI.CPUAccessFlags.Write,
                Memory = RHI.MemoryUsage.Dynamic,
                Mode = RHI.BufferMode.None,
                Stride = (uint)Unsafe.SizeOf<cbObjectDataStruct>(),
                Usage = RHI.BufferUsage.ConstantBuffer
            }, nint.Zero);

            _cbWorldData.Name = "ForwardRP - cbWorld";
            _cbObjectData.Name = "ForwardRP - cbObject";

            _copyCommandBuffer = manager.GraphicsDevice.CreateCopyCommandBuffer();

            _buffersBindGroup = new ShaderBindGroup("__Internal_Buffers",
                new ShaderBindGroupVariable(ShaderVariableType.ConstantBuffer, "cbWorld"),
                new ShaderBindGroupVariable(ShaderVariableType.ConstantBuffer, "cbObject"),
                new ShaderBindGroupVariable(ShaderVariableType.StructuredBuffer, "sbFlagBuffer"));

            _lightingBindGroup = new ShaderBindGroup("__Internal_Lighting",
                new ShaderBindGroupVariable(ShaderVariableType.ConstantBuffer, "cbDirectional"),
                new ShaderBindGroupVariable(ShaderVariableType.StructuredBuffer, "sbLightBuffer"),
                new ShaderBindGroupVariable(ShaderVariableType.StructuredBuffer, "sbShadowBuffer"),
                new ShaderBindGroupVariable(ShaderVariableType.StructuredBuffer, "sbShadowCubemaps"),
                new ShaderBindGroupVariable(ShaderVariableType.Texture2D, "txShadowAtlas"));

            _opaquePass = new ForwardOpaquePass();

            _shadowPass = new ShadowPass();

            _finalBlit = new FinalBlitPass();

            _sDebugOpaquePass = new SDebugOpaquePass();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _opaquePass?.Dispose();
                    _shadowPass?.Dispose();
                    //_finalBlit?.Dispose();
                    _sDebugOpaquePass?.Dispose();

                    _shadowManager?.Dispose();
                    _lightManager?.Dispose();

                    _matrixBuffer?.Dispose();
                    _cbWorldData?.Dispose();
                    _cbObjectData?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void PreparePasses(RenderPassData passData)
        {
            RenderingManager renderer = Engine.GlobalSingleton.RenderingManager;
            RenderBatcher batcher = renderer.RenderBatcher;

            using (new ProfilingScope("Fwd-PrepareFrameData"))
            {
                _copyCommandBuffer.Begin();

                _lightManager.PrepareLights(_copyCommandBuffer, _shadowManager);
                _shadowManager.PrepareShadows(_copyCommandBuffer, passData.Get<RenderPassViewportData>()!);

                PrepareFrameData(renderer, batcher, passData.Get<RenderPassViewportData>()!, passData.Get<RenderPassLightingData>()!);
                SetupBindGroups();

                _copyCommandBuffer.End();
                Engine.GlobalSingleton.RenderingManager.GraphicsDevice.Submit(_copyCommandBuffer);
            }
        }

        public void ExecutePasses(RenderPass renderPass)
        {
            RenderingManager renderer = Engine.GlobalSingleton.RenderingManager;

            switch (renderer.Configuration.RenderMode)
            {
                case RenderingMode.Lit:
                    {
                        _shadowPass.ExecutePass(renderPass);
                        _opaquePass.ExecutePass(renderPass);
                        _finalBlit.ExecutePass(renderPass);

                        break;
                    }
                case RenderingMode.Lighting:
                case RenderingMode.DetailLighting:
                case RenderingMode.Wireframe:
                case RenderingMode.Normals:
                case RenderingMode.Overdraw:
                case RenderingMode.Unlit:
                    {
                        _sDebugOpaquePass.ExecutePass(renderPass);
                        _finalBlit.ExecutePass(renderPass);
                        break;
                    }
                case RenderingMode.Reflections:
                    {
                        break;
                    }
                case RenderingMode.ShaderComplexity:
                    {
                        break;
                    }
            }
        }

        public void EmitDebugStatistics(DebugDataContainer container)
        {

        }

        private void PrepareFrameData(RenderingManager renderer, RenderBatcher batcher, RenderPassViewportData viewportData, RenderPassLightingData lightingData)
        {
            Span<FlagRenderBatch> batches = batcher.UsedBatches;

            int totalMatrixCount = 0;
            for (int i = 0; i < batches.Length; i++)
            {
                Span<RenderMeshBatchData> batchDatas = batches[i].RenderMeshBatches;
                for (int j = 0; j < batchDatas.Length; j++)
                {
                    totalMatrixCount += batchDatas[j].BatchableFlags.Count;
                }
            }

            if (totalMatrixCount > 0)
            {
                bool recreateMatrixBuffer = _matrixBuffer == null || _matrixBufferSize < totalMatrixCount;
                if (recreateMatrixBuffer)
                {
                    uint matrixCountUp = BitOperations.RoundUpToPowerOf2((uint)totalMatrixCount);

                    _matrixBuffer?.Dispose();
                    _matrixBuffer = renderer.GraphicsDevice.CreateBuffer(new RHI.BufferDescription
                    {
                        ByteWidth = (uint)(Unsafe.SizeOf<BatchedRenderFlag>() * matrixCountUp),
                        CpuAccessFlags = RHI.CPUAccessFlags.Write,
                        Memory = RHI.MemoryUsage.Dynamic,
                        Mode = RHI.BufferMode.Structured,
                        Stride = (uint)Unsafe.SizeOf<BatchedRenderFlag>(),
                        Usage = RHI.BufferUsage.ShaderResource
                    }, nint.Zero);

                    _matrixBuffer.Name = "ForwardRP - MatrixBuffer";

                    _matrixBufferSize = (int)matrixCountUp;
                }

                ulong dataSize = (ulong)totalMatrixCount * (ulong)Unsafe.SizeOf<BatchedRenderFlag>();
                nint dataPointer = _copyCommandBuffer.Map(_matrixBuffer!, RHI.MapIntent.Write, dataSize);
                if (dataPointer != nint.Zero)
                {
                    unsafe
                    {
                        Span<BatchedRenderFlag> bufferSpan = new Span<BatchedRenderFlag>(dataPointer.ToPointer(), totalMatrixCount);

                        int totalOffsetInBuffer = 0;
                        for (int i = 0; i < batches.Length; i++)
                        {
                            Span<RenderMeshBatchData> batchDatas = batches[i].RenderMeshBatches;
                            for (int j = 0; j < batchDatas.Length; j++)
                            {
                                RenderMeshBatchData batchData = batchDatas[j];
                                if (batchData.BatchableFlags.Count == 0)
                                    continue;

                                batchData.BatchableFlags.AsSpan().CopyTo(bufferSpan.Slice(totalOffsetInBuffer, batchData.BatchableFlags.Count));

                                totalOffsetInBuffer += batchData.BatchableFlags.Count;
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("Failed to map matrix buffer!");
                }

                _copyCommandBuffer.Unmap(_matrixBuffer!);
            }

            if (!MapUploadSingle(_copyCommandBuffer, _cbWorldData, new cbWorldDataStruct
            {
                VP = viewportData.VP,

                ViewPos = viewportData.ViewPosition,

                HasDirLight = _lightManager.HasDirectionalLight ? 1u : 0,
                RawLightCount = (uint)_lightManager.GpuLightList.Count
            }))
                Log.Error("Failed to map world data constant buffer!");
        }

        private void SetupBindGroups()
        {
            _buffersBindGroup.SetResource("cbWorld", _cbWorldData);
            _buffersBindGroup.SetResource("cbObject", _cbObjectData);
            _buffersBindGroup.SetResource("sbFlagBuffer", _matrixBuffer);

            _lightingBindGroup.SetResource("cbDirectional", _lightManager.DirectionalBuffer);
            _lightingBindGroup.SetResource("sbLightBuffer", _lightManager.GpuLightList.GpuBuffer);
            _lightingBindGroup.SetResource("sbShadowBuffer", _shadowManager.ShadowBuffer);
            _lightingBindGroup.SetResource("sbShadowCubemaps", _shadowManager.ShadowCubemapBuffer);
            _lightingBindGroup.SetResource("txShadowAtlas", _shadowManager.ShadowAtlas.DepthTexture);
        }

        private static unsafe bool MapUploadSingle<T>(RHI.CopyCommandBuffer commandBuffer, RHI.Buffer buffer, T data) where T : unmanaged
        {
            nint mapped = commandBuffer.Map(buffer, RHI.MapIntent.Write, (ulong)sizeof(T));
            if (mapped == nint.Zero)
            {
                return false;
            }

            NativeMemory.Copy(Unsafe.AsPointer(ref data), mapped.ToPointer(), (nuint)sizeof(T));
            commandBuffer.Unmap(buffer);

            return true;
        }

        internal RHI.Buffer WorldMatrixBuffer => _matrixBuffer!;
        internal RHI.Buffer CbWorldData => _cbWorldData;
        internal RHI.Buffer CbObjectData => _cbObjectData;

        public LightManager Lights => _lightManager;
        public ShadowManager Shadows => _shadowManager;

        internal ShaderBindGroup BuffersBindGroup => _buffersBindGroup;
        internal ShaderBindGroup LightingBindGroup => _lightingBindGroup;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct cbWorldDataStruct
    {
        public Matrix4x4 VP;

        public Vector3 ViewPos;

        public uint HasDirLight;
        public uint RawLightCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct cbObjectDataStruct
    {
        public uint MatrixId;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct cbDirectionalLightDataStruct
    {
        public Vector3 Direction;
        private float __padding0;
        public Vector3 Ambient;
        private float __padding1;
        public Vector3 Specular;
        private float __padding2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct sbPointLightDataStruct
    {
        public Vector3 Position;

        public Vector3 Ambient;
        public Vector3 Diffuse;
        public Vector3 Specular;

        public float Constant;
        public float Linear;
        public float Quadratic;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct sbSpotLightDataStruct
    {
        public Vector3 Position;
        public Vector3 Direction;

        public float CutOff;
        public float OuterCutOff;

        public Vector3 Ambient;
        public Vector3 Diffuse;
        public Vector3 Specular;

        public float Constant;
        public float Linear;
        public float Quadratic;
    }
}
