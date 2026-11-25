using Editor.Components;
using Primary.Scenes.Components;

namespace Primary.Components
{
    internal static class RegisterComponentsDefault
    {
        public static void RegisterDefault()
        {
            SceneEntityManager.Instance.RegisterComponent<GeoSceneComponent>();
            SceneEntityManager.Instance.RegisterComponent<GeoSceneGeneratedTag>();
            SceneEntityManager.Instance.RegisterComponent<GizmoTriangleComponent>();

            SceneEntityManager.Instance.RebuildDependencyGraph();
        }
    }
}
