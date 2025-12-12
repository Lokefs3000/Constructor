using Editor.DearImGui.Properties;
using Editor.Inspector.Editors;
using Editor.Inspector.Entities;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Components;
using Primary.Rendering.Data;
using Primary.Scenes;
using System.Collections.Frozen;
using System.Numerics;
using System.Reflection;
using TerraFX.Interop.Windows;

namespace Editor.Inspector.Components
{
    internal class DefaultObjectEditor : ObjectEditor
    {
        private object? _obj;

        private List<ValueStorageBase> _values;

        public DefaultObjectEditor()
        {
            _obj = null;

            _values = new List<ValueStorageBase>();
        }

        public override void SetupInspectorFields(object obj)
        {
            _obj = obj;

            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < fields.Length; i++)
            {
                Type? type = fields[i].FieldType;
                bool skipCurrent = false;

                do
                {
                    skipCurrent = false;
                    if (s_valueTypes.TryGetValue(type, out var constructor))
                    {
                        _values.Add(constructor(fields[i]));
                        break;
                    }

                    if (type.IsAssignableTo(typeof(IAssetDefinition)))
                    {
                        type = typeof(IAssetDefinition);
                        skipCurrent = true;
                    }
                } while (skipCurrent || (type = type.BaseType) != null);
            }
        }

        public override void DrawInspector()
        {
            if (_obj != null)
            {
                for (int i = 0; i < _values.Count; i++)
                {
                    _values[i].Render(_values[i].Field.Name, ref _obj);
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
            { typeof(RawRenderMesh), (x) => new VSRawRenderMesh(x) },
            { typeof(IAssetDefinition), (x) => new VSGenericAsset(x) }
        }.ToFrozenDictionary();
    }
}
