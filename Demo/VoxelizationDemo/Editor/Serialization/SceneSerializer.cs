using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Arch.Core;
using CommunityToolkit.Diagnostics;
using Primary.Collections;
using Primary.Components;
using Primary.Reflection;
using Primary.Scenes;
using Primary.Scenes.Json;
using VoxelizationDemo.Core;

namespace VoxelizationDemo.Editor.Serialization
{
    internal static class SceneSerializer
    {
        public static void Serialize(Stream stream, Scene scene)
        {
            JsonWriterOptions writerOptions = new JsonWriterOptions
            {
                Indented = true,
                IndentSize = 4
            };

            Utf8JsonWriter writer = new Utf8JsonWriter(stream, writerOptions);
            writer.WriteStartObject();

            using RentedList<ComponentToSerialize> componentsToSerialize = new RentedList<ComponentToSerialize>();

            {
                using RentedStack<(SceneEntity Entity, int Id)> entityStack = new RentedStack<(SceneEntity Entity, int Id)>();
                entityStack.Push((scene.Root, -1));

                writer.WriteStartArray("Entities"u8);

                int currentId = -1;
                while (entityStack.TryPop(out (SceneEntity Entity, int Id) currentEntityInfo))
                {
                    int thisId = currentId++;
                    if (currentEntityInfo.Entity != scene.Root)
                    {
                        writer.WriteStartObject();

                        writer.WriteNumber("Id"u8, currentEntityInfo.Id);
                        if (currentEntityInfo.Id != -1)
                            writer.WriteNumber("Parent"u8, thisId);
                        writer.WriteBoolean("Enabled"u8, currentEntityInfo.Entity.Enabled);
                        writer.WriteString("Name"u8, currentEntityInfo.Entity.Name);

                        writer.WriteStartArray("Components"u8);
                        foreach (ComponentType type in currentEntityInfo.Entity.ComponentTypes)
                        {
                            writer.WriteStringValue(SceneJsonSerializer.GetComponentKey(type.Type));
                            componentsToSerialize.Add(new ComponentToSerialize(currentEntityInfo.Entity, type));
                        }
                        writer.WriteEndArray();

                        writer.WriteEndObject();
                    }

                    SceneEntityChildren children = currentEntityInfo.Entity.Children;
                    for (int i = children.Count - 1; i >= 0; i--)
                    {
                        entityStack.Push((children[i], thisId));
                    }
                }

                writer.WriteEndArray();
            }
            {
                writer.WriteStartArray("Components"u8);

                SceneDeserializer deserializer = VoxelRuntime.Instance.SceneManager.Deserializer;
                foreach (ComponentToSerialize toSerialize in componentsToSerialize)
                {
                    SceneEntity entity = toSerialize.Entity;

                    IComponent? boxedComponent = toSerialize.Entity.GetComponent(toSerialize.ComponentType);
                    Guard.IsNotNull(boxedComponent, $"Failed to get component of type '{toSerialize.ComponentType}' on entity");

                    string? fullName = SceneJsonSerializer.GetComponentKey(toSerialize.ComponentType);
                    Guard.IsNotNull(fullName, $"Failed to get component key for type '{toSerialize.ComponentType}'");

                    ComponentReflection reflection = deserializer.ReflectionCache.GetReflection(fullName);

                    writer.WriteStartObject();
                    writer.WriteString("Key"u8, fullName);
                    
                    foreach (var (name, field) in reflection.Fields)
                    {
                        object? fieldValue = field.GetValue(boxedComponent);
                        if (fieldValue != null)
                        {
                            writer.WritePropertyName(name);
                            bool didActuallySerialize = SceneJsonSerializer.SerializeGeneric(writer, fieldValue, ref entity);
                        }
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }
        }

        private readonly record struct ComponentToSerialize(SceneEntity Entity, Type ComponentType);
    }
}
