using VoxelizationDemo.Core;

namespace VoxelizationDemo
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            using (VoxelRuntime runtime = new VoxelRuntime(args))
            {
                runtime.Run();
            }
        }
    }
}
