using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Primary.RHI.Direct3D12
{
    internal sealed class ResourceTracker
    {
        private ConcurrentDictionary<object, bool> _aliveResources;

        internal ResourceTracker()
        {
            _aliveResources = new ConcurrentDictionary<object, bool>();
        }

        internal void Track(object obj)
        {
            _aliveResources[obj] = true;
        }

        internal void Untrack(object obj)
        {
            _aliveResources.TryRemove(obj, out _);
        }

        internal void PrintUnreleased()
        {
            foreach (var (obj, _) in _aliveResources)
            {
                EngLog.RHI?.Error("Unreleased resource: {res}", obj);
            }
        }
    }
}
