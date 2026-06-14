using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Pass
{
    internal sealed class PassArrayStorage
    {
        private UsedResourceData[] _usedResources;
        private UsedRenderTargetData[] _usedRenderTargets;

        private int _usedResourcesIndex;
        private int _usedRenderTargetsIndex;

        internal PassArrayStorage()
        {
            _usedResources = Array.Empty<UsedResourceData>();
            _usedRenderTargets = Array.Empty<UsedRenderTargetData>();

            _usedResourcesIndex = 0;
            _usedRenderTargetsIndex = 0;
        }

        internal void ClearArrays()
        {
            Array.Clear(_usedResources, 0, _usedResourcesIndex);
            Array.Clear(_usedRenderTargets, 0, _usedRenderTargetsIndex);

            _usedResourcesIndex = 0;
            _usedRenderTargetsIndex = 0;
        }

        internal void EnsureUsedResourceSize(int newSize)
        {
            if (_usedResources.Length < newSize)
            {
                Array.Resize(ref _usedResources, Math.Max(_usedResources.Length * 2, 8));
            }
        }

        internal int CommitUsedResourceData(int size)
        {
            int index = _usedResourcesIndex;

            _usedResourcesIndex = size;
            EnsureUsedResourceSize(_usedResourcesIndex);
            return index;
        }

        internal void EnsureUsedRenderTargetSize(int newSize)
        {
            if (_usedRenderTargets.Length < newSize)
            {
                Array.Resize(ref _usedRenderTargets, Math.Max(_usedRenderTargets.Length * 2, 8));
            }
        }

        internal int CommitUsedRenderTargetData(int size)
        {
            int index = _usedRenderTargetsIndex;

            _usedRenderTargetsIndex = size;
            EnsureUsedRenderTargetSize(_usedRenderTargetsIndex);
            return index;
        }

        public UsedResourceData[] UsedResources => _usedResources;
        public UsedRenderTargetData[] UsedRenderTargets => _usedRenderTargets;

        public int UsedResourcesIndex => _usedResourcesIndex;
        public int UsedRenderTargetsIndex => _usedRenderTargetsIndex;
    }

    internal ref struct PassArray
    {
        private readonly PassArrayStorage _storage;

        private int _startResIndex;
        private int _startRtIndex;

        private int _resIndex;
        private int _rtIndex;

        internal PassArray(PassArrayStorage passArrayStorage)
        {
            _storage = passArrayStorage;

            _startResIndex = passArrayStorage.UsedResourcesIndex;
            _startRtIndex = passArrayStorage.UsedRenderTargetsIndex;

            _resIndex = passArrayStorage.UsedResourcesIndex;
            _rtIndex = passArrayStorage.UsedRenderTargetsIndex;
        }

        internal readonly void Commit()
        {
            _storage.CommitUsedResourceData(_resIndex);
            _storage.CommitUsedRenderTargetData(_rtIndex);
        }

        internal void AddResource(UsedResourceData resourceData)
        {
            int currentIndex = _resIndex++;

            _storage.EnsureUsedResourceSize(_resIndex);
            _storage.UsedResources[currentIndex] = resourceData;
        }

        internal void AddRenderTarget(UsedRenderTargetData renderTargetData)
        {
            int currentIndex = _rtIndex++;

            _storage.EnsureUsedRenderTargetSize(_rtIndex);
            _storage.UsedRenderTargets[currentIndex] = renderTargetData;
        }

        internal readonly ArraySegment<UsedResourceData> UsedResources => new ArraySegment<UsedResourceData>(_storage.UsedResources, _startResIndex, _resIndex - _startResIndex);
        internal readonly ArraySegment<UsedRenderTargetData> UsedRenderTargets => new ArraySegment<UsedRenderTargetData>(_storage.UsedRenderTargets, _startRtIndex, _rtIndex - _startRtIndex);
    }
}
