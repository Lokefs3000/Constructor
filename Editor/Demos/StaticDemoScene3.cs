using Arch.Core;
using Primary.Assets;
using Primary.Components;
using Primary.Rendering.Data;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Demos
{
    internal static class StaticDemoScene3
    {
        public static void Load(Editor editor)
        {
            Scene scene = editor.SceneManager.Scenes[0];

            RenderMesh renderMesh = AssetManager.LoadAsset<ModelAsset>("Engine/Models/Cube.fbx").GetRenderMesh("Cube");
            MaterialAsset material = AssetManager.LoadAsset<MaterialAsset>("Content/Textures/Metal055A/Metal055A.mat");

            Quaternion q = Quaternion.CreateFromYawPitchRoll(MathF.PI * 0.25f, MathF.PI * 0.25f, MathF.PI * 0.25f);

            {
                SceneEntity dir = scene.CreateEntity(SceneEntity.Null);

                ref Transform transform = ref dir.AddComponent<Transform>();
                transform.Rotation = Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateLookAt(Vector3.One, Vector3.Zero, Vector3.UnitY));

                ref Light light = ref dir.AddComponent<Light>();
                light.Brightness = 1.25f;
            }

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    for (int z = 0; z < 10; z++)
                    {
                        SceneEntity entity = scene.CreateEntity(SceneEntity.Null);
                        entity.Name = $"{x}x{y}x{z}";

                        ref Transform transform = ref entity.AddComponent<Transform>();
                        transform.Position = new Vector3(x, y, z) * 4.0f;
                        transform.Rotation = q;
                        transform.Scale = new Vector3(0.5f);

                        ref MeshRenderer renderer = ref entity.AddComponent<MeshRenderer>();
                        renderer.Mesh = renderMesh;
                        renderer.Material = material;
                    }
                }
            }
        }
    }
}
