using Editor.UI.Assets;
using Primary;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.UI.Font
{
    public sealed class GlyphCache : IDisposable
    {
        private readonly string _basePath;

        private Dictionary<int, RangeData> _ranges;

        private bool _disposedValue;

        internal GlyphCache(AssetId assetId, FontStyle style, FontWeight weight)
        {
            _basePath = Path.Combine(Engine.GlobalSingleton.AssetManager.CacheProvider.GetCacheLocation(assetId)!, $"{(int)style}{(int)weight}_");
            _ranges = new Dictionary<int, RangeData>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (var kvp in _ranges)
                    {
                        if (kvp.Value.Cache.IsModified && !kvp.Value.Cache.IsEmpty)
                        {
                            TrySaveCacheData(kvp.Value.Cache);
                        }
                    }

                    _ranges.Clear();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void RemoveUnusedRanges(long timestamp)
        {
            using RentedList<int> cullList = new RentedList<int>();
            foreach (var kvp in _ranges)
            {
                if (timestamp - kvp.Value.LastAccessTimestamp > CacheLifetime)
                {
                    cullList.Add(kvp.Key);
                }
            }

            if (!cullList.IsEmpty)
            {
                foreach (int cullKey in cullList)
                {
                    RangeData data = _ranges[cullKey];
                    if (data.Cache.IsModified && !data.Cache.IsEmpty)
                    {
                        TrySaveCacheData(data.Cache);
                    }

                    _ranges.Remove(cullKey);
                }
            }
        }

        private ref RangeData GetGlyphRange(char ch)
        {
            int rangeKey = ch / CachedGlyphRange.RangeSize;

            ref RangeData rangeData = ref CollectionsMarshal.GetValueRefOrAddDefault(_ranges, rangeKey, out bool exists);
            if (!exists)
            {
                CachedGlyphRange range = new CachedGlyphRange((char)(rangeKey * CachedGlyphRange.RangeSize));

                string path = _basePath + rangeKey.ToString();
                if (File.Exists(path))
                {
                    try
                    {
                        using Stream stream = FileUtility.TryWaitOpen(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4, 50);
                        range.LoadFromStream(stream);
                    }
                    catch (Exception)
                    {
                        UIManager.Logger?.Debug("Failed to load cached glyph range");

                        range.ClearRange();
                        File.Delete(path);
                    }
                }

                _ranges[rangeKey] = new RangeData(range, Stopwatch.GetTimestamp());
                rangeData = ref CollectionsMarshal.GetValueRefOrNullRef(_ranges, rangeKey);
            }

            return ref rangeData;
        }

        // TODO: put this in a Task to not stall main thread
        private void TrySaveCacheData(CachedGlyphRange range)
        {
            string path = _basePath + (range.BaseCharIndex / CachedGlyphRange.RangeSize).ToString();

            try
            {
                if (range.IsEmpty)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                else
                {
                    using Stream stream = FileUtility.TryWaitOpen(path, FileMode.Create, FileAccess.Write, FileShare.None, 4, 50);
                    range.FlushToStream(stream);
                }
            }
            catch (Exception)
            {
                UIManager.Logger?.Warning("Failed to save cached glyph range");
                FileUtility.TryDelete(path);
            }
        }

        internal bool TryGetBitmap(char ch, out CachedGlyphBitmap bitmap)
        {
            ref RangeData data = ref GetGlyphRange(ch);

            data.LastAccessTimestamp = Stopwatch.GetTimestamp();
            return data.Cache.TryGetBitmap(ch, out bitmap);
        }

        internal void StoreBitmap(char ch, CachedGlyphBitmap bitmap)
        {
            ref RangeData data = ref GetGlyphRange(ch);

            data.LastAccessTimestamp = Stopwatch.GetTimestamp();
            data.Cache.StoreBitmap(ch, bitmap);
        }

        internal void RemoveBitmap(char ch)
        {
            ref RangeData data = ref GetGlyphRange(ch);

            data.LastAccessTimestamp = Stopwatch.GetTimestamp();
            data.Cache.RemoveBitmap(ch);
        }

        private record struct RangeData(CachedGlyphRange Cache, long LastAccessTimestamp);

        public static readonly long CacheLifetime = (long)(60.0 * Stopwatch.Frequency);
    }
}
