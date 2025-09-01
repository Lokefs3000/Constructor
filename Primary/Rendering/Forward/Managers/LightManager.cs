using Arch.Core;
using Arch.Core.Extensions;
using Primary.Common;
using Primary.Components;
using Primary.GUI.ImGui;
using Primary.Profiling;
using Primary.Rendering.Collections;
using Primary.Scenes;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Primary.Rendering.Forward.Managers
{
    public sealed class LightManager : IDisposable
    {
        private readonly RenderingManager _manager;

        private RHI.Buffer _directionalLight;

        private bool _hasDirectionalLight;

        private GpuList<RawLight> _gpuRawLightList;
        private bool _needsStructureRebuild;

        private HashSet<SceneEntity> _pendingLightUpdates;

        private bool _disposedValue;

        internal LightManager(RenderingManager manager)
        {
            _manager = manager;

            _hasDirectionalLight = false;

            _gpuRawLightList = new GpuList<RawLight>((int)Constants.rLightBufferMinimumSize, Constants.rLightBufferShrinkPercentage);
            _needsStructureRebuild = true;

            _directionalLight = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
            {
                ByteWidth = (uint)Unsafe.SizeOf<DirectionalLightData>(),
                Stride = (uint)Unsafe.SizeOf<DirectionalLightData>(),
                Memory = RHI.MemoryUsage.Default,
                Usage = RHI.BufferUsage.ConstantBuffer,
                Mode = RHI.BufferMode.None,
                CpuAccessFlags = RHI.CPUAccessFlags.Write
            }, nint.Zero);

            _directionalLight.Name = "ForwardRP - DirLight";

            _pendingLightUpdates = new HashSet<SceneEntity>();

            SceneEntityManager.AddComponentRemovedCallback<LightRenderingData>((e) =>
            {
                int hashId = e.GetHashCode();
                int i = _gpuRawLightList.FindIndex((ref readonly RawLight rl) => rl.LightId == hashId);

                if (i >= 0)
                {
                    ((ForwardRenderPath)manager.RenderPath).Shadows.UpdateCaster(e.WrappedEntity, LightType.DirectionalLight, ShadowImportance.None);
                    _gpuRawLightList.Remove(i);
                }
            });
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _directionalLight?.Dispose();
                    _gpuRawLightList?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal unsafe void PrepareLights(RHI.CopyCommandBuffer commandBuffer, ShadowManager shadowManager)
        {
            using (new ProfilingScope("PrepareLights"))
            {
                if (_needsStructureRebuild)
                {
                    _gpuRawLightList.Clear();
                    shadowManager.Clear();

                    RunResolveDirtyLights(true, shadowManager);

                    _needsStructureRebuild = false;
                }
                else
                {
                    RunResolveDirtyLights(false, shadowManager);
                }

                _gpuRawLightList.Flush(commandBuffer);
            }
        }

        private void RunResolveDirtyLights(bool bypassDirtyRequirement, ShadowManager shadowManager)
        {
            ResolveDirtyLights job = new ResolveDirtyLights
            {
                This = this,
                Shadows = shadowManager,

                BypassDirtyRequirement = bypassDirtyRequirement,
            };

            World world = Engine.GlobalSingleton.SceneManager.World;
            world.InlineEntityQuery<ResolveDirtyLights, EntityEnabled, WorldTransform, LightRenderingData, Light>(ResolveDirtyLights.Query, ref job);
        }

        public int LightListCount => _gpuRawLightList.Count;
        public int LightListCapacity => _gpuRawLightList.Capacity;

        public int PendingLightUpdateCount => _pendingLightUpdates.Count;

        internal bool HasDirectionalLight => _hasDirectionalLight;

        internal RHI.Buffer DirectionalBuffer => _directionalLight;
        internal GpuList<RawLight> GpuLightList => _gpuRawLightList;

        private struct ResolveDirtyLights : IForEachWithEntity<EntityEnabled, WorldTransform, LightRenderingData, Light>
        {
            public static QueryDescription Query =
                new QueryDescription().WithAll<EntityEnabled, WorldTransform, LightRenderingData, Light>();

            public LightManager This;
            public ShadowManager Shadows;

            public bool BypassDirtyRequirement;

            public void Update(Entity e, ref EntityEnabled enabled, ref WorldTransform transform, ref LightRenderingData renderingData, ref Light light)
            {
                if (light.Dirty || BypassDirtyRequirement)
                {
                    if (light.Type > LightType.DirectionalLight)
                    {
                        int hashId = HashCode.Combine(e.Id, e.WorldId);
                        int i = This._gpuRawLightList.FindIndex((ref readonly RawLight rl) => rl.LightId == (uint)hashId);

                        if (enabled.Enabled)
                        {
                            RawLight newRawLightData = new RawLight
                            {
                                Type = (uint)light.Type,

                                LightId = (uint)hashId,
                                ShadowIndex = uint.MaxValue,

                                Position = transform.Transformation.Translation,
                                Direction = -transform.ForwardVector,

                                SpotInnerCone = light.InnerCutOff,
                                SpotOuterCone = light.OuterCutOff,

                                Diffuse = light.Diffuse * light.Brightness,
                                Specular = light.Specular * MathF.Min(light.Brightness, 1.0f),
                            };

                            Shadows.UpdateCaster(e, light.Type, light.ShadowImportance);
                            if (light.ShadowImportance > ShadowImportance.None)
                                newRawLightData.ShadowIndex = (uint)Shadows.GetStableId(e);

                            if (i >= 0)
                                This._gpuRawLightList.Replace(i, newRawLightData);
                            else
                                This._gpuRawLightList.Add(newRawLightData);
                        }
                        else if (i >= 0)
                        {
                            Shadows.UpdateCaster(e, light.Type, ShadowImportance.None);
                            This._gpuRawLightList.Remove(i);
                        }
                    }
                    else
                    {
                        //dir light
                    }

                    light.Dirty = false;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal record struct DirectionalLightData
    {
        public Vector3 Direction;

        public Vector3 Ambient;
        public Vector3 Diffuse;
        public Vector3 Specular;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal record struct RawLight
    {
        public uint Type;

        public uint LightId;
        public uint ShadowIndex;

        public Vector3 Position;
        public Vector3 Direction;

        public float SpotInnerCone;
        public float SpotOuterCone;

        public Vector3 Diffuse;
        public Vector3 Specular;
    }

    internal enum RawLightType : uint
    {
        Disabled = 0,
        Spot,
        Point,
    }
}
