using CommunityToolkit.HighPerformance;
using Primary.Common;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Processors
{
    public sealed class BundleProcessor : IAssetProcessor
    {
        public bool Execute(object args_in)
        {
            BundleProcessorArgs args = (BundleProcessorArgs)args_in;

            using FileStream stream = File.Open(args.AbsoluteOutputPath, FileMode.Create, FileAccess.Write);
            using BinaryWriter bw = new BinaryWriter(stream);

            Span<string> files = args.AbsoluteFilepaths.Span;
            using PoolArray<FileInfo> infos = new PoolArray<FileInfo>(ArrayPool<FileInfo>.Shared.Rent(files.Length), true);

            try
            {
                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];
                    int index = file.IndexOf('~');

                    string real = Path.GetFullPath(index != -1 ? file.Substring(0, index) : file);
                    string remapped = index != -1 ? file.Substring(index + 1) : file;

                    infos[i] = new FileInfo(real, remapped, File.Open(real, FileMode.Open));
                }

                bw.Write(HeaderMagic);
                bw.Write(HeaderVersion);

                bw.Write((uint)files.Length);

                for (int i = 0; i < files.Length; i++)
                {
                    ref FileInfo fi = ref infos[i];

                    bw.Write(fi.RemappedPath);
                    bw.Write((ulong)(fi.Stream?.Length ?? 0));
                }

                using PoolArray<byte> readBuffer = ArrayPool<byte>.Shared.Rent(8096);

                for (int i = 0; i < files.Length; i++)
                {
                    ref FileInfo fi = ref infos[i];
                    if (fi.Stream != null)
                    {
                        int read = 0;
                        while ((read = fi.Stream.Read(readBuffer.AsSpan())) > 0)
                        {
                            bw.Write(readBuffer.AsSpan(0, read));
                        }
                    }
                }
            }
            finally
            {
                for (int i = 0; i < files.Length; i++)
                {
                    infos[i].Stream?.Dispose();
                }
            }

            return true;
        }

        private record struct FileInfo(string RealPath, string RemappedPath, FileStream? Stream);

        private const uint HeaderMagic = 0x4c444e42;
        private const uint HeaderVersion = 0;
    }

    public struct BundleProcessorArgs
    {
        public Memory<string> AbsoluteFilepaths;
        public string AbsoluteOutputPath;
    }
}
