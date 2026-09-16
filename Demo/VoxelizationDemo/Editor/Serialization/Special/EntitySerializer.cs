using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Arch.Core;
using Collections.Pooled;
using CommunityToolkit.Diagnostics;
using Primary.Collections;
using Primary.Components;
using Primary.Reflection;
using Primary.Scenes;

namespace VoxelizationDemo.Editor.Serialization.Special
{
    internal static class EntitySerializer
    {
        public static void Serialize(Stream stream, ReadOnlySpan<SceneEntity> entities)
        {
            JsonWriterOptions writerOptions = new JsonWriterOptions { };
            using Utf8JsonWriter writer = new Utf8JsonWriter(stream, writerOptions);

            for (int i = 0; i < entities.Length; ++i)
            {
                WriteDescending(entities[i]);
            }

            void WriteDescending(SceneEntity entity)
            {
                if (entity.HasComponent<DontSerializeTag>())
                    return;

                writer.WriteStartObject();

                writer.WriteNumber("id"u8, entity.WrappedEntity.Id);
                writer.WriteBoolean("enabled"u8, entity.Enabled);
                writer.WriteString("name"u8, entity.Name);

                {
                    writer.WriteStartArray("components"u8);

                    SerializationManager manager = EditorManager.Instance.SerializationManager;

                    foreach (ComponentType type in entity.ComponentTypes)
                    {
                        if (type.Type.GetCustomAttribute<DontSerializeComponentAttribute>() != null)
                            continue;

                        IComponent? boxedComponent = entity.GetComponent(type);
                        Guard.IsNotNull(boxedComponent, $"Failed to get component of type '{type}' on entity");

                        string? fullName = TypeKeyReflector.TypeToKey(type);
                        Guard.IsNotNull(fullName, $"Failed to get component key for type '{type}'");

                        writer.WriteStartObject();
                        writer.WriteString("key"u8, fullName);

                        manager.ObjectSerializer.SerializeFromBox(writer, boxedComponent);

                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                }

                {
                    writer.WriteStartArray("children"u8);

                    foreach (SceneEntity child in entity.Children)
                    {
                        WriteDescending(child);
                    }

                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }
        }
    }
}
