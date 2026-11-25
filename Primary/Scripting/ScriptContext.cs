using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Xml.Linq;
using Primary.Collections.ReadOnly;
using Primary.Common;

namespace Primary.Scripting
{
    public sealed class ScriptContext : IDisposable
    {
        private readonly ScriptingManager _manager;
        private readonly string _name;

        private WeakReference _loadContext;

        private readonly HashSet<Assembly> _loadedAssemblies;

        private bool _isUnloaded;
        private bool _disposedValue;

        internal ScriptContext(ScriptingManager manager, string name)
        {
            _manager = manager;
            _name = name;

            _loadContext = new WeakReference(null, trackResurrection: true);

            _loadedAssemblies = new HashSet<Assembly>();

            _isUnloaded = false;
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

            AssemblyLoadContext? alc = _loadContext.Target as AssemblyLoadContext;
            if (alc == null)
            {
                alc = new AssemblyLoadContext(_name, true);
                _loadContext.Target = alc;
            }

            Assembly? assembly = null;

            try
            {
                using FileStream binaryStream = File.OpenRead(path);
                using FileStream? symbolsStream = FileUtility.TryWaitOpenNoThrow(Path.ChangeExtension(path, ".pdb"), FileMode.Open, FileAccess.Read, FileShare.Read, 1);

                assembly = alc.LoadFromStream(binaryStream, symbolsStream);
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

            foreach (Assembly currentAssembly in alc.Assemblies)
            {
                if (_loadedAssemblies.Add(currentAssembly))
                    _manager.RegisterNewAssembly(this, currentAssembly);
            }

            _manager.RegisterNewAssembly(this, assembly);
            _loadedAssemblies.Add(assembly);

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void UnloadAssemblies()
        {
            // IMPORTANT: No references that relate to this ALC are allowed to exist when this is called.

            string? alcName = _name;
            string?[] alcAssemblies = [.. _loadedAssemblies.Select(static (x) => x.FullName)];

            {
                foreach (Assembly assembly in _loadedAssemblies!)
                {
                    _manager.UnregisterAssembly(assembly);
                }

                _loadedAssemblies.Clear();
            }

            {
                AssemblyLoadContext? alc = _loadContext.Target as AssemblyLoadContext;
                alc?.Unload();
                alc = null;
            }

            for (int i = 0; _loadContext.IsAlive && (i < 10); ++i)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                Thread.Yield();
            }

            if (_loadContext.IsAlive)
            {
                EngLog.Scene.Error("Failed to unload script context: {ctx}\n    {asms}", alcName, alcAssemblies);
            }
            else
            {
                EngLog.Script.Debug("Fully unloaded script context: {ctx}\n    {asms}", alcName, alcAssemblies);
                _isUnloaded = true;
            }
        }

        internal AssemblyLoadContext.ContextualReflectionScope EnterContextualReflection()
        {
            return (_loadContext.Target as AssemblyLoadContext)?.EnterContextualReflection() ?? default;
        }

        public string Name => _name;

        public ROHashSet<Assembly> LoadedAssemblies => _loadedAssemblies;

        public bool IsDisposed => _disposedValue;
        public bool IsUnloaded => _isUnloaded;
    }
}
