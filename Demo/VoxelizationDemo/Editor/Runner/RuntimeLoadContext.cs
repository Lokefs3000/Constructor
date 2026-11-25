using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace VoxelizationDemo.Editor.Runner
{
    public sealed class RuntimeLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _dependencyResolver;
        private readonly ConcurrentDictionary<string, string> _duplicateDlls;

        private string _duplicateDllFolder;

        internal RuntimeLoadContext(string assemblyPath) : base(true)
        {
            _dependencyResolver = new AssemblyDependencyResolver(assemblyPath);
            _duplicateDlls = new ConcurrentDictionary<string, string>();

            _duplicateDllFolder = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "native-dll");
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            string? assemblyPath = _dependencyResolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override nint LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = _dependencyResolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return nint.Zero;
        }
    }
}
