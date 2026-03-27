using Primary.Timing;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Primary.Rendering.Pass
{
    internal sealed class PassDataStorage
    {
        private Dictionary<Type, StorageEntry> _entries;

        internal PassDataStorage()
        {
            _entries = new Dictionary<Type, StorageEntry>();
        }

        internal void ClearEntries()
        {
            Type? pendingRemoval = null;

            foreach (var kvp in _entries)
            {
                ref StorageEntry entry = ref CollectionsMarshal.GetValueRefOrNullRef(_entries, kvp.Key);

                for (int i = 0; i < entry.Index; ++i)
                    entry.Data[i].Clear();

                entry.Analyser.Sample(entry.Index, Time.DeltaTime);

                if (entry.Analyser.IsValid)
                {
                    int calculated = Math.Max(entry.Analyser.Max(), entry.Index);
                    if (calculated == 0)
                    {
                        pendingRemoval ??= kvp.Key;
                    }
                    else if (calculated < entry.Data.Length)
                    {
                        EngLog.Render.Debug("Culling excess render pass data: {fr} -> {to} ({t})", entry.Data, calculated, kvp.Key);

                        Array.Resize(ref entry.Data, calculated);
                    }
                }

                entry.Index = 0;
            }

            if (pendingRemoval != null)
            {
                EngLog.Render.Debug("Removing unreferenced render pass data: {t}", pendingRemoval);

                _entries.Remove(pendingRemoval);
            }
        }

        internal T GetPassData<T>() where T : class, IPassData, new()
        {
            Type type = typeof(T);

            ref StorageEntry entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_entries, type, out bool exists);
            if (exists)
            {
                if (entry.Index == entry.Data.Length)
                {
                    T data = new T();

                    Array.Resize(ref entry.Data, entry.Index + 1);
                    entry.Data[entry.Index++] = data;

                    return data;
                }

                return Unsafe.As<T>(entry.Data[entry.Index++]);
            }
            else
            {
                T data = new T();
                entry = new StorageEntry(data);

                return data;
            }
        }

        private struct StorageEntry
        {
            public AverageAnalyser<int> Analyser;
            public IPassData[] Data;
            public int Index;

            public StorageEntry(IPassData first)
            {
                Analyser = new AverageAnalyser<int>(8, 0.5f);
                Data = [first];
                Index = 1;
            }
        }
    }
}
