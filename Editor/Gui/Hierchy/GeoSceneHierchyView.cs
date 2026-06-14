using Editor.Components;
using Editor.Geo;
using Editor.Gui.Windows;
using Editor.UI;
using Editor.UI.Elements.Tree;
using Editor.UI.Interaction;
using Editor.UI.Visual;
using Primary.Assets;
using Primary.Common;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.Gui.Hierchy
{
    [HierchyNodeViewComponents(typeof(GeoSceneComponent))]
    internal sealed class GeoSceneHierchyView : HierchyNodeView
    {
        private TextureAsset? _openExternalIcon;

        public GeoSceneHierchyView(HierchyWindow window) : base(window)
        {
            _openExternalIcon = AssetManager.LoadAsset<TextureAsset>("Editor/UI/Icons/OpenExternal.png");
        }

        public override EntityTreeNode CreateTreeNode(SceneEntity entity)
        {
            GeoSceneTreeNode treeNode = new GeoSceneTreeNode(entity)
            {
                OpenExternalIcon = _openExternalIcon
            };

            treeNode.OnOpenExternallyPressed += () =>
            {
                ref GeoSceneComponent component = ref entity.GetComponent<GeoSceneComponent>();
                if (Unsafe.IsNullRef(in component))
                    return;
                if (component.Scene == null)
                    return;

                EditorRuntime runtime = EditorRuntime.GlobalSingleton;
                runtime.GeoSceneManager.SetNewFocus(entity, component.Scene, _hierchy.ParentHost as UIDockHost);
            };

            return treeNode;
        }

        public override Type GetNodeTypeFor(SceneEntity entity)
        {
            return typeof(GeoSceneTreeNode);
        }
    }

    internal sealed class GeoSceneTreeNode : EntityTreeNode, IInteractionShape
    {
        private TextureAsset? _openExternalIcon;

        private bool _isHovered;
        private bool _isHeld;

        public GeoSceneTreeNode(SceneEntity entity) : base(entity)
        {
        }

        public override void DrawVisual(Vector2 position, UIPainterContext painter)
        {
            base.DrawVisual(position, painter);

            if (_openExternalIcon != null)
            {
                float xOffset = ParentTree!.ViewSize.X - (position.X - ParentTree!.ViewCoordinates.Minimum.X) - 25.0f;

                position.X += xOffset;
                Boundaries boundaries = new Boundaries(position, position + new Vector2(18.0f));

                const float CornerRounding = 3.0f;

                if (_isHeld)
                    painter.DrawRoundedRect(boundaries, UIPaint.FromColor(s_heldBgColor), CornerRounding);
                else if (_isHovered)
                    painter.DrawRoundedRect(boundaries, UIPaint.FromColor(s_hoveredBgColor), CornerRounding);

                painter.DrawImage(new Boundaries(
                    boundaries.Minimum + new Vector2(2.0f),
                    boundaries.Maximum - new Vector2(2.0f)), UIPaint.FromColor(Color.White), _openExternalIcon);
            }
        }

        public override void HandleEvent(ref readonly UIEvent @event)
        {
            switch (@event.Type)
            {
                case UIEventType.MouseEnter: _isHovered = true; break;
                case UIEventType.MouseLeave: _isHovered = false; break;
                case UIEventType.MouseDown:
                    {
                        if (@event.Mouse.Button == MouseButton.Left)
                            _isHeld = true;
                        break;
                    }
                case UIEventType.MouseUp:
                    {
                        if (@event.Mouse.Button == MouseButton.Left)
                            _isHeld = false;
                        break;
                    }
                case UIEventType.MouseActivate:
                    {
                        if (@event.Mouse.Button == MouseButton.Left)
                            OnOpenExternallyPressed?.Invoke();
                        break;
                    }
            }
        }

        public bool Intersects(Vector2 point)
        {
            if (ParentTree != null)
            {
                if (point.X >= ParentTree.ViewSize.X - 26.0f)
                    return true;
            }

            return false;
        }

        public TextureAsset? OpenExternalIcon { get => _openExternalIcon; set => _openExternalIcon = value; }

        public override IInteractionShape? Shape => this;

        public event Action? OnOpenExternallyPressed;

        private static readonly Color s_hoveredBgColor = Color.FromHex("6d6d6d");
        private static readonly Color s_heldBgColor = Color.FromHex("3f3f3f");
    }
}
