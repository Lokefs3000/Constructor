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
using Primary.Common;
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

        public static SceneEntity Deserialize(Stream stream, Scene scene, SceneEntity parentEntity)
        {
            RentedList<SceneEntity> rootEntities = new RentedList<SceneEntity>();

            using RentedArray<byte> readFromStream = new RentedArray<byte>((int)stream.Length);

            stream.Seek(0, SeekOrigin.Begin);
            stream.ReadExactly(readFromStream.Span);

            JsonReaderOptions readerOptions = new JsonReaderOptions { };
            Utf8JsonReader reader = new Utf8JsonReader(readFromStream.Span, readerOptions);

            try
            {
                while (reader.BytesConsumed < readFromStream.Count)
                {
                    ReadDescending(ref reader, ref rootEntities, parentEntity, true);
                }

                return rootEntities[0];
            }
            catch (Exception)
            {
                foreach (SceneEntity rootEntity in rootEntities)
                {
                    rootEntity.Destroy();
                }

                rootEntities.Dispose();
                throw;
            }

            void ReadDescending(ref Utf8JsonReader reader, ref RentedList<SceneEntity> rootEntities, SceneEntity parentEntity, bool isRootValue)
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.EndArray)
                    return;
                else if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException("Expected start object");
                
                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals("id"u8))
                    throw new JsonException("Expected id property");

                reader.Read();
                int id = reader.GetInt32();

                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals("enabled"u8))
                    throw new JsonException("Expected enabled property");

                reader.Read();
                bool enabled = reader.GetBoolean();

                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals("name"u8))
                    throw new JsonException("Expected name property");

                reader.Read();
                string name = reader.GetString() ?? string.Empty;

                SceneEntity newEntity = scene.CreateEntity(parentEntity);
                newEntity.Enabled = enabled;
                newEntity.Name = name;

                if (isRootValue)
                    rootEntities.Add(newEntity);

                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals("components"u8))
                        throw new JsonException("Expected components property");

                    reader.Read();
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new JsonException("Expected start array");

                    SerializationManager manager = EditorManager.Instance.SerializationManager;

                    while (reader.TokenType != JsonTokenType.EndArray)
                    {
                        reader.Read();
                        if (reader.TokenType == JsonTokenType.EndArray)
                            break;
                        else if (reader.TokenType != JsonTokenType.StartObject)
                            throw new JsonException("Expected start object");

                        reader.Read();
                        if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals("key"u8))
                            throw new JsonException("Expected key property");

                        reader.Read();

                        Type? type = TypeKeyReflector.KeyToType(reader.GetString() ?? string.Empty);
                        Guard.IsNotNull(type, $"Failed to get type from component key '{reader.GetString()}'");

                        IComponent? boxedComponent = newEntity.AddComponent(type);
                        Guard.IsNotNull(boxedComponent, $"Failed to add component of type '{type}' to entity");

                        while (reader.TokenType != JsonTokenType.EndObject)
                        {
                            reader.Read();
                            if (reader.TokenType == JsonTokenType.EndObject)
                                break;
                            else if (reader.TokenType != JsonTokenType.PropertyName)
                                throw new JsonException("Expected property name");

                            ReadOnlySpan<byte> propertyName = reader.ValueSpan;

                            reader.Read();
                            manager.ObjectSerializer.DeserializeIntoBox(boxedComponent, propertyName, ref reader);
                        }

                        newEntity.SetComponent(boxedComponent, type);
                    }
                }

                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals("children"u8))
                        throw new JsonException("Expected children property");

                    reader.Read();
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new JsonException("Expected start array");

                    while (reader.TokenType != JsonTokenType.EndArray)
                    {
                        ReadDescending(ref reader, ref rootEntities, newEntity, false);
                    }
                }

                reader.Read();
                if (reader.TokenType != JsonTokenType.EndObject)
                    throw new JsonException("Expected end object");
            }
        }
    }
}
