using Editor.Assets;
using Editor.Assets.Types;
using Editor.Geo.Menu;
using Editor.Geo.Selection;
using Editor.Geo.UI;
using Editor.Geometry;
using Editor.Geometry.Mesh;
using Editor.Geometry.Serialization;
using Editor.Gui.View;
using Editor.Gui.Windows;
using Editor.Interaction;
using Editor.Rendering;
using Editor.UI;
using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Elements.Tree;
using Editor.UI.Menu;
using Primary.Assets;
using Primary.Common;
using Primary.Components;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

namespace Editor.Geo
{
    public sealed class GeoHierchyWindow : UIWindow
    {
        private readonly UIFontAsset _mainFont;

        private readonly ContextMenuAsset _groupCtxMenu;
        private readonly ContextMenuAsset _brushCtxMenu;

        private readonly GeoSelectionGroup _selectionGroup;

        private UITreeView? _hierchyView;

        private UIElement? _contentElement;
        private UILabel? _placeholderText;

        private UIButton? _saveButton;

        private SceneEntity _currentEntity;
        private GeoSceneAsset? _sceneAsset;
        private GeoScene? _geoScene;

        private Dictionary<BrushGroup, TreeNode> _groupNodes;
        private Dictionary<Brush, TreeNode> _brushNodes;

        private FaceViewSnippet _faceViewSnippet;

        public GeoHierchyWindow(int uniqueWindowId) : base(uniqueWindowId)
        {
            _mainFont = AssetManager.LoadAsset<UIFontAsset>("Editor/Fonts/Inter.uifont").WaitIfNotLoaded();

            _groupCtxMenu = AssetManager.LoadAsset<ContextMenuAsset>("Editor/UI/Geo/HierchyGroupMenu.ctxmenu");
            _brushCtxMenu = AssetManager.LoadAsset<ContextMenuAsset>("Editor/UI/Geo/HierchyBrushMenu.ctxmenu");

            _hierchyView = null;

            _contentElement = null;
            _placeholderText = null;

            _currentEntity = SceneEntity.Null;
            _sceneAsset = null;
            _geoScene = null;

            _groupNodes = new Dictionary<BrushGroup, TreeNode>();
            _brushNodes = new Dictionary<Brush, TreeNode>();

            EditorViewWindow editorView = UIManager.Instance.FindWindow<EditorViewWindow>()!;
            _faceViewSnippet = editorView.AddViewSnippet<FaceViewSnippet>("Editor/UI/Snippets/Geo/EditorViewFace.snippet", this);

            GeoSceneManager geoSceneManager = EditorRuntime.GlobalSingleton.GeoSceneManager;
            _selectionGroup = geoSceneManager.SelectionGroup;

            OnShown += OnFocused;
            OnHidden += OnBlurred;
        }

        protected override void InitializePostLoad()
        {
            _hierchyView = FindElementWithId<UITreeView>("Hierchy");

            _contentElement = FindElementWithId<UIElement>("ContentElement");
            _placeholderText = FindElementWithId<UILabel>("PlaceholderText");

            _saveButton = FindElementWithId<UIButton>("header-save-button");

            if (_hierchyView != null)
            {
                _hierchyView.OnNodePressed += OnNodePressed;
                _hierchyView.OnNodeSelected += OnNodeSelected;
                _hierchyView.OnNodeDeselected += OnNodeDeselected;
            }

            if (_sceneAsset != null)
            {
                _sceneAsset.WaitIfNotLoaded();

                _contentElement?.IsEnabled = true;
                _placeholderText?.IsEnabled = false;

                AssetManager.ListenForAssetLoad(_sceneAsset, this, OnAssetLoaded);
            }

            _selectionGroup.OnSelected += OnBrushSelected;
            _selectionGroup.OnDeselected += OnBrushDeselected;

            _saveButton?.OnPressed += SaveAssetData;
        }

        protected override void CleanupSelf()
        {
            if (_hierchyView != null)
            {
                _hierchyView.OnNodePressed -= OnNodePressed;
                _hierchyView.OnNodeSelected -= OnNodeSelected;
                _hierchyView.OnNodeDeselected -= OnNodeDeselected;
            }

            if (_sceneAsset != null)
            {
                AssetManager.ListenForAssetLoad(_sceneAsset, this, null);
            }

            _selectionGroup.OnSelected -= OnBrushSelected;
            _selectionGroup.OnDeselected -= OnBrushDeselected;

            _saveButton?.OnPressed -= SaveAssetData;
        }

        public override void Update()
        {
            base.Update();
        }

        private void OnFocused()
        {

        }

        private void OnBlurred()
        {

        }

        private void SaveAssetData()
        {
            if (_sceneAsset != null)
            {
                string? fullPath = EditorRuntime.GlobalSingleton.AssetPipeline.Identifier.RetrievePathForId(_sceneAsset.Id);
                if (fullPath != null && AssetPipeline.TryGetFullPathFromLocal(fullPath, out fullPath))
                {
                    AssetPipeline.SkipNextFileChange(_sceneAsset.Id);

                    string backupFilePath = Path.Combine(EditorFilepaths.LibraryPath, "oldfile.backup");
                    File.Copy(fullPath, backupFilePath, true);

                    SceneSerializer.Serialize(fullPath, _sceneAsset.Scene!);

                    File.Delete(backupFilePath);
                }
            }
        }

        private void OnBrushSelected(Brush brush, BrushSelectionData sd)
        {
            if (sd.IsBrushActive && _brushNodes.TryGetValue(brush, out TreeNode? treeNode))
                treeNode.Select();
        }

        private void OnBrushDeselected(Brush brush, BrushSelectionData sd)
        {
            if (_brushNodes.TryGetValue(brush, out TreeNode? treeNode))
                treeNode.Deselect();
        }

        private void OnNodePressed(BaseTreeNode treeNode, MouseButton button)
        {
            if (button == MouseButton.Right)
            {
                if (treeNode is GroupTreeNode groupTreeNode)
                {
                    ContextMenu.Open(_groupCtxMenu, new GroupContextMenuListener(this, groupTreeNode.Group));
                }
                else if (treeNode is BrushTreeNode brushTreeNode)
                {
                    ContextMenu.Open(_brushCtxMenu, new BrushContextMenuListener(this, brushTreeNode.Brush));
                }
            }
        }

        private void OnNodeSelected(BaseTreeNode treeNode)
        {
            if (treeNode is BrushTreeNode brushTreeNode)
            {
                _selectionGroup.Select(brushTreeNode.Brush);
            }
            else if (treeNode is FaceTreeNode faceTreeNode)
            {
                Brush brush = ((BrushTreeNode)faceTreeNode.Parent!).Brush;
                _selectionGroup.Select(brush, faceTreeNode.FaceIndex);
            }
        }

        private void OnNodeDeselected(BaseTreeNode treeNode)
        {
            if (treeNode is BrushTreeNode brushTreeNode)
            {
                _selectionGroup.Deselect(brushTreeNode.Brush);
            }
            else if (treeNode is FaceTreeNode faceTreeNode)
            {
                Brush brush = ((BrushTreeNode)faceTreeNode.Parent!).Brush;
                _selectionGroup.Deselect(brush, faceTreeNode.FaceIndex);
            }
        }

        private void OnAssetLoaded(GeoSceneAsset asset, bool wasReloaded)
        {
            if (wasReloaded)
            {
                _sceneAsset = null;
                ChangeSceneAsset(asset);
            }
        }

        private void OnSceneGroupCreated(BrushGroup group)
        {
            if (!_groupNodes.ContainsKey(group))
            {
                CreateBrushGroupNode(group);

                foreach (Brush brush in group.Brushes)
                {
                    OnSceneBrushCreated(brush);
                }
            }
        }

        private void OnSceneGroupDestroyed(BrushGroup group)
        {
            if (_groupNodes.TryGetValue(group, out TreeNode? treeNode))
            {
                _hierchyView?.RemoveNode(treeNode);
                _groupNodes.Remove(group);
            }
        }

        private void OnSceneBrushCreated(Brush brush)
        {
            if (!_brushNodes.ContainsKey(brush))
            {
                if (_groupNodes.TryGetValue(brush.Group, out TreeNode? parent))
                {
                    TreeNode node = CreateBrushNode(brush);
                    parent.AddNode(node);
                }
            }
        }

        private void OnSceneBrushDestroyed(Brush brush)
        {
            if (_brushNodes.TryGetValue(brush, out TreeNode? treeNode))
            {
                treeNode.Parent?.RemoveNode(treeNode);
                _brushNodes.Remove(brush);
            }
        }

        private void ChangeSceneAsset(GeoSceneAsset? newAsset)
        {
            if (_sceneAsset == newAsset)
                return;

            if (_sceneAsset != null)
            {
                _hierchyView?.ClearChildren();

                _groupNodes.Clear();
                _brushNodes.Clear();

                if (_geoScene != null)
                {
                    _geoScene.OnGroupCreated -= OnSceneGroupCreated;
                    _geoScene.OnGroupDestroyed -= OnSceneGroupDestroyed;

                    _geoScene.OnBrushCreated -= OnSceneBrushCreated;
                    _geoScene.OnBrushDestroyed -= OnSceneBrushDestroyed;
                }

                AssetManager.ListenForAssetLoad(_sceneAsset, this, null);
            }

            if (newAsset != null)
            {
                // TODO: wait for load instead of blocking
                newAsset.WaitIfNotLoaded();

                GeoScene scene = newAsset.Scene!;
                foreach (BrushGroup group in scene.Groups)
                {
                    TreeNode node = CreateBrushGroupNode(group);

                    foreach (Brush brush in group.Brushes)
                    {
                        node.AddNode(CreateBrushNode(brush));
                    }
                }

                _geoScene = newAsset.Scene;
                if (_geoScene != null)
                {
                    _geoScene.OnGroupCreated += OnSceneGroupCreated;
                    _geoScene.OnGroupDestroyed += OnSceneGroupDestroyed;

                    _geoScene.OnBrushCreated += OnSceneBrushCreated;
                    _geoScene.OnBrushDestroyed += OnSceneBrushDestroyed;
                }

                _contentElement?.IsEnabled = true;
                _placeholderText?.IsEnabled = false;

                AssetManager.ListenForAssetLoad(newAsset, this, OnAssetLoaded);
            }
            else
            {
                _contentElement?.IsEnabled = true;
                _placeholderText?.IsEnabled = false;

                _geoScene = null;
            }

            _sceneAsset = newAsset;
        }

        internal TreeNode CreateBrushGroupNode(BrushGroup group)
        {
            GroupTreeNode node = new GroupTreeNode(group)
            {
                Font = _mainFont,
                FontWeight = FontWeight._600,
                Text = group.Name,

                TextColor = Color.White,
            };

            _groupNodes.Add(group, node);
            _hierchyView?.AddNode(node);

            return node;
        }

        internal BrushTreeNode CreateBrushNode(Brush brush)
        {
            bool isSelected = _selectionGroup.TryGetSelectionData(brush, out BrushSelectionData selectionData);

            BrushTreeNode node = new BrushTreeNode(brush)
            {
                Font = _mainFont,
                Text = $"Brush {brush.Id.LocalId}",

                TextColor = Color.White,

                IsSelected = isSelected && selectionData.IsBrushActive
            };

            for (int i = 0; i < 6; i++)
            {
                BrushFaceIndex faceIndex = (BrushFaceIndex)i;

                FaceTreeNode faceNode = new FaceTreeNode(faceIndex)
                {
                    Font = _mainFont,
                    FontStyle = FontStyle.Italic,
                    Text = faceIndex switch
                    {
                        BrushFaceIndex.Front => "Front (Z+)",
                        BrushFaceIndex.Back => "Back (Z-)",
                        BrushFaceIndex.Left => "Left (X+)",
                        BrushFaceIndex.Right => "Right (X-)",
                        BrushFaceIndex.Top => "Top (Y+)",
                        BrushFaceIndex.Bottom => "Bottom (Y-)",
                        _ => throw new NotImplementedException()
                    },

                    TextColor = Flags.HasFlag(brush.Faces[i].Flags, BrushFaceFlags.Invisible) ? new Color(1.0f, 0.5f) : new Color(1.0f, 0.8f),

                    IsSelected = isSelected && selectionData.IsFaceSelected(faceIndex)
                };

                node.AddNode(faceNode);
            }

            _brushNodes.Add(brush, node);

            return node;
        }

        internal bool TryGetBrushGroupNode(BrushGroup group, [NotNullWhen(true)] out TreeNode? treeNode) => _groupNodes.TryGetValue(group, out treeNode);

        public SceneEntity TargetEntity { get => _currentEntity; internal set => _currentEntity = value; }
        public GeoSceneAsset? TargetAsset { get => _sceneAsset; internal set => ChangeSceneAsset(value); }
    }
}
