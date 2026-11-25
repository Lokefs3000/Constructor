using CommunityToolkit.HighPerformance.Buffers;
using Primary.Serialization.Json;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace Editor.Geometry.Serialization
{
    public static class SceneSerializer
    {
        public static void Serialize(string outPath, GeoScene scene)
        {
            using MemoryStream buffer = new MemoryStream();

            {
                using Utf8JsonWriter writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true, IndentSize = 4 });

                writer.WriteStartObject();

                writer.WriteStartArray("Groups");

                foreach (BrushGroup group in scene.Groups)
                {
                    writer.WriteStartObject();

                    writer.WriteString("Name", group.Name);

                    writer.WriteStartArray("Brushes");

                    foreach (Brush brush in group.Brushes)
                    {
                        writer.WriteStartObject();

                        writer.WriteNumber("Id", brush.Id.LocalId);

                        writer.WriteStartObject("Transform");

                        writer.WriteStartArray("Rotation");
                        writer.WriteNumberValue(0.0f);
                        writer.WriteNumberValue(0.0f);
                        writer.WriteNumberValue(0.0f);
                        writer.WriteNumberValue(1.0f);
                        writer.WriteEndArray();

                        writer.WriteStartArray("Origin");
                        writer.WriteNumberValue(0.0f);
                        writer.WriteNumberValue(0.0f);
                        writer.WriteNumberValue(0.0f);
                        writer.WriteEndArray();

                        writer.WriteEndObject();

                        writer.WriteStartArray("Vertices");

                        foreach (Vector3 vertex in brush.Vertices)
                        {
                            writer.WriteStartArray();
                            writer.WriteNumberValue(vertex.X);
                            writer.WriteNumberValue(vertex.Y);
                            writer.WriteNumberValue(vertex.Z);
                            writer.WriteEndArray();
                        }

                        writer.WriteEndArray();

                        writer.WriteStartArray("Faces");

                        foreach (BrushFace face in brush.Faces)
                        {
                            writer.WriteStartObject();

                            if (face.Material != null)
                                writer.WriteString("Material", face.Material.Id.ToString());
                            else
                                writer.WriteNull("Material");

                            writer.WriteStartArray("UVScale");
                            writer.WriteNumberValue(face.UVScale.X);
                            writer.WriteNumberValue(face.UVScale.Y);
                            writer.WriteEndArray();

                            writer.WriteStartArray("UVOffset");
                            writer.WriteNumberValue(face.UVOffset.X);
                            writer.WriteNumberValue(face.UVOffset.Y);
                            writer.WriteEndArray();

                            writer.WriteNumber("Flags", (uint)face.Flags);

                            writer.WriteEndObject();
                        }

                        writer.WriteEndArray();
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();

                writer.WriteEndObject();
            }

            {
                using Stream stream = File.Open(outPath, FileMode.Create);

                int read;
                byte[] temp = new byte[1024];

                buffer.Seek(0, SeekOrigin.Begin);
                while ((read = buffer.Read(temp)) > 0)
                    stream.Write(temp[..read]);
            }
        }
    }
}
