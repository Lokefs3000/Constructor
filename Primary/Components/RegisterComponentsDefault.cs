using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Components
{
    internal static class RegisterComponentsDefault
    {
        public static void RegisterDefault()
        {
            SceneEntityManager.Register<EntityEnabled>();
            SceneEntityManager.Register<EntityName>();
            SceneEntityManager.Register<Transform>();
            SceneEntityManager.Register<LocalTransform>();
            SceneEntityManager.Register<WorldTransform>();
            SceneEntityManager.Register<Camera>();
            SceneEntityManager.Register<CameraProjectionData>();
            SceneEntityManager.Register<MeshRenderer>();
            SceneEntityManager.Register<RenderableAdditionalData>();
            SceneEntityManager.Register<DirectionalLight>();
            SceneEntityManager.Register<Light>();
            SceneEntityManager.Register<LightRenderingData>();

            SceneEntityManager.BuildRequirementHierchies();
        }
    }
}
