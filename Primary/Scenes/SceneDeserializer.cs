using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Components;
using Primary.Reflection;
using Primary.Scenes.Json;
using Primary.Scenes.Types;
using Primary.Serialization;
using Primary.Serialization.Structural;
using System.Numerics;
using System.Reflection;
using System.Text.Json;

namespace Primary.Scenes
{
    public sealed class SceneDeserializer
    {
        private ComponentReflectionCache _componentCache;
        private SceneJsonSerializer _jsonSerializer;
        
        internal SceneDeserializer()
        {
            _componentCache = new ComponentReflectionCache();
            _jsonSerializer = new SceneJsonSerializer();
        }

        public void Deserialize(Stream source, Scene scene)
        {
            Utf8JsonReader reader;
            {
                byte[] fullData = new byte[source.Length];
                source.ReadExactly(fullData);

                reader = new Utf8JsonReader(fullData.AsSpan(), new JsonReaderOptions { });
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                EngLog.Scene.Error("Scene file should start with an object");
                return;
            }

            Dictionary<int, SceneEntity> entityDict = new Dictionary<int, SceneEntity>();
            List<SerializedEntity> entityList = new List<SerializedEntity>();

            while (reader.TokenType != JsonTokenType.Null)
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.EndObject)
                    return;
                else if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    EngLog.Scene.Error("Scene file expects a name for data entry");
                    return;
                }

                string key = reader.GetString()!;
                if (key == "Entities")
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.StartArray)
                    {
                        EngLog.Scene.Error("Expected array start for entites list");
                        return;
                    }

                    while (reader.TokenType != JsonTokenType.Null)
                    {
                        reader.Read();
                        if (reader.TokenType == JsonTokenType.EndArray)
                            break;
                        else if (reader.TokenType != JsonTokenType.StartObject)
                        {
                            EngLog.Scene.Error("Expected object start for entity");
                            return;
                        }

                        SceneEntity currentEntity = scene.CreateEntity(SceneEntity.Null);

                        int entityId = -1;
                        int parentId = -1;
                        int componentCount = 0;

                        while (reader.TokenType != JsonTokenType.Null)
                        {
                            reader.Read();
                            if (reader.TokenType == JsonTokenType.EndObject)
                                break;
                            else if (reader.TokenType != JsonTokenType.PropertyName)
                            {
                                EngLog.Scene.Error("Expected parameter name in entity object");
                                return;
                            }

                            key = reader.GetString()!;
                            if (key == "Id")
                            {
                                reader.Read();
                                if (reader.TokenType != JsonTokenType.Number)
                                {
                                    EngLog.Scene.Error("Expected number for entity id");
                                    return;
                                }
                                else if (reader.TryGetInt32(out int id))
                                {
                                    if (!entityDict.TryAdd(id, currentEntity))
                                        EngLog.Scene.Error("Duplicate entity id {id}", id);
                                    else
                                        entityId = id;
                                }
                            }
                            else if (key == "Parent")
                            {
                                reader.Read();
                                if (reader.TokenType != JsonTokenType.Number)
                                {
                                    EngLog.Scene.Error("Expected number for entity parent id");
                                    return;
                                }
                                else if (reader.TryGetInt32(out int id))
                                {
                                    parentId = id;
                                }
                            }
                            else if (key == "Enabled")
                            {
                                reader.Read();
                                if (reader.TokenType != JsonTokenType.True && reader.TokenType != JsonTokenType.False)
                                {
                                    EngLog.Scene.Error("Expected boolean for entity enabled state");
                                    return;
                                }
                                else
                                {
                                    currentEntity.Enabled = reader.GetBoolean();
                                }
                            }
                            else if (key == "Name")
                            {
                                reader.Read();
                                if (reader.TokenType != JsonTokenType.String)
                                {
                                    EngLog.Scene.Error("Expected string for entity name");
                                    return;
                                }
                                else
                                {
                                    currentEntity.Name = reader.GetString()!;
                                }
                            }
                            else if (key == "Components")
                            {
                                reader.Read();
                                if (reader.TokenType != JsonTokenType.StartArray)
                                {
                                    EngLog.Scene.Error("Expected array start for entity component list");
                                    return;
                                }
                                else
                                {
                                    while (reader.TokenType != JsonTokenType.EndArray)
                                    {
                                        reader.Read();
                                        if (reader.TokenType == JsonTokenType.String)
                                        {
                                            if (!_jsonSerializer.CreateComponent(currentEntity, reader.GetString()!))
                                            {
                                                EngLog.Scene.Error("Failed to create component specified in entity list {c}", reader.GetString());
                                                return;
                                            }

                                            ++componentCount;

                                            continue;
                                        }
                                        else if (reader.TokenType == JsonTokenType.EndArray)
                                            break;

                                        EngLog.Scene.Error("Unexpected token in entity component list");
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                EngLog.Scene.Error("Unexpected extra entry in entity {v}", key);
                                return;
                            }
                        }

                        entityList.Add(new SerializedEntity(currentEntity, parentId, componentCount));
                    }

                    if (reader.TokenType != JsonTokenType.EndArray)
                    {
                        EngLog.Scene.Error("Expected array end for entity list");
                        return;
                    }
                }
                else if (key == "Components")
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.StartArray)
                    {
                        EngLog.Scene.Error("Expected array start for components list");
                        return;
                    }

                    for (int i = 0; i < entityList.Count; i++)
                    {
                        SerializedEntity serialized = entityList[i];

                        if (serialized.ParentId != -1)
                        {
                            if (entityDict.TryGetValue(serialized.ParentId, out SceneEntity parent))
                            {
                                SceneEntity entity = serialized.Entity;
                                entity.Parent = parent;
                            }
                            else
                                EngLog.Scene.Error("Failed to find parent entity with id {id}", serialized.ParentId);
                        }

                        for (int j = 0; j < serialized.ComponentCount; j++)
                        {
                            reader.Read();
                            if (reader.TokenType != JsonTokenType.StartObject)
                            {
                                EngLog.Scene.Error("Expected a start object for component");

                                reader.TrySkip();
                                continue;
                            }

                            reader.Read();
                            if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals("Key"))
                            {
                                EngLog.Scene.Error("Expected a key property for component");

                                reader.TrySkip();
                                continue;
                            }

                            reader.Read();
                            if (reader.TokenType != JsonTokenType.String)
                            {
                                EngLog.Scene.Error("Expected a string value for component key");

                                reader.TrySkip();
                                continue;
                            }

                            _jsonSerializer.DeserializeComponent(serialized.Entity, reader.GetString()!, ref reader);

                            if (reader.TokenType != JsonTokenType.EndObject)
                            {
                                EngLog.Scene.Error("Component is still open after deserialization");

                                reader.TrySkip();
                                continue;
                            }
                        }
                    }
               
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.EndArray)
                    {
                        EngLog.Scene.Error("Expected array end for components list");
                        return;
                    }
                }
                else
                {
                    EngLog.Scene.Error("Unexpected entry in scene data object {v}", key);
                    return;
                }
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

        public ComponentReflectionCache ReflectionCache => _componentCache;
        public SceneJsonSerializer JsonSerializer => _jsonSerializer;

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
        };

        private readonly record struct SerializedEntity(SceneEntity Entity, int ParentId, int ComponentCount);
    }
}
