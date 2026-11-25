using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using EditorUI.Built;
using Primary.Pooling;

namespace EditorUI.Text
{
    public sealed class TextCache
    {
        private ObjectPool<TextShapingData> _shapingDataPool;
        private Stack<TextShapingData> _shapingDataReturnStack;

        private Dictionary<ShapingDataKey, CachedShapingData> _cachedShapingData;
        private Dictionary<object, ShapingDataKey> _ownerDataKeyLookup;

        internal TextCache()
        {
            _shapingDataPool = new ObjectPool<TextShapingData>(new PoolPolicy());
            _shapingDataReturnStack = new Stack<TextShapingData>();

            _cachedShapingData = new Dictionary<ShapingDataKey, CachedShapingData>();
            _ownerDataKeyLookup = new Dictionary<object, ShapingDataKey>();
        }

        internal void FinishAfterFrame()
        {
            while (_shapingDataReturnStack.TryPop(out TextShapingData? result))
            {
                _shapingDataPool.Return(result);
            }
        }

        internal TextShapingData GetNewOrCachedData(ShapingDataKey dataKey, object owner, TextShapingData? oldShapingData, out bool exists)
        {
            ref CachedShapingData shapingData = ref CollectionsMarshal.GetValueRefOrAddDefault(_cachedShapingData, dataKey, out exists);
            if (!exists)
            {
                shapingData = new CachedShapingData(_shapingDataPool.Get(), 0);
                shapingData.ShapingData.SetDataKey(dataKey);
            }

            ref ShapingDataKey ownerDataKey = ref CollectionsMarshal.GetValueRefOrAddDefault(_ownerDataKeyLookup, owner, out bool ownerDataKeyExists);
            if (!ownerDataKeyExists || ownerDataKey != dataKey)
            {
                if (!ownerDataKeyExists)
                {
                    ownerDataKey = dataKey;
                }

                if (ownerDataKeyExists)
                {
                    ReturnShapingDataFrom(owner, false);
                }

                ownerDataKey = dataKey;
                ++shapingData.RefCount;
            }

            return shapingData.ShapingData;
        }

        internal void ReturnShapingDataFrom(object owner, bool removeEntry = true)
        {
            if (_ownerDataKeyLookup.TryGetValue(owner, out ShapingDataKey dataKey))
            {
                if (removeEntry)
                    _ownerDataKeyLookup.Remove(owner);

                ref CachedShapingData shapingData = ref CollectionsMarshal.GetValueRefOrNullRef(_cachedShapingData, dataKey);
                if (!Unsafe.IsNullRef(in shapingData))
                {
                    if (--shapingData.RefCount == 0)
                    {
                        _shapingDataReturnStack.Push(shapingData.ShapingData);
                        _cachedShapingData.Remove(dataKey);
                    }
                }
            }
        }

        internal TextShapingData GetTemporaryShapingData(ReadOnlySpan<char> textBuffer)
        {
            TextShapingData shapingData = _shapingDataPool.Get();

            _shapingDataReturnStack.Push(shapingData);
            return shapingData;
        }

        private sealed class PoolPolicy : IObjectPoolPolicy<TextShapingData>
        {
            public TextShapingData Create() => new TextShapingData();
            public bool Return(ref TextShapingData obj)
            {
                obj.ClearInternalValues();
                return true;
            }
        }

        public int CurrentCacheSize => _cachedShapingData.Count;

        private record struct CachedShapingData(TextShapingData ShapingData, int RefCount);
    }

    public readonly record struct ShapingDataKey(int TextHash, BuiltTextBuilder TextBuilder, FontStyleData StyleData, float PixelSize)
    {
        public override int GetHashCode() => HashCode.Combine(TextHash, TextBuilder, StyleData, PixelSize);
    }
}
