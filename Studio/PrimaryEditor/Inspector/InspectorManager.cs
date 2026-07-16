using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
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

        internal InspectorManager()
        {
            _valueSourceGenerator = new ValueSourceGenerator();
            _descriptionCache = new DescriptionCache(_valueSourceGenerator);
            _viewManager = new ViewManager();

            _inspectorWidgetPool = new InspectorWidgetPool();

            _enumValueCache = new EnumValueCache();

            _contexts = new List<InspectorContext>();
            _primaryContext = null;
        }

        internal void UpdateContexts()
        {
            for (int i = 0; i < _contexts.Count; ++i)
            {
                _contexts[i].UpdateValues();
            }
        }

        public InspectorContext? StartInspect<T, TContext>(ref TContext context) where T : InspectorContext where TContext : notnull
        {
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

        public ValueSourceGenerator ValueSourceGenerator => _valueSourceGenerator;
        public DescriptionCache DescriptionCache => _descriptionCache;
        public ViewManager ViewManager => _viewManager;

        public InspectorWidgetPool InspectorWidgetPool => _inspectorWidgetPool;

        public EnumValueCache EnumValueCache => _enumValueCache;

        public event Action<InspectorContext>? OnInspectStart;
    }
}
