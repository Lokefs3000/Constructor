using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Primary.Collections.ReadOnly;

namespace Primary.Scripting
{
    public sealed class ScriptingManager : IDisposable
    {
        private readonly AssemblyTypeLoader _typeLoader;

        private readonly List<ScriptContext> _contexts;
        private readonly List<ScriptContext> _pendingUnloads;

        private readonly Dictionary<Assembly, ScriptContext> _assemblies;
        private readonly Dictionary<string, ScriptContext> _assemblyNames;

        private readonly Dictionary<string, ScriptContext>.AlternateLookup<ReadOnlySpan<char>> _assemblyNamesAlt;

        private bool _disposedValue;

        internal ScriptingManager()
        {
            _typeLoader = new AssemblyTypeLoader(this);

            _contexts = new List<ScriptContext>();
            _pendingUnloads = new List<ScriptContext>();

            _assemblies = new Dictionary<Assembly, ScriptContext>();
            _assemblyNames = new Dictionary<string, ScriptContext>();

            _assemblyNamesAlt = _assemblyNames.GetAlternateLookup<ReadOnlySpan<char>>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    while (_contexts.Count > 0)
                    {
                        _contexts[0].Dispose();
                    }
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void RegisterNewAssembly(ScriptContext context, Assembly assembly)
        {
            if (_assemblies.TryAdd(assembly, context))
            {
                _assemblyNames.Add(assembly.GetName().Name!, context);

                EngLog.Scene.Debug("Registed new assembly: {asm}", assembly);

                _typeLoader.LoadTypesOnAssembly(assembly);

                try
                {
                    NewAssemblyLoaded?.Invoke(assembly);
                }
                catch (Exception ex)
                {
                    EngLog.Script.Error(ex, "Invoking new assembly load event caused exceptions");
                }
            }
        }

        internal void UnregisterAssembly(Assembly assembly)
        {
            _assemblies.Remove(assembly);
            _assemblyNames.Remove(assembly.GetName().Name!);

            try
            {
                AssemblyUnloading?.Invoke(assembly);
            }
            catch (Exception ex)
            {
                EngLog.Script.Error(ex, "Invoking assembly unloading event caused exceptions");
            }
        }

        internal void RegisterForUnload(ScriptContext context)
        {
            _contexts.Remove(context);
            _pendingUnloads.Add(context);
        }

        public void ProcessPendingScripts()
        {
            if (_pendingUnloads.Count > 0)
            {
                Engine.GlobalSingleton.JobScheduler.WaitForAllJobs();

                for (int i = 0; i < _pendingUnloads.Count; i++)
                {
                    ScriptContext ctx = _pendingUnloads[i];
                    ContextUnloading?.Invoke(ctx);

                    ctx.UnloadAssemblies();
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                _pendingUnloads.Clear();
                OnUnloadingFinished?.Invoke();
            }
        }

        public ScriptContext CreateContext(string? name)
        {
            if (name != null)
            {
                for (int i = 0; i < _contexts.Count; i++)
                {
                    ScriptContext ctx = _contexts[i];
                    if (ctx.Name == name)
                        return ctx;
                }
            }

            ScriptContext context = new ScriptContext(this, name ?? $"ScriptCtx{(ushort)Stopwatch.GetTimestamp()}");
            _contexts.Add(context);

            return context;
        }

        public bool TryGetAssemblyContext(ReadOnlySpan<char> qualifiedName, [NotNullWhen(true)] out ScriptContext? context)
        {
            return _assemblyNamesAlt.TryGetValue(qualifiedName, out context);
        }

        public AssemblyTypeLoader TypeLoader => _typeLoader;

        public ROList<ScriptContext> Contexts => _contexts;

        public event Action<Assembly>? NewAssemblyLoaded;
        public event Action<Assembly>? AssemblyUnloading;

        public event Action<ScriptContext>? ContextUnloading;

        public event Action? OnUnloadingFinished;
    }
}
