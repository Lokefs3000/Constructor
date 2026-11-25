using System.Diagnostics;
using Primary;
using VoxelizationDemo.Core;
using VoxelizationDemo.Runtime;

namespace VoxelizationDemo
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            if (args.Contains("--runtime"))
            {
                using (PlayRuntime runtime = new PlayRuntime(args))
                {
                    runtime.Run();
                }
            }
            else
            {
                using (VoxelRuntime runtime = new VoxelRuntime(args))
                {
                    runtime.Run();
                }
            }
        }
    }
}
