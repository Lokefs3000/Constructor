using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Collections.ReadOnly;
using PrimaryEditor.Inspector.Caching;
using PrimaryEditor.Inspector.Pooling;
using PrimaryEditor.Inspector.Reflection;
using PrimaryEditor.Inspector.Views;

namespace PrimaryEditor.Inspector
{
    public sealed class InspectorManager
    {
        private readonly ValueSourceGenerator _valueSourceGenerator;
        private readonly DescriptionCache _descriptionCache;
        private readonly ViewManager _viewManager;

        private readonly InspectorWidgetPool _inspectorWidgetPool;

        private readonly EnumValueCache _enumValueCache;

        private List<InspectorContext> _contexts;
        private InspectorContext? _primaryContext;

        public InspectorManager()
        {
            s_instance.Target = this;

            _valueSourceGenerator = new ValueSourceGenerator();
            _descriptionCache = new DescriptionCache(_valueSourceGenerator);
            _viewManager = new ViewManager();

            _inspectorWidgetPool = new InspectorWidgetPool();

            _enumValueCache = new EnumValueCache();

            _contexts = new List<InspectorContext>();
            _primaryContext = null;
        }

        public void UpdateContexts()
        {
            for (int i = 0; i < _contexts.Count; ++i)
            {
                _contexts[i].UpdateValues();
            }
        }

        public InspectorContext? StartInspect<T, TContext>(ref TContext context) where T : InspectorContext where TContext : notnull
        {
            foreach (InspectorContext alreadyActiveContext in _contexts)
            {
                if (alreadyActiveContext.MatchesContext(ref context))
                {
                    return alreadyActiveContext;
                }
            }

            try
            {
                T inspectorContext = (T)Activator.CreateInstance(typeof(T), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [context], null)!;
                _primaryContext ??= inspectorContext;

                OnInspectStart?.Invoke(inspectorContext);

                _contexts.Add(inspectorContext);
                return inspectorContext;
            }
            catch (Exception ex)
            {
                EdLog.Reflection.Error(ex, "Failed to setup inspection context");
                return null;
            }
        }

        public bool StopInspect<TContext>(ref TContext context) where TContext : notnull
        {
            for (int i = 0; i < _contexts.Count; ++i)
            {
                InspectorContext inspectorContext = _contexts[i];
                if (inspectorContext.MatchesContext(ref context))
                {
                    _contexts.RemoveAt(i);
                    OnInspectEnd?.Invoke(inspectorContext);
                    return true;
                }
            }

            return false;
        }

        public ValueSourceGenerator ValueSourceGenerator => _valueSourceGenerator;
        public DescriptionCache DescriptionCache => _descriptionCache;
        public ViewManager ViewManager => _viewManager;

        public InspectorWidgetPool InspectorWidgetPool => _inspectorWidgetPool;

        public EnumValueCache EnumValueCache => _enumValueCache;

        public ROList<InspectorContext> Contexts => _contexts;
        public InspectorContext? PrimaryContext => _primaryContext;

        public event Action<InspectorContext>? OnInspectStart;
        public event Action<InspectorContext>? OnInspectEnd;

        internal static InspectorManager Instance => Unsafe.As<InspectorManager>(s_instance.Target)!;

        private static WeakReference s_instance = new WeakReference(null);
    }
}
