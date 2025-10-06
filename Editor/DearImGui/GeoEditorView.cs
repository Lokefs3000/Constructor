using CommunityToolkit.HighPerformance;
using Editor.Assets.Types;
using Editor.Components;
using Editor.GeoEdit;
using Editor.Interaction;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Scenes;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Editor.DearImGui
{
    internal class GeoEditorView
    {
        private ActiveTool _tool;
        private PlaceShape _shape;

        private IGeoTool[] _tools;

        private List<GeoSceneSelectionData> _selection;

        private GeoSceneAsset? _activeScene;
        private int _activeSceneLocks;

        private GeoPickResult? _lastPickResult;

        internal GeoEditorView()
        {
            _tool = unchecked((ActiveTool)(-1));
            _shape = PlaceShape.Box;

            _tools = [
                new PlaceTool(this)
                ];

            _selection = new List<GeoSceneSelectionData>();

            _activeScene = null;
            _activeSceneLocks = 0;

            _lastPickResult = null;

            SelectionManager.NewSelected += (@base) =>
            {
                if (@base is SelectedSceneEntity entity)
                {
                    ref GeoSceneComponent component = ref entity.Entity.GetComponent<GeoSceneComponent>();
                    if (!Unsafe.IsNullRef(ref component))
                    {
                        _selection.Add(new GeoSceneSelectionData(entity.Entity, component.Scene));
                    }
                }
            };

            SelectionManager.OldDeselected += (@base) =>
            {
                if (@base is SelectedSceneEntity entity)
                {
                    int idx = _selection.FindIndex((x) => x.Entity == entity.Entity);
                    if (idx != -1)
                    {
                        _selection.RemoveAt(idx);
                    }
                }
            };

            SceneEntityManager.AddComponentAddedCallback<GeoSceneComponent>((entity) =>
            {
                if (SelectionManager.IsSelected<SelectedSceneEntity>((x) => x.Entity == entity))
                {
                    ref GeoSceneComponent component = ref entity.GetComponent<GeoSceneComponent>();

                    int idx = _selection.FindIndex((x) => x.Entity == entity);
                    if (idx != -1)
                        _selection[idx] = new GeoSceneSelectionData(entity, component.Scene);
                    else
                        _selection.Add(new GeoSceneSelectionData(entity, component.Scene));
                }
            });

            SceneEntityManager.AddComponentRemovedCallback<GeoSceneComponent>((entity) =>
            {
                int idx = _selection.FindIndex((x) => x.Entity == entity);
                if (idx != -1)
                {
                    _selection.RemoveAt(idx);
                }
            });

            ChangeCurrentTool(ActiveTool.Place);
        }

        internal void Render()
        {
            if (_activeSceneLocks == 0)
                _activeScene = null;
            _lastPickResult = null;

            if (ImGui.Begin("Geo viewer"u8))
            {
                if (ImGui.IsWindowFocused())
                {
                    ToolManager tools = Editor.GlobalSingleton.ToolManager;
                    if (tools.ActiveControlToolType != EditorControlTool.GeoEdit)
                    {
                        tools.SwitchControl(EditorControlTool.GeoEdit);
                    }
                }

                Span<GeoSceneSelectionData> selections = _selection.AsSpan();

                if (ImGui.BeginTabBar("##ROOT"))
                {
                    for (int i = 0; i < selections.Length; i++)
                    {
                        ref GeoSceneSelectionData activeScene = ref selections[i];

                        {
                            ref GeoSceneComponent comp = ref activeScene.Entity.GetComponent<GeoSceneComponent>();
                            if (Unsafe.IsNullRef(ref comp))
                            {
                                if (activeScene.Scene != null)
                                    activeScene = new GeoSceneSelectionData(activeScene.Entity, comp.Scene);
                            }
                            else if (activeScene.Scene != comp.Scene)
                                activeScene = new GeoSceneSelectionData(activeScene.Entity, comp.Scene);
                        }

                        if (activeScene.Scene != null)
                        {
                            if (ImGui.BeginTabItem(activeScene.Scene.Name, (_activeSceneLocks > 0 && _activeScene == activeScene.Scene) ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
                            {
                                _activeScene = activeScene.Scene;
                                if (GeoPicker.Pick(Editor.GlobalSingleton.SceneView.CameraMouseRay, activeScene.Scene, true, out GeoPickResult tmp))
                                {
                                    _lastPickResult = tmp;

                                    if (ToolManager.IsSnappingActive)
                                    {
                                        tmp = new GeoPickResult(Vector3.Round(tmp.Position / ToolManager.SnapScale) * ToolManager.SnapScale, tmp.Normal, tmp.HitBrush, tmp.FaceIndex);
                                    }
                                }

                                ImGui.EndTabItem();
                            }
                        }
                    }

                    ImGui.EndTabBar();
                }
            }
            ImGui.End();

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 3.0f);

            bool windowRet = ImGui.Begin("##Geo edit"u8, ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize);

            if (windowRet)
            {
                if (CustomButton("PLCE"u8, new Vector2(32.0f), _tool == ActiveTool.Place)) ChangeCurrentTool(ActiveTool.Place);
                if (CustomButton("EDIT"u8, new Vector2(32.0f), _tool == ActiveTool.Edit)) ChangeCurrentTool(ActiveTool.Edit);
                if (CustomButton("SURF"u8, new Vector2(32.0f), _tool == ActiveTool.Surface)) ChangeCurrentTool(ActiveTool.Surface);
            }
            ImGui.End();

            if (_tool == ActiveTool.Place)
            {
                if (ImGui.Begin("##Geo place"u8, ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
                {
                    if (CustomButton("Box"u8, new Vector2(32.0f), _shape == PlaceShape.Box)) _shape = PlaceShape.Box;
                }
                ImGui.End();
            }

            ImGui.PopStyleVar(2);

            if ((int)_tool < _tools.Length)
                _tools[(int)_tool].Update();
        }

        private void ChangeCurrentTool(ActiveTool tool)
        {
            if (_tool != tool)
            {
                if ((int)_tool < _tools.Length)
                    _tools[(int)_tool].DisconnectEvents();

                _tools[(int)tool].ConnectEvents();
                _tool = tool;
            }
        }

        private static bool CustomButton(ReadOnlySpan<byte> label, Vector2 size, bool selected)
        {
            ImGuiContextPtr context = ImGui.GetCurrentContext();
            ref ImGuiWindowPtr window = ref context.CurrentWindow;

            uint id = ImGui.GetID(label);
            ImRect bb = new ImRect(window.DC.CursorPos, window.DC.CursorPos + size);

            bool hovered = false, held = false;
            bool pressed = ImGuiP.ButtonBehavior(bb, id, ref hovered, ref held);

            ImGuiP.ItemAdd(bb, id);
            ImGuiP.ItemSize(bb);

            window.DrawList.AddRectFilled(bb.Min, bb.Max, new Color32(context.Style.Colors[(int)(held ? ImGuiCol.ButtonActive : (hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button))]).ABGR, context.Style.FrameRounding);
            window.DrawList.AddRect(bb.Min, bb.Max, selected ? 0xff2070a0 : (new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR), context.Style.FrameRounding);
            window.DrawList.AddText(bb.Min + context.Style.FramePadding, 0xff0000ff, label);

            return pressed;
        }

        internal void LockCurrentScene() => _activeSceneLocks++;
        internal void UnlockCurrentScene() => _activeSceneLocks = Math.Max(_activeSceneLocks - 1, 0);

        internal GeoSceneAsset? ActiveScene => _activeScene;

        internal GeoPickResult? LastPickResult => _lastPickResult;

        internal ActiveTool Tool => _tool;
        internal PlaceShape Shape => _shape;

        internal IGeoTool? CurrentTool => (int)_tool < _tools.Length ? _tools[(int)_tool] : null;

        internal enum ActiveTool : byte
        {
            Place = 0,
            Edit,
            Surface
        }

        internal enum PlaceShape : byte
        {
            Box = 0
        }

        private enum ActiveEventListeners : byte
        {
            None = 0,

            PlaceEvents = 1 << 0
        }
    }

    internal readonly record struct GeoSceneSelectionData
    {
        public readonly SceneEntity Entity;
        public readonly GeoSceneAsset? Scene;

        public GeoSceneSelectionData(SceneEntity entity, GeoSceneAsset? scene)
        {
            Entity = entity;
            Scene = scene;
        }
    }
}
