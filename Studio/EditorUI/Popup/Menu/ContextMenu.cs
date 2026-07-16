using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using EditorUI.Assets;
using EditorUI.Common;
using EditorUI.Input;
using EditorUI.Styling;
using EditorUI.Text;
using EditorUI.Visual;
using Primary.Assets;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Extensions;
using Primary.Mathematics;
using Primary.Windowing;

namespace EditorUI.Popup.Menu
{
    public sealed class ContextMenu : PopupMenu
    {
        private List<PopupMenuItem> _items;
        private List<PopupMenuMenu> _currentMenuStack;

        private PopupMenuItem? _currentMouseItem;
        private long _currentHoverStartTimestamp;

        private StateFlags _stateFlags;
        private bool _isMenuLocked;

        private Vector2 _rootItemMenuSize;

        private IAssetProvider<FontFamily>? _fontFamily;
        private FontStyle _fontStyle;
        private FontWeight _fontWeight;

        private float _fontSize;

        private Vector4 _itemPadding;
        private float _maxWidth;

        private UIColor _backgroundColor;

        public ContextMenu()
        {
            _items = new List<PopupMenuItem>();
            _currentMenuStack = new List<PopupMenuMenu>();

            _currentMouseItem = null;
            _currentHoverStartTimestamp = 0;

            _stateFlags = StateFlags.InvalidLayout | StateFlags.InvalidStyle;
            _isMenuLocked = false;

            _rootItemMenuSize = Vector2.Zero;

            _fontFamily = null;
            _fontStyle = FontStyle.Normal;
            _fontWeight = FontWeight.Normal;

            _fontSize = 24.0f;

            _itemPadding = new Vector4(4.0f);
            _maxWidth = 200.0f;

            _backgroundColor = Color.White;
        }

        public void Destroy()
        {
            ThrowIfLocked();

            UIManager.Instance.InputManager.ForgetInteractable(this);

            foreach (PopupMenuItem item in _items)
            {
                item.DestroySelf();
            }

            _currentMenuStack.Clear();
            _items.Clear();
        }

        protected internal override void StartHostingSelf(Window window)
        {
        }

        protected internal override void CleanupSelf()
        {
            UIManager.Instance.InputManager.ForgetInteractable(this);
            _currentMenuStack.Clear();
        }

        internal override void Lock()
        {
            _isMenuLocked = true;
        }

        internal override void Unlock()
        {
            _isMenuLocked = false;
            _currentMenuStack.Clear();

            foreach (PopupMenuItem item in _items)
            {
                item.ForgetState();
            }

            UIManager.Instance.InputManager.ForgetInteractable(this);
        }

        protected internal override void UpdateSelf(Window window)
        {
            if (_fontFamily == null || !_fontFamily.IsReadyToUse)
                return;

            if (_currentMouseItem is PopupMenuMenu subMenu && subMenu.Items.Count > 0 && !_currentMenuStack.Contains(subMenu))
            {
                TimeSpan timeSinceHover = Stopwatch.GetElapsedTime(_currentHoverStartTimestamp);
                if (timeSinceHover.TotalSeconds > 0.3)
                {
                    _currentMenuStack.Add(subMenu);
                    UpdateItemList(subMenu, subMenu.Items);
                }
            }

            if (_stateFlags.HasFlags(StateFlags.InvalidLayout))
            {
                UpdateItemList(null, _items);
                RemoveStateFlags(StateFlags.SelfInvalidLayout);
            }
        }

        protected internal override void PaintSelf(ref readonly PainterContext painter, Window window)
        {
            if (_fontFamily == null || !_fontFamily.IsReadyToUse)
                return;

            Boundaries safeBounds = window.Display.UsableBoundaries.AsBoundaries();

            Vector2 topLeftPadding = _itemPadding.GetLower();
            float totalVerticalPadding = _itemPadding.Y + _itemPadding.W;

            DrawItemList(in painter, Vector2.Zero, _rootItemMenuSize, _items);

            foreach (PopupMenuMenu subMenu in _currentMenuStack)
            {
                DrawItemList(in painter, subMenu.MenuPosition, subMenu.ItemMenuSize, subMenu.Items);
            }

            void DrawItemList(ref readonly PainterContext painter, Vector2 positionOnPage, Vector2 itemMenuSize, ROList<PopupMenuItem> itemList)
            {
                float availableWidth = itemMenuSize.X - _itemPadding.X - _itemPadding.Z;

                painter.AddRectangle(new Boundaries(positionOnPage, positionOnPage + itemMenuSize), new Paint(_backgroundColor));
                foreach (PopupMenuItem menuItem in itemList)
                {
                    menuItem.PaintSelf(in painter, positionOnPage + menuItem.Position + topLeftPadding, _itemPadding, availableWidth);
                }
            }
        }

        public override bool HandleEventSelf(ref readonly UIInputEvent inputEvent)
        {
            return false;
        }

        internal override void BeginItemHover(PopupMenuItem item)
        {
            _currentMouseItem = item;
            _currentHoverStartTimestamp = Stopwatch.GetTimestamp();

            PopupMenuMenu? currentOwningSubItem = item.OwningItem;
            for (int i = _currentMenuStack.Count - 1; i >= 0; --i)
            {
                if (currentOwningSubItem != _currentMenuStack[i] && item != _currentMenuStack[i])
                    _currentMenuStack.RemoveAt(i);
                else
                    break;
            }
        }

        internal override void EndItemHover(PopupMenuItem item)
        {
            if (_currentMouseItem == item)
            {
                _currentMouseItem = null;
            }
        }

        internal override void HandleItemPress(PopupMenuItem item)
        {
            if (item is PopupMenuAction action)
            {
                OnItemPressed?.Invoke(action.Name);
                UIManager.Instance.PopupManager.ClosePopup(this);
            }
            else if (item is PopupMenuMenu)
            {
                _currentHoverStartTimestamp = 0;
            }
        }

        public override IInteractable GetInteractable(Vector2 point)
        {
            return SearchForItemAtPoint(_items, new Boundaries(Vector2.Zero, _rootItemMenuSize), point, _itemPadding.Y + _itemPadding.W) ?? this;

            static IInteractable? SearchForItemAtPoint(ROList<PopupMenuItem> items, Boundaries menuBounds, Vector2 point, float paddingY)
            {
                if (items.Count == 0)
                    return null;

                bool isWithin = menuBounds.IsWithin(point);
                float y = items[0].Position.Y;

                for (int i = 0; i < items.Count; ++i)
                {
                    PopupMenuItem item = items[i];
                    float nextY = y + item.ItemHeight + paddingY;

                    if (isWithin && point.Y >= y && point.Y <= nextY)
                    {
                        return item.GetInteractable(point);
                    }
                    else if (item is PopupMenuMenu menu)
                    {
                        IInteractable? foundItem = SearchForItemAtPoint(menu.Items, new Boundaries(menu.MenuPosition, menu.MenuPosition + menu.ItemMenuSize), point, paddingY);
                        if (foundItem != null)
                            return foundItem;
                    }

                    y = nextY;
                }

                return null;
            }
        }

        private void UpdateItemList(PopupMenuMenu? owner, ROList<PopupMenuItem> items)
        {
            float currentWidth = 0.0f;
            float currentHeight = 0.0f;

            float maxAvailWidth = _maxWidth - _itemPadding.X - _itemPadding.Z;
            float paddingY = _itemPadding.Y + _itemPadding.W;

            foreach (PopupMenuItem menuItem in items)
            {
                menuItem.MeasureSelf(maxAvailWidth);
                menuItem.Position = new Vector2(0.0f, currentHeight);

                currentWidth = Math.Max(menuItem.ItemWidth, currentWidth);
                currentHeight += menuItem.ItemHeight + paddingY;
            }

            currentWidth = Math.Min(currentWidth, _maxWidth) + _itemPadding.X + _itemPadding.Z;

            if (owner != null)
            {
                owner.ItemMenuSize = new Vector2(currentWidth, currentHeight);
                owner.MenuPosition = (owner.OwningItem?.MenuPosition ?? Vector2.Zero) + new Vector2(owner.OwningItem?.ItemMenuSize.X ?? _rootItemMenuSize.X, owner.Position.Y);
            }
            else
                _rootItemMenuSize = new Vector2(currentWidth, currentHeight);
        }

        public PopupMenuAction AddAction(string actionName, string actionText, Sprite? sprite = null)
        {
            PopupMenuAction action = new PopupMenuAction(this, null, actionName, actionText, sprite);
            _items.Add(action);

            _stateFlags |= StateFlags.InvalidLayout;
            FontDataChangedCallback();

            return action;
        }

        public PopupMenuMenu AddMenu(string menuName, string menuText)
        {
            PopupMenuMenu action = new PopupMenuMenu(this, null, menuName, menuText);
            _items.Add(action);

            _stateFlags |= StateFlags.InvalidLayout;
            FontDataChangedCallback();

            return action;
        }

        public PopupMenuSeparator AddSeparator()
        {
            PopupMenuSeparator action = new PopupMenuSeparator(this, null);
            _items.Add(action);

            _stateFlags |= StateFlags.InvalidLayout;
            FontDataChangedCallback();

            return action;
        }

        [StyleUpdateCallback(nameof(FontFamily), nameof(FontStyle), nameof(FontWeight), nameof(FontSize), nameof(MaxWidth))]
        private void FontDataChangedCallback()
        {
            foreach (PopupMenuItem item in _items)
            {
                item.ClearSavedData();
            }
        }

        #region StyledObject
        public override void AddStateFlags(StateFlags flags)
        {
            _stateFlags |= flags;
        }

        public override void RemoveStateFlags(StateFlags flags)
        {
            _stateFlags &= ~flags;
        }

        protected internal override void GetUnstyledObjects(ref StyleQueueContext queue)
        {
            foreach (PopupMenuItem item in _items)
            {
                queue.TryEnqueue(item);
            }
        }

        protected internal override StyledObject? ParentObject => null;
        #endregion

        #region Serializable
        [Styled(nameof(_fontFamily), StateFlags.SelfInvalidLayout)] public override IAssetProvider<FontFamily>? FontFamily { get => _fontFamily; set => SetStyledField(value); }
        [Styled(nameof(_fontStyle), StateFlags.SelfInvalidLayout)] public override FontStyle FontStyle { get => _fontStyle; set => SetStyledField(value); }
        [Styled(nameof(_fontWeight), StateFlags.SelfInvalidLayout)] public override FontWeight FontWeight { get => _fontWeight; set => SetStyledField(value); }
        
        [Styled(nameof(_fontSize), StateFlags.SelfInvalidLayout)] public override float FontSize { get => _fontSize; set => SetStyledField(value); }

        [Styled(nameof(_itemPadding), StateFlags.SelfInvalidLayout)] public override Vector4 ItemPadding { get => _itemPadding; set => SetStyledField(value); }
        [Styled(nameof(_maxWidth), StateFlags.SelfInvalidLayout)] public override float MaxWidth { get => _maxWidth; set => SetStyledField(value); }

        [Styled(nameof(_backgroundColor))] public UIColor BackgroundColor { get => _backgroundColor; set => SetStyledField(value); }
        #endregion

        public override StateFlags StateFlags => _stateFlags;

        public override bool IsMenuLocked => _isMenuLocked;

        public event Action<string>? OnItemPressed;
    }
}
