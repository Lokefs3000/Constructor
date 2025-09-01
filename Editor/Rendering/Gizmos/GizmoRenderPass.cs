using Arch.Core;
using Arch.Core.Extensions;
using Collections.Pooled;
using Microsoft.Extensions.ObjectPool;
using Primary.Assets;
using Primary.Components;
using Primary.Rendering;
using Primary.Rendering.Data;
using Primary.Rendering.Forward;
using Primary.Rendering.Pooling;
using Primary.Rendering.Raw;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using RHI = Primary.RHI;

namespace Editor.Rendering.Gizmos
{
    [RenderPassPriority(true, typeof(FinalBlitPass))]
    internal sealed class GizmoRenderPass : IRenderPass
    {
        private ObjectPool<PooledList<IconDrawArgs>> _iconPool;
        private Dictionary<TextureAsset, PooledList<IconDrawArgs>> _iconBatches;

        private ShaderAsset _iconBillboard;
        private ShaderBindGroup _iconBindGroup;

        private TextureAsset _floatingIcons;

        private RHI.Buffer _frameBuffer;
        private RHI.Buffer? _billboardBuffer;

        private int _billboardBufferSize;

        public GizmoRenderPass()
        {
            _billboardBufferSize = 0;

            _iconPool = ObjectPool.Create(new IconDrawArgsPoolPolicy());
            _iconBatches = new Dictionary<TextureAsset, PooledList<IconDrawArgs>>();

            _iconBillboard = AssetManager.LoadAsset<ShaderAsset>("Hidden/Editor/IconBillboard")!;
            _iconBindGroup = _iconBillboard.CreateDefaultBindGroup();

            _floatingIcons = AssetManager.LoadAsset<TextureAsset>("Content/FloatingIcons.png")!;

            _frameBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
            {
                ByteWidth = (uint)Unsafe.SizeOf<FrameBufferData>(),
                Stride = (uint)Unsafe.SizeOf<FrameBufferData>(),
                CpuAccessFlags = RHI.CPUAccessFlags.Write,
                Memory = RHI.MemoryUsage.Dynamic,
                Mode = RHI.BufferMode.None,
                Usage = RHI.BufferUsage.ConstantBuffer
            }, nint.Zero);

            _frameBuffer.Name = "EGizmo - FrameBuffer";

            _billboardBuffer = null;
        }

        public void Dispose()
        {
            ((IDisposable)_iconPool).Dispose();
            _iconBatches.Clear();

            _frameBuffer?.Dispose();
            _billboardBuffer?.Dispose();
        }

        public void PrepareFrame(IRenderPath path, RenderPassData passData)
        {
            World world = Editor.GlobalSingleton.SceneManager.World;

            world.Query(s_lightQuery, (Entity e, ref WorldTransform transform, ref Light lrd) =>
            {
                if (lrd.Type == LightType.SpotLight)
                    AddIcon(transform.Transformation.Translation, _floatingIcons, new Vector2(0.666666f, 0.0f), Vector2.One);
                else if (lrd.Type == LightType.PointLight)
                    AddIcon(transform.Transformation.Translation, _floatingIcons, new Vector2(0.0f, 0.0f), new Vector2(0.3333333f, 1.0f));
            });
        }

        public void ExecutePass(IRenderPath path, RenderPassData passData)
        {
            CommandBuffer commandBuffer = CommandBufferPool.Get();

            RenderPassViewportData viewportData = passData.Get<RenderPassViewportData>()!;

            using (new CommandBufferEventScope(commandBuffer, "Gizmos"))
            {
                UploadGizmoData(commandBuffer, viewportData);
                RenderGizmos(commandBuffer, viewportData);
            }

            CommandBufferPool.Return(commandBuffer);
        }

        private unsafe void UploadGizmoData(CommandBuffer commandBuffer, RenderPassViewportData viewportData)
        {
            {
                FrameBufferData bufferData = new FrameBufferData
                {
                    VP = viewportData.VP,
                    Camera = viewportData.ViewPosition
                };

                FrameBufferData* ptr = (FrameBufferData*)commandBuffer.Map(_frameBuffer, RHI.MapIntent.Write);
                if (ptr == null)
                {
                    throw new Exception("placeholder error");
                }

                *ptr = bufferData;
                commandBuffer.Unmap(_frameBuffer);
            }

            int totalBillboards = 0;
            foreach (var kvp in _iconBatches)
            {
                totalBillboards += kvp.Value.Count;
            }

            totalBillboards = Math.Max((int)BitOperations.RoundUpToPowerOf2((uint)totalBillboards), 8);
            if (_billboardBuffer == null || _billboardBufferSize < totalBillboards)
            {
                _billboardBuffer?.Dispose();
                _billboardBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
                {
                    ByteWidth = (uint)(Unsafe.SizeOf<IconDrawArgs>() * totalBillboards),
                    Stride = (uint)Unsafe.SizeOf<IconDrawArgs>(),
                    CpuAccessFlags = RHI.CPUAccessFlags.Write,
                    Memory = RHI.MemoryUsage.Dynamic,
                    Mode = RHI.BufferMode.Structured,
                    Usage = RHI.BufferUsage.ShaderResource
                }, nint.Zero);

                _billboardBufferSize = totalBillboards;
            }

            Vector3 cameraFwd = new Vector3(viewportData.View.M31, viewportData.View.M32, viewportData.View.M33);
            Vector3 cameraUp = new Vector3(viewportData.View.M21, viewportData.View.M22, viewportData.View.M23);

            {
                Span<IconDrawArgs> args = commandBuffer.Map<IconDrawArgs>(_billboardBuffer!, RHI.MapIntent.Write, totalBillboards);
                if (!args.IsEmpty)
                {
                    int offset = 0;
                    foreach (var kvp in _iconBatches)
                    {
                        Span<IconDrawArgs> span = kvp.Value.Span;
                        for (int i = 0; i < span.Length; i++)
                        {
                            ref IconDrawArgs draw = ref span[i];
                            draw.Model = Matrix4x4.CreateBillboard(draw.Model.Translation, viewportData.ViewPosition, Vector3.UnitY, cameraFwd);
                        }

                        kvp.Value.CopyTo(args.Slice(offset));
                        offset += kvp.Value.Count;
                    }
                }

                commandBuffer.Unmap(_billboardBuffer);
            }
        }

        private void RenderGizmos(CommandBuffer commandBuffer, RenderPassViewportData viewportData)
        {
            commandBuffer.SetRenderTarget(viewportData.BackBufferRenderTarget);
            commandBuffer.SetDepthStencil(viewportData.CameraRenderTarget);
            commandBuffer.SetScissorRect(new RHI.ScissorRect(0, 0, 100000, 100000));

            commandBuffer.SetShader(_iconBillboard);
            commandBuffer.SetBindGroups(_iconBindGroup);

            _iconBindGroup.SetResource("cbFrame", _frameBuffer);
            _iconBindGroup.SetResource("sbBillboards", _billboardBuffer);

            int offset = 0;
            foreach (var kvp in _iconBatches)
            {
                _iconBindGroup.SetResource("txAlbedo", _floatingIcons);

                commandBuffer.CommitShaderResources();
                commandBuffer.DrawInstanced(new RHI.DrawInstancedArgs(6, 0, (uint)kvp.Value.Count, (uint)offset));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CleanupFrame(IRenderPath path, RenderPassData passData)
        {
            foreach (var kvp in _iconBatches)
                _iconPool.Return(kvp.Value);
            _iconBatches.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddIcon(Vector3 world, TextureAsset texture, Vector2 uvMin, Vector2 uvMax)
        {
            if (!_iconBatches.TryGetValue(texture, out PooledList<IconDrawArgs>? list))
            {
                list = _iconPool.Get();
                _iconBatches.Add(texture, list);
            }

            list.Add(new IconDrawArgs(Matrix4x4.CreateTranslation(world), uvMin, uvMax));
        }

        private static QueryDescription s_lightQuery = new QueryDescription().WithAll<WorldTransform, Light>();

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private record struct IconDrawArgs(Matrix4x4 Model, Vector2 UVMin, Vector2 UVMax);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private record struct FrameBufferData(Matrix4x4 VP, Vector3 Camera);

        private record struct IconDrawArgsPoolPolicy : IPooledObjectPolicy<PooledList<IconDrawArgs>>
        {
            public PooledList<IconDrawArgs> Create()
            {
                return new PooledList<IconDrawArgs>(16);
            }

            public bool Return(PooledList<IconDrawArgs> obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
