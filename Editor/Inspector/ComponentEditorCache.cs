using Editor.Inspector.Editors;
using System.Reflection;

namespace Editor.Inspector
{
    internal sealed class ComponentEditorCache
    {
        private Dictionary<Assembly, AssemblyCache> _cache;

        internal ComponentEditorCache()
        {
            _cache = new Dictionary<Assembly, AssemblyCache>();

            ScanAssembly(typeof(ComponentEditorCache).Assembly);
        }

        private void ScanAssembly(Assembly assembly)
        {
            AssemblyCache cache = new AssemblyCache
            {
                EditorTypes = new Dictionary<Type, ComponentEditorType>()
            };

            int editorCount = 0;

            Type baseType = typeof(ComponentEditor);
            foreach (Type type in assembly.DefinedTypes)
            {
                if (type.IsAssignableTo(baseType) && !type.IsAbstract && type.IsClass)
                {
                    IEnumerable<CustomComponentInspectorAttribute> customAttributes = type.GetCustomAttributes<CustomComponentInspectorAttribute>();
                    if (customAttributes.Count() > 0)
                    {
                        foreach (CustomComponentInspectorAttribute attribute in customAttributes)
                        {
                            cache.EditorTypes.TryAdd(attribute.InspectedType, new ComponentEditorType(type, attribute.InspectedType));
                        }
                    }

                    editorCount++;
                }
            }

            _cache.TryAdd(assembly, cache);

            EdLog.Reflection.Debug("Found {x} custom component inspectors in assembly: {asm}", editorCount, assembly.GetName().Name);
        }

        internal Type? FindCustomEditor(Type componentType)
        {
            foreach (AssemblyCache cache in _cache.Values)
            {
                if (cache.EditorTypes.TryGetValue(componentType, out ComponentEditorType editorType))
                    return editorType.ClassType;
            }

            return null;
        }

        private struct AssemblyCache
        {
            public Dictionary<Type, ComponentEditorType> EditorTypes;
        }
    }

    internal readonly record struct ComponentEditorType(Type ClassType, Type ComponentType);
}
