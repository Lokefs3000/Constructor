using Editor.DearImGui.Properties;
using Editor.Inspector;
using Editor.Serialization.Types;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Components;
using Primary.Scenes;
using System.Numerics;
using System.Text;

namespace Editor.Serialization
{
    public static class SceneSerializer
    {
        public static string Serialize(Scene scene)
        {
            StringBuilder sb = new StringBuilder();

            foreach (SceneEntity entity in scene.Root.Children)
            {
                SerializeEntity(sb, entity, 0);
            }

            return sb.ToString();
        }

        public static string Serialize(SceneEntity entity)
        {
            StringBuilder sb = new StringBuilder();
            SerializeEntity(sb, entity, 0);

            return sb.ToString();
        }

        private static void SerializeEntity(StringBuilder sb, SceneEntity entity, int depth)
        {
            string depthPreString = string.Empty;
            for (int i = 0; i < depth; i++)
                depthPreString = depthPreString + "    ";

            depth++;

            sb.Append(depthPreString);
            sb.AppendLine("Entity{");

            string newDepthPreString = depthPreString + "    ";

            sb.Append(newDepthPreString);
            sb.AppendLine("@SceneEntityData{");
            sb.Append(newDepthPreString);
            sb.Append("    Enabled = ");
            sb.AppendLine(entity.Enabled.ToString());
            sb.Append(newDepthPreString);
            sb.Append("    Name = \"");
            sb.Append(entity.Name);
            sb.AppendLine("\"");
            sb.Append(newDepthPreString);
            sb.AppendLine("}");

            SerializeEntityComponent(sb, entity, depth);

            if (entity.Children.IsEmpty)
            {
                sb.Append(newDepthPreString);
                sb.AppendLine("@Children{}");
            }
            else
            {
                sb.Append(newDepthPreString);
                sb.AppendLine("@Children{");

                foreach (SceneEntity child in entity.Children)
                {
                    SerializeEntity(sb, child, depth);
                }

                sb.Append(newDepthPreString);
                sb.AppendLine("}");
            }

            sb.Append(depthPreString);
            sb.AppendLine("}");
        }

        private static void SerializeEntityComponent(StringBuilder sb, SceneEntity entity, int depth)
        {
            string depthPreString = string.Empty;
            for (int i = 0; i < depth; i++)
                depthPreString = depthPreString + "    ";

            PropReflectionCache reflectionCache = Editor.GlobalSingleton.PropertiesView.GetPropertiesViewer<EntityProperties>(typeof(EntityProperties.TargetData))!.ReflectionCache;
            foreach (IComponent component in entity.Components)
            {
                Type type = component.GetType();
                CachedReflection cached = reflectionCache.Get(type);

                if (cached.IsSerialized)
                {
                    if (cached.Fields.Length == 0)
                    {
                        sb.Append(depthPreString);
                        sb.Append(component.GetType().FullName);
                        sb.AppendLine("{}");
                    }
                    else
                    {
                        sb.Append(depthPreString);
                        sb.Append(component.GetType().FullName);
                        sb.AppendLine("{");

                        string newDepthPreString = depthPreString + "    ";

                        for (int i = 0; i < cached.Fields.Length; i++)
                        {
                            ReflectionField field = cached.Fields[i];
                            if (field.IsField)
                            {
                                Type fieldType = field.Type;
                                if (fieldType.IsEnum)
                                    fieldType = typeof(Enum);
                                if (fieldType.IsAssignableTo(typeof(IAssetDefinition)) && !s_serializers.ContainsKey(fieldType))
                                    fieldType = typeof(IAssetDefinition);

                                if (s_serializers.TryGetValue(fieldType, out ISceneTypeSerializer? serializer))
                                {
                                    sb.Append(newDepthPreString);
                                    sb.Append(field.Name);
                                    sb.Append(" = ");
                                    serializer.Serialize(sb, field.GetValue(component));
                                    sb.AppendLine();
                                }
                                else
                                    EdLog.Serialization.Warning("Failed to find scene serializer for field: {c}.{f} ({t})", type.FullName, field.Name, fieldType);
                            }
                        }

                        sb.Append(depthPreString);
                        sb.AppendLine("}");
                    }
                }
            }
        }

        private static Dictionary<Type, ISceneTypeSerializer> s_serializers = new Dictionary<Type, ISceneTypeSerializer>
        {
            { typeof(byte), new ToStringSerializer() },
            { typeof(sbyte), new ToStringSerializer() },
            { typeof(short), new ToStringSerializer() },
            { typeof(ushort), new ToStringSerializer() },
            { typeof(int), new ToStringSerializer() },
            { typeof(uint), new ToStringSerializer() },
            { typeof(long), new ToStringSerializer() },
            { typeof(ulong), new ToStringSerializer() },
            { typeof(float), new ToStringSerializer() },
            { typeof(double), new ToStringSerializer() },
            { typeof(string), new StringSerializer() },
            { typeof(Vector2), new Vector2Serializer() },
            { typeof(Vector3), new Vector3Serializer() },
            { typeof(Vector4), new Vector4Serializer() },
            { typeof(Quaternion), new QuaternionSerializer() },
            { typeof(Color), new ColorSerializer() },
            { typeof(Color32), new Color32Serializer() },
            { typeof(Enum), new EnumSerializer() },
            { typeof(RenderMesh), new RenderMeshSerializer() },
            { typeof(IAssetDefinition), new AssetDefinitionSerializer() },
        };
    }
}
