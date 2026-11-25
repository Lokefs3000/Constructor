using Editor.Gui.Hierchy;
using Editor.Reflection;
using Editor.UI;
using Editor.UI.Assets;
using Editor.UI.Elements;
using Editor.UI.Elements.Tree;
using Primary.Assets;
using Primary.Common;
using Primary.Scenes;
using Primary.Scenes.Components;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Gui.Windows
{
    public sealed class HierchyWindow : UIWindow
    {
        private readonly UIFontAsset _mainFont;

        private UITreeView? _hierchyView;

        private Dictionary<Type, HierchyNodeView> _customNodeViews;

        private Dictionary<Scene, TreeNode> _sceneNodes;
        private Dictionary<SceneEntity, EntityTreeNode> _entityNodes;

        private bool _needsHierchyRefresh;

        public HierchyWindow(int uniqueWindowId) : base(uniqueWindowId)
        {
            _mainFont = AssetManager.LoadAsset<UIFontAsset>("Editor/Fonts/Inter.uifont").WaitIfNotLoaded();

            _customNodeViews = new Dictionary<Type, HierchyNodeView>();

            _sceneNodes = new Dictionary<Scene, TreeNode>();
            _entityNodes = new Dictionary<SceneEntity, EntityTreeNode>();

            _needsHierchyRefresh = false;

            EditorRuntime runtime = EditorRuntime.GlobalSingleton;
            runtime.SceneManager.SceneLoaded += OnSceneLoaded;
            runtime.ReflectionManager.TypeLoader.AddCallback<HierchyNodeViewComponents>(OnHierchyViewTypeLoaded);

            SceneEntityManager.Events |= EntityEvents.EntityData | EntityEvents.EntityRelationship | EntityEvents.EntityComponents;
            SceneEntityManager.EntityRenamed += OnEntityRenamed;
            SceneEntityManager.EntityEnabled += OnEntityEnabled;
            SceneEntityManager.EntityParentChange += OnEntityParentChanged;
            SceneEntityManager.EntityCreated += OnEntityCreated;
            SceneEntityManager.ComponentAdded += OnComponentAdded;
            SceneEntityManager.ComponentRemoved += OnComponentRemoved;
        }

        protected override void InitializePostLoad()
        {
            _hierchyView = FindElementWithId<UITreeView>("Hierchy");

            if (_hierchyView != null)
            {
                TextureAtlasAsset icons = AssetManager.LoadAsset<TextureAtlasAsset>("Editor/UI/Atlases/HierchyTreeIcons.atlas").WaitIfNotLoaded();
                
                _hierchyView.AddButtonColumn("enabled", icons.TryFindSpriteOrNull("Hidden"), icons.TryFindSpriteOrNull("Visible"), true);

                _hierchyView.OnColumnButtonChanged += OnColumnButtonChanged;
                _hierchyView.OnNewColumnValueNeeded += OnNewColumnValueNeeded;
            }
        }

        protected override void CleanupSelf()
        {
            if (_hierchyView != null)
            {
                _hierchyView.OnColumnButtonChanged -= OnColumnButtonChanged;
                _hierchyView.OnNewColumnValueNeeded -= OnNewColumnValueNeeded;
            }
        }

        public override void Update()
        {
            if (_hierchyView != null && _needsHierchyRefresh)
            {
                _hierchyView.ClearNodes();

                _sceneNodes.Clear();
                _entityNodes.Clear();

                foreach (Scene scene in EditorRuntime.GlobalSingleton.SceneManager.Scenes)
                {
                    TreeNode node = CreateNodeForScene(scene.Root);
                    TraverseSceneEntities(scene.Root, node);
                }

                _needsHierchyRefresh = false;
            }

            base.Update();
        }

        private void OnColumnButtonChanged(BaseTreeNode treeNode, string id, bool value)
        {
            if (id == "enabled")
            {
                if (treeNode is EntityTreeNode entityTreeNode)
                {
                    SceneEntity entity = entityTreeNode.Entity;
                    entity.Enabled = value;
                }
            }
        }

        private bool OnNewColumnValueNeeded(BaseTreeNode treeNode, string id)
        {
            if (id == "enabled")
            {
                if (treeNode is EntityTreeNode entityTreeNode)
                    return entityTreeNode.Entity.Enabled;
            }

            return false;
        }

        private void OnSceneLoaded(Scene scene)
        {
            if (_hierchyView != null && !_sceneNodes.ContainsKey(scene))
            {
                //TreeNode node = new TreeNode
                //{
                //    FontStyle = _mainFont.FindStyle(null),
                //    Text = scene.Name,
                //
                //    TextColor = Color.White
                //};

                //_hierchyView.AddNode(node);
                //_sceneNodes.Add(scene, node);

                //TraverseSceneEntities(scene.Root, node);
            }
        }

        private void OnEntityRenamed(SceneEntity entity)
        {
            if (entity.IsSceneRoot)
                return;

            if (!_entityNodes.TryGetValue(entity, out EntityTreeNode? node))
                node = CreateNodeForEntity(entity);

            node.Text = entity.Name;
        }

        private void OnEntityEnabled(SceneEntity entity)
        {
            if (entity.IsSceneRoot)
                return;

            if (!_entityNodes.TryGetValue(entity, out EntityTreeNode? node))
                node = CreateNodeForEntity(entity);

            bool value = entity.Enabled;

            node.TextColor = value ? Color.White : new Color(1.0f, 0.5f);
            _hierchyView?.SetNodeColumnValue(node, "enabled", value);
        }

        private void OnEntityParentChanged(SceneEntity entity)
        {
            if (entity.IsSceneRoot)
                return;

            if (!_entityNodes.TryGetValue(entity, out EntityTreeNode? node))
                node = CreateNodeForEntity(entity);

            SceneEntity newParent = entity.Parent;

            if (newParent != SceneEntity.Null)
            {
                TreeNode? parentNode = null;
                if (newParent.IsSceneRoot)
                {
                    if (!_sceneNodes.TryGetValue(newParent.Scene, out parentNode))
                        parentNode = CreateNodeForScene(newParent);
                }
                else
                {
                    if (!_entityNodes.TryGetValue(newParent, out EntityTreeNode? temp))
                        temp = CreateNodeForEntity(newParent);

                    parentNode = temp;
                }

                parentNode.AddNode(node);
            }
            else
            {
                node.Parent?.RemoveNode(node);
            }
        }

        private void OnEntityCreated(SceneEntity entity)
        {
            OnEntityParentChanged(entity);
        }

        private void OnHierchyViewTypeLoaded(Type type, object attribute)
        {
            HierchyNodeView? nodeView = Activator.CreateInstance(type, [this]) as HierchyNodeView;
            if (nodeView == null)
            {
                EdLog.Gui.Warning("Failed to active hierchy node view {nv}", type);
                return;
            }

            HierchyNodeViewComponents components = (HierchyNodeViewComponents)attribute;
            foreach (Type componentType in components.Types)
            {
                if (!_customNodeViews.TryAdd(componentType, nodeView))
                {
                    EdLog.Gui.Warning("Component {t} already has a node view assigned!", componentType);
                }
                else
                    _needsHierchyRefresh = true;
            }
        }

        private void OnComponentAdded(SceneEntity entity, Type addedType)
        {
            if (_customNodeViews.ContainsKey(addedType))
            {
                if (!_entityNodes.TryGetValue(entity, out EntityTreeNode? currentNode))
                    return;

                Type nodeType = currentNode.GetType();
                foreach (Type componentType in entity.ComponentTypes)
                {
                    if (_customNodeViews.TryGetValue(componentType, out HierchyNodeView? nodeView))
                    {
                        if (nodeType != nodeView.GetNodeTypeFor(entity))
                        {
                            goto RecreateNode;
                        }
                    }
                }

                return;

            RecreateNode:
                TreeNode newNode = CreateNodeForEntity(entity);

                if (currentNode.Parent != null)
                {
                    BaseTreeNode parent = currentNode.Parent;

                    int index = parent.Children.IndexOf(currentNode);

                    parent.AddNode(newNode);
                    parent.RemoveNode(currentNode);

                    parent.MoveNode(newNode, index);
                }
                else if (currentNode.ParentTree == _hierchyView && _hierchyView != null)
                {
                    int index = _hierchyView.Nodes.IndexOf(currentNode);

                    _hierchyView.AddNode(newNode);
                    _hierchyView.RemoveNode(currentNode);

                    _hierchyView.MoveNode(newNode, index);
                }
            }
        }

        private void OnComponentRemoved(SceneEntity entity, Type removedType)
        {
            OnComponentAdded(entity, removedType);
        }

        private void TraverseSceneEntities(SceneEntity entity, TreeNode node)
        {
            foreach (SceneEntity child in entity.Children)
            {
                EntityTreeNode childNode = CreateNodeForEntity(child);
                node.AddNode(childNode);

                if (!child.Children.IsEmpty)
                    TraverseSceneEntities(child, childNode);
            }
        }

        private TreeNode CreateNodeForScene(SceneEntity entity)
        {
            if (!entity.IsSceneRoot)
                throw new NotSupportedException();

            Scene scene = entity.Scene;

            TreeNode node = new TreeNode
            {
                Font = _mainFont,
                Text = scene.Name,

                TextColor = Color.White
            };

            _hierchyView?.AddNode(node);
            _sceneNodes[scene] = node;

            return node;
        }

        private EntityTreeNode CreateNodeForEntity(SceneEntity entity)
        {
            if (entity.IsSceneRoot)
                throw new NotSupportedException();

            EntityTreeNode? node = null;
            foreach (Type type in entity.ComponentTypes)
            {
                if (_customNodeViews.TryGetValue(type, out HierchyNodeView? nodeView))
                {
                    node = nodeView.CreateTreeNode(entity);
                }
            }

            if (node == null)
                node = new EntityTreeNode(entity);

            node.Font = _mainFont;
            node.FontWeight = FontWeight.Lighter;

            node.Text = entity.Name;

            node.TextColor = entity.Enabled ? Color.White : new Color(1.0f, 0.5f);

            SceneEntity parent = entity.Parent;
            if (parent != SceneEntity.Null)
            {
                TreeNode? parentNode;
                if (parent.IsSceneRoot)
                {
                    if (!_sceneNodes.TryGetValue(parent.Scene, out parentNode))
                        parentNode = CreateNodeForScene(parent);
                }
                else
                {
                    if (!_entityNodes.TryGetValue(parent, out EntityTreeNode? temp))
                        temp = CreateNodeForEntity(parent);

                    // stupid type shenanigans
                    parentNode = temp;
                }

                parentNode.AddNode(node);
            }

            _entityNodes[entity] = node;

            return node;
        }
    }
}
