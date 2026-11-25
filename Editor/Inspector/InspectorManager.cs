using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Editor.Gui.Inspector;
using Editor.Inspector.Gui;
using Editor.Inspector.IL;
using Editor.Inspector.Layout;
using Editor.Reflection;

namespace Editor.Inspector
{
    public sealed class InspectorManager
    {
        private MethodGenerator _methodGenerator;
        private InspectorTypeCache _typeCache;
        private InspectorConstructors _constructors;
        private InspectorViewManager _viewManager;

        private Dictionary<Type, InspectorLayout> _layoutObjects;
        private Dictionary<Type, ICustomInspector> _customInspectors;

        internal InspectorManager()
        {
            _methodGenerator = new MethodGenerator();
            _typeCache = new InspectorTypeCache();
            _constructors = new InspectorConstructors();
            _viewManager = new InspectorViewManager();

            _layoutObjects = new Dictionary<Type, InspectorLayout>();
            _customInspectors = new Dictionary<Type, ICustomInspector>();

            AssemblyTypeLoader typeLoader = EditorRuntime.GlobalSingleton.ReflectionManager.TypeLoader;
            typeLoader.AddCallback<InspectorTargetAttribute>(OnCustomInspectorTypeLoaded);
        }

        private void OnCustomInspectorTypeLoaded(Type type, object attribData)
        {
            if (!type.IsAssignableTo(typeof(ICustomInspector)) || !type.IsClass)
                return;

            InspectorTargetAttribute attribute = (InspectorTargetAttribute)attribData;
            if (_customInspectors.ContainsKey(attribute.Type))
            {
                EdLog.Inspector.Warning("A custom inspector has already previously been assigned to the type {t}", attribute.Type);
                return;
            }

            ICustomInspector instance;
            try
            {
                instance = (ICustomInspector)Activator.CreateInstance(type)!;
            }
            catch (Exception ex)
            {
                EdLog.Inspector.Error(ex, "Failed to create custom inspector {t}", type);
                return;
            }

            _customInspectors.Add(attribute.Type, instance);
        }

        internal void StartInspectingElement(InspectorHolder holder, object obj)
        {
            Type type = obj.GetType();
            if (_customInspectors.TryGetValue(type, out ICustomInspector? inspector))
            {
                InspectorLayout layout = GetLayoutForInspector(type, inspector);
                InspectorValueTable valueTable = new InspectorValueTable();

                valueTable.SetupInspectorValues(this, inspector, layout);
                holder.AddValueTable(valueTable);

                return;
            }

            // fallback to default reflection-based inspector
        }

        internal void StopInspectingElement(InspectorHolder holder, object obj)
        {

        }

        internal InspectorLayout GetLayoutForInspector(Type type, ICustomInspector inspector)
        {
            if (_layoutObjects.TryGetValue(type, out InspectorLayout? layout))
                return layout;

            layout = new InspectorLayout();
            layout.SetupLayoutData(this, type, inspector);

            _layoutObjects.Add(type, layout);
            return layout;
        }

        internal bool GetLayoutForType(Type type, [NotNullWhen(true)] out InspectorLayout? layout)
        {
            if (_customInspectors.TryGetValue(type, out ICustomInspector? customInspector))
            {
                layout = GetLayoutForInspector(type, customInspector);
                return true;
            }

            layout = null;
            return false;
        }

        internal MethodGenerator MethodGenerator => _methodGenerator;
        internal InspectorTypeCache TypeCache => _typeCache;
        internal InspectorConstructors Constructors => _constructors;
        internal InspectorViewManager ViewManager => _viewManager;
    }
}
