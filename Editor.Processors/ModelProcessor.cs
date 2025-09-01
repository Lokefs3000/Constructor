using CommunityToolkit.HighPerformance;
using Silk.NET.Assimp;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.Processors
{
    public unsafe class ModelProcessor : IAssetProcessor
    {
        /*
         
        Header:
            uint Id
            uint Version
            uint VertexCount
            uint IndexCount
            Mesh[] Meshes (ushort idx)
            Material[] Materials (ushort idx)
            Light[] Lights (ushort idx)
            Node RootNode
         
        Node:
            string Name
            Matrix4x4 Transformation
            uint[] Meshes (ushort idx)
            Node[] Children (ushort idx)

        Mesh:
            string Name
            uint VertexCount
            uint IndexCount
            byte NumUVs (bool encoded)
            uint Material
            Vertex[] Vertices
            ushort[] Indices
  
        Material:
            string Name
            Texture[] Textures (byte idx)
            
        Texture:
            ModelTextureType Type
            ModelTextureDataType DataType
            string Path (DataType == Real)
            byte[] Data (uint idx, DataType == Embedded)

        Light:
            string Name
            ModelLightType Type
            Vector3 Position (Type == Spot | Point| Area)
            Vector3 Direction (Type == Spot | Directional| Area)
            float Brightness
            float Range (Type == Spot | Point | Area)
            Vector3 ColorDiffuse
            Vector3 ColorSpecular
            float AngleInnerCone (Type == Spot)
            float AngleOuterCone (Type == Spot)
            Vector2 Size (Type == Area)

        Vertex:
            Vector3 Position
            Vector3 Normal
            Vector3 Tangent
            Vector3 Bitangent
            Vector2[] UVs

        */

        public bool Execute(object args_in)
        {
            ModelProcessorArgs args = (ModelProcessorArgs)args_in;

            using Assimp assimp = Assimp.GetApi();

            PostProcessSteps steps = PostProcessPreset.TargetRealTimeMaximumQuality;

            Scene* scene = assimp.ImportFile(args.AbsoluteFilepath, (uint)steps);
            if (scene == null)
            {
                //bad
                return false;
            }

            using FileStream stream = System.IO.File.Open(args.AbsoluteOutputPath, FileMode.Create, FileAccess.Write);
            using BinaryWriter bw = new BinaryWriter(stream);

            WriteHeader(bw, scene);
            WriteMeshes(bw, scene);
            WriteMaterials(bw, scene);
            WriteLights(bw, scene);
            WriteNode(bw, scene, scene->MRootNode);

            return true;
        }

        private void WriteHeader(BinaryWriter bw, Scene* scene)
        {
            bw.Write(HeaderId);
            bw.Write(HeaderVersion);

            uint totalVertices = 0;
            uint totalIndices = 0;

            for (uint i = 0; i < scene->MNumMeshes; i++)
            {
                Mesh* mesh = scene->MMeshes[i];
                totalVertices += mesh->MNumVertices;

                for (uint j = 0; j < mesh->MNumFaces; j++)
                {
                    Face face = mesh->MFaces[j];
                    totalIndices += face.MNumIndices;
                }
            }

            bw.Write(totalVertices);
            bw.Write(totalIndices);
        }

        private void WriteMeshes(BinaryWriter bw, Scene* scene)
        {
            bw.Write((ushort)scene->MNumMeshes);

            List<nint> uvChannels = new List<nint>(8);
            for (uint i = 0; i < scene->MNumMeshes; i++)
            {
                Mesh* mesh = scene->MMeshes[i];

                bw.Write(mesh->MName.AsString);
                bw.Write(mesh->MNumVertices);

                uint totalIndices = 0;
                for (uint j = 0; j < mesh->MNumFaces; j++)
                {
                    Face face = mesh->MFaces[j];
                    totalIndices += face.MNumIndices;
                }

                bw.Write(totalIndices);

                uvChannels.Clear();

                byte numUVCoded = 0;
                for (int j = 0; j < 8; j++)
                {
                    if (mesh->MTextureCoords[j] != null)
                    {
                        numUVCoded |= (byte)(1 << j);
                        uvChannels.Add((nint)(mesh->MTextureCoords.Element0 + j));
                    }
                }

                bw.Write(numUVCoded);
                bw.Write(mesh->MMaterialIndex);

                int channels = 12 + BitOperations.PopCount(numUVCoded) * 2;

                float* vertices = (float*)NativeMemory.Alloc((nuint)(channels * sizeof(float) * mesh->MNumVertices));
                ushort* indices = (ushort*)NativeMemory.Alloc(totalIndices * sizeof(ushort));

                for (uint j = 0; j < mesh->MNumVertices; j++)
                {
                    uint index = j * (uint)channels;

                    vertices[index] = mesh->MVertices[j].X;
                    vertices[index + 1] = mesh->MVertices[j].Y;
                    vertices[index + 2] = mesh->MVertices[j].Z;

                    vertices[index + 3] = mesh->MNormals[j].X;
                    vertices[index + 4] = mesh->MNormals[j].Y;
                    vertices[index + 5] = mesh->MNormals[j].Z;

                    vertices[index + 6] = mesh->MTangents[j].X;
                    vertices[index + 7] = mesh->MTangents[j].Y;
                    vertices[index + 8] = mesh->MTangents[j].Z;

                    vertices[index + 9] = mesh->MBitangents[j].X;
                    vertices[index + 10] = mesh->MBitangents[j].Y;
                    vertices[index + 11] = mesh->MBitangents[j].Z;

                    index += 12;
                    for (int k = 0; k < uvChannels.Count; k++)
                    {
                        Vector3* channel = (Vector3*)uvChannels[k];

                        vertices[index] = channel[j].X;
                        vertices[index + 1] = channel[j].Y;

                        index += 2;
                    }
                }

                ushort* currentIndex = indices;
                for (uint j = 0; j < mesh->MNumFaces; j++)
                {
                    Face face = mesh->MFaces[j];

                    for (uint k = 0; k < face.MNumIndices; k++)
                    {
                        *currentIndex = (ushort)face.MIndices[k];
                        currentIndex++;
                    }
                }

                bw.Write(new ReadOnlySpan<float>(vertices, (int)(channels * mesh->MNumVertices)));
                bw.Write(new ReadOnlySpan<ushort>(indices, (int)totalIndices));

                NativeMemory.Free(vertices);
                NativeMemory.Free(indices);
            }
        }

        private void WriteMaterials(BinaryWriter bw, Scene* scene)
        {
            bw.Write((ushort)scene->MNumMaterials);

            for (uint i = 0; i < scene->MNumMaterials; i++)
            {
                Material* material = scene->MMaterials[i];

                for (uint j = 0; j < material->MNumProperties; j++)
                {
                    MaterialProperty* property = material->MProperties[j];
                    if (property->MKey == "?mat.name")
                    {
                        bw.Write(Encoding.UTF8.GetString(property->MData, (int)property->MDataLength));
                        break;
                    }
                }

                long prev = bw.BaseStream.Position;
                bw.Write(byte.MinValue);

                int written = 0;
                for (uint j = 0; j < material->MNumProperties; j++)
                {
                    MaterialProperty* property = material->MProperties[j];

                    ModelTextureType textureType = ModelTextureType.Diffuse;
                    ModelTextureDataType dataType = ModelTextureDataType.Real;

                    switch ((TextureType)property->MSemantic)
                    {
                        case TextureType.Diffuse: textureType = ModelTextureType.Diffuse; break;
                        case TextureType.Metalness: textureType = ModelTextureType.Metallic; break;
                        case TextureType.DiffuseRoughness: textureType = ModelTextureType.Roughness; break;
                        case TextureType.Normals: textureType = ModelTextureType.Normal; break;
                        default: continue;
                    }

                    throw new NotImplementedException();
                }
            }
        }

        private void WriteLights(BinaryWriter bw, Scene* scene)
        {
            bw.Write((ushort)scene->MNumLights);

            for (uint j = 0; j < scene->MNumLights; j++)
            {
                Light* light = scene->MLights[j];
                ModelLightType lightType = ModelLightType.Directional;

                bw.Write(light->MName.AsString);

                switch (light->MType)
                {
                    case LightSourceType.Directional: bw.Write((byte)ModelLightType.Directional); lightType = ModelLightType.Directional; break;
                    case LightSourceType.Point: bw.Write((byte)ModelLightType.Point); lightType = ModelLightType.Point; break;
                    case LightSourceType.Spot: bw.Write((byte)ModelLightType.Spot); lightType = ModelLightType.Spot; break;
                    case LightSourceType.Area: bw.Write((byte)ModelLightType.Area); lightType = ModelLightType.Area; break;
                    default: bw.Write((byte)lightType); break;
                }

                if (lightType != ModelLightType.Directional)
                    bw.Write(light->MPosition.X);
                if (lightType != ModelLightType.Point)
                    bw.Write(light->MDirection.X);

                throw new NotImplementedException();
            }
        }

        private void WriteNode(BinaryWriter bw, Scene* scene, Node* node)
        {
            bw.Write(node->MName.AsString);
            bw.Write(node->MTransformation);

            bw.Write((ushort)node->MNumMeshes);
            bw.Write(new ReadOnlySpan<uint>(node->MMeshes, (int)node->MNumMeshes));

            bw.Write((ushort)node->MNumChildren);
            for (uint i = 0; i < node->MNumChildren; i++)
            {
                WriteNode(bw, scene, node->MChildren[i]);
            }
        }

        private static uint HeaderId = 0x46444d45;
        private static uint HeaderVersion = 0;
    }

    public struct ModelProcessorArgs
    {
        public string AbsoluteFilepath;
        public string AbsoluteOutputPath;


    }

    public enum ModelTextureType : byte
    {
        Diffuse = 0,
        Metallic,
        Roughness,
        Normal
    }

    public enum ModelTextureDataType : byte
    {
        Real = 0,
        Embedded
    }

    public enum ModelLightType : byte
    {
        Directional = 0,
        Spot,
        Point,
        Area
    }
}
