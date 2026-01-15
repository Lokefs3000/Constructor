using Editor.Assets.Types;
using Editor.Components;
using Editor.DearImGui;
using Hexa.NET.ImGui;
using Primary.Scenes;
using System.Numerics;

namespace Editor.Inspector.Components
{
    [CustomComponentInspector(typeof(GeoSceneComponent))]
    internal class GeoSceneEditor : ComponentEditor
    {
        private SceneEntity _entity;

        private object? _temp;

        public override void SetupInspectorFields(SceneEntity entity, Type type)
        {
            _entity = entity;
            _temp = null;
        }

        public override void DrawInspector()
        {
            ref GeoSceneComponent comp = ref _entity.GetComponent<GeoSceneComponent>();

            ImGuiWidgets.SelectorAsset("Scene:", comp.Scene, (x) => _temp = x);

            if (_temp is GeoSceneAsset geoScene)
            {
                comp.Scene = geoScene;
            }

            ImGui.Button("Open editor"u8, new Vector2(-1.0f, 0.0f));
        }
    }
}
