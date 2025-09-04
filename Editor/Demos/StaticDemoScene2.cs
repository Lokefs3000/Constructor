using Primary.Assets;
using Primary.Components;
using Primary.Scenes;
using System.Numerics;

namespace Editor.Demos
{
    internal static class StaticDemoScene2
    {
        public static void Load(Editor editor)
        {
            Scene scene = editor.SceneManager.Scenes[0];

            //camera
            {
                SceneEntity entity = scene.CreateEntity(SceneEntity.Null);
                entity.Name = "MainCamera";

                ref Camera camera = ref entity.AddComponent<Camera>();
                ref Transform transform = ref entity.AddComponent<Transform>();

                transform.Position = new Vector3(0.0f, 6.0f, -15.0f);
                transform.Rotation = Quaternion.CreateFromYawPitchRoll(0.0f, float.DegreesToRadians(20.0f), 0.0f);

                //flashlight
                if (false)
                {
                    SceneEntity flashlight = scene.CreateEntity(SceneEntity.Null);
                    flashlight.Name = "Flashlight";
                    flashlight.Parent = entity;

                    ref Light spotLight = ref flashlight.AddComponent<Light>();

                    spotLight.Type = LightType.SpotLight;
                    spotLight.Brightness = 18.0f;

                    spotLight.InnerCutOff = float.DegreesToRadians(40.0f);
                    spotLight.OuterCutOff = float.DegreesToRadians(45.0f);

                    spotLight.ShadowImportance = ShadowImportance.High;

                    ref Transform t2 = ref flashlight.AddComponent<Transform>();
                }

            }

            ModelAsset? model = AssetManager.LoadAsset<ModelAsset>("Content/Sponza/sponza.obj", true)!;

            PlaceModel(scene, model, "sponza", "roof.mat");
            PlaceModel(scene, model, "sponza.001", "leaf.mat");
            PlaceModel(scene, model, "sponza.002", "Material__57.mat");
            PlaceModel(scene, model, "sponza.003", "vase_round.mat");
            PlaceModel(scene, model, "sponza.004", "Material__298.mat");
            PlaceModel(scene, model, "sponza.005", "bricks.mat");
            PlaceModel(scene, model, "sponza.006", "arch.mat");
            PlaceModel(scene, model, "sponza.007", "ceiling.mat");
            PlaceModel(scene, model, "sponza.008", "column_a.mat");
            PlaceModel(scene, model, "sponza.009", "floor.mat");
            PlaceModel(scene, model, "sponza.010", "column_c.mat");
            PlaceModel(scene, model, "sponza.011", "details.mat");
            PlaceModel(scene, model, "sponza.012", "column_b.mat");
            PlaceModel(scene, model, "sponza.013", "Material__47.mat");
            PlaceModel(scene, model, "sponza.014", "flagpole.mat");
            PlaceModel(scene, model, "sponza.015", "fabric_e.mat");
            PlaceModel(scene, model, "sponza.016", "fabric_d.mat");
            PlaceModel(scene, model, "sponza.017", "fabric_a.mat");
            PlaceModel(scene, model, "sponza.018", "fabric_g.mat");
            PlaceModel(scene, model, "sponza.019", "fabric_c.mat");
            PlaceModel(scene, model, "sponza.020", "fabric_f.mat");
            PlaceModel(scene, model, "sponza.021", "chain.mat");
            PlaceModel(scene, model, "sponza.022", "vase_hanging.mat");
            PlaceModel(scene, model, "sponza.023", "vase.mat");
            PlaceModel(scene, model, "sponza.024", "Material__25.mat");

            PlaceSpotLight(scene, new Vector3(0.0f, 0.5f, -2.0f), new Vector3(0.0f, 25.0f, 0.0f), new Vector3(1.0f, 0.2f, 0.2f));
            PlaceSpotLight(scene, new Vector3(4.0f, 2.0f, -1.5f), new Vector3(20.0f, -50.0f, 0.0f), new Vector3(0.1f, 1.0f, 0.2f));
        }

        private static void PlaceModel(Scene scene, ModelAsset model, string meshName, string materialName)
        {
            MaterialAsset? material = AssetManager.LoadAsset<MaterialAsset>("Content/Sponza/Materials/" + materialName, true);

            SceneEntity body = scene.CreateEntity(SceneEntity.Null);
            body.Name = meshName;

            ref MeshRenderer renderer = ref body.AddComponent<MeshRenderer>();
            renderer.Mesh = model.GetRenderMesh(meshName);
            renderer.Material = material;

            ref Transform transform = ref body.GetComponent<Transform>();
            transform.Scale = new Vector3(0.1f);
        }

        private static void PlaceSpotLight(Scene scene, Vector3 pos, Vector3 euler, Vector3 color)
        {
            SceneEntity body = scene.CreateEntity(SceneEntity.Null);
            body.Name = "SpotLight";

            ref Light light = ref body.AddComponent<Light>();
            light.Type = LightType.SpotLight;
            light.ShadowImportance = ShadowImportance.High;
            light.Diffuse = color;
            light.Specular = color;

            ref Transform transform = ref body.GetComponent<Transform>();
            transform.Position = pos;

            euler = Vector3.DegreesToRadians(euler);
            transform.Rotation = Quaternion.CreateFromYawPitchRoll(euler.Y, euler.X, euler.Z);
        }
    }
}
