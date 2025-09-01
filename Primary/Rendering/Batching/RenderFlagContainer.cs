using Arch.LowLevel;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Components;
using Primary.Memory;
using Primary.Scenes;
using Primary.Utility.Scopes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering.Batching
{
    internal unsafe sealed class RenderFlagContainer : IDisposable
    {
        private static WeakReference s_instance = new WeakReference(null);

        private MaterialAsset _missingMaterial;

        private ConcurrentStack<RenderMeshFlagContainer> _rmFlagPool;
        private ConcurrentDictionary<int, ShaderFlagContainer> _usedShaders;

        private MaterialContainer _materials;

        private bool _disposedValue;

        internal RenderFlagContainer()
        {
            s_instance.Target = this;

            _missingMaterial = NullableUtility.AlwaysThrowIfNull(AssetManager.LoadAsset<MaterialAsset>("Content/Missing.mat", true));

            _rmFlagPool = new ConcurrentStack<RenderMeshFlagContainer>();
            _usedShaders = new ConcurrentDictionary<int, ShaderFlagContainer>();

            _materials = new MaterialContainer();

            //SceneEntityManager.AddEntityEnabledCallback((e, x) =>
            //{
            //
            //});
            //
            //SceneEntityManager.AddTransformChangedCallback((e) =>
            //{
            //    if (e.ha)
            //});
        }

        internal void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (var kvp in _usedShaders)
                    {
                        kvp.Value.Dispose();
                    }

                    _usedShaders.Clear();

                    while (_rmFlagPool.TryPop(out RenderMeshFlagContainer? container))
                        container.Dispose();

                    _materials.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ChangeMesh(SceneEntity e, MaterialAsset? material, RenderMesh? old, RenderMesh? @new)
        {
            if (old == @new || (old == null && @new == null))
                return;

            ref MeshRenderer renderer = ref e.GetComponent<MeshRenderer>();
            ref RenderableAdditionalData additionalData = ref e.GetComponent<RenderableAdditionalData>();

            material ??= _missingMaterial;
            if (additionalData.BatchId != uint.MaxValue && old != null)
            {
                RemoveFromBatch(material, old, additionalData.BatchId);
            }

            if (@new != null)
            {
                additionalData.BatchId = AddToBatch(material, @new);
            }
        }

        private uint AddToBatch(MaterialAsset material, RenderMesh mesh)
        {
            ShaderFlagContainer flagContainer = _usedShaders.GetOrAdd(material.Shader!.HashCode, (x) =>
            {
                return new ShaderFlagContainer(_rmFlagPool, material.Shader!);
            });

            RenderMeshFlagContainer renderMeshContainer = flagContainer.UsedMeshes.GetOrAdd(mesh, (x) =>
            {
                if (!_rmFlagPool.TryPop(out RenderMeshFlagContainer? result))
                    result = new RenderMeshFlagContainer();

                result.Setup(mesh);
                return result;
            });

            return renderMeshContainer.Add(_materials.GetIndexForMaterialAndIncrRef(material));
        }

        private void RemoveFromBatch(MaterialAsset material, RenderMesh mesh, uint batchId)
        {
            if (_usedShaders.TryGetValue(material.Shader!.HashCode, out ShaderFlagContainer flagContainer))
            {
                if (flagContainer.UsedMeshes.TryGetValue(mesh, out RenderMeshFlagContainer? renderMeshContainer))
                {
                    if (renderMeshContainer.Remove(batchId) && flagContainer.UsedMeshes.TryRemove(mesh, out _))
                    {
                        renderMeshContainer.Reset();
                        _rmFlagPool.Push(renderMeshContainer);
                    }
                }

                if (flagContainer.UsedMeshes.IsEmpty && _usedShaders.TryRemove(material.Shader!.HashCode, out _))
                    flagContainer.Dispose();
            }
        }

        internal static RenderFlagContainer Instance => NullableUtility.ThrowIfNull((RenderFlagContainer?)s_instance.Target);
    }

    internal record struct ShaderFlagContainer : IDisposable
    {
        private readonly ConcurrentStack<RenderMeshFlagContainer> _containerPool;

        internal readonly ShaderAsset Shader;

        internal ConcurrentDictionary<RenderMesh, RenderMeshFlagContainer> UsedMeshes;

        public ShaderFlagContainer(ConcurrentStack<RenderMeshFlagContainer> containers, ShaderAsset shader)
        {
            _containerPool = containers;

            Shader = shader;

            UsedMeshes = new ConcurrentDictionary<RenderMesh, RenderMeshFlagContainer>();
        }

        public void Dispose()
        {
            foreach (var kvp in UsedMeshes)
            {
                kvp.Value.Reset();
                _containerPool.Push(kvp.Value);
            }

            UsedMeshes.Clear();
        }
    }

    internal sealed class RenderMeshFlagContainer : IDisposable
    {
        private SemaphoreSlim _semaphore;

        private RenderMesh? _renderMesh;

        private UnsafeFragmentedBuffer<RenderFlag> _flags;

        private bool _disposedValue;

        internal RenderMeshFlagContainer()
        {
            _semaphore = new SemaphoreSlim(1);

            _renderMesh = null;

            _flags = new UnsafeFragmentedBuffer<RenderFlag>(Constants.rFlagListStartSize);
        }

        internal void Reset()
        {
            _renderMesh = null;

            _flags.Resize(Constants.rFlagListStartSize, false);
            _flags.Clear();
        }

        internal void Setup(RenderMesh renderMesh)
        {
            _renderMesh = renderMesh;

            _flags.Clear();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _renderMesh = null;
                    _flags.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal uint Add(WeakRef<uint> materialData)
        {
            using (new SemaphoreScope(_semaphore))
            {
                if (_flags.Count == _flags.Capacity)
                {
                    _flags.Resize(BitOperations.RoundUpToPowerOf2(_flags.Count * 2), true);
                }

                return _flags.Add(new RenderFlag
                {
                    Model = Matrix4x4.Identity,
                    Material = materialData
                });
            }
        }

        internal bool Remove(uint batchId)
        {
            using (new SemaphoreScope(_semaphore))
            {
                _flags.Remove(batchId);
                return _flags.IsEmpty;
            }
        }
    }

    internal record struct RenderFlag
    {
        public Matrix4x4 Model;
        public WeakRef<uint> Material;
    }
}
