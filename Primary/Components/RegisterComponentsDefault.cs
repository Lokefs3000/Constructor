using Primary.Scenes.Components;

namespace Primary.Components
{
    internal static class RegisterComponentsDefault
    {
        public static void RegisterDefault()
        {
            SceneEntityManager.Instance.RegisterComponent<EntityEnabled>();
            SceneEntityManager.Instance.RegisterComponent<EntityName>();
            SceneEntityManager.Instance.RegisterComponent<Transform>();
            SceneEntityManager.Instance.RegisterComponent<LocalTransform>();
            SceneEntityManager.Instance.RegisterComponent<WorldTransform>();
            SceneEntityManager.Instance.RegisterComponent<Camera>();
            SceneEntityManager.Instance.RegisterComponent<CameraProjectionData>();
            SceneEntityManager.Instance.RegisterComponent<MeshRenderer>();
            SceneEntityManager.Instance.RegisterComponent<RenderableAdditionalData>();
            SceneEntityManager.Instance.RegisterComponent<DirectionalLight>();
            SceneEntityManager.Instance.RegisterComponent<Light>();
            SceneEntityManager.Instance.RegisterComponent<LightRenderingData>();
            SceneEntityManager.Instance.RegisterComponent<EntityScene>();
            SceneEntityManager.Instance.RegisterComponent<RenderBounds>();
            SceneEntityManager.Instance.RegisterComponent<DontSerializeTag>();
            SceneEntityManager.Instance.RegisterComponent<PostProcessingVolume>();
            SceneEntityManager.Instance.RegisterComponent<RenderOctantInfo>();

            SceneEntityManager.Instance.RebuildDependencyGraph();
        }
    }
}
