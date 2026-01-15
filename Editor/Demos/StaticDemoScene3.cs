using Primary.Assets;
using Primary.Components;
using Primary.Scenes;
using System.Numerics;

namespace Editor.Demos
{
    internal static class StaticDemoScene3
    {
        public static void Load(Editor editor)
        {
            Scene scene = editor.SceneManager.Scenes[0];

            RenderMesh[] renderMeshes = [
                AssetManager.LoadAsset<ModelAsset>("Engine/Models/Capsule.fbx").GetRenderMesh("Capsule"),
                AssetManager.LoadAsset<ModelAsset>("Engine/Models/Cone.fbx").GetRenderMesh("Cone"),
                AssetManager.LoadAsset<ModelAsset>("Engine/Models/Cube.fbx").GetRenderMesh("Cube"),
                AssetManager.LoadAsset<ModelAsset>("Engine/Models/Cylinder.fbx").GetRenderMesh("Cylinder"),
                AssetManager.LoadAsset<ModelAsset>("Engine/Models/Icosphere.fbx").GetRenderMesh("Icosphere"),
                AssetManager.LoadAsset<ModelAsset>("Engine/Models/Torus.fbx").GetRenderMesh("Torus"),
                ];
            MaterialAsset material = AssetManager.LoadAsset<MaterialAsset>("Content/Textures/Metal055A/Metal055A.mat2");

            Quaternion q = Quaternion.CreateFromYawPitchRoll(MathF.PI * 0.25f, MathF.PI * 0.25f, MathF.PI * 0.25f);

            {
                SceneEntity dir = scene.CreateEntity(SceneEntity.Null);

                ref Transform transform = ref dir.AddComponent<Transform>();
                transform.Rotation = Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateLookAt(Vector3.One, Vector3.Zero, Vector3.UnitY));

                ref Light light = ref dir.AddComponent<Light>();
                light.Brightness = 1.25f;
            }

            {
                SceneEntity cam = scene.CreateEntity(SceneEntity.Null);

                ref Camera camera = ref cam.AddComponent<Camera>();
                ref Transform transform = ref cam.AddComponent<Transform>();

                transform.Position = new Vector3(0.0f, 0.0f, -10.0f);
            }

            const int Size = 5;
            const float Spacing = 5.0f;

            int startIndex = 0; //(int)((uint)Stopwatch.GetTimestamp() % renderMeshes.Length);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        SceneEntity entity = scene.CreateEntity(SceneEntity.Null);
                        entity.Name = $"{x}x{y}x{z}";

                        ref Transform transform = ref entity.AddComponent<Transform>();
                        transform.Position = new Vector3(x, y, z) * Spacing;
                        transform.Rotation = Quaternion.CreateFromYawPitchRoll(x, y, z);
                        transform.Scale = new Vector3(1.0f);

                        ref MeshRenderer renderer = ref entity.AddComponent<MeshRenderer>();
                        renderer.Mesh = renderMeshes[startIndex];
                        renderer.Material = material;

                        startIndex++;
                        if (startIndex >= renderMeshes.Length)
                            startIndex = 0;
                    }
                }
            }
        }
    }
}
