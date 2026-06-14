using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Common.Streams;
using Primary.Mathematics;
using Primary.Memory.Native;
using SDL;

namespace PrimaryEditor.Startup
{
    internal unsafe sealed class StartupSplash : IDisposable
    {
        private SDL_Window* _window;
        private SDL_Renderer* _renderer;

        private Image _backgroundImage;

        private Font _interLight;
        private Font _interRegular;
        private Font _interSemiBold;

        private Thread _updateThread;
        private CancellationTokenSource _updateCts;
        private AutoResetEvent _updateEvent;

        private float _actionNameTextWidth;

        private Lock _updateLock;
        private string? _actionName;

        private bool _disposedValue;

        internal StartupSplash()
        {
            long startTimestamp = Stopwatch.GetTimestamp();

            {
                _window = SDL3.SDL_CreateWindow("Splash", WindowWidth, WindowHeight, SDL_WindowFlags.SDL_WINDOW_BORDERLESS | SDL_WindowFlags.SDL_WINDOW_HIDDEN);
                _renderer = SDL3.SDL_CreateRenderer(_window, "software"u8);
            }

            {
                BundleReader bundle = new BundleReader(File.OpenRead("SplashData.bundle"), true);

                _backgroundImage = new Image(_renderer, bundle, ".\\Splash\\Splash_85p.bmp.lz4");

                _interLight = new Font(_renderer, bundle, ".\\Font\\InterLight");
                _interRegular = new Font(_renderer, bundle, ".\\Font\\InterRegular");
                _interSemiBold = new Font(_renderer, bundle, ".\\Font\\InterSemiBold");
            }

            {
                _updateThread = new Thread(UpdateProc);
                _updateCts = new CancellationTokenSource();
                _updateEvent = new AutoResetEvent(false);
            }

            {
                _updateLock = new Lock();
                _actionName = null;
            }

            _actionNameTextWidth = 0.0f;

            SDL3.SDL_ShowWindow(_window);
            SDL3.SDL_SetWindowHitTest(_window, &WindowHitTest, nint.Zero);

            PresentInitialScreen();

            SDL3.SDL_PumpEvents();
            SDL3.SDL_FlushEvents(0, uint.MaxValue);

            TimeSpan splashLoadTime = Stopwatch.GetElapsedTime(startTimestamp);

            _updateThread.Start();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _updateCts.Cancel();
                    _updateEvent.Set();
                    _updateThread.Join();

                    _updateEvent.Dispose();
                    _updateCts.Dispose();

                    _backgroundImage.Dispose();
                    _interLight.Image.Dispose();
                    _interRegular.Image.Dispose();
                    _interSemiBold.Image.Dispose();

                    SDL3.SDL_DestroyRenderer(_renderer);
                    SDL3.SDL_DestroyWindow(_window);
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #region Threading
        private void UpdateProc()
        {
            TimeSpan maxWaitTime = TimeSpan.FromSeconds(0.05);
            while (!_updateCts.IsCancellationRequested)
            {
                _updateEvent.WaitOne(maxWaitTime);
                _updateEvent.Reset();

                bool hasModifiedImage = false;

                using (_updateLock.EnterScope())
                {
                    if (_actionName != null)
                    {
                        Int2 position = new Int2(14, WindowHeight - 102);
                        ResetRegionToBackground(new Rect(new Int2(position.X, position.Y - 16), new Int2(Math.Min((int)MathF.Ceiling(_actionNameTextWidth), WindowWidth - 14), 20)));
                        
                        _actionNameTextWidth = DrawText(position, _interRegular, _actionName);
                        _actionName = null;

                        hasModifiedImage = true;
                    }
                }

                if (hasModifiedImage)
                {
                    SDL3.SDL_RenderPresent(_renderer);
                }
            }
        }
        #endregion
        #region Drawing
        private void PresentInitialScreen()
        {
            SDL3.SDL_RenderTexture(_renderer, _backgroundImage.Pixels, null, null);

            SDL3.SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
            DrawText(new Int2(8, WindowHeight - 128), _interSemiBold, "ProjectName");
            _actionNameTextWidth = DrawText(new Int2(14, WindowHeight - 102), _interRegular, "Starting..");

            SDL3.SDL_RenderPresent(_renderer);
        }

        private void ResetRegionToBackground(Rect rect)
        {
            SDL_FRect drawRect = new SDL_FRect
            {
                x = rect.X,
                y = rect.Y,
                w = rect.Width,
                h = rect.Height
            };

            SDL3.SDL_RenderTexture(_renderer, _backgroundImage.Pixels, &drawRect, &drawRect);
        }

        private float DrawText(Int2 position, Font font, string text)
        {
            Vector2 currentPos = position.AsVector2();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (font.TryGetGlyph(c, out FontGlyph value))
                {
                    SDL_FRect src = new SDL_FRect
                    {
                        x = value.AtlasBounds.X,
                        y = value.AtlasBounds.Y,
                        w = value.AtlasBounds.Width,
                        h = value.AtlasBounds.Height
                    };

                    SDL_FRect dst = new SDL_FRect
                    {
                        x = value.PlaneBounds.Minimum.X + currentPos.X,
                        y = value.PlaneBounds.Minimum.Y + currentPos.Y,
                        w = value.AtlasBounds.Width,
                        h = value.AtlasBounds.Height
                    };

                    SDL3.SDL_RenderTexture(_renderer, font.Image.Pixels, &src, &dst);

                    currentPos.X += value.Advance;
                    if (currentPos.X > WindowWidth)
                        break;
                }
            }

            return currentPos.X - position.X;
        }
        #endregion

        public string ActionName
        {
            set
            {
                using (_updateLock.EnterScope())
                {
                    _actionName = value;
                    _updateEvent.Set();
                }
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static SDL_HitTestResult WindowHitTest(SDL_Window* window, SDL_Point* point, nint data)
        {
            return SDL_HitTestResult.SDL_HITTEST_DRAGGABLE;
        }

        private const int WindowWidth = 518;
        private const int WindowHeight = 544;
    }
}
