using Primary.Assets;
using Primary.Components;
using Primary.Scenes;
using System.Numerics;

namespace Editor.Demos
{
    internal static class StaticDemoScene4
    {
        public static void Load(Editor editor)
        {
            Scene scene = editor.SceneManager.Scenes[0];

            RenderMesh renderMeshe = AssetManager.LoadAsset<ModelAsset>("Engine/Models/Sphere.fbx").GetRenderMesh("Sphere");
            MaterialAsset material = AssetManager.LoadAsset<MaterialAsset>("Content/Demo.mat");

            {
                SceneEntity dir = scene.CreateEntity(SceneEntity.Null);

                ref Transform transform = ref dir.AddComponent<Transform>();
                transform.Rotation = Quaternion.CreateFromYawPitchRoll(float.DegreesToRadians(135.0f), float.DegreesToRadians(45.0f), 0.0f);

                ref Light light = ref dir.AddComponent<Light>();
                light.Brightness = 5.0f;
            }

            {
                SceneEntity fx = scene.CreateEntity(SceneEntity.Null);

                ref PostProcessingVolume volume = ref fx.AddComponent<PostProcessingVolume>();
                volume.Asset = AssetManager.LoadAsset<PostProcessingVolumeAsset>("Content/Volumes/MainVolume.fxvol");
            }

            for (int y = 0; y < 1; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    SceneEntity entity = scene.CreateEntity(SceneEntity.Null);
                    entity.Name = $"{x}x{y}";

                    ref Transform transform = ref entity.AddComponent<Transform>();
                    transform.Position = new Vector3(x, y, 0.0f) * 2.25f;

                    ref MeshRenderer renderer = ref entity.AddComponent<MeshRenderer>();
                    renderer.Mesh = renderMeshe;
                    renderer.Material = material;
                }
            }
        }
    }
}
