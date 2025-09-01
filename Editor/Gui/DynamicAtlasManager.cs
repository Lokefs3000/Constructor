using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Mathematics;
using Primary.RenderLayer;
using Serilog;
using StbImageSharp;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Gui
{
    public sealed class DynamicAtlasManager : IDisposable
    {
        private List<DynamicIconSet> _iconSets;
        private List<DynamicSubAtlas> _subAtlasses;

        private Task? _runningTask;

        private bool _disposedValue;

        internal DynamicAtlasManager()
        {
            _iconSets = new List<DynamicIconSet>();
            _subAtlasses = new List<DynamicSubAtlas>();

            _runningTask = null;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    for (int i = 0; i < _subAtlasses.Count; i++)
                    {
                        _subAtlasses[i].Dispose();
                    }

                    _subAtlasses.Clear();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <remarks>Not thread-safe</remarks>
        public DynamicIconSet CreateIconSet(params string[] iconSet)
        {
            DynamicIconSet icons = new DynamicIconSet(iconSet);
            _iconSets.Add(icons);

            return icons;
        }

        /// <remarks>Not thread-safe</remarks>
        public void RemoveIconSet(DynamicIconSet iconSet)
        {
            _iconSets.Remove(iconSet);
        }

        /// <remarks>Not thread-safe</remarks>
        public void TriggerRebuild()
        {
            if (_runningTask != null)
            {
                lock (_runningTask)
                {
                    _runningTask = _runningTask.ContinueWith((_) =>
                    {
                        try
                        {
                            AtlasRebuild(_runningTask);
                        }
                        catch (Exception ex)
                        {
                            EdLog.Gui.Error(ex, "Failed to rebuild gui atlas!");
                        }
                    });
                }
            }
            else
            {
                _runningTask = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        AtlasRebuild(_runningTask);
                    }
                    catch (Exception ex)
                    {
                        EdLog.Gui.Error(ex, "Failed to rebuild gui atlas!");
                    }
                });
            }
        }

        private unsafe void AtlasRebuild(Task? current)
        {
            EdLog.Gui.Information("Rebuilding dynamic atlas..");

            DateTime startTime = DateTime.UtcNow;
            int totalIcons = 0;

            ConcurrentQueue<CachedDynamicIconSetData> iconSets = new ConcurrentQueue<CachedDynamicIconSetData>();

            Task[] tasks = new Task[_iconSets.Count];
            for (int i = 0; i < _iconSets.Count; i++)
            {
                DynamicIconSet iconSet = _iconSets[i];
                tasks[i] = Task.Factory.StartNew(() =>
                {
                    ReadOnlySpan<string> icons = iconSet.Icons;

                    CachedDynamicIconSetData setData = new CachedDynamicIconSetData(iconSet, new int[icons.Length], new int[icons.Length], new byte[icons.Length][]);

                    Array.Fill(setData.Ids, int.MinValue);
                    Array.Fill(setData.Sizes, int.MinValue);
                    Array.Fill(setData.ImageData, Array.Empty<byte>());

                    for (int i = 0; i < icons.Length; i++)
                    {
                        string icon = icons[i];

                        try
                        {
                            using Stream? stream = AssetFilesystem.OpenStream(icon);
                            using ImageResult result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

                            ExceptionUtility.Assert(result.Width == result.Height);
                            ExceptionUtility.Assert(BitOperations.IsPow2(result.Width));
                            ExceptionUtility.Assert(result.Width <= DynamicSubAtlas.DynAtlasInitialTileSize);

                            setData.ImageData[i] = result.Data.ToArray();
                            setData.Sizes[i] = result.Width;
                            setData.Ids[i] = DynamicIconSet.GetIdForString(icon);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed to load icon: \"{ic}\" from set: {uh}", icon, iconSet.UniqueHash);
                        }
                    }

                    iconSets.Enqueue(setData);
                });
            }

            for (int i = 0; i < _subAtlasses.Count; i++)
            {
                _subAtlasses[i].Reset();
            }

            Task.WaitAll(tasks);

            do
            {
                if (iconSets.TryDequeue(out CachedDynamicIconSetData iconSet))
                {
                    totalIcons += iconSet.IconSet.Icons.Length;

                    bool consumedIconSet = false;
                    for (int i = 0; i < _subAtlasses.Count; i++)
                    {
                        DynamicSubAtlas subAtlas = _subAtlasses[i];
                        if (subAtlas.TryConsumeIconSet(iconSet))
                        {
                            consumedIconSet = true;
                            iconSet.IconSet.UpdateAtlasData(subAtlas);
                            break;
                        }
                    }

                    if (!consumedIconSet)
                    {
                        DynamicSubAtlas subAtlas = new DynamicSubAtlas(iconSet);
                        iconSet.IconSet.UpdateAtlasData(subAtlas);

                        _subAtlasses.Add(subAtlas);
                    }
                }
            }
            while (iconSets.Count > 0);

            for (int i = 0; i < _subAtlasses.Count; i++)
            {
                DynamicSubAtlas subAtlas = _subAtlasses[i];

                if (subAtlas.IsEmpty)
                {
                    subAtlas.Dispose();
                    _subAtlasses.RemoveAt(i--);
                }
            }

            if (tasks.Length < _subAtlasses.Count)
                tasks = new Task[_subAtlasses.Count];
            for (int i = 0; i < _subAtlasses.Count; i++)
            {
                DynamicSubAtlas subAtlas = _subAtlasses[i];
                tasks[i] = Task.Factory.StartNew(subAtlas.BuildAtlasTexture);
            }

            Task.WaitAll(tasks.AsSpan(0, _subAtlasses.Count));

            EdLog.Gui.Information("Finished rebuilding dynamic atlas took: {secs}s (Icon sets: {is}, Total icons: {ti})", (DateTime.UtcNow - startTime).TotalSeconds, _iconSets.Count, totalIcons);
        }
    }

    internal record struct CachedDynamicIconSetData(DynamicIconSet IconSet, int[] Ids, int[] Sizes, byte[][] ImageData);

    public sealed class DynamicIconSet
    {
        private DynamicSubAtlas? _subAtlas;

        private int _uniqueHash;
        private List<string> _iconSet;

        private bool _isModified;

        public DynamicIconSet(params string[] iconSet)
        {
            _subAtlas = null;

            _iconSet = [.. iconSet];
            _uniqueHash = 0;

            _isModified = false;

            RecalculateUniqueHash();
        }

        private void RecalculateUniqueHash()
        {
            _uniqueHash = 23;
            for (int i = 0; i < _iconSet.Count; i++)
                _uniqueHash = _uniqueHash * 31 + _iconSet[i].GetDjb2HashCode();
        }

        internal void UpdateAtlasData(DynamicSubAtlas? subAtlas)
        {
            _subAtlas = subAtlas;
            _isModified = false;
        }

        /// <remarks>Not thread-safe</remarks>
        public void AddIcons(ReadOnlySpan<string> icons)
        {
            _iconSet.AddRange(icons);
            RecalculateUniqueHash();
            _isModified = true;
        }

        /// <remarks>Not thread-safe</remarks>
        public void RemoveIcons(ReadOnlySpan<string> icons)
        {
            for (int i = 0; i < icons.Length; i++)
                _iconSet.Remove(icons[i]);
            RecalculateUniqueHash();
            _isModified = true;
        }

        /// <remarks>Not thread-safe</remarks>
        public void AddIcon(string icon) => AddIcons(new ReadOnlySpan<string>(ref icon));
        /// <remarks>Not thread-safe</remarks>
        public void RemoveIcon(string icon) => RemoveIcons(new ReadOnlySpan<string>(ref icon));

        /// <remarks>Thread-safe</remarks>
        public int GetIdForIndex(int index) => _iconSet[index].GetDjb2HashCode();

        public bool TryGetAtlasIcon(int id, out DynAtlasIcon icon)
        {
            if (_subAtlas != null)
                return _subAtlas.TryGetAtlasIcon(id, out icon);
            Unsafe.SkipInit(out icon);
            return true;
        }
        public bool TryGetAtlasIcon(string str, out DynAtlasIcon icon) => TryGetAtlasIcon(GetIdForString(str), out icon);

        public override int GetHashCode() => _uniqueHash;

        internal DynamicSubAtlas? SubAtlas => _subAtlas;
        public GfxTexture AtlasTexture => _subAtlas?.AtlasTexture ?? GfxTexture.Null;

        public ReadOnlySpan<string> Icons => _iconSet.AsSpan();
        public int UniqueHash => _uniqueHash;

        internal bool IsModified => _isModified;

        public static int GetIdForString(in string str) => str.GetDjb2HashCode();
    }
}
