using Editor.Gui.Inspector;
using Editor.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Inspector
{
    internal class InspectorStorage
    {
        private Dictionary<Type, object> _inspectorTypes;

        internal InspectorStorage()
        {
            _inspectorTypes = new Dictionary<Type, object>();

            AssemblyTypeLoader typeLoader = EditorRuntime.GlobalSingleton.ReflectionManager.TypeLoader;
            typeLoader.AddCallback<InspectorTargetAttribute>(OnInspectorTargetTypeLoaded);
        }

        private void OnInspectorTargetTypeLoaded(Type type, object attribData)
        {
            if (!type.IsAssignableTo(typeof(ICustomInspector<>)) || type.IsValueType)
                return;

            InspectorTargetAttribute attribute = (InspectorTargetAttribute)attribData;

            if (_inspectorTypes.ContainsKey(attribute.Type))
            {
                EdLog.Inspector.Warning("Inspector targeting {t} has already been added", attribute.Type);
            }
            else
            {
                object obj;
                try
                {
                    obj = Activator.CreateInstance(type)!;
                }
                catch (Exception ex)
                {
                    EdLog.Inspector.Error(ex, "Failed to create inspector of type {t}", type);
                    return;
                }

                _inspectorTypes.Add(attribute.Type, obj);
                EdLog.Inspector.Debug("Found new inspector for type {t}", attribute.Type);
            }
        }
    }
}
