using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ExportValidator
{
    public class Program
    {
        private static unsafe int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No exe supplied");
                return 1;
            }

            string exe = Path.GetFullPath(args[0]);
            if (!NativeLibrary.TryLoad(exe, out nint handle))
            {
                Console.WriteLine("Failed to load exe");
                return 1;
            }

            if (!NativeLibrary.TryGetExport(handle, "D3D12SDKPath", out nint d3d12SdkPath))
            {
                Console.WriteLine("Failed to get \"D3D12SDKPath\" export");
                return 1;
            }

            if (!NativeLibrary.TryGetExport(handle, "D3D12SDKVersion", out nint d3d12SdkVersion))
            {
                Console.WriteLine("Failed to get \"D3D12SDKVersion\" export");
                return 1;
            }

            ulong val = Unsafe.ReadUnaligned<ulong>(d3d12SdkPath.ToPointer());
            string path = new string((sbyte*)(nint)val);

            Console.WriteLine("D3D12SDKPath: " + path);
            Console.WriteLine("D3D12SDKVersion: " + *((int*)d3d12SdkVersion.ToPointer()));

            Console.WriteLine();

            string find = Path.Combine(Path.GetDirectoryName(exe)!, path, "D3D12Core.dll");
            if (File.Exists(find))
            {
                if (!NativeLibrary.TryLoad(find, out nint handle2))
                {
                    Console.WriteLine("Failed to load: " + find);
                }
                else if (NativeLibrary.TryGetExport(handle2, "D3D12SDKVersion", out nint d3d12SdkVersion2))
                {
                    Console.WriteLine("SDK version supplied: " + *((int*)d3d12SdkVersion2.ToPointer()));
                    Console.WriteLine("SDK versions: " + ((*((int*)d3d12SdkVersion2.ToPointer()) == *((int*)d3d12SdkVersion.ToPointer())) ? "Match" : "Dont match"));
                }
                else
                    Console.WriteLine("Failed to get sdk version export");
            }
            else
                Console.WriteLine("Failed to find: " + find);

            Console.ReadLine();
            return 0;
        }
    }

    internal static partial class Win32
    {
        [LibraryImport("user32.dll")]
        public static unsafe partial int LoadStringA(nint hInstance, uint uId, byte* lpBuffer, int cchBufferMax);
    }
}