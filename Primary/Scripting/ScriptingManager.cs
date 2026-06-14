using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Primary.Scripting
{
    public sealed class ScriptingManager : IDisposable
    {
        private List<ScriptContext> _contexts;
        private List<ScriptContext> _pendingUnloads;

        private Dictionary<Assembly, ScriptContext> _assemblies;

        private bool _disposedValue;

        internal ScriptingManager()
        {
            _contexts = new List<ScriptContext>();
            _pendingUnloads = new List<ScriptContext>();

            _assemblies = new Dictionary<Assembly, ScriptContext>();
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
                EngLog.Scene.Debug("Registed new assembly: {asm}", assembly);

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
                for (int i = 0; i < _pendingUnloads.Count; i++)
                {
                    ScriptContext ctx = _pendingUnloads[i];
                    ctx.UnloadAssemblies();
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                _pendingUnloads.Clear();
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

        public event Action<Assembly>? NewAssemblyLoaded;
        public event Action<Assembly>? AssemblyUnloading;

        public event Action<ScriptContext>? ContextUnloading;
    }
}
