using Arch.Core;
using Arch.Core.Extensions;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Components;
using Primary.Profiling;
using Primary.Rendering.Data;
using Serilog;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering.Forward.Managers
{
    public sealed class ShadowManager : IDisposable
    {
        private readonly RenderingManager _manager;

        private RHI.RenderTarget _shadowAtlas;
        private RHI.Buffer _shadowBuffer;
        private RHI.Buffer _shadowCubemapBuffer;

        private int _shadowBufferCapacity;
        private int _shadowCubemapBufferCapacity;

        private Dictionary<int, ShadowCasterData> _activeCasters;

        private HashSet<uint> _lockedCasterIndices;
        private HashSet<uint> _lockedCubemapIndices;

        private List<FrameCasterData> _frameCasters;
        private List<CubemapCasterData> _cubemapCasters;

        private bool _disposedValue;

        internal ShadowManager(RenderingManager manager)
        {
            _manager = manager;

            _shadowBufferCapacity = (int)Constants.rShadowBufferMinimumSize;
            _shadowCubemapBufferCapacity = (int)Constants.rShadowCubemapBufferMinimumSize;

            _shadowAtlas = RenderingManager.Device.CreateRenderTarget(new RHI.RenderTargetDescription
            {
                Dimensions = new Size((int)Constants.rShadowMapResolution, (int)Constants.rShadowMapResolution),

                ColorFormat = RHI.RenderTargetFormat.Undefined,
                DepthFormat = RHI.DepthStencilFormat.R32t,

                ShaderVisibility = RHI.RenderTargetVisiblity.Depth
            });

            _shadowBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
            {
                ByteWidth = (uint)(Unsafe.SizeOf<ShadowData>() * Constants.rShadowBufferMinimumSize),
                Stride = (uint)Unsafe.SizeOf<ShadowData>(),
                CpuAccessFlags = RHI.CPUAccessFlags.Write,
                Memory = RHI.MemoryUsage.Dynamic,
                Mode = RHI.BufferMode.Structured,
                Usage = RHI.BufferUsage.ShaderResource
            }, nint.Zero);

            _shadowCubemapBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
            {
                ByteWidth = (uint)(Unsafe.SizeOf<ShadowCubemap>() * Constants.rShadowCubemapBufferMinimumSize),
                Stride = (uint)Unsafe.SizeOf<ShadowCubemap>(),
                CpuAccessFlags = RHI.CPUAccessFlags.Write,
                Memory = RHI.MemoryUsage.Dynamic,
                Mode = RHI.BufferMode.Structured,
                Usage = RHI.BufferUsage.ShaderResource
            }, nint.Zero);

            _shadowAtlas.Name = "ForwardRP - ShadowAtlas";
            _shadowBuffer.Name = "ForwardRP - ShadowBuffer";
            _shadowCubemapBuffer.Name = "ForwardRP - ShadowCubemapBuffer";

            _activeCasters = new Dictionary<int, ShadowCasterData>();

            _lockedCasterIndices = new HashSet<uint>();
            _lockedCubemapIndices = new HashSet<uint>();

            _frameCasters = new List<FrameCasterData>();
            _cubemapCasters = new List<CubemapCasterData>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _shadowAtlas?.Dispose();
                    _shadowBuffer?.Dispose();
                    _shadowCubemapBuffer?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal unsafe void PrepareShadows(RHI.CopyCommandBuffer commandBuffer, RenderPassViewportData viewportData)
        {
            using (new ProfilingScope("PrepareShadows"))
            {
                CalculateShadowResolutions();
                UpdateShadowBuffer(commandBuffer, viewportData);
            }
        }

        private List<Vector2> _ladder = new List<Vector2>();
        private void CalculateShadowResolutions()
        {
            _frameCasters.Clear();
            _cubemapCasters.Clear();

            foreach (var kvp in _activeCasters)
            {
                ShadowCasterData casterData = kvp.Value;

                uint stableId = casterData.Type == LightType.PointLight ? casterData.CubemapId : casterData.StableIds.DangerousGetReferenceAt(0);
                if (casterData.Importance == ShadowImportance.None || stableId == uint.MaxValue)
                    continue;

                ExceptionUtility.Assert(casterData.Entity.IsAlive());

                ref WorldTransform transform = ref casterData.Entity.Get<WorldTransform>();
                ref Light light = ref casterData.Entity.Get<Light>();

                if (light.Type == LightType.SpotLight)
                {
                    Matrix4x4 lightProjection =
                        Matrix4x4.CreateLookTo(transform.Transformation.Translation, transform.ForwardVector, transform.UpVector) *
                        Matrix4x4.CreatePerspectiveFieldOfView(float.Pi - light.OuterCutOff * 2.0f/*float.DegreesToRadians(125.0f)*/, 1.0f, 0.1f, 20.0f);

                    _frameCasters.Add(new FrameCasterData(lightProjection, transform.Transformation.Translation, Vector2.Zero, casterData.Importance switch
                    {
                        ShadowImportance.Low => 128,
                        ShadowImportance.Medium => 256,
                        ShadowImportance.High => 512,
                        _ => throw new NotImplementedException(),
                    }, stableId, false));
                }
                else if (light.Type == LightType.PointLight)
                {
                    Matrix4x4 lightProjection =
                        Matrix4x4.CreatePerspectiveFieldOfView(float.DegreesToRadians(90.0f), 1.0f, 0.1f, 20.0f);

                    for (int i = 0; i < 6; i++)
                    {
                        Matrix4x4 lightFaceProjection =
                            Matrix4x4.CreateLookTo(transform.Transformation.Translation, s_cubemapDirections[i].Forward, s_cubemapDirections[i].Up) *
                            lightProjection;

                        _frameCasters.Add(new FrameCasterData(lightFaceProjection, transform.Transformation.Translation, Vector2.Zero, casterData.Importance switch
                        {
                            ShadowImportance.Low => 128,
                            ShadowImportance.Medium => 256,
                            ShadowImportance.High => 512,
                            _ => throw new NotImplementedException(),
                        }, casterData.StableIds[i], true));
                    }

                    _cubemapCasters.Add(new CubemapCasterData(casterData.CubemapId, casterData.StableIds));
                }
            }

            _frameCasters.Sort((x, y) => x.AtlasResolution.CompareTo(y.AtlasResolution));

            Span<FrameCasterData> frameCasters = _frameCasters.AsSpan();

            _ladder.Clear();

            Vector2 point = Vector2.Zero;
            //https://lisyarus.github.io/blog/posts/texture-packing.html
            for (int i = 0; i < frameCasters.Length; i++)
            {
                ref FrameCasterData frameCaster = ref frameCasters[i];
                frameCaster.AtlasOffset = point;

                point.X += frameCaster.AtlasResolution;

                int back = _ladder.Count - 1;
                if (back >= 0 && _ladder[back].Y == point.Y + frameCaster.AtlasResolution)
                    _ladder[back] = new Vector2(point.X, _ladder[back].Y);
                else
                    _ladder.Add(new Vector2(point.X, point.Y + frameCaster.AtlasResolution));

                if (point.X == Constants.rShadowMapResolution)
                {
                    _ladder.RemoveAt(_ladder.Count - 1);

                    point.Y += frameCaster.AtlasResolution;
                    if (_ladder.Count > 0)
                        point.X = _ladder[_ladder.Count - 1].X;
                    else
                        point.X = 0.0f;
                }
            }
        }

        private unsafe void UpdateShadowBuffer(RHI.CopyCommandBuffer commandBuffer, RenderPassViewportData viewportData)
        {
            int minShadowBufferSize = (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(_frameCasters.Count, (int)Constants.rShadowBufferMinimumSize));
            if (_shadowBufferCapacity < minShadowBufferSize || _shadowBufferCapacity / (float)minShadowBufferSize > Constants.rShadowBufferShrinkPercentage)
            {
                Log.Information("Resizing shadow buffer from: {x} elements to {y} elements", _shadowBufferCapacity, minShadowBufferSize);

                _shadowBuffer?.Dispose();
                _shadowBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
                {
                    ByteWidth = (uint)(Unsafe.SizeOf<ShadowData>() * minShadowBufferSize),
                    Stride = (uint)Unsafe.SizeOf<ShadowData>(),
                    CpuAccessFlags = RHI.CPUAccessFlags.Write,
                    Memory = RHI.MemoryUsage.Dynamic,
                    Mode = RHI.BufferMode.Structured,
                    Usage = RHI.BufferUsage.ShaderResource
                }, nint.Zero);

                _shadowBufferCapacity = minShadowBufferSize;
            }

            int minShadowCubemapBufferSize = (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(_cubemapCasters.Count, (int)Constants.rShadowCubemapBufferMinimumSize));
            if (_shadowCubemapBufferCapacity < minShadowCubemapBufferSize || _shadowCubemapBufferCapacity / (float)minShadowCubemapBufferSize > Constants.rShadowCubemapBufferShrinkPercentage)
            {
                Log.Information("Resizing shadow cubemap buffer from: {x} elements to {y} elements", _shadowCubemapBufferCapacity, minShadowCubemapBufferSize);

                _shadowCubemapBuffer?.Dispose();
                _shadowCubemapBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
                {
                    ByteWidth = (uint)(Unsafe.SizeOf<ShadowCubemap>() * minShadowCubemapBufferSize),
                    Stride = (uint)Unsafe.SizeOf<ShadowCubemap>(),
                    CpuAccessFlags = RHI.CPUAccessFlags.Write,
                    Memory = RHI.MemoryUsage.Dynamic,
                    Mode = RHI.BufferMode.Structured,
                    Usage = RHI.BufferUsage.ShaderResource
                }, nint.Zero);

                _shadowCubemapBufferCapacity = minShadowCubemapBufferSize;
            }

            {
                ShadowData* mappedData = (ShadowData*)commandBuffer.Map(_shadowBuffer, RHI.MapIntent.Write);
                if (mappedData == null)
                {
                    throw new NotImplementedException("placeholder error");
                }

                Span<FrameCasterData> frameCasters = _frameCasters.AsSpan();
                for (int i = 0; i < frameCasters.Length; i++)
                {
                    ref FrameCasterData casterData = ref frameCasters[i];
                    mappedData[casterData.Index] = new ShadowData
                    {
                        UVMinimum = casterData.AtlasOffset / Constants.rShadowMapResolution,
                        UVSize = new Vector2(casterData.AtlasResolution / (float)Constants.rShadowMapResolution),

                        LightProjection = casterData.LightProjection
                    };
                }

                commandBuffer.Unmap(_shadowBuffer);
            }

            {
                ShadowCubemap* mappedData = (ShadowCubemap*)commandBuffer.Map(_shadowCubemapBuffer, RHI.MapIntent.Write);
                if (mappedData == null)
                {
                    throw new NotImplementedException("placeholder error");
                }

                Span<CubemapCasterData> cubemapCasters = _cubemapCasters.AsSpan();
                for (int i = 0; i < cubemapCasters.Length; i++)
                {
                    ref CubemapCasterData cubemapCaster = ref cubemapCasters[i];
                    cubemapCaster.StableIds.CopyTo(new Span<uint>(mappedData[cubemapCaster.CubemapId].Indices, 6));
                }

                commandBuffer.Unmap(_shadowCubemapBuffer);
            }
        }

        internal void Clear()
        {
            _activeCasters.Clear();

            _lockedCasterIndices.Clear();
            _lockedCubemapIndices.Clear();
        }

        internal uint GetStableId(Entity e)
        {
            int hashId = HashCode.Combine(e.Id, e.WorldId);
            ref ShadowCasterData casterData = ref GetCaster(e, hashId);

            if (casterData.StableIds.Length == 0)
            {
                //TODO: improve search function

                uint[] ids = new uint[casterData.Type == LightType.PointLight ? 6 : 1];

                for (int i = 0; i < ids.Length; i++)
                {
                    uint id = 0;
                    while (_lockedCasterIndices.Contains(id))
                    {
                        id++;
                    }

                    _lockedCasterIndices.Add(id);
                    ids[i] = id;
                }

                casterData.StableIds = ids;
            }

            if (casterData.Type == LightType.PointLight)
            {
                if (casterData.CubemapId == uint.MaxValue)
                {
                    uint id = 0;
                    while (_lockedCubemapIndices.Contains(id))
                    {
                        id++;
                    }

                    casterData.CubemapId = id;
                    _lockedCubemapIndices.Add(id);
                }

                return casterData.CubemapId;
            }

            return casterData.StableIds.DangerousGetReferenceAt(0);
        }

        internal void UpdateCaster(Entity e, LightType type, ShadowImportance importance)
        {
            int hashId = HashCode.Combine(e.Id, e.WorldId);
            if (importance == ShadowImportance.None)
            {
                _activeCasters.Remove(hashId);
            }

            ref ShadowCasterData casterData = ref GetCaster(e, hashId);
            casterData.Importance = importance;

            if (casterData.StableIds.Length > 0 && casterData.Type != type)
            {
                for (int i = 0; i < casterData.StableIds.Length; i++)
                {
                    _lockedCasterIndices.Remove(casterData.StableIds[i]);
                }

                casterData.StableIds = Array.Empty<uint>();
            }

            if (casterData.Type == LightType.PointLight && type != LightType.PointLight && casterData.CubemapId != uint.MaxValue)
            {
                _lockedCubemapIndices.Remove(casterData.CubemapId);
                casterData.CubemapId = uint.MaxValue;
            }

            casterData.Type = type;
        }

        private ref ShadowCasterData GetCaster(Entity e, int key)
        {
            ref ShadowCasterData data = ref CollectionsMarshal.GetValueRefOrAddDefault(_activeCasters, key, out bool exists);
            if (!exists)
            {
                data.Entity = e;
                data.Type = LightType.DirectionalLight;
                data.Importance = ShadowImportance.None;
                data.StableIds = Array.Empty<uint>();
                data.CubemapId = uint.MaxValue;
            }

            return ref data;
        }

        public int ShadowBufferCapacity => _shadowBufferCapacity;
        public int ShadowCubemapBufferCapacity => _shadowCubemapBufferCapacity;

        public int ActiveCastersCount => _activeCasters.Count;

        internal RHI.Buffer ShadowBuffer => _shadowBuffer;
        internal RHI.Buffer ShadowCubemapBuffer => _shadowCubemapBuffer;
        internal RHI.RenderTarget ShadowAtlas => _shadowAtlas;

        internal Span<FrameCasterData> FrameCasters => _frameCasters.AsSpan();

        private static (Vector3 Forward, Vector3 Up)[] s_cubemapDirections = [
            ( Vector3.UnitX, -Vector3.UnitY), //good
            (-Vector3.UnitX, -Vector3.UnitY), //good
            ( Vector3.UnitY,  Vector3.UnitZ),
            (-Vector3.UnitY, -Vector3.UnitZ),
            ( Vector3.UnitZ, -Vector3.UnitY),
            (-Vector3.UnitZ, -Vector3.UnitY), //good
            ];

        internal record struct ShadowCasterData(Entity Entity, LightType Type, ShadowImportance Importance, uint[] StableIds, uint CubemapId);
        internal record struct FrameCasterData(Matrix4x4 LightProjection, Vector3 WorldPosition, Vector2 AtlasOffset, int AtlasResolution, uint Index, bool IsPointLight);
        internal record struct CubemapCasterData(uint CubemapId, uint[] StableIds);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal record struct ShadowData
    {
        public Vector2 UVMinimum;
        public Vector2 UVSize;

        public Matrix4x4 LightProjection;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal unsafe struct ShadowCubemap
    {
        public fixed uint Indices[6];
    }
}
