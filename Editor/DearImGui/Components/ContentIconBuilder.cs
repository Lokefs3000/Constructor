using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering;
using RectpackSharp;
using StbImageSharp;
using System.Buffers;
using System.Collections.Frozen;
using System.Numerics;
using System.Runtime.CompilerServices;
using RHI = Primary.RHI;

namespace Editor.DearImGui.Components
{
    internal sealed class ContentIconBuilder : IDisposable
    {
        private RHI.Texture? _iconTexture;

        private HashSet<string> _iconPaths;
        private FrozenDictionary<int, Boundaries> _iconUVPositions;

        private bool _disposedValue;

        internal ContentIconBuilder()
        {
            _iconTexture = null;

            _iconPaths = new HashSet<string>();
            _iconUVPositions = FrozenDictionary<int, Boundaries>.Empty;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _iconTexture?.Dispose();
                    _iconPaths.Clear();

                    _iconTexture = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ClearIcons()
        {
            _iconPaths.Clear();
        }

        internal int AddIcon(string path)
        {
            _iconPaths.Add(path);
            return path.GetDjb2HashCode();
        }

        internal void Build()
        {
            (string Path, ImageData Data)[] images = new (string Path, ImageData Data)[_iconPaths.Count];
            PackingRectangle[] rectangle = new PackingRectangle[_iconPaths.Count];

            _iconTexture?.Dispose();

            try
            {
                int i = 0;
                foreach (string path in _iconPaths)
                {
                    if (AssetFilesystem.Exists(path))
                    {
                        ImageData data = LoadImage(path);

                        images[i] = (path, data);
                        rectangle[i] = new PackingRectangle(0, 0, (uint)data.Width, (uint)data.Height, i);

                        i++;
                    }
                }

                RectanglePacker.Pack(rectangle.AsSpan(0, i), out PackingRectangle bounds);

                using PoolArray<byte> atlas = ArrayPool<byte>.Shared.Rent((int)(bounds.Width * bounds.Height * 4));
                Span<byte> atlasPixels = atlas.AsSpan();

                Dictionary<int, Boundaries> boundaries = new Dictionary<int, Boundaries>();

                int atlasRowSize = (int)(bounds.Width * 4);
                for (int j = 0; j < i; j++)
                {
                    ref PackingRectangle rect = ref rectangle[j];

                    string path = images[rect.Id].Path;
                    ImageData image = images[rect.Id].Data;

                    int rowSize = image.Width * 4;

                    int sourceCopyOffset = 0;
                    int atlasCopyOffset = (int)(rect.X * 4 + rect.Y * atlasRowSize);

                    Span<byte> sourcePixels = image.Pixels.AsSpan();
                    for (int y = 0; y < rect.Height; y++)
                    {
                        sourcePixels.Slice(sourceCopyOffset, rowSize).CopyTo(atlasPixels.Slice(atlasCopyOffset, rowSize));

                        sourceCopyOffset += rowSize;
                        atlasCopyOffset += atlasRowSize;
                    }

                    Vector2 uvMin = new Vector2(rect.X, rect.Y) / new Vector2(bounds.Width, bounds.Height);
                    Vector2 uvSize = new Vector2(rect.Width, rect.Height) / new Vector2(bounds.Width, bounds.Height);
                    boundaries.Add(path.GetDjb2HashCode(), new Boundaries(uvMin, uvMin + uvSize));
                }

                _iconUVPositions = boundaries.ToFrozenDictionary();

                if (bounds.Width > 0 && bounds.Height > 0)
                {
                    unsafe
                    {
                        fixed (byte* ptr = atlasPixels)
                        {
                            nint ptrV = (nint)ptr;

                            _iconTexture = RenderingManager.Device.CreateTexture(new RHI.TextureDescription
                            {
                                Width = bounds.Width,
                                Height = bounds.Height,
                                Depth = 1,

                                MipLevels = 1,

                                Dimension = RHI.TextureDimension.Texture2D,
                                Format = RHI.TextureFormat.RGBA8un,
                                Memory = RHI.MemoryUsage.Immutable,
                                Usage = RHI.TextureUsage.ShaderResource,
                                CpuAccessFlags = RHI.CPUAccessFlags.None,

                                Swizzle = RHI.TextureSwizzle.Default,
                            }, new Span<nint>(ref ptrV));
                        }
                    }
                }

                EdLog.Gui.Information("Built content view icons..");
            }
            finally
            {
                for (int i = 0; i < images.Length; i++)
                {
                    ImageData data = images[i].Data;
                    data.Pixels.Dispose();
                }
            }
        }

        private unsafe ImageData LoadImage(string path)
        {
            using Stream? stream = AssetFilesystem.OpenStream(path);
            if (stream == null)
            {
                return new ImageData(PoolArray<byte>.Empty, 0, 0);
            }

            using ImageResult result = ImageResult.FromStream(stream);
            if (result.Data.IsEmpty)
            {
                return new ImageData(PoolArray<byte>.Empty, 0, 0);
            }

            PoolArray<byte> pixels = ArrayPool<byte>.Shared.Rent(result.Width * result.Height * 4);
            result.Data.CopyTo(pixels.AsSpan());

            return new ImageData(pixels, result.Width, result.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetIcon(int iconId, out Boundaries boundaries)
        {
            return _iconUVPositions.TryGetValue(iconId, out boundaries);
        }

        public RHI.Texture? Texture => _iconTexture;

        private record struct ImageData(PoolArray<byte> Pixels, int Width, int Height);
    }
}
