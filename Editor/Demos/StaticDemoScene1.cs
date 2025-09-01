using Arch.Core;
using Primary.Assets;
using Primary.Components;
using Primary.Scenes;
using System.Numerics;

namespace Editor.Demos
{
    internal static class StaticDemoScene1
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

                //box
                if (false)
                {
                    SceneEntity child = scene.CreateEntity(SceneEntity.Null);
                    child.Name = "Box1";
                    child.Parent = entity;

                    ref Transform t2 = ref child.AddComponent<Transform>();
                    t2.Position = new Vector3(0.0f, 0.0f, 2.0f);
                    t2.Scale = new Vector3(0.2f);

                    ref MeshRenderer meshRenderer = ref child.AddComponent<MeshRenderer>();
                    meshRenderer.Mesh = AssetManager.LoadAsset<ModelAsset>("Content/cube.emdf", true)!.GetRenderMesh("Cube.001");
                    meshRenderer.Material = AssetManager.LoadAsset<MaterialAsset>("Content/demo/plain.mat", true)!;
                }
            }

            //shadow testing
            if (false)
            {
                SceneEntity flashlight = scene.CreateEntity(SceneEntity.Null);
                flashlight.Name = "ShadowPointTest";

                ref Light spotLight = ref flashlight.AddComponent<Light>();
                spotLight.Type = LightType.PointLight;
                spotLight.Brightness = 20.0f;

                spotLight.ShadowImportance = ShadowImportance.Medium;

                ref Transform t2 = ref flashlight.AddComponent<Transform>();
                t2.Position = new Vector3(3.0f, 3.0f, 2.5f);
                t2.Rotation = Quaternion.CreateFromYawPitchRoll(0.0f, MathF.PI * 0.5f, 0.0f);

                if (false)
                {
                    SpawnBox(Vector3.UnitX, new Vector3(0.0f, -90.0f, 0.0f));
                    SpawnBox(Vector3.UnitY, new Vector3(90.0f, 0.0f, 0.0f));
                    SpawnBox(Vector3.UnitZ, new Vector3(0.0f, 180.0f, 0.0f));
                    SpawnBox(-Vector3.UnitX, new Vector3(0.0f, 90.0f, 0.0f));
                    SpawnBox(-Vector3.UnitY, new Vector3(-90.0f, 0.0f, 0.0f));
                    SpawnBox(-Vector3.UnitZ, Vector3.Zero);

                    void SpawnBox(Vector3 dir, Vector3 rot, bool cube = true)
                    {
                        if (cube)
                        {
                            SceneEntity child = scene.CreateEntity(SceneEntity.Null);
                            child.Name = "Box" + dir.ToString();

                            ref Transform t2 = ref child.AddComponent<Transform>();
                            t2.Position = dir * 2.5f;
                            t2.Scale = new Vector3(0.5f);

                            ref MeshRenderer meshRenderer = ref child.AddComponent<MeshRenderer>();
                            meshRenderer.Mesh = AssetManager.LoadAsset<ModelAsset>("Content/cube.emdf", true)!.GetRenderMesh("Cube.001");
                            meshRenderer.Material = AssetManager.LoadAsset<MaterialAsset>("Content/demo/plain.mat", true)!;
                        }

                        {
                            SceneEntity child = scene.CreateEntity(SceneEntity.Null);
                            child.Name = "Plane" + dir.ToString();

                            ref Transform t2 = ref child.AddComponent<Transform>();
                            t2.Position = dir * 5.0f;
                            t2.Rotation = Quaternion.CreateFromYawPitchRoll(float.DegreesToRadians(rot.Y), float.DegreesToRadians(rot.X), float.DegreesToRadians(rot.Z));
                            t2.Scale = new Vector3(5.0f);

                            ref MeshRenderer meshRenderer = ref child.AddComponent<MeshRenderer>();
                            meshRenderer.Mesh = AssetManager.LoadAsset<ModelAsset>("Content/plane.emdf", true)!.GetRenderMesh("Plane");
                            meshRenderer.Material = AssetManager.LoadAsset<MaterialAsset>("Content/demo/plain.mat", true)!;
                        }
                    }

                    return;
                }
            }

            {
                ModelAsset model = AssetManager.LoadAsset<ModelAsset>("Content/demo/hallway_t.emdf", true)!;
                ModelAsset model2 = AssetManager.LoadAsset<ModelAsset>("Content/demo/room1.emdf", true)!;

                MaterialAsset material1 = AssetManager.LoadAsset<MaterialAsset>("Content/demo/plain.mat", true)!;

                //body
                {
                    SceneEntity entity = scene.CreateEntity(SceneEntity.Null);
                    entity.Name = "HallwayT";

                    ref MeshRenderer renderer = ref entity.AddComponent<MeshRenderer>();
                    renderer.Mesh = model.GetRenderMesh("Body");
                    renderer.Material = material1;

                    ref Transform transform = ref entity.AddComponent<Transform>();
                    transform.Position = new Vector3(0.0f, 0.0f, 3.0f);
                    transform.Rotation = Quaternion.CreateFromYawPitchRoll(MathF.PI * -0.5f, 0.0f, 0.0f);
                }

                //room1
                {
                    SceneEntity entity = scene.CreateEntity(SceneEntity.Null);
                    entity.Name = "Room1";

                    ref MeshRenderer renderer = ref entity.AddComponent<MeshRenderer>();
                    renderer.Mesh = model2.GetRenderMesh("Plane");
                    renderer.Material = material1;

                    ref Transform transform = ref entity.AddComponent<Transform>();
                    transform.Position = new Vector3(3.0f * 4.0f + 2.5f, 0.0f, 2.0f * 3.0f);
                    transform.Rotation = Quaternion.CreateFromYawPitchRoll(MathF.PI * 0.5f, 0.0f, 0.0f);
                }
            }

            //wall light
            if (true)
            {
                SceneEntity flashlight = scene.CreateEntity(SceneEntity.Null);
                flashlight.Name = "FloorLight";

                ref Light spotLight = ref flashlight.AddComponent<Light>();
                spotLight.Type = LightType.SpotLight;
                spotLight.Brightness = 28.0f;

                spotLight.InnerCutOff = float.DegreesToRadians(40.0f);
                spotLight.OuterCutOff = float.DegreesToRadians(45.0f);

                spotLight.ShadowImportance = ShadowImportance.Medium;

                ref Transform t2 = ref flashlight.AddComponent<Transform>();
                t2.Position = new Vector3(0.0f, 4.2f, 4.25f);
                t2.Rotation = Quaternion.CreateFromYawPitchRoll(0.0f, MathF.PI * 0.5f, 0.0f);
            }

            PlaceHallways(scene, Vector3.Zero, -Vector3.UnitZ, 7);
            PlaceHallways(scene, new Vector3(-3.0f, 0.0f, 3.0f), -Vector3.UnitX, 3);
            PlaceHallways(scene, new Vector3(3.0f, 0.0f, 3.0f), Vector3.UnitX, 4);
        }

        private static void PlaceHallways(Scene scene, Vector3 position, Vector3 direction, int count)
        {
            ModelAsset model = AssetManager.LoadAsset<ModelAsset>("Content/demo/hallway.emdf", true)!;

            MaterialAsset material1 = AssetManager.LoadAsset<MaterialAsset>("Content/demo/plain.mat", true)!;
            MaterialAsset material2 = AssetManager.LoadAsset<MaterialAsset>("Content/demo/rust/rust.mat", true)!;

            Quaternion q = Quaternion.Concatenate(
                Quaternion.CreateFromYawPitchRoll(MathF.PI * 0.5f, 0.0f, 0.0f),
                Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateLookTo(Vector3.Zero, direction, Vector3.UnitY)));

            for (int i = 0; i < count; i++)
            {
                SceneEntity body = scene.CreateEntity(SceneEntity.Null);

                //body
                {
                    body.Name = "Hallway";

                    ref MeshRenderer renderer = ref body.AddComponent<MeshRenderer>();
                    renderer.Mesh = model.GetRenderMesh("Body.006");
                    renderer.Material = material1;

                    ref Transform transform = ref body.AddComponent<Transform>();

                    transform.Position = position;
                    transform.Rotation = q;
                }

                //arch
                {
                    SceneEntity entity = scene.CreateEntity(SceneEntity.Null);
                    entity.Name = "HallwayArch";
                    entity.Parent = body;

                    ref MeshRenderer renderer = ref entity.AddComponent<MeshRenderer>();
                    renderer.Mesh = model.GetRenderMesh("Arch.001");
                    renderer.Material = material2;

                    ref Transform transform = ref entity.AddComponent<Transform>();
                }

                if (i % 3 == 0)
                {
                    //light
                    {
                        SceneEntity entity = scene.CreateEntity(SceneEntity.Null);
                        entity.Name = "Light";
                        entity.Parent = body;

                        ref Light light = ref entity.AddComponent<Light>();
                        light.Type = LightType.PointLight;
                        light.Brightness = 8.0f;
                        light.Diffuse = new Vector3(1.0f, 0.5f, 0.2f);
                        light.Specular = light.Diffuse;

                        light.ShadowImportance = ShadowImportance.Low;

                        ref Transform transform = ref entity.AddComponent<Transform>();
                        transform.Position = new Vector3(0.0f, 4.2f, 0.0f);
                    }
                }

                position += direction * 3.0f;
            }
        }
    }
}
