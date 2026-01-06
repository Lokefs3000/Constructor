using Primary.Common.Streams;

namespace BundleExplorer
{
    public sealed class Program
    {
        internal static void Main(string[] args)
        {
            using BundleReader reader = new BundleReader(File.OpenRead(args[0]), true);
            foreach (string file in reader.Files)
            {
                File.WriteAllBytes(file, reader.ReadBytes(file)!);
            }
        }
    }
}