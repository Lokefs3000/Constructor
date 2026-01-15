using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Mathematics;
using Primary.RHI2;
using Primary.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Editor.UI
{
    internal sealed class DynamicSubAtlas : IDisposable
    {
        private bool _hasAnyTiles;
        private int _atlasSize;
        private Queue<AtlasTile>[] _tileSizes;

        private List<FittedTile> _fittedIcons;

        private RHITexture? _texture;
        private ConcurrentDictionary<int, DynAtlasIcon> _icons;

        private bool _disposedValue;

        internal DynamicSubAtlas()
        {
            _hasAnyTiles = false;
            _atlasSize = 0;
            _tileSizes = new Queue<AtlasTile>[4];
            for (int i = 0; i < _tileSizes.Length; i++)
                _tileSizes[i] = new Queue<AtlasTile>();

            _fittedIcons = new List<FittedTile>();

            _texture = null;
            _icons = new ConcurrentDictionary<int, DynAtlasIcon>();
        }

        internal DynamicSubAtlas(CachedDynamicIconSetData iconSet) : this()
        {
            ExceptionUtility.Assert(TryConsumeIconSet(iconSet));
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal unsafe void BuildAtlasTexture()
        {
            SafePtrArray<uint> pixels = SafePtrArray<uint>.Null;
            try
            {
                pixels = SafePtrArray<uint>.Allocate(_atlasSize * _atlasSize * 4);

                int atlasRowPitch = _atlasSize;

                Span<FittedTile> tiles = _fittedIcons.AsSpan();
                for (int i = 0; i < tiles.Length; i++)
                {
                    ref FittedTile tile = ref tiles[i];

                    int tileRowPitch = tile.Size;
                    int currentIndex = tile.Offset.X + tile.Offset.Y * atlasRowPitch;

                    fixed (byte* ptr = tile.Pixels)
                    {
                        SafePtrArray<uint> source = new SafePtrArray<uint>((uint*)ptr, tile.Pixels.Length);
                        for (int y = 0; y < tile.Size; y++)
                        {
                            SafePtrArray<uint>.Copy(source, y * tile.Size, pixels, currentIndex, tileRowPitch);

                            currentIndex += atlasRowPitch;
                        }
                    }
                }

                //TODO: replace this when the RHI can handle multithreaded resource creation
                ThreadHelper.ExecuteOnMainThread(() =>
                {
                    nint basePixelsPtr = pixels.OpaquePointer;

                    _texture?.Dispose();
                    _texture = RHIDevice.Instance!.CreateTexture(new RHITextureDescription
                    {
                        Width = _atlasSize,
                        Height = _atlasSize,
                        DepthOrArraySize = 1,

                        MipLevels = 1,

                        Dimension = RHIDimension.Texture2D,
                        Format = RHIFormat.RGBA8_UNorm,
                        Usage = RHIResourceUsage.ShaderResource,

                        Swizzle = RHISwizzle.RGBA,
                    }, new Span<nint>(ref basePixelsPtr), $"DynAtlas-{_fittedIcons.Count}");
                }).Wait();
            }
            catch (Exception ex)
            {
                EdLog.Gui.Error(ex, "Failed to build atlas texture!");
            }
            finally
            {
                pixels.Dispose();
            }
        }

        internal void Reset()
        {
            _hasAnyTiles = false;
            _atlasSize = 0;
            for (int i = 0; i < _tileSizes.Length; i++)
                _tileSizes[i].Clear();
            _fittedIcons.Clear();
            _icons.Clear();
        }

        internal bool TryConsumeIconSet(CachedDynamicIconSetData iconSet)
        {
            if (!_hasAnyTiles)
            {
                int atlasSize = DynAtlasInitialSize;

                //brute force
                do
                {
                    Reset();
                    CreateAtlasOfSize(atlasSize);

                    if (TryFitIconSet(ref iconSet))
                    {
                        break;
                    }

                    atlasSize *= 2;
                } while (true);

                _hasAnyTiles = true;
                return true;
            }
            else
            {
                return TryFitIconSet(ref iconSet);
            }
        }

        private bool TryFitIconSet(ref readonly CachedDynamicIconSetData iconSet)
        {
            int startCount = _fittedIcons.Count;

            for (int i = 0; i < iconSet.Sizes.Length; i++)
            {
                if (iconSet.Sizes[i] == int.MinValue || iconSet.ImageData[i].Length == 0)
                    continue;

                int size = iconSet.Sizes[i] < 64 ? 64 : (int)BitOperations.RoundUpToPowerOf2((uint)iconSet.Sizes[i]);
                if (!TryGetFittingTile(size, out Int2 offset))
                {
                    RemoveNewlyFitted();
                    return false;
                }

                FittedTile tile = new FittedTile(iconSet.Ids[i], offset, size, iconSet.ImageData[i]);
                _fittedIcons.Add(tile);
            }

            for (int i = startCount; i < _fittedIcons.Count; i++)
            {
                FittedTile tile = _fittedIcons[i];
                DynAtlasIcon icon = new DynAtlasIcon(tile.Size, new Boundaries(
                    tile.Offset.AsVector2() / new Vector2(_atlasSize),
                    (tile.Offset + new Int2(tile.Size)).AsVector2() / new Vector2(_atlasSize)));

                bool ret = _icons.TryAdd(tile.Id, icon);
                if (!ret)
                    EdLog.Gui.Warning("Failed to add icon: {id} to internal dictionary!", tile.Id);
            }

            return true;

            void RemoveNewlyFitted() => _fittedIcons.RemoveRange(startCount, _fittedIcons.Count - startCount);
        }

        private void CreateAtlasOfSize(int size)
        {
            int tileCount = size / DynAtlasInitialTileSize;
            int lastIndex = 0;

            for (int y = 0; y < tileCount; y++)
            {
                for (int x = 0; x < tileCount; x++)
                {
                    AtlasTile tile = new AtlasTile(new Int2(x, y) * DynAtlasInitialTileSize, DynAtlasInitialTileSize);
                    _tileSizes[lastIndex].Enqueue(tile);
                }
            }

            _atlasSize = size;
        }

        private bool TryGetFittingTile(int size, out Int2 offset)
        {
            Unsafe.SkipInit(out offset);

            bool hasTriedSubdivideOnce = false;

        TryDequeueTile:
            int currentIndex = Math.Min(BitOperations.LeadingZeroCount((uint)size) - 23, 3);
            if (_tileSizes[currentIndex].TryDequeue(out AtlasTile tile))
            {
                offset = tile.Offset;

                Debug.Assert(tile.Size == size);
                return true;
            }

            if (size == DynAtlasInitialTileSize || currentIndex + 1 >= _tileSizes.Length)
                return false;

            if (!hasTriedSubdivideOnce)
            {
                hasTriedSubdivideOnce = true;
                if (TrySubdivideLargerTiles(currentIndex))
                {
                    goto TryDequeueTile;
                }
            }

            return false;
        }

        private bool TrySubdivideLargerTiles(int currentIndex)
        {
            Queue<AtlasTile> nextInLine = _tileSizes[currentIndex - 1];
            if (nextInLine.Count == 0)
            {
                if (!TrySubdivideLargerTiles(currentIndex - 1))
                    return false;
            }

            AtlasTile tile = nextInLine.Dequeue();
            int halfSize = tile.Size / 2;

            Queue<AtlasTile> currentLine = _tileSizes[currentIndex];

            currentLine.Enqueue(new AtlasTile(tile.Offset, halfSize));
            currentLine.Enqueue(new AtlasTile(tile.Offset + new Int2(halfSize, 0), halfSize));
            currentLine.Enqueue(new AtlasTile(tile.Offset + new Int2(0, halfSize), halfSize));
            currentLine.Enqueue(new AtlasTile(tile.Offset + new Int2(halfSize), halfSize));

            return true;
        }

        internal bool TryGetAtlasIcon(int id, out DynAtlasIcon icon) => _icons.TryGetValue(id, out icon);

        internal bool IsEmpty => _fittedIcons.Count == 0;

        internal RHITexture? AtlasTexture => _texture;

        public const int DynAtlasInitialSize = 512;
        public const int DynAtlasInitialTileSize = 256;

        private record struct AtlasTile(Int2 Offset, int Size);
        private record struct FittedTile(int Id, Int2 Offset, int Size, byte[] Pixels);
    }

    public record struct DynAtlasIcon(int Size, Boundaries UVs);
}
