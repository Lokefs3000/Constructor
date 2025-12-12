using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Components;
using Primary.Reflection;
using Primary.Scenes.Types;
using Primary.Serialization;
using Primary.Serialization.Structural;
using System.Numerics;
using System.Reflection;

namespace Primary.Scenes
{
    public sealed class SceneDeserializer
    {
        private ComponentReflectionCache _componentCache;

        internal SceneDeserializer()
        {
            _componentCache = new ComponentReflectionCache();
        }

        public void Deserialize(ReadOnlySpan<char> source, Scene scene)
        {
            SDFReader reader = new SDFReader(source);
            SDFDocument document = SDFDocument.Parse(ref reader);

            foreach (var val in document)
            {
                if (val.Name == "Entity")
                    DeserializeEntity(val, scene, SceneEntity.Null);
            }
        }

        public SceneEntity DeserializeEntity(ReadOnlySpan<char> source, Scene scene)
        {
            SDFReader reader = new SDFReader(source);
            SDFDocument document = SDFDocument.Parse(ref reader);

            return DeserializeEntity(document[0], scene, SceneEntity.Null);
        }

        private SceneEntity DeserializeEntity(SDFObject rootObject, Scene scene, SceneEntity parent)
        {
            SceneEntity entity = scene.CreateEntity(parent);

            {
                SDFObject sceneEntityDataObj = (rootObject["@SceneEntityData"] as SDFObject)!;
                entity.Enabled = (sceneEntityDataObj["Enabled"] as SDFProperty)!.GetBoolean();
                entity.Name = (sceneEntityDataObj["Name"] as SDFProperty)!.RawValueString!;
            }

            foreach (var kvp in rootObject)
            {
                if (kvp.Key != "@SceneEntityData" && kvp.Key != "@Children")
                {
                    DeserializeComponent((SDFObject)kvp.Value, entity);
                }
            }

            return entity;
        }

        private void DeserializeComponent(SDFObject componentObject, SceneEntity entity)
        {
            ComponentReflection reflection = _componentCache.GetReflection(componentObject.Name!);
            if (reflection.Component != null)
            {
                IComponent? component = entity.AddComponent(reflection.Component);
                if (component == null)
                {
                    EngLog.Scene.Error("Failed to add component: {c}", reflection.Component);
                    return;
                }

                foreach (var kvp in componentObject)
                {
                    string key = kvp.Key;
                    if (reflection.Fields.TryGetValue(key, out FieldInfo? field))
                    {
                        Type typeSpec = field.FieldType;
                        if (typeSpec.BaseType == typeof(Enum))
                            typeSpec = typeof(Enum);
                        else if (typeSpec.IsAssignableTo(typeof(IAssetDefinition)))
                            typeSpec = typeof(IAssetDefinition);

                        if (s_deserializers.TryGetValue(typeSpec, out ISceneTypeDeserializer? deserializer))
                        {
                            SDFBase @base = kvp.Value;
                            field.SetValue(component, deserializer!.Deserialize(ref @base, field.FieldType));
                        }
                        else
                            EngLog.Scene.Error("Failed to find deserializer for type: {t}", typeSpec);
                    }
                    else
                        EngLog.Scene.Error("Failed to find field: {f} in component: {c}", key, reflection.Component);
                }

                entity.SetComponent(component, reflection.Component);
            }
            else
                EngLog.Scene.Error("Failed to find component reflection: {c}", componentObject.Name);
        }

        private static Dictionary<Type, ISceneTypeDeserializer> s_deserializers = new Dictionary<Type, ISceneTypeDeserializer>
        {
            { typeof(sbyte), new SByteDeserializer() },
            { typeof(short), new ShortDeserializer() },
            { typeof(int), new IntDeserializer() },
            { typeof(long), new LongDeserializer() },
            { typeof(byte), new ByteDeserializer() },
            { typeof(ushort), new UShortDeserializer() },
            { typeof(uint), new UIntDeserializer() },
            { typeof(ulong), new ULongDeserializer() },
            { typeof(float), new SingleDeserializer() },
            { typeof(double), new DoubleDeserializer() },
            { typeof(string), new StringDeserializer() },
            { typeof(Enum), new EnumDeserializer() },
            { typeof(Color), new ColorDeserializer() },
            { typeof(Color32), new Color32Deserializer() },
            { typeof(Vector2), new Vector2Deserializer() },
            { typeof(Vector3), new Vector3Deserializer() },
            { typeof(Vector4), new Vector4Deserializer() },
            { typeof(Quaternion), new QuaternionDeserializer() },
            { typeof(IAssetDefinition), new AssetDefinitionDeserializer() },
            { typeof(RenderMesh), new RenderMeshDeserializer() },
        };
    }
}
