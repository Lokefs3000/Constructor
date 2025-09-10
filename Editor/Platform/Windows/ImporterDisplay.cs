using SDL;
using StbImageSharp;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

using static SDL.SDL3;
using Win32 = TerraFX.Interop.Windows.Windows;

namespace Editor.Platform.Windows
{
    internal unsafe sealed class ImporterDisplay : IDisposable
    {
        private SDL_Window* _window;
        private SDL_Renderer* _renderer;

        private SDL_Texture* _texture;

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

            //Splash icon
            {
                using ImageResult result = ImageResult.FromMemory(File.ReadAllBytes("splash.png"), ColorComponents.RedGreenBlueAlpha);

                SDL_Surface* surface = SDL_CreateSurfaceFrom(result.Width, result.Height, SDL_PixelFormat.SDL_PIXELFORMAT_ARGB8888, (nint)result.DataPtr, result.Width * 4);
                _texture = SDL_CreateTextureFromSurface(_renderer, surface);

                SDL_DestroySurface(surface);
            }

            _startTime = DateTime.UtcNow;

            //Draw initial
            {
                SDL_FRect dstRect = new SDL_FRect { w = 500, h = 250 };

                SDL_SetRenderDrawColor(_renderer, 59, 59, 59, 255);
                SDL_RenderClear(_renderer);

                SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
                SDL_RenderTexture(_renderer, _texture, null, &dstRect);

                SDL_FRect lineRect = new SDL_FRect { x = 3, y = 253, w = 494, h = 44 };
                SDL_FRect barRect = new SDL_FRect { x = 4, y = 254, w = 492, h = 42 };

                SDL_SetRenderDrawColor(_renderer, 98, 98, 98, 255);
                SDL_RenderRect(_renderer, &lineRect);

                SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255);
                SDL_RenderFillRect(_renderer, &barRect);

                SDL_RenderPresent(_renderer);

                SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

                }

                SDL_DestroyTexture(_texture);
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
            string statsText = $"{_completed}/{_total} - {(DateTime.UtcNow - _startTime).ToString(null, CultureInfo.InvariantCulture)}s";

            SDL_FRect barRect = new SDL_FRect { x = 4, y = 254, w = 492 * _progress, h = 42 };
            SDL_FRect statsRect = new SDL_FRect { x = 16, y = 16, w = 8 * statsText.Length, h = 7 };

            SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255);
            SDL_RenderFillRect(_renderer, &statsRect);

            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
            SDL_RenderFillRect(_renderer, &barRect);

            SDL_RenderDebugText(_renderer, 16.0f, 16.0f, statsText);

            SDL_RenderPresent(_renderer);
        }

        internal float Progress { get => _progress; set => _progress = MathF.Min(MathF.Max(value, 0.0f), 1.0f); }

        internal int Completed { get => _completed; set => _completed = value; }
        internal int Total { get => _total; set => _total = value; }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static SDL_HitTestResult HitTest(SDL_Window* window, SDL_Point* point, nint userData)
        {
            return SDL_HitTestResult.SDL_HITTEST_DRAGGABLE;
        }
    }
}
