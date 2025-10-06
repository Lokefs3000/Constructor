using Primary.Common.Streams;
using SDL;
using StbImageSharp;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

using static SDL.SDL3;

namespace Editor.Platform.Windows
{
    internal unsafe sealed class ImporterDisplay : IDisposable
    {
        private SDL_Window* _window;
        private SDL_Renderer* _renderer;

        private SDL_Texture* _splashTexture;
        private SDL_Texture* _glyphTexture;

        private GlyphData[] _glyphs;

        private float _progress;
        private int _completed;
        private int _total;

        private DateTime _startTime;

        private bool _disposedValue;

        internal ImporterDisplay()
        {
            _window = SDL_CreateWindow("Importer", 500, 300, SDL_WindowFlags.SDL_WINDOW_BORDERLESS);
            _renderer = SDL_CreateRenderer(_window, "software");

            SDL_SetWindowHitTest(_window, &HitTest, nint.Zero);

            using BundleReader reader = new BundleReader(File.OpenRead("SplashData.bundle")!, true);

            //Splash icon
            {
                using ImageResult result = ImageResult.FromMemory(reader.ReadBytes("Splash")!, ColorComponents.RedGreenBlueAlpha);

                SDL_Surface* surface = SDL_CreateSurfaceFrom(result.Width, result.Height, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888, (nint)result.DataPtr, result.Width * 4);
                _splashTexture = SDL_CreateTextureFromSurface(_renderer, surface);

                SDL_DestroySurface(surface);
            }

            //Glyph texture
            {
                using ImageResult result = ImageResult.FromMemory(reader.ReadBytes("Font")!, ColorComponents.RedGreenBlueAlpha);

                SDL_Surface* surface = SDL_CreateSurfaceFrom(result.Width, result.Height, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888, (nint)result.DataPtr, result.Width * 4);

                int length = result.Width * result.Height * 4;
                for (int i = 0; i < length; i += 4)
                {
                    ((byte*)surface->pixels)[i + 3] = (byte)Math.Clamp((int)MathF.Pow(((byte*)surface->pixels)[i], 2.0f), 0, 255);
                }

                _glyphTexture = SDL_CreateTextureFromSurface(_renderer, surface);

                SDL_DestroySurface(surface);
            }

            //Glyph data
            {
                _glyphs = new GlyphData[96];

                JsonNode node = JsonNode.Parse(reader.ReadString("Data")!)!;

                foreach (JsonObject glyph in node["glyphs"]!.AsArray()!)
                {
                    int unicode = glyph["unicode"]!.GetValue<int>();

                    JsonNode? planeBounds = glyph["planeBounds"];
                    JsonNode? atlasBounds = glyph["atlasBounds"];

                    _glyphs[unicode - 32] = new GlyphData
                    {
                        IsDrawable = planeBounds != null && atlasBounds != null,
                        Boundaries = planeBounds != null ? new Vector4(planeBounds["left"]!.GetValue<float>(), planeBounds["top"]!.GetValue<float>(), planeBounds["right"]!.GetValue<float>(), planeBounds["bottom"]!.GetValue<float>()) : default,
                        Source = atlasBounds != null ? Constructor(atlasBounds["left"]!.GetValue<float>(), atlasBounds["top"]!.GetValue<float>(), atlasBounds["right"]!.GetValue<float>(), atlasBounds["bottom"]!.GetValue<float>()) : default,
                        Advance = glyph["advance"]!.GetValue<float>()
                    };
                }

                static SDL_FRect Constructor(float left, float top, float right, float bottom)
                {
                    return new SDL_FRect
                    {
                        x = left,
                        y = top,
                        w = right - left,
                        h = bottom - top,
                    };
                }
            }

            _startTime = DateTime.UtcNow;

            DrawInitial();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

                }

                SDL_DestroyTexture(_splashTexture);
                SDL_DestroyRenderer(_renderer);
                SDL_DestroyWindow(_window);

                _disposedValue = true;
            }
        }

        ~ImporterDisplay()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void Poll()
        {
            SDL_WaitEventTimeout(null, 100);

            SDL_Event @event = new SDL_Event();
            while (SDL_PollEvent(&@event)) ;
        }

        internal void Draw()
        {
            SDL_FRect srcRect = new SDL_FRect { x = 11, y = 254, w = 200 - 11, h = 14 };
            SDL_RenderTexture(_renderer, _splashTexture, &srcRect, &srcRect);

            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
            DrawText(new Vector2(10.0f, 266.0f), 14.0f, $"{_completed}/{_total} - {(DateTime.UtcNow - _startTime).ToString(@"hh\.mm\:ss\:ff", CultureInfo.InvariantCulture)}");

            SDL_FRect barRect = new SDL_FRect { x = 11, y = 273, w = 348 * MathF.Min((float)(_completed / (double)_total), 1.0f), h = 16 };

            SDL_SetRenderDrawColor(_renderer, 184, 118, 51, 255);
            SDL_RenderFillRect(_renderer, &barRect);

            SDL_RenderPresent(_renderer);
        }

        private void DrawInitial()
        {
            SDL_FRect dstRect = new SDL_FRect { w = 500, h = 300 };

            SDL_SetRenderDrawColor(_renderer, 59, 59, 59, 255);
            SDL_RenderClear(_renderer);

            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
            SDL_RenderTexture(_renderer, _splashTexture, null, &dstRect);

            SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);

            DrawText(new Vector2(10.0f, 248.0f), 24.0f, "Importing assets..");
            DrawText(new Vector2(10.0f, 266.0f), 14.0f, $"{_completed}/{_total} - {(DateTime.UtcNow - _startTime).ToString(@"hh\.mm\:ss\:ff", CultureInfo.InvariantCulture)}");

            SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_NONE);

            SDL_FRect bgRect = new SDL_FRect { x = 10, y = 272, w = 350, h = 18 };

            SDL_SetRenderDrawColor(_renderer, 40, 40, 40, 255);
            SDL_RenderFillRect(_renderer, &bgRect);

            //SDL_FRect lineRect = new SDL_FRect { x = 3, y = 253, w = 494, h = 44 };
            //SDL_FRect barRect = new SDL_FRect { x = 4, y = 254, w = 492, h = 42 };

            //SDL_SetRenderDrawColor(_renderer, 98, 98, 98, 255);
            //SDL_RenderRect(_renderer, &lineRect);

            //SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255);
            //SDL_RenderFillRect(_renderer, &barRect);

            SDL_RenderPresent(_renderer);

            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
        }

        private void DrawText(Vector2 pos, float scale, in string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                int c = text[i] - 32;
                if (c < 96)
                {
                    ref GlyphData data = ref _glyphs[c];
                    if (data.IsDrawable)
                    {
                        Vector4 offset = data.Boundaries * scale + new Vector4(pos.X, pos.Y, pos.X, pos.Y);

                        SDL_FRect src = data.Source;
                        SDL_FRect dst = new SDL_FRect { x = offset.X, y = offset.Y, w = offset.Z - offset.X, h = offset.W - offset.Y };

                        SDL_RenderTexture(_renderer, _glyphTexture, &src, &dst);
                    }

                    pos.X += data.Advance * scale;
                }
            }
        }

        internal float Progress { get => _progress; set => _progress = MathF.Min(MathF.Max(value, 0.0f), 1.0f); }

        internal int Completed { get => _completed; set => _completed = value; }
        internal int Total { get => _total; set => _total = value; }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static SDL_HitTestResult HitTest(SDL_Window* window, SDL_Point* point, nint userData)
        {
            return SDL_HitTestResult.SDL_HITTEST_DRAGGABLE;
        }

        private readonly record struct GlyphData(bool IsDrawable, Vector4 Boundaries, SDL_FRect Source, float Advance);
    }
}
