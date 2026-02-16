using CommunityToolkit.HighPerformance;
using Editor.Gui;
using Editor.Interaction;
using Editor.Serialization;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.IO;
using Primary.Scenes;
using Primary.Utility;
using System.Diagnostics;
using System.Numerics;

namespace Editor.DearImGui
{
    internal sealed class HierchyView
    {
        private DynamicIconSet _iconSet;

        private int _eyeVisibleId;
        private int _eyeHiddenId;
        private int _objectId;

        private Dictionary<int, SceneRootNode> _rootNodes;

        private HashSet<SceneEntity> _activeEntities;

        private HashSet<SceneEntity> _previouslySelectedEntities;
        private HashSet<SceneEntity> _recentSelectionEntities;

        private List<TempIconDrawData> _tempIconPositions;

        private float _lastFrameLargetX;

        private object? _hoveredObject;
        private SelectionContext _hoveredContext;
        private bool _hoveredState;
        private Scene? _activeScene;

        private SelectingType _currentSelectionType;
        private SelectionContext _currentSelectionContext;
        private int _selectionKeyIndex;
        private bool _selectInputWithinWindow;

        internal HierchyView()
        {
            _iconSet = Editor.GlobalSingleton.GuiAtlasManager.CreateIconSet(
                "Editor/Textures/Icons/EyeVisible.png",
                "Editor/Textures/Icons/EyeHidden.png",
                "Editor/Textures/Icons/Object.png");

            _eyeVisibleId = _iconSet.GetIdForIndex(0);
            _eyeHiddenId = _iconSet.GetIdForIndex(1);
            _objectId = _iconSet.GetIdForIndex(2);

            _rootNodes = new Dictionary<int, SceneRootNode>();

            _activeEntities = new HashSet<SceneEntity>();

            _previouslySelectedEntities = new HashSet<SceneEntity>();
            _recentSelectionEntities = new HashSet<SceneEntity>();

            _tempIconPositions = new List<TempIconDrawData>();

            _hoveredObject = null;

            SelectionManager selection = Editor.GlobalSingleton.SelectionManager;

            selection.Selected += (x) =>
            {
                if (x is SelectedSceneEntity selected)
                {
                    _activeEntities.Add(selected.Entity);
                }
            };

            selection.Deselected += (x) =>
            {
                if (x is SelectedSceneEntity selected)
                {
                    _activeEntities.Remove(selected.Entity);
                }
            };

            SceneManager sceneManager = Editor.GlobalSingleton.SceneManager;
            sceneManager.SceneLoaded += (scene) =>
            {
                Debug.Assert(!_rootNodes.ContainsKey(scene.Id));
                _rootNodes[scene.Id] = new SceneRootNode(scene);
            };
            sceneManager.SceneUnloaded += (scene) =>
            {
                Debug.Assert(_rootNodes.ContainsKey(scene.Id));
                _rootNodes.Remove(scene.Id);
            };

            SceneEntityManager.SceneEntityCreated += (entity) =>
            {
                if (entity.SceneId == int.MinValue || entity.IsSceneRoot)
                    return;

                if (!_rootNodes.TryGetValue(entity.SceneId, out SceneRootNode rootNode))
                    return;
                rootNode.Nodes.Add(new SceneHierchyNode(entity));
            };
            SceneEntityManager.SceneEntityDeleted += (entity) =>
            {
                if (entity.SceneId == int.MinValue || entity.IsSceneRoot)
                    return;

                if (!_rootNodes.TryGetValue(entity.SceneId, out SceneRootNode rootNode))
                    return;
                if (entity.Parent.IsNull)
                    rootNode.Nodes.RemoveWhere((x) => x.Entity == entity);
                else if (FindNodeWithinTree(rootNode, entity.Parent, out SceneHierchyNode hierchyNode))
                    hierchyNode.Nodes.RemoveWhere((x) => x.Entity == entity);
                else
                    EdLog.Gui.Warning("Failed to find entity: {e} within tree", entity);
            };
            SceneEntityManager.SceneEntityParentChanged += (entity, old) =>
            {
                if (entity.SceneId == int.MinValue || entity.IsSceneRoot)
                    return;

                if (!_rootNodes.TryGetValue(entity.SceneId, out SceneRootNode rootNode))
                    return;
                SceneHierchyNode entityNode = default;

                if (old.IsNull || old.IsSceneRoot)
                {
                    int idx = rootNode.Nodes.FindIndex((x) => x.Entity == entity);

                    entityNode = rootNode.Nodes[idx];
                    rootNode.Nodes.RemoveAt(idx);
                }
                else if (FindNodeWithinTree(rootNode, old, out SceneHierchyNode oldParentNode))
                {
                    int idx = oldParentNode.Nodes.FindIndex((x) => x.Entity == entity);

                    entityNode = oldParentNode.Nodes[idx];
                    oldParentNode.Nodes.RemoveAt(idx);
                }

                if (entity.Parent.IsNull)
                    rootNode.Nodes.Add(entityNode);
                else if (entity.Parent.IsSceneRoot)
                    rootNode.Nodes.Add(entityNode);
                else if (FindNodeWithinTree(rootNode, entity.Parent, out SceneHierchyNode parentNode))
                    parentNode.Nodes.Add(entityNode);
                else
                    EdLog.Gui.Warning("Failed to find new parent: {p} for entity: {e}", entity.Parent, entity);
            };
        }

        internal void Render()
        {
            _activeScene = null;
            _tempIconPositions.Clear();

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

            bool windowBeginRet = ImGui.Begin("Hierchy"u8, ImGuiWindowFlags.HorizontalScrollbar);

            ImGui.PopStyleVar();

            if (windowBeginRet)
            {
                if (!ImGui.IsPopupOpen("##HIERCHY_CTX"u8))
                    _hoveredObject = null;

                ImGuiContextPtr context = ImGui.GetCurrentContext();
                Vector2 cursor = ImGui.GetCursorScreenPos();

                float iconWidth = context.FontSize;

                DrawingData data = new DrawingData()
                {
                    DrawList = ImGui.GetWindowDrawList(),
                    Context = context,
                    Cursor = cursor,
                    TreeCursor = cursor + new Vector2(iconWidth * 2.0f + context.Style.FramePadding.X * 3.0f, 0.0f),
                    Avail = ImGui.GetContentRegionAvail(),
                    IconWidth = iconWidth,

                    SelectionMode = _currentSelectionType,
                    SelectionDirection = SelectionDirection.Forwards,
                    SelectionContext = _currentSelectionContext
                };

                data.Avail.X = MathF.Max(ImGui.GetWindowSize().X, data.TreeCursor.X - data.Cursor.X + _lastFrameLargetX);

                bool any = data.Context.IO.MouseDown_0 || data.Context.IO.MouseDown_1 || data.Context.IO.MouseDown_2;
                if (ImGui.IsWindowHovered() && any)
                {
                    _selectInputWithinWindow = true;
                }

                ImGui.Dummy(new Vector2(data.Avail.X, 1.0f));
                _lastFrameLargetX = 0.0f;

                if (_selectInputWithinWindow)
                {
                    if (any)
                    {
                        int idx = data.Context.IO.MouseDown_0 ? 0 : 1;

                        if (Vector2.DistanceSquared(data.Context.IO.MouseClickedPos[idx], data.Context.IO.MousePos) > data.Context.IO.MouseDragThreshold)
                        {
                            if (_currentSelectionType == SelectingType.None)
                            {
                                ResetSelectionData();
                                if (data.Context.IO.KeyCtrl == 0)
                                    SelectionManager.Clear();

                                _currentSelectionType = _hoveredContext switch
                                {
                                    SelectionContext.Node => data.Context.IO.MouseDown_0 ? SelectingType.Select : SelectingType.Deselect,
                                    SelectionContext.Arrow => _hoveredState ? SelectingType.Deselect : SelectingType.Select,
                                    SelectionContext.Enablement => SelectingType.Select,
                                    _ => SelectingType.None
                                };
                                _currentSelectionContext = _hoveredContext;
                                _selectionKeyIndex = idx;
                                _selectInputWithinWindow = true;

                                _previouslySelectedEntities = [.. _activeEntities];
                            }
                        }
                    }
                    else
                    {
                        _currentSelectionType = SelectingType.None;
                        ResetSelectionData();
                    }
                }
                else
                    ResetSelectionData();

                if (_currentSelectionType != SelectingType.None)
                {
                    Vector2 start = data.Context.IO.MouseClickedPos[_selectionKeyIndex];
                    data.SelectionDirection = start.Y > data.Context.IO.MousePos.Y ? SelectionDirection.Backwards : SelectionDirection.Forwards;
                }

                _hoveredContext = SelectionContext.None;

                DrawSideBar(ref data);

                IReadOnlyList<Scene> scenes = Editor.GlobalSingleton.SceneManager.Scenes;
                for (int i = 0; i < scenes.Count; i++)
                {
                    SceneRootNode rootNode = _rootNodes[scenes[i].Id];
                    if (SceneHeaderTreeNode(ref data, rootNode.Scene, rootNode.Scene.Name, rootNode.Scene.Id))
                    {
                        Span<SceneHierchyNode> span = rootNode.Nodes.AsSpan();
                        for (int j = 0; j < span.Length; j++)
                        {
                            DrawTreeNodeForNode(ref data, ref span[j]);
                        }

                        ImGui.TreePop();
                    }
                    PopSceneHeaderData();
                }

                DrawTempIcons(ref data);

                ImGui.Dummy(new Vector2(0.0f, data.TreeCursor.Y - data.Cursor.Y));

                if (data.SelectionMode == SelectingType.None)
                {
                    if (data.Context.IO.MouseClicked_0 && _hoveredObject is not SceneEntity && ImGui.IsWindowHovered())
                    {
                        SelectionManager.Clear();
                    }

                    if (ImGui.BeginPopupContextWindow("##HIERCHY_CTX"u8))
                    {
                        if (_hoveredObject is Scene)
                        {
                            if (ImGui.MenuItem("Save scene"u8)) ;
                            if (ImGui.MenuItem("Save scene as"u8)) ;
                            if (ImGui.MenuItem("Save all"u8)) ;

                            ImGui.Separator();

                            if (ImGui.MenuItem("Unload scene"u8)) ;
                            if (ImGui.MenuItem("Remove scene"u8)) ;
                            if (ImGui.MenuItem("Discard changes"u8)) ;

                            ImGui.Separator();

                            if (ImGui.MenuItem("Select scene asset"u8)) ;
                            if (ImGui.MenuItem("Add new scene"u8)) ;

                            ImGui.Separator();
                        }

                        if (_hoveredObject is SceneEntity hoveredEntity)
                        {
                            if (ImGui.MenuItem("Copy"u8))
                            {
                                string? serialized = EntityQuickSerializer.Serialize(hoveredEntity);
                                if (serialized != null)
                                {
                                    Clipboard.SetText(serialized);
                                }
                            }
                            if (ImGui.MenuItem("Cut"u8))
                            {
                                string? serialized = EntityQuickSerializer.Serialize(hoveredEntity);
                                if (serialized != null)
                                {
                                    Clipboard.SetText(serialized);
                                    //hoveredEntity.Destroy();
                                }
                            }
                            if (ImGui.MenuItem("Paste"u8))
                            {
                                string? serialized = Clipboard.GetText();
                                if (serialized != null)
                                {
                                    SceneEntity newEntity = EntityQuickSerializer.Deserialize(serialized, hoveredEntity.IsNull ? _activeScene! : hoveredEntity.Scene);
                                    if (!newEntity.IsNull)
                                        newEntity.Parent = hoveredEntity;
                                }
                            }

                            ImGui.Separator();

                            if (ImGui.MenuItem("Rename"u8)) ;
                            if (ImGui.MenuItem("Duplicate"u8)) ;
                            if (ImGui.MenuItem("Delete"u8)) ;

                            ImGui.Separator();
                        }

                        if (_activeScene != null)
                        {
                            if (ImGui.MenuItem("Create empty"u8))
                            {
                                if (_hoveredObject is SceneEntity entity)
                                    _activeScene.CreateEntity(entity);
                                else
                                    _activeScene.CreateEntity(SceneEntity.Null);
                            }
                        }

                        ImGui.EndPopup();
                    }
                }
            }
            else
            {
                _hoveredObject = null;
                ResetSelectionData();
            }

            ImGui.End();

            void ResetSelectionData()
            {
                _recentSelectionEntities.Clear();
                _currentSelectionType = SelectingType.None;
                _currentSelectionContext = SelectionContext.None;
                _selectionKeyIndex = 0;
                _selectInputWithinWindow = false;
            }
        }

        /// <summary>Not thread-safe</summary>
        private bool FindNodeWithinTree(SceneRootNode rootNode, SceneEntity entity, out SceneHierchyNode hierchyNode)
        {
            SceneEntity parent = entity.Parent;
            if (!parent.IsNull && !parent.IsSceneRoot)
            {
                if (FindNodeWithinTree(rootNode, parent, out SceneHierchyNode parentNode))
                {
                    int idx = parentNode.Nodes.FindIndex((x) => x.Entity == entity);
                    if (idx != -1)
                    {
                        hierchyNode = parentNode.Nodes[idx];
                        return true;
                    }
                }
            }
            else
            {
                int idx = rootNode.Nodes.FindIndex((x) => x.Entity == entity);
                if (idx != -1)
                {
                    hierchyNode = rootNode.Nodes[idx];
                    return true;
                }
            }

            hierchyNode = default;
            return false;
        }

        private void DrawTreeNodeForNode(ref DrawingData data, ref SceneHierchyNode hierchyNode, int nodeDepth = 0)
        {
            SceneEntity localEntity = hierchyNode.Entity;
            if (EntityTreeNode(ref data, localEntity, nodeDepth, localEntity.Name, localEntity.Enabled, localEntity.GetHashCode(), localEntity.Children.IsEmpty))
            {
                nodeDepth++;

                Span<SceneHierchyNode> childNodes = hierchyNode.Nodes.AsSpan();
                for (int i = 0; i < childNodes.Length; i++)
                {
                    DrawTreeNodeForNode(ref data, ref childNodes[i], nodeDepth);
                }

                ImGui.TreePop();
            }
        }

        #region Widgets
        private void DrawSideBar(ref DrawingData data)
        {
            data.DrawList.AddRectFilled(data.Cursor, new Vector2(data.TreeCursor.X - data.Context.Style.FramePadding.X, data.Avail.Y), new Color32(data.Context.Style.Colors[(int)ImGuiCol.ScrollbarBg]).ABGR);
        }

        private bool SceneHeaderTreeNode(ref DrawingData data, Scene scene, string sceneName, int sceneId)
        {
            ImGui.PushID(sceneId);

            uint arrowId = ImGui.GetID(1);
            uint id = ImGui.GetID(2);

            ImRect arrowBounds = new ImRect(data.TreeCursor, data.TreeCursor + new Vector2(data.IconWidth) + data.Context.Style.FramePadding * 2.0f);
            ImRect bounds = new ImRect(new Vector2(data.Cursor.X, data.TreeCursor.Y), new Vector2(data.Cursor.X, data.TreeCursor.Y) + new Vector2(data.Avail.X, data.IconWidth + data.Context.Style.FramePadding.Y * 2.0f));

            bool opened = ImGuiP.TreeNodeGetOpen(id);

            bool arrowHovered = false, arrowHeld = false;
            if (ImGuiP.ButtonBehavior(arrowBounds, arrowId, ref arrowHovered, ref arrowHeld))
                ImGuiP.TreeNodeSetOpen(id, !opened);

            bool hovered = false, held = false;
            if (ImGuiP.ButtonBehavior(bounds, id, ref hovered, ref held))
                ;

            if (data.Context.IO.MousePos.Y >= data.TreeCursor.Y)
                _activeScene = scene;
            if (hovered)
                _hoveredObject = scene;

            ImGuiP.ItemAdd(bounds, id);
            ImGuiP.ItemAdd(arrowBounds, arrowId);

            if (held || hovered)
                data.DrawList.AddRectFilled(bounds.Min, bounds.Max, new Color32(data.Context.Style.Colors[(int)(held ? ImGuiCol.ButtonActive : (hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button))]).ABGR);
            data.DrawList.AddRectFilled(bounds.Min, bounds.Max, new Color32(data.Context.Style.Colors[(int)ImGuiCol.Header]).ABGR);
            data.DrawList.AddText(new Vector2(arrowBounds.Max.X, arrowBounds.Min.Y) + data.Context.Style.FramePadding, 0xffffffff, sceneName);

            if (arrowHeld || arrowHovered)
                data.DrawList.AddRectFilled(arrowBounds.Min, arrowBounds.Max, new Color32(data.Context.Style.Colors[(int)(arrowHeld ? ImGuiCol.ButtonActive : (arrowHovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button))]).ABGR);
            ImGuiP.RenderArrow(data.DrawList, data.TreeCursor + data.Context.Style.FramePadding, 0xffffffff, opened ? ImGuiDir.Down : ImGuiDir.Right);

            if (opened)
            {
                ImGui.TreePush(sceneName);
            }

            Vector2 treeCursor = data.TreeCursor;
            treeCursor.Y += data.IconWidth + data.Context.Style.FramePadding.Y * 2.0f;
            data.TreeCursor = treeCursor;

            return opened;
        }

        private void PopSceneHeaderData() => ImGui.PopID();

        private bool EntityTreeNode(ref DrawingData data, SceneEntity entity, int treeDepth, string entityName, bool entityEnabled, int entityId, bool childless)
        {
            Vector2 baseCursor = data.TreeCursor + new Vector2(data.IconWidth * treeDepth, 0.0f);
            Vector2 textRegion = ImGui.CalcTextSize(entityName);

            float containedLargestX = data.IconWidth * treeDepth + textRegion.X + data.IconWidth + data.Context.Style.FramePadding.X * 4.0f;
            _lastFrameLargetX = MathF.Max(_lastFrameLargetX, containedLargestX);

            ImGui.PushID(entityId);

            uint arrowId = childless ? 0 : ImGui.GetID(1);
            uint id = ImGui.GetID(2);

            ImRect arrowBounds = new ImRect(baseCursor, baseCursor + new Vector2(data.IconWidth) + data.Context.Style.FramePadding * 2.0f);
            ImRect bounds = new ImRect(baseCursor + new Vector2(data.IconWidth + data.Context.Style.FramePadding.X * 2.0f, 0.0f), new Vector2(data.Avail.X, arrowBounds.Max.Y));

            bool opened = ImGuiP.TreeNodeGetOpen(id);

            bool arrowHovered = false, arrowHeld = false;
            if (!childless)
            {
                if (_currentSelectionType == SelectingType.None && ImGuiP.ButtonBehavior(arrowBounds, arrowId, ref arrowHovered, ref arrowHeld))
                    ImGuiP.TreeNodeSetOpen(id, !opened);
            }

            bool hovered = false, held = false;
            if (_currentSelectionType == SelectingType.None && ImGuiP.ButtonBehavior(bounds, id, ref hovered, ref held))
            {
                if (data.Context.IO.KeyCtrl > 0)
                {
                    if (_activeEntities.Contains(entity))
                        SelectionManager.Deselect<SelectedSceneEntity>((x) => x.Entity == entity);
                    else
                        SelectionManager.Select(new SelectedSceneEntity { Entity = entity }, SelectionMode.Multi);
                }
                else
                    SelectionManager.Select(new SelectedSceneEntity { Entity = entity });
            }

            if (arrowHovered)
            {
                _hoveredContext = SelectionContext.Arrow;
                _hoveredState = opened;
            }
            else if (hovered)
                _hoveredContext = SelectionContext.Node;

            if (hovered || held)
            {
                _hoveredObject = entity;
            }

            bool isWithinQueue = false;
            if (_currentSelectionType != SelectingType.None)
            {
                switch (data.SelectionDirection)
                {
                    case SelectionDirection.Forwards:
                        {
                            if (!(bounds.Max.Y < data.Context.IO.MouseClickedPos[_selectionKeyIndex].Y) &&
                                bounds.Min.Y < data.Context.IO.MousePos.Y)
                            {
                                isWithinQueue = true;
                            }

                            break;
                        }
                    case SelectionDirection.Backwards:
                        {
                            if (bounds.Min.Y < data.Context.IO.MouseClickedPos[_selectionKeyIndex].Y &&
                                bounds.Max.Y > data.Context.IO.MousePos.Y)
                            {
                                isWithinQueue = true;
                            }

                            break;
                        }
                }

                if (isWithinQueue && data.SelectionContext == SelectionContext.Arrow && !_recentSelectionEntities.Contains(entity))
                {
                    ImGuiP.TreeNodeSetOpen(id, data.SelectionMode == SelectingType.Select);
                    _recentSelectionEntities.Add(entity);
                }

                if (data.SelectionContext == SelectionContext.Node)
                {
                    if (isWithinQueue)
                    {
                        if (!_recentSelectionEntities.Contains(entity))
                        {
                            if (_currentSelectionType == SelectingType.Deselect)
                            {
                                if (_previouslySelectedEntities.Contains(entity))
                                {
                                    SelectionManager.Deselect<SelectedSceneEntity>((x) => x.Entity == entity);
                                    _recentSelectionEntities.Add(entity);
                                }
                            }
                            else if (!_activeEntities.Contains(entity))
                            {
                                if (!_previouslySelectedEntities.Contains(entity))
                                {
                                    SelectionManager.Select(new SelectedSceneEntity { Entity = entity }, SelectionMode.Multi);
                                    _recentSelectionEntities.Add(entity);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (_recentSelectionEntities.Contains(entity))
                        {
                            if (_previouslySelectedEntities.Contains(entity))
                            {
                                if (!_activeEntities.Contains(entity))
                                    SelectionManager.Select(new SelectedSceneEntity { Entity = entity }, SelectionMode.Multi);
                            }
                            else
                            {
                                if (_activeEntities.Contains(entity))
                                    SelectionManager.Deselect<SelectedSceneEntity>((x) => x.Entity == entity);
                            }

                            _recentSelectionEntities.Remove(entity);
                        }
                    }
                }
            }

            ImGuiP.ItemAdd(arrowBounds, arrowId);
            ImGuiP.ItemAdd(bounds, id);

            {
                uint visIconId = ImGui.GetID(10);

                Vector2 baseIconCursor = new Vector2(data.Cursor.X, data.TreeCursor.Y) + data.Context.Style.FramePadding;
                ImRect visIconBb = new ImRect(baseIconCursor, baseIconCursor + new Vector2(data.IconWidth) + data.Context.Style.FramePadding);

                bool visIconHovered = false, visIconHeld = false;
                if (ImGuiP.ButtonBehavior(visIconBb, visIconId, ref visIconHovered, ref visIconHeld))
                    entity.Enabled = !entity.Enabled;

                if (visIconHovered)
                    _hoveredContext = SelectionContext.Enablement;

                if (isWithinQueue && data.SelectionContext == SelectionContext.Enablement && !_recentSelectionEntities.Contains(entity))
                {
                    entity.Enabled = !entity.Enabled;
                    _recentSelectionEntities.Add(entity);
                }

                uint col = visIconHeld ? 0xffb0b0b0 : (visIconHovered ? 0xffffffff : 0xffd0d0d0);
                if (!entity.Enabled)
                    col = (col & 0x00ffffff) | 0x80000000;

                _tempIconPositions.Add(new TempIconDrawData(visIconBb, entity.Enabled ? _eyeVisibleId : _eyeHiddenId, col));

                ImGuiP.ItemAdd(visIconBb, visIconId);
            }

            ImGui.PopID();

            if (!hovered)
                hovered = _activeEntities.Contains(entity);

            hovered = hovered || (!arrowHovered && new Boundaries(new Vector2(data.Cursor.X, bounds.Min.Y), bounds.Max).IsWithin(data.Context.IO.MousePos));
            if (hovered || held)
                data.DrawList.AddRectFilled(bounds.Min, bounds.Max, new Color32(data.Context.Style.Colors[(int)(held ? ImGuiCol.ButtonActive : (hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button))]).ABGR);
            data.DrawList.AddText(baseCursor + data.Context.Style.FramePadding + new Vector2(data.IconWidth + data.Context.Style.FramePadding.X * 2.0f, 0.0f), 0xffffffff, entityName);

            if (!childless)
            {
                if (arrowHovered || arrowHeld)
                    data.DrawList.AddRectFilled(arrowBounds.Min, arrowBounds.Max, new Color32(data.Context.Style.Colors[(int)(arrowHeld ? ImGuiCol.ButtonActive : (arrowHovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button))]).ABGR);
                ImGuiP.RenderArrow(data.DrawList, baseCursor + data.Context.Style.FramePadding, 0xffffffff, opened ? ImGuiDir.Down : ImGuiDir.Right);
            }

            if (_currentSelectionType != SelectingType.None)
            {
                if (_recentSelectionEntities.Contains(entity))
                {
                    float sizeX = ImGui.GetWindowSize().X;

                    if (_currentSelectionType == SelectingType.Select)
                        data.DrawList.AddRectFilled(new Vector2(sizeX - 4.0f, bounds.Min.Y), new Vector2(sizeX, bounds.Max.Y), 0xffb03015);
                    else
                        data.DrawList.AddRectFilled(new Vector2(sizeX - 4.0f, bounds.Min.Y), new Vector2(sizeX, bounds.Max.Y), 0xff30a0ef);

                }
            }

            if (!childless && opened)
            {
                unsafe
                {
                    ImGui.TreePush((void*)&entityId);
                }
            }
            else
                opened = false;

            Vector2 treeCursor = data.TreeCursor;
            treeCursor.Y += data.IconWidth + data.Context.Style.FramePadding.Y * 2.0f;
            data.TreeCursor = treeCursor;

            return opened;
        }

        private void PopEntityData() => ImGui.TreePop();

        private void DrawTempIcons(ref DrawingData data)
        {
            if (_iconSet.AtlasTexture == null)
                return;
            //ImTextureRef textureRef = ImGuiUtility.GetTextureRef(_iconSet.AtlasTexture.Handle);
            //
            //Span<TempIconDrawData> span = _tempIconPositions.AsSpan();
            //for (int i = 0; i < span.Length; i++)
            //{
            //    ref TempIconDrawData icon = ref span[i];
            //    if (_iconSet.TryGetAtlasIcon(icon.IconId, out DynAtlasIcon atlasIcon))
            //    {
            //        data.DrawList.AddImage(textureRef, icon.Boundaries.Min, icon.Boundaries.Max, atlasIcon.UVs.Minimum, atlasIcon.UVs.Maximum, icon.Color);
            //    }
            //}
        }
        #endregion

        private ref struct DrawingData
        {
            public ImDrawListPtr DrawList;
            public ImGuiContextPtr Context;
            public Vector2 Cursor;
            public Vector2 TreeCursor;
            public Vector2 Avail;
            public float IconWidth;

            public SelectingType SelectionMode;
            public SelectionDirection SelectionDirection;
            public SelectionContext SelectionContext;
        };

        private enum SelectingType : byte
        {
            None = 0,
            Select,
            Deselect
        }

        private enum SelectionContext : byte
        {
            None = 0,

            Node,
            Arrow,
            Enablement
        }

        private enum SelectionDirection : byte
        {
            Forwards,
            Backwards
        }

        private readonly struct SceneRootNode
        {
            public readonly Scene Scene;
            public readonly List<SceneHierchyNode> Nodes;

            public SceneRootNode(Scene scene)
            {
                Scene = scene;
                Nodes = new List<SceneHierchyNode>();
            }
        }

        private readonly struct SceneHierchyNode
        {
            public readonly SceneEntity Entity;
            public readonly List<SceneHierchyNode> Nodes;

            public SceneHierchyNode(SceneEntity entity)
            {
                Entity = entity;
                Nodes = new List<SceneHierchyNode>();
            }
        }

        private readonly record struct TempIconDrawData(ImRect Boundaries, int IconId, uint Color);
    }
}
