using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Arch.Core;
using Primary.Assets;
using Primary.Common;
using Primary.Components;
using Primary.Mathematics;
using Primary.Scenes;
using VoxelizationDemo.Components;
using VoxelizationDemo.Core;

using DirectionalLight = VoxelizationDemo.Components.DirectionalLight;

namespace VoxelizationDemo.Procedural
{
    internal static class TestScene1
    {
        internal static Scene Generate(VoxelRuntime runtime)
        {
            const int ChunkCountX = 8;
            const int ChunkCountZ = 8;
            const int ChunkSizeXZ = 8;

            const float TerrainHeight = 6.0f;
            const float TerrainScale = 3.0f;
            const float TerrainNoiseScale = 3.0f;

            const int SpiralPointCount = 32;

            long startTime = Stopwatch.GetTimestamp();
            int entityCount = 0;

            Scene scene = runtime.SceneManager.CreateScene("Test 1", LoadSceneMode.Additive);

            MeshAsset cubeMesh = AssetManager.LoadAsset<ModelAsset>("Engine/Models/Cube.fbx")
                .WaitIfNotLoaded()
                .Meshes[0].Asset.Value;

            MeshAsset cylinderMesh = AssetManager.LoadAsset<ModelAsset>("Engine/Models/Cylinder.fbx")
               .WaitIfNotLoaded()
               .Meshes[0].Asset.Value;

            MeshAsset sphereMesh = AssetManager.LoadAsset<ModelAsset>("Engine/Models/Sphere.fbx")
              .WaitIfNotLoaded()
              .Meshes[0].Asset.Value;

            MeshAsset torusMesh = AssetManager.LoadAsset<ModelAsset>("Engine/Models/Torus.fbx")
              .WaitIfNotLoaded()
              .Meshes[0].Asset.Value;

            MaterialAsset spireBodyMat = AssetManager.LoadAsset<MaterialAsset>("Content/Materials/Testing/SpireBody.mat");
            MaterialAsset spireBulbMat = AssetManager.LoadAsset<MaterialAsset>("Content/Materials/Testing/SpireBulb.mat");
            MaterialAsset groundMat = AssetManager.LoadAsset<MaterialAsset>("Content/Materials/Testing/Ground.mat");

            FastNoiseLite fnl = new FastNoiseLite();
            fnl.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            fnl.SetFractalOctaves(2);

            for (int chunkX = 0; chunkX < ChunkCountX; ++chunkX)
            {
                for (int chunkZ = 0; chunkZ < ChunkCountZ; chunkZ++)
                {
                    SceneEntity entity = scene.CreateEntity(SceneEntity.Null);
                    entity.Name = $"Group_{chunkX}x{chunkZ}";
                    ++entityCount;

                    for (int x = 0; x < ChunkSizeXZ; ++x)
                    {
                        for (int z = 0; z < ChunkSizeXZ; ++z)
                        {
                            Vector2 xz = new Vector2(chunkX * ChunkSizeXZ + x, chunkZ * ChunkSizeXZ + z);
                            float heightValue = fnl.GetNoise(xz.X * TerrainNoiseScale, xz.Y * TerrainNoiseScale);

                            SceneEntity mesh = scene.CreateEntity(entity);
                            mesh.Name = $"Cube_{x}x{z}";

                            ref Transform transform = ref mesh.GetComponent<Transform>();
                            transform.Position = new Vector3(
                                xz.X * TerrainScale,
                                heightValue * TerrainHeight,
                                xz.Y * TerrainScale);
                            transform.Scale = new Vector3(TerrainScale, 2.0f, TerrainScale);

                            ref MeshRenderer meshRenderer = ref mesh.AddComponent<MeshRenderer>();
                            meshRenderer.Mesh = cubeMesh;
                            meshRenderer.Material = groundMat;

                            ++entityCount;
                        }
                    }
                }
            }

            float goldenRatio = (1.0f + MathF.Sqrt(5.0f)) / 2.0f;
            float goldenAngle = 2.0f * MathF.PI * (1.0f - (1.0f / goldenRatio));

            float radiusScale = (ChunkCountX * ChunkSizeXZ - 2) * TerrainScale / MathF.Sqrt(SpiralPointCount - 1) * 0.5f;

            Vector3 center = new Vector3(ChunkCountX, 0.0f, ChunkCountZ) * ChunkSizeXZ * TerrainScale * 0.5f;

            for (int i = 0; i < SpiralPointCount; ++i)
            {
                float radius = radiusScale * MathF.Sqrt(i);
                float theta = i * goldenAngle;

                Vector3 position = new Vector3(MathF.Cos(theta), 0.0f, MathF.Sin(theta)) * radius + center;
                
                SceneEntity spire = scene.CreateEntity(SceneEntity.Null);
                spire.Name = $"Sprire_{i}";
                ++entityCount;

                {
                    float heightValue = fnl.GetNoise(position.X / TerrainScale * TerrainNoiseScale, position.Z / TerrainScale * TerrainNoiseScale);

                    ref Transform transform = ref spire.GetComponent<Transform>();
                    transform.Position = position + new Vector3(0.0f, heightValue * TerrainHeight, 0.0f);
                    transform.Rotation = Quaternion.CreateFromYawPitchRoll(
                        fnl.GetNoise(position.X, position.Y, 25892.0f) * float.Pi,
                        fnl.GetNoise(position.X, position.Y, 83156.0f) * 0.065f * float.Pi,
                        fnl.GetNoise(position.X, position.Y, 12567.0f) * 0.065f * float.Pi);
                }

                {
                    SceneEntity body = scene.CreateEntity(spire);
                    body.Name = "Body";

                    ref Transform transform = ref body.GetComponent<Transform>();
                    transform.Position = new Vector3(0.0f, 10.0f, 0.0f);
                    transform.Scale = new Vector3(2.0f, 20.0f, 2.0f);

                    ref MeshRenderer meshRenderer = ref body.AddComponent<MeshRenderer>();
                    meshRenderer.Mesh = cylinderMesh;
                    meshRenderer.Material = spireBodyMat;

                            ++entityCount;
                }

                {
                    SceneEntity head = scene.CreateEntity(spire);
                    head.Name = "Head";

                    ref Transform transform = ref head.GetComponent<Transform>();
                    transform.Position = new Vector3(0.0f, 20.375f, 0.0f);
                    transform.Scale = new Vector3(3.5f, 3.5f, 3.5f);

                    ref MeshRenderer meshRenderer = ref head.AddComponent<MeshRenderer>();
                    meshRenderer.Mesh = torusMesh;
                    meshRenderer.Material = spireBodyMat;

                    ++entityCount;
                }

                {
                    SceneEntity bulb = scene.CreateEntity(spire);
                    bulb.Name = "Bulb";

                    ref Transform transform = ref bulb.GetComponent<Transform>();
                    transform.Position = new Vector3(0.0f, 22.25f, 0.0f);
                    transform.Scale = new Vector3(3.0f, 5.5f, 3.0f);

                    ref MeshRenderer meshRenderer = ref bulb.AddComponent<MeshRenderer>();
                    meshRenderer.Mesh = sphereMesh;
                    meshRenderer.Material = spireBulbMat;

                    ref PointLight light = ref bulb.AddComponent<PointLight>();
                    light.Diffuse = new Color(217, 200, 165);
                    light.Radius = 33.0f;
                    light.Intensity = 6.0f;

                    ++entityCount;
                }
            }

            {
                SceneEntity dirLight = scene.CreateEntity(SceneEntity.Null);
                dirLight.Name = "Sun";

                ref Transform transform = ref dirLight.GetComponent<Transform>();
                transform.Rotation = Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3(1.0f, 2.0f, 1.0f), Vector3.UnitY));

                ref DirectionalLight light = ref dirLight.AddComponent<DirectionalLight>();
                light.Intensity = 0.75f;

                ++entityCount;
            }

            AppLog.Primary.Information("Test scene took {secs}s to generate with {c} entities", Stopwatch.GetElapsedTime(startTime).TotalSeconds, entityCount);
            return scene;
        }
    }
}
