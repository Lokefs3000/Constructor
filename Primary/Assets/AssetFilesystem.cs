﻿using CommunityToolkit.HighPerformance;
using Primary.Common.Streams;
using System.Runtime.CompilerServices;

namespace Primary.Assets
{
    public sealed class AssetFilesystem : IDisposable
    {
        private static AssetFilesystem? s_instance = null;

        private List<ISubFilesystem> _filesystem;
        private bool _disposedValue;

        private ShaderLibrary _shaderLibrary;

        internal AssetFilesystem()
        {
            s_instance = this;

            _filesystem = new List<ISubFilesystem>();
            _shaderLibrary = new ShaderLibrary();
        }

        public void AddFilesystem(ISubFilesystem filesystem)
        {
            if (!_filesystem.Contains(filesystem))
            {
                _filesystem.Add(filesystem);
            }
        }

        public void RemoveFilesystem(ISubFilesystem filesystem)
        {
            _filesystem.Remove(filesystem);
        }

        public string? ReadAsString(ReadOnlySpan<char> path, BundleReader? bundleToReadFrom = null)
        {
            try
            {
                if (bundleToReadFrom?.ContainsFile(path.ToString()) ?? false)
                {
                    return bundleToReadFrom.ReadString(path.ToString());
                }

                for (int i = 0; i < _filesystem.Count; i++)
                {
                    ISubFilesystem filesystem = _filesystem[i];
                    if (filesystem.Exists(path))
                        return filesystem.ReadString(path);
                }
            }
            catch (Exception ex)
            {
                
            }

            return null;
        }

        public Stream? OpenAsStream(ReadOnlySpan<char> path, BundleReader? bundleToReadFrom = null)
        {
            try
            {
                if (bundleToReadFrom?.ContainsFile(path.ToString()) ?? false)
                {
                    Memory<byte> memory = bundleToReadFrom.ReadBytes(path.ToString());
                    return memory.AsStream();
                }

                for (int i = 0; i < _filesystem.Count; i++)
                {
                    ISubFilesystem filesystem = _filesystem[i];
                    if (filesystem.Exists(path))
                        return filesystem.OpenStream(path);
                }
            }
            catch (Exception ex)
            {

            }

            return null;
        }

        public bool DoesFileExist(ReadOnlySpan<char> path, BundleReader? bundleToReadFrom = null)
        {
            try
            {
                if (bundleToReadFrom != null)
                {
                    return bundleToReadFrom.ContainsFile(path.ToString());
                }
                else
                {
                    for (int i = 0; i < _filesystem.Count; i++)
                    {
                        ISubFilesystem filesystem = _filesystem[i];
                        if (filesystem.Exists(path))
                            return true;
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return false;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _shaderLibrary.Dispose();
                    s_instance = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? ReadString(ReadOnlySpan<char> path, BundleReader? bundleToReadFrom = null) => s_instance!.ReadAsString(path, bundleToReadFrom);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Stream? OpenStream(ReadOnlySpan<char> path, BundleReader? bundleToReadFrom = null) => s_instance!.OpenAsStream(path, bundleToReadFrom);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Exists(ReadOnlySpan<char> path, BundleReader? bundleToReadFrom = null) => s_instance!.DoesFileExist(path, bundleToReadFrom);

        public static ShaderLibrary ShaderLibrary => s_instance!._shaderLibrary;
    }
}
