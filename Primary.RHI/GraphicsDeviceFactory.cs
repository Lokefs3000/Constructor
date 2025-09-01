using Serilog;
using System.Reflection;
using System.Runtime.Loader;

namespace Primary.RHI
{
    public static class GraphicsDeviceFactory
    {
        /// <summary>
        /// Loads the assembly which contains the implementation for <paramref name="api"/> and creates a new graphics device.
        /// </summary>
        /// <param name="api">API assembly to use.</param>
        /// <returns>A graphics device interface from the specified API</returns>
        public static GraphicsDevice Create(GraphicsAPI api, ILogger logger)
        {
            string libName = $"Primary.RHI.{api}.dll";

            if (!File.Exists(libName))
            {
                throw new FileNotFoundException(libName);
            }

            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(libName));
            Type factory = assembly.GetType($"Primary.RHI.{api}.GraphicsDeviceFactory", true)!;
            MethodInfo method = factory.GetMethod("Create", BindingFlags.Static | BindingFlags.Public)!;
            GraphicsDeviceFactoryCreateImpl impl = method.CreateDelegate<GraphicsDeviceFactoryCreateImpl>();

            return impl(logger);
        }
    }

    public delegate GraphicsDevice GraphicsDeviceFactoryCreateImpl(ILogger logger);

    public enum GraphicsAPI : byte
    {
        None = 0,

        Vulkan,
        Direct3D12
    }
}
