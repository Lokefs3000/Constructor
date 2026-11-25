using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Streams;
using K4os.Compression.LZ4.Streams.Frames;
using Primary.Common.Streams;
using SDL;

namespace PrimaryEditor.Startup
{
    internal unsafe sealed class Image : IDisposable
    {
        private SDL_Texture* _pixels;

        private bool _disposedValue;

        internal Image(SDL_Renderer* renderer, BundleReader reader, string imageName, bool blackAsAlpha = false)
        {
            using LZ4DecoderStream stream = LZ4Stream.Decode(new MemoryStream(reader.ReadBytes(imageName)!, false));

            byte[] allRead = new byte[stream.Length];
            stream.ReadExactly(allRead);

            fixed (byte* ptr = allRead)
            {
                SDL_IOStream* ioStream = SDL3.SDL_IOFromMem((nint)ptr, (nuint)allRead.LongLength);
                SDL_Surface* surface = SDL3.SDL_LoadBMP_IO(ioStream, true);

                if (surface == null)
                    throw new NotSupportedException("Failed to load BMP");

                if (blackAsAlpha)
                {
                    SDL_Surface* newSurface = SDL3.SDL_CreateSurface(surface->w, surface->h, SDL_PixelFormat.SDL_PIXELFORMAT_RGBA8888);
                    for (int i = 0; i < newSurface->w * newSurface->h; i++)
                    {
                        ref OpaquePixel src = ref ((OpaquePixel*)surface->pixels)[i];
                        ref Pixel dst = ref ((Pixel*)newSurface->pixels)[i];

                        dst = new Pixel(src.R, 255, 255, 255);
                    }

                    SDL3.SDL_DestroySurface(surface);
                    surface = newSurface;
                }

                _pixels = SDL3.SDL_CreateTextureFromSurface(renderer, surface);
                SDL3.SDL_DestroySurface(surface);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                SDL3.SDL_DestroyTexture(_pixels);

                _disposedValue = true;
            }
        }

        ~Image()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal SDL_Texture* Pixels => _pixels;
    }

    internal readonly record struct OpaquePixel(byte R, byte G, byte B);
    internal readonly record struct Pixel(byte R, byte G, byte B, byte A);
}
