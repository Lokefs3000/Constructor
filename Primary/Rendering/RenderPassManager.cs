using Primary.Profiling;
using Primary.Rendering.Pass;
using Serilog;
using System.Reflection;

namespace Primary.Rendering
{
    public sealed class RenderPassManager : IDisposable
    {
        private List<PassConfiguration> _renderPasses;
        private List<IRenderPass> _orderedPasses;

        private bool _needsReorder;
        private bool _disposedValue;

        internal RenderPassManager()
        {
            _renderPasses = new List<PassConfiguration>();
            _orderedPasses = new List<IRenderPass>();

            _needsReorder = false;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (PassConfiguration pass in _renderPasses)
                    {
                        pass.Pass.Dispose();
                    }

                    _renderPasses.Clear();
                    _orderedPasses.Clear();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public T? AddRenderPass<T>() where T : class, IRenderPass, new()
        {
            if (_renderPasses.Exists((x) => x.Pass is T))
            {
                Log.Warning("Cannot add render pass as it already exists within collection: {pass}", typeof(T).Name);
                return default;
            }

            T pass = new T();

            _renderPasses.Add(new PassConfiguration
            {
                Pass = pass,
                Priority = typeof(T).GetCustomAttribute<RenderPassPriorityAttribute>()
            });

            _needsReorder = true;
            return pass;
        }

        public void RemoveRenderPass<T>() where T : class, IRenderPass
        {
            int index = _renderPasses.FindIndex((x) => x.Pass is T);
            if (index >= 0)
            {
                PassConfiguration config = _renderPasses[index];
                config.Pass.Dispose();

                _renderPasses.RemoveAt(index);
                _needsReorder = true;
            }
            else
            {
                Log.Information("No render pass of type: {pass} within collection", typeof(T).Name);
            }
        }

        internal void ReorganizePassesIfRequired()
        {
            if (_needsReorder)
            {
                _orderedPasses.Clear();

                List<int> passesLeft = new List<int>();
                HashSet<Type> realPasses = new HashSet<Type>();
                HashSet<KeyValuePair<Type, Type>> warnedPasses = new HashSet<KeyValuePair<Type, Type>>();

                for (int i = 0; i < _renderPasses.Count; i++)
                {
                    passesLeft.Add(i);
                    realPasses.Add(_renderPasses[i].Pass.GetType());
                }

                while (passesLeft.Count > 0)
                {
                    for (int i = 0; i < passesLeft.Count; i++)
                    {
                        int index = passesLeft[i];
                        PassConfiguration config = _renderPasses[index];

                        bool hasAllRequirmentsMet = true;
                        if (config.Priority != null)
                        {
                            foreach (Type requirement in config.Priority.Requirements)
                            {
                                if (!realPasses.Contains(requirement) || !config.Priority.RequiresAllPasses)
                                {
                                    if (!warnedPasses.Contains(new KeyValuePair<Type, Type>(config.Pass.GetType(), requirement)))
                                    {
                                        Log.Warning("Render pass: {pass} requires a pass that does not exist: {reqPass}", config.Pass.GetType().Name, requirement.Name);
                                        warnedPasses.Add(new KeyValuePair<Type, Type>(config.Pass.GetType(), requirement));
                                    }

                                    if (config.Priority.RequiresAllPasses)
                                    {
                                        hasAllRequirmentsMet = false;
                                        passesLeft.RemoveAt(i);
                                        break;
                                    }
                                }
                                else if (!_orderedPasses.Exists((x) => x.GetType() == requirement))
                                {
                                    hasAllRequirmentsMet = false;
                                    break;
                                }
                            }
                        }

                        if (hasAllRequirmentsMet)
                        {
                            _orderedPasses.Add(config.Pass);
                            passesLeft.RemoveAt(i);
                        }
                    }
                }

                _needsReorder = false;
            }
        }

        internal void ExecuteAllPasses(IRenderPath path, RenderPassData passData)
        {
            for (int i = 0; i < _orderedPasses.Count; i++)
            {
                IRenderPass pass = _orderedPasses[i];

                using (new ProfilingScope(pass.GetType().Name))
                {
                    pass.PrepareFrame(path, passData);
                    pass.ExecutePass(path, passData);
                    pass.CleanupFrame(path, passData);
                }
            }
        }

        internal struct PassConfiguration
        {
            public IRenderPass Pass;
            public RenderPassPriorityAttribute? Priority;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RenderPassPriorityAttribute : Attribute
    {
        private readonly Type[] _requirements;
        private readonly bool _requiresAllPasses;

        public RenderPassPriorityAttribute(bool requiresAllPasses, params Type[] requirements)
        {
            _requirements = requirements;
            _requiresAllPasses = requiresAllPasses;
        }

        public Type[] Requirements => _requirements;
        public bool RequiresAllPasses => _requiresAllPasses;
    }
}
