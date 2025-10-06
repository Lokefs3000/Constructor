using Editor.DearImGui.Properties;
using Editor.Inspector.Entities;
using Primary.Assets;
using Primary.Components;
using Primary.Scenes;
using System.Collections.Frozen;
using System.Numerics;

namespace Editor.Inspector.Editors
{
    internal class DefaultComponentEditor : ComponentEditor
    {
        private SceneEntity _entity;
        private Type? _component;

        private List<ValueStorageBase> _values;

        public DefaultComponentEditor()
        {
            _values = new List<ValueStorageBase>();
        }

        public override void SetupInspectorFields(SceneEntity entity, Type type)
        {
            _entity = entity;
            _component = type;

            EntityProperties? entityProperties = Editor.GlobalSingleton.PropertiesView.GetPropertiesViewer<EntityProperties>(typeof(EntityProperties.TargetData));
            if (entityProperties != null)
            {
                PropReflectionCache reflectionCache = entityProperties.ReflectionCache;
                CachedReflection cached = reflectionCache.Get(type);

                if (cached.IsSerialized)
                {
                    for (int i = 0; i < cached.Fields.Length; i++)
                    {
                        if (cached.Fields[i].IsProperty && s_valueTypes.TryGetValue(cached.Fields[i].Type, out var constructor))
                            _values.Add(constructor(cached.Fields[i]));
                    }
                }
            }
        }

        public override void DrawInspector()
        {
            if (_component != null)
            {
                object? value = _entity.GetComponent(_component);
                if (value != null)
                {
                    bool wasUpdated = false;
                    for (int i = 0; i < _values.Count; i++)
                    {
                        object? ret = _values[i].Render(_values[i].Field.Name, ref value);
                        if (ret != null)
                        {
                            value = ret;
                            wasUpdated = true;
                        }
                    }

                    if (wasUpdated)
                        _entity.SetComponent((IComponent)value, _component);
                }
            }
        }

        private static readonly FrozenDictionary<Type, Func<ReflectionField, ValueStorageBase>> s_valueTypes = new Dictionary<Type, Func<ReflectionField, ValueStorageBase>>()
        {
            { typeof(float), (x) => new VSSingle(x) },
            { typeof(Vector2), (x) => new VSVector2(x) },
            { typeof(Vector3), (x) => new VSVector3(x) },
            { typeof(Quaternion), (x) => new VSQuaternion(x) },
            { typeof(Enum), (x) => new VSEnum(x) },
            { typeof(RenderMesh), (x) => new VSMesh(x) },
            { typeof(MaterialAsset), (x) => new VSMaterial(x) },
        }.ToFrozenDictionary();
    }
}
