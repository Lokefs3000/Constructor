using Primary.Assets;
using Primary.Components;
using Primary.Scenes;
using Primary.Serialization.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace Editor.Geometry.Serialization
{
    public static class SceneDeserializer
    {
        public static void Deserialize(ReadOnlySpan<byte> source, GeoScene scene)
        {
            Utf8JsonReader reader = new Utf8JsonReader(source);

            reader.Read();
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new GeoSceneJsonException("Geo scene file should start with an object");

            while (reader.BytesConsumed < source.Length)
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;
                else if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new GeoSceneJsonException("Expected property name for root object value");

                string? key = reader.GetString();
                if (key == "Groups")
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new GeoSceneJsonException("Expected array start for groups list");

                    while (reader.BytesConsumed < source.Length)
                    {
                        DeserializeGroup(ref reader, scene, source.Length);
                        if (reader.TokenType == JsonTokenType.EndArray)
                            break;
                    }

                    if (reader.TokenType != JsonTokenType.EndArray)
                        throw new GeoSceneJsonException("Expected array end for groups list");
                }
                else
                    throw new GeoSceneJsonException($"Unexpected property in root object {key}");
            }
        }

        private static void DeserializeGroup(ref Utf8JsonReader reader, GeoScene scene, int sourceLength)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndArray)
                return;
            else if (reader.TokenType != JsonTokenType.StartObject)
                throw new GeoSceneJsonException("Expected object start for group");

            BrushGroup? group = null;

            while (reader.BytesConsumed < sourceLength)
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;
                else if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new GeoSceneJsonException("Expected property name for group value");

                string? key = reader.GetString();
                if (key == "Name")
                {
                    if (group != null)
                        throw new GeoSceneJsonException("Group already has a name");

                    reader.Read();
                    if (reader.TokenType != JsonTokenType.String)
                        throw new GeoSceneJsonException("Expected string for group name");

                    group = scene.CreateGroup(reader.GetString()!);
                }
                else if (key == "Brushes")
                {
                    if (group == null)
                        throw new GeoSceneJsonException("Group cannot be assigned brushes yet because it is not defined");

                    reader.Read();
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new GeoSceneJsonException("Expected array start for brush list");

                    while (reader.BytesConsumed < sourceLength)
                    {
                        DeserializeBrush(ref reader, group, sourceLength);
                        if (reader.TokenType == JsonTokenType.EndArray)
                            break;
                    }

                    if (reader.TokenType != JsonTokenType.EndArray)
                        throw new GeoSceneJsonException("Expected array end for brush list");
                }
                else
                    throw new GeoSceneJsonException($"Unexpected property in group {key}");
            }

            if (reader.TokenType != JsonTokenType.EndObject)
                throw new GeoSceneJsonException("Expected object end for group");
        }

        private static void DeserializeBrush(ref Utf8JsonReader reader, BrushGroup group, int sourceLength)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndArray)
                return;
            else if (reader.TokenType != JsonTokenType.StartObject)
                throw new GeoSceneJsonException("Expected object start for brush");

            Brush? brush = null;

            while (reader.BytesConsumed < sourceLength)
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;
                else if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new GeoSceneJsonException("Expected property name for brush value");

                string? key = reader.GetString();
                if (key == "Id")
                {
                    if (brush != null)
                        throw new GeoSceneJsonException("Brush already has an id");

                    reader.Read();
                    if (reader.TokenType != JsonTokenType.Number)
                        throw new GeoSceneJsonException("Expected number for brush id");

                    brush = group.CreateBrushWithId(reader.GetInt32());
                }
                else if (key == "Transform")
                {
                    if (brush == null)
                        throw new GeoSceneJsonException("Brush cannot have its data set yet because it is not defined");

                    reader.Read();
                    if (reader.TokenType != JsonTokenType.StartObject)
                        throw new GeoSceneJsonException("Expected object start for transform");

                    DeserializeTransform(ref reader, brush.Transform, sourceLength);

                    if (reader.TokenType != JsonTokenType.EndObject)
                        throw new GeoSceneJsonException("Expected object end for transform");
                }
                else if (key == "Vertices")
                {
                    if (brush == null)
                        throw new GeoSceneJsonException("Brush cannot have its data set yet because it is not defined");

                    reader.Read();
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new GeoSceneJsonException("Expected array start for vertex list");

                    for (int i = 0; i < 8; i++)
                    {
                        brush.Vertices[i] = Vector3JsonConverter.Default.Read(ref reader, typeof(Vector3), JsonSerializerOptions.Default);
                    }

                    reader.Read();
                    if (reader.TokenType != JsonTokenType.EndArray)
                        throw new GeoSceneJsonException("Expected array end for vertex list");
                }
                else if (key == "Faces")
                {
                    if (brush == null)
                        throw new GeoSceneJsonException("Brush cannot have its data set yet because it is not defined");

                    reader.Read();
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new GeoSceneJsonException("Expected array start for face list");

                    for (int i = 0; i < 6; i++)
                    {
                        DeserializeBrushFace(ref reader, ref brush.Faces[i], sourceLength);
                    }

                    reader.Read();
                    if (reader.TokenType != JsonTokenType.EndArray)
                        throw new GeoSceneJsonException("Expected array end for face list");
                }
                else
                    throw new GeoSceneJsonException($"Unexpected property in brush {key}");
            }

            if (reader.TokenType != JsonTokenType.EndObject)
                throw new GeoSceneJsonException("Expected object end for group");
        }

        private static void DeserializeTransform(ref Utf8JsonReader reader, BrushTransform transform, int sourceLength)
        {
            while (reader.BytesConsumed < sourceLength)
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;
                else if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new GeoSceneJsonException("Expected property name for transform value");

                string? key = reader.GetString();
                if (key == "Rotation")
                    transform.Rotation = QuaternionJsonConverter.Default.Read(ref reader, typeof(Quaternion), JsonSerializerOptions.Default);
                else if (key == "Origin")
                    transform.Origin = Vector3JsonConverter.Default.Read(ref reader, typeof(Vector3), JsonSerializerOptions.Default);
                else
                    throw new GeoSceneJsonException($"Unexpected property in transform {key}");
            }
        }

        private static void DeserializeBrushFace(ref Utf8JsonReader reader, ref BrushFace face, int sourceLength)
        {
            reader.Read();
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new GeoSceneJsonException("Expected object start for brush face");

            while (reader.BytesConsumed < sourceLength)
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;
                else if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new GeoSceneJsonException("Expected property name for brush face value");

                string? key = reader.GetString();
                if (key == "Material")
                    face.Material = (MaterialAsset?)AssetJsonConverter.Default.Read(ref reader, typeof(MaterialAsset), JsonSerializerOptions.Default);
                else if (key == "UVScale")
                    face.UVScale = Vector2JsonConverter.Default.Read(ref reader, typeof(Vector2), JsonSerializerOptions.Default);
                else if (key == "UVOffset")
                    face.UVOffset = Vector2JsonConverter.Default.Read(ref reader, typeof(Vector2), JsonSerializerOptions.Default);
                else if (key == "Flags")
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.Number)
                        throw new GeoSceneJsonException("Expected number for brush face flags");

                    face.Flags = (BrushFaceFlags)reader.GetUInt32();
                }
                else
                    throw new GeoSceneJsonException($"Unexpected property in brush face {key}");
            }

            if (reader.TokenType != JsonTokenType.EndObject)
                throw new GeoSceneJsonException("Expected object end for brush face");
        }
    }

    public sealed class GeoSceneJsonException : Exception
    {
        public GeoSceneJsonException()
        {
        }

        public GeoSceneJsonException(string? message) : base(message)
        {
        }
    }
}
