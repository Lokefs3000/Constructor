using Arch.Core;
using Primary.Assets;
using Primary.Common;
using Primary.Serialization;
using Serilog;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Scenes
{
    internal static class SceneDeserializer
    {
        internal static void Deserialize(ReadOnlySpan<char> path, Scene scene)
        {
            Stream? stream = AssetFilesystem.OpenStream(path);
            if (stream == null)
            {
                Log.Error("Failed to open stream: {path} for scene deserialization", path.ToString());
                return;
            }

            using PoolArray<char> array = ArrayPool<char>.Shared.Rent((int)stream.Length);

            stream.ReadExactly(MemoryMarshal.Cast<char, byte>(array.AsSpan()));
            stream?.Dispose();

            SDFReader reader = new SDFReader(array.Array);

            Stack<Entity> currentEntity = new Stack<Entity>();
            while (!reader.IsEOF)
            {
                ReadSingleObject();
            }

            void ReadSingleObject()
            {
                ExceptionUtility.Assert(reader.BeginObject(out string? objName));

                if (objName == "Entity")
                {
                    /*Entity e = scene.CreateEntity(currentEntity.Count > 0 ? currentEntity.Peek() : Entity.Null);
                    currentEntity.Push(e);

                    while (reader.IsObjectActive)
                    {
                        if (reader.TryPeekObject(out string? newObjName))
                        {
                            if (newObjName == "@Children")
                            {
                                ExceptionUtility.Assert(reader.BeginObject(out string? _));

                                while (reader.IsObjectActive)
                                {
                                    ReadSingleObject();
                                }

                                ExceptionUtility.Assert(reader.EndObject());
                            }
                            else if (!SerializerTypes.TryDeserializeComponent(newObjName!, reader, e))
                            {
                                Log.Warning("Unknown object: {obj}", newObjName);
                            }
                        }
                    }

                    currentEntity.Pop();*/
                }

                ExceptionUtility.Assert(reader.EndObject());
            }
        }
    }
}
