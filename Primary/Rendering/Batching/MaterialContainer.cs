using Primary.Assets;
using Primary.Common;
using Serilog;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering.Batching
{
    internal unsafe sealed class MaterialContainer : IDisposable
    {
        private Dictionary<nint, MaterialIndex> _materialIndices;
        private HashSet<MaterialAsset> _usedMaterials;

        private bool _disposedValue;

        internal MaterialContainer()
        {
            _materialIndices = new Dictionary<nint, MaterialIndex>();
            _usedMaterials = new HashSet<MaterialAsset>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _usedMaterials.Clear();
                }

                foreach (var kvp in _materialIndices)
                {
                    //TODO: get material asset name instead
                    if (kvp.Value.RefCount > 0)
                        Log.Warning("Material: {m} still has references!", kvp.Value.Asset);
                    NativeMemory.Free(kvp.Value.Id.Pointer);
                }

                _materialIndices.Clear();

                _disposedValue = true;
            }
        }

        ~MaterialContainer()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal WeakRef<uint> GetIndexForMaterialAndIncrRef(MaterialAsset material)
        {
            lock (this)
            {
                ref MaterialIndex index = ref CollectionsMarshal.GetValueRefOrAddDefault(_materialIndices, material.Handle, out bool exists);
                if (!exists)
                {
                    uint* ptr = (uint*)NativeMemory.Alloc(sizeof(uint));
                    *ptr = (uint)_materialIndices.Count;

                    index.Asset = material;
                    index.Id = ptr;

                    _usedMaterials.Add(material);
                }

                index.RefCount++;
                return index.Id;
            }
        }

        internal void DecrRefForMaterial(MaterialAsset material)
        {
            lock (this)
            {
                ref MaterialIndex index = ref CollectionsMarshal.GetValueRefOrNullRef(_materialIndices, material.Handle);
                if (!Unsafe.IsNullRef(ref index))
                {
                    index.RefCount--;
                    if (index.RefCount == 0)
                    {
                        *index.Id.Pointer = uint.MaxValue;
                        NativeMemory.Free(index.Id.Pointer);

                        _materialIndices.Remove(material.Handle);
                        _usedMaterials.Remove(material);
                    }
                }
            }
        }

        private record struct MaterialIndex
        {
            public MaterialAsset Asset;
            public Ptr<uint> Id;
            public uint RefCount;
        }
    }
}
