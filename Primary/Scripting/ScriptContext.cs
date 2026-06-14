using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace Primary.Scripting
{
    public sealed class ScriptContext : IDisposable
    {
        private readonly ScriptingManager _manager;
        private readonly AssemblyLoadContext _loadContext;

        private readonly HashSet<Assembly> _loadedAssemblies;

        private bool _disposedValue;

        internal ScriptContext(ScriptingManager manager, string name)
        {
            _manager = manager;
            _loadContext = new AssemblyLoadContext(name, true);

            _loadedAssemblies = new HashSet<Assembly>();


            _loadContext.Unloading += OnUnloading;
        }

        private void OnUnloading(AssemblyLoadContext ctx)
        {
            EngLog.Script.Debug("Unloading script context: {ctx}\n    ", ctx, ctx.Assemblies);

            //_manager.RegisterAsUnloaded(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _manager.RegisterForUnload(this);
                }

                _disposedValue = true;
            }
        }

        /// <summary>Not thread-safe</summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public bool LoadAssembly(string path)
        {
            if (Engine.IsAOTBuild)
                throw new NotSupportedException("Assemblies cannot be loaded in an AOT build");

            ObjectDisposedException.ThrowIf(_disposedValue, this);

            Assembly? assembly = null;

            try
            {
                assembly = _loadContext.LoadFromAssemblyPath(path);
            }
            catch (FileLoadException ex)
            {
                EngLog.Script.Error(ex, "Error while loading assembly file: {p}", path);
            }
            catch (FileNotFoundException ex)
            {
                EngLog.Script.Error(ex, "Failed to find assembly to load at path: {p}", path);
            }
            catch (BadImageFormatException ex)
            {
                EngLog.Script.Error(ex, "Trying to load assembly that is not valid: {p}", path);
            }

            if (assembly == null)
                return false;

            foreach (Assembly currentAssembly in _loadContext.Assemblies)
            {
                _manager.RegisterNewAssembly(this, currentAssembly);
                _loadedAssemblies.Add(currentAssembly);
            }

            _manager.RegisterNewAssembly(this, assembly);
            _loadedAssemblies.Add(assembly);

            return true;
        }

        internal void UnloadAssemblies()
        {
            foreach (Assembly assembly in _loadedAssemblies)
            {
                _manager.UnregisterAssembly(assembly);
            }

            _loadContext.Unload();
            _loadedAssemblies.Clear();
        }

        public string Name => _loadContext.Name!;

        public HashSet<Assembly> LoadedAssemblies => _loadedAssemblies;
    }
}
