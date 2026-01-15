using Primary;
using Primary.Common.Streams;
using SDL;
using StbImageSharp;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

using static SDL.SDL3;

namespace Editor.Platform.Windows
{
    internal unsafe sealed class StartupDisplayUI : IDisposable
    {
        private SDL_Window* _window;
        private SDL_Renderer* _renderer;

        private SDL_Texture* _splashTexture;
        private SDL_Texture* _glyphTexture;
        private SDL_Texture* _hourglassTexture;

        private GlyphData[] _glyphs;

        private AutoResetEvent _updateEvent;
        private object _updateLock;

        private bool _stepsHaveChanged;
        private bool _progressHasChanged;

        private Stack<LoadingStep> _activeSteps;
        private Stack<BackupStep> _progressStack;

        private int _idleHourglassTime;

        private string _currentDescription;
        private float _currentProgress;

        private DateTime _startTime;
        private long _lastTickTime;

        private bool _disposedValue;

        internal StartupDisplayUI()
        {
            _window = SDL_CreateWindow("Startup", 500, 300, SDL_WindowFlags.SDL_WINDOW_BORDERLESS);
            _renderer = SDL_CreateRenderer(_window, "software");

            SDL_SetWindowHitTest(_window, &HitTest, nint.Zero);
            SDL_SetWindowProgressState(_window, SDL_ProgressState.SDL_PROGRESS_STATE_NORMAL);

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

            //Hourglass icon
            {
                using ImageResult result = ImageResult.FromMemory(reader.ReadBytes("Hourglass")!, ColorComponents.RedGreenBlueAlpha);

                SDL_Surface* surface = SDL_CreateSurfaceFrom(result.Width, result.Height, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888, (nint)result.DataPtr, result.Width * 4);
                _hourglassTexture = SDL_CreateTextureFromSurface(_renderer, surface);

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

            _updateEvent = new AutoResetEvent(false);
            _updateLock = new object();

            _stepsHaveChanged = false;
            _progressHasChanged = false;

            _activeSteps = new Stack<LoadingStep>();
            _progressStack = new Stack<BackupStep>();

            _idleHourglassTime = 0;

            _currentDescription = string.Empty;
            _currentProgress = 0.0f;

            _startTime = DateTime.UtcNow;
            _lastTickTime = long.MinValue;

            DrawInitial();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

                }

                SDL_DestroyTexture(_hourglassTexture);
                SDL_DestroyTexture(_glyphTexture);
                SDL_DestroyTexture(_splashTexture);
                SDL_DestroyRenderer(_renderer);
                SDL_DestroyWindow(_window);

                _disposedValue = true;
            }
        }

        ~StartupDisplayUI()
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
            SDL_WaitEventTimeout(null, 48);
            SDL_FlushEvents(SDL_EventType.SDL_EVENT_FIRST, SDL_EventType.SDL_EVENT_LAST);
        }

        internal void Draw()
        {
            if (Stopwatch.GetTimestamp() >= _lastTickTime)
                _lastTickTime = Stopwatch.GetTimestamp() + s_tickMaxDifference;
            else
                return;

            _idleHourglassTime = (_idleHourglassTime + 1) % s_hourglassFrameOffsets.Length;

            lock (_updateLock)
            {
                if (_progressHasChanged)
                {
                    if (_activeSteps.Count == 0)
                        return;

                    if (_stepsHaveChanged)
                    {
                        ResetForStepChange();
                    }

                    SDL_FRect srcRect = new SDL_FRect { x = 11, y = 254, w = 230 - 11, h = 14 };
                    SDL_RenderTexture(_renderer, _splashTexture, &srcRect, &srcRect);

                    SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
                    DrawText(new Vector2(10.0f, 266.0f), 14.0f, $"{_currentDescription} - {(DateTime.UtcNow - _startTime).ToString(@"hh\.mm\:ss\:ff", CultureInfo.InvariantCulture)}");

                    SDL_FRect barRect = new SDL_FRect { x = 11, y = 273, w = 348 * _currentProgress, h = 16 };

                    SDL_SetRenderDrawColor(_renderer, 184, 118, 51, 255);
                    SDL_RenderFillRect(_renderer, &barRect);

                    if (((int)_idleHourglassTime) != ((int)((_idleHourglassTime + 1) % s_hourglassFrameOffsets.Length)))
                    {
                        SDL_FRect hourglassSrcRect = new SDL_FRect { x = s_hourglassFrameOffsets[(int)_idleHourglassTime].X, y = s_hourglassFrameOffsets[(int)_idleHourglassTime].Y, w = 22, h = 22 };
                        SDL_FRect hourglassDstRect = new SDL_FRect { x = 370, y = 270, w = 22, h = 22 };

                        SDL_RenderTexture(_renderer, _splashTexture, &hourglassDstRect, &hourglassDstRect);
                        SDL_RenderTexture(_renderer, _hourglassTexture, &hourglassSrcRect, &hourglassDstRect);
                    }

                    SDL_RenderPresent(_renderer);
                    SDL_SetWindowProgressValue(_window, _currentProgress);
                }
            }
        }

        private void DrawInitial()
        {
            SDL_FRect dstRect = new SDL_FRect { w = 500, h = 300 };

            SDL_SetRenderDrawColor(_renderer, 59, 59, 59, 255);
            SDL_RenderClear(_renderer);

            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
            SDL_RenderTexture(_renderer, _splashTexture, null, &dstRect);

            SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);

            DrawText(new Vector2(10.0f, 248.0f), 24.0f, "???");
            //DrawText(new Vector2(10.0f, 266.0f), 14.0f, $"{_completed}/{_total} - {(DateTime.UtcNow - _startTime).ToString(@"hh\.mm\:ss\:ff", CultureInfo.InvariantCulture)}");
            DrawText(new Vector2(10.0f, 20.0f), 16.0f, typeof(Editor).Assembly.GetName().Version!.ToString());
            DrawText(new Vector2(10.0f, 40.0f), 16.0f, typeof(Engine).Assembly.GetName().Version!.ToString());

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

        private void ResetForStepChange()
        {
            SDL_FRect dstRect = new SDL_FRect { x = 10, y = 224, w = 490, h = 24 };

            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
            SDL_RenderTexture(_renderer, _splashTexture, &dstRect, &dstRect);

            DrawText(new Vector2(10.0f, 248.0f), 24.0f, _activeSteps.Peek().Title);

            SDL_FRect bgRect = new SDL_FRect { x = 10, y = 272, w = 350, h = 18 };

            SDL_SetRenderDrawColor(_renderer, 40, 40, 40, 255);
            SDL_RenderFillRect(_renderer, &bgRect);
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

        public void PushStep(string title, string? newDescription = null, float newProgress = 0.0f)
        {
            lock (_updateLock)
            {
                _activeSteps.Push(new LoadingStep(title));
                _progressStack.Push(new BackupStep(_currentProgress, _currentDescription, _startTime));

                _currentDescription = newDescription ?? string.Empty;
                _currentProgress = newProgress;
                _startTime = DateTime.UtcNow;

                _stepsHaveChanged = true;
                _progressHasChanged = true;
            }
        }

        public void PopStep()
        {
            lock (_updateLock)
            {
                _activeSteps.Pop();
                BackupStep backup = _progressStack.Pop();

                _currentDescription = backup.Description;
                _currentProgress = backup.Progress;
                _startTime = backup.Time;

                _stepsHaveChanged = true;
                _progressHasChanged = true;
            }
        }

        public string Description
        {
            get
            {
                lock (_updateLock)
                {
                    return _currentDescription;
                }
            }
            set
            {
                lock (_updateLock)
                {
                    _currentDescription = value;
                    _progressHasChanged = true;
                }
            }
        }

        public float Progress
        {
            get
            {
                lock (_updateLock)
                {
                    return _currentProgress;
                }
            }
            set
            {
                lock (_updateLock)
                {
                    _currentProgress = value;
                    _progressHasChanged = true;
                }
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static SDL_HitTestResult HitTest(SDL_Window* window, SDL_Point* point, nint userData)
        {
            return SDL_HitTestResult.SDL_HITTEST_DRAGGABLE;
        }

        private static readonly Vector2[] s_hourglassFrameOffsets = [
            new Vector2(0.0f, 0.0f),
            new Vector2(22.0f, 0.0f),
            new Vector2(44.0f, 0.0f),
            new Vector2(88.0f, 0.0f),

            new Vector2(0.0f, 22.0f),
            new Vector2(22.0f, 22.0f),
            new Vector2(44.0f, 22.0f),
            new Vector2(88.0f, 22.0f),

            new Vector2(0.0f, 44.0f),
            new Vector2(22.0f, 44.0f),
            new Vector2(44.0f, 44.0f),
            new Vector2(88.0f, 44.0f),

            new Vector2(0.0f, 66.0f),
            new Vector2(22.0f, 66.0f),
            new Vector2(44.0f, 66.0f),
            new Vector2(88.0f, 66.0f),

            new Vector2(0.0f, 88.0f),
            new Vector2(22.0f, 88.0f),
            new Vector2(44.0f, 88.0f),
            ];

        private static long s_tickMaxDifference = (long)(0.1 * Stopwatch.Frequency);

        private readonly record struct GlyphData(bool IsDrawable, Vector4 Boundaries, SDL_FRect Source, float Advance);
        private readonly record struct LoadingStep(string Title);
        private readonly record struct BackupStep(float Progress, string Description, DateTime Time);
    }
}
