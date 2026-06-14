using Editor.UI.Assets;
using Editor.UI.Layout;
using Editor.UI.Text;
using Editor.UI.Visual;
using Primary;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;
using Primary.Polling;
using Primary.RHI;
using Primary.Windowing;
using SDL;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.UI.Menu
{
    public sealed class ContextMenuHost : IEventHandler, IInterfaceHost, ILayoutHost, IRenderableHost
    {
        private bool _isCleanedUp;

        private readonly Window _window;
        private readonly Vector2 _originPosition;
        private readonly ContextMenuAsset _asset;
        private readonly IContextMenuListener _listener;

        private RHITexture? _texture;

        private Vector2 _baseSize;
        private List<VisibleItem> _visibleItems;

        private ContextMenuItem? _hoveredItem;
        private int _hoveredItemIndex;
        private DateTime _hoverTimeStart;

        private UIStateFlags _stateFlags;
        private Boundaries _invalidVisualRegion;

        internal ContextMenuHost(Window window, Vector2 origin, ContextMenuAsset asset, IContextMenuListener listener)
        {
            _isCleanedUp = false;

            _window = window;
            _originPosition = origin;
            _asset = asset.WaitIfNotLoaded();
            _listener = listener;

            _texture = null;

            _baseSize = Vector2.Zero;
            _visibleItems = new List<VisibleItem>();

            _hoveredItem = null;
            _hoveredItemIndex = -1;

            _stateFlags = UIStateFlags.InvalidAll;
            _invalidVisualRegion = Boundaries.Zero;

            window.OnFocusLost += OnWindowFocusLost;

            UIManager.Instance.AddContextMenuAsHost(this);
            Engine.GlobalSingleton.EventManager.AddHandler(this);

            MeasureNewRegionSize(null);
        }

        internal void Cleanup()
        {
            _isCleanedUp = true;

            _texture?.Dispose();
            _texture = null;

            _window.OnFocusLost -= OnWindowFocusLost;

            UIManager manager = UIManager.Instance;
            manager.LayoutManager.RemoveInvalidHost(this);
            manager.Renderer.RemoveHostToRedrawQueue(this);

            manager.RemoveContextMenuAsHost(this);
            Engine.GlobalSingleton.EventManager.RemoveHandler(this);
        }

        private void OnWindowFocusLost(Window window) => Close();

        internal void MeasureNewRegionSize(ContextMenuBase? itemToMeasure, bool disableWindowLayout = false)
        {
            ObjectDisposedException.ThrowIf(_isCleanedUp, this);

            if (itemToMeasure == null)
            {
                Vector2 baseSize = Vector2.Zero;
                foreach (ContextMenuBase item in _asset.Items)
                {
                    Vector2 size = Primary.Mathematics.Extensions.Ceiling(item.MeasureSize()) + new Vector2(4.0f);
                    item.VerticalOffset = baseSize.Y;

                    baseSize = new Vector2(Math.Max(baseSize.X, size.X), baseSize.Y + size.Y);
                }

                _baseSize = baseSize + new Vector2(2.0f);
                AddStateFlags(UIStateFlags.InvalidLayout);
            }
            else if (itemToMeasure is ContextMenuItem menuItem)
            {
                Vector2 baseSize = Vector2.Zero;
                foreach (ContextMenuBase item in menuItem.Items)
                {
                    Vector2 size = Primary.Mathematics.Extensions.Ceiling(item.MeasureSize()) + new Vector2(4.0f);
                    item.VerticalOffset = baseSize.Y;

                    baseSize = new Vector2(Math.Max(baseSize.X, size.X), baseSize.Y + size.Y);
                }

                menuItem.TotalSize = baseSize + new Vector2(2.0f);

                if (_visibleItems.Exists((x) => x.Item == itemToMeasure))
                    AddStateFlags(UIStateFlags.InvalidLayout);
            }
        }

        public void RecalculateLayout()
        {
            Vector2 startPosition = _originPosition;
            Vector2 currentPosition = startPosition;
            Vector2 absoluteSize = _baseSize;

            bool isDirectionReversed = false;

            Display display = _window.Display;
            ContextMenuBase? prev = null;

            for (int i = 0; i < _visibleItems.Count; ++i)
            {
                VisibleItem item = _visibleItems[i];

                float prevWidth = prev?.TotalSize.X ?? _baseSize.X;
                if (isDirectionReversed)
                    prevWidth = -prevWidth;

                Vector2 nextPosition = currentPosition + new Vector2(prevWidth, item.Item.VerticalOffset);
                Vector2 clampedPosition = Vector2.Clamp(nextPosition, display.UsableBoundaries.Position.AsVector2(), display.UsableBoundaries.Maximum.AsVector2() - item.Item.TotalSize);

                if (clampedPosition.X > nextPosition.X)
                    isDirectionReversed = false;
                else if (clampedPosition.X < nextPosition.X)
                    isDirectionReversed = true;

                absoluteSize = Vector2.Max(absoluteSize, clampedPosition - startPosition + item.Item.TotalSize);
                startPosition = Vector2.Min(startPosition, clampedPosition);

                currentPosition = clampedPosition;

                _visibleItems[i] = new VisibleItem(clampedPosition, item.Item);
            }

            Int2 intAbsoluteSize = absoluteSize.AsInt2();

            if (intAbsoluteSize != _window.ClientSize || _texture == null)
            {
                _texture?.Dispose();
                _texture = RHIDevice.Instance?.CreateTexture(new RHITextureDescription
                {
                    Width = intAbsoluteSize.X,
                    Height = intAbsoluteSize.Y,

                    MipLevels = 1,

                    Usage = RHIResourceUsage.ShaderResource | RHIResourceUsage.RenderTarget,
                    Dimension = RHIDimension.Texture2D,
                    Format = RHIFormat.RGB10A2_UNorm
                }, [], _asset.Name);
            }

            _window.Position = startPosition.AsInt2();
            _window.ClientSize = intAbsoluteSize;

            _window.Show();

            RemoveStateFlags(UIStateFlags.InvalidLayout);
        }

        public void DrawVisual(UIPainterContext painter)
        {
            ObjectDisposedException.ThrowIf(_isCleanedUp, this);

            painter.PushMatrix(Matrix3x2.CreateTranslation(-_window.Position.AsVector2()));

            for (int i = 0; i < _visibleItems.Count + 1; ++i)
            {
                ROList<ContextMenuBase> items;
                Boundaries boundaries;

                if (i == 0)
                {
                    items = _asset.Items;
                    boundaries = new Boundaries(_originPosition, _originPosition + _baseSize);
                }   
                else
                {
                    VisibleItem item = _visibleItems[i - 1];

                    items = item.Item.Items;
                    boundaries = new Boundaries(item.Position, item.Position + item.Item.TotalSize);
                }

                Vector2 actualSize = boundaries.Size - new Vector2(4.0f);
                boundaries = Boundaries.Grow(boundaries, -Vector2.One);

                painter.DrawRoundedRect(boundaries, UIPaint.FromColor(s_normalColor).SetStroke(true).SetStrokeColor(s_strokeColor), 4.0f);

                for (int j = 0; j < items.Count; ++j)
                {
                    ContextMenuBase item = items[j];

                    Vector2 basePosition = new Vector2(boundaries.Minimum.X, boundaries.Minimum.Y + item.VerticalOffset);

                    if (_hoveredItem == item)
                        painter.DrawRoundedRect(new Boundaries(basePosition, new Vector2(boundaries.Maximum.X, basePosition.Y + TextManager.PixelsPerEM + 4.0f)), UIPaint.FromColor(s_hoveredColor), 4.0f);

                    item.DrawVisual(basePosition + new Vector2(2.0f), actualSize, painter);
                }
            }

            painter.PopMatrix();
        }

        public void Update()
        {
            if (_hoveredItem != null)
            {
                TimeSpan hoveredTime = DateTime.Now - _hoverTimeStart;
                if (hoveredTime.TotalSeconds > 0.4)
                {
                    if (_hoveredItemIndex < _visibleItems.Count - 1)
                    {
                        ++_hoveredItemIndex;
                        _visibleItems.RemoveRange(_hoveredItemIndex, _visibleItems.Count - _hoveredItemIndex);

                        AddStateFlags(UIStateFlags.InvalidLayout);
                    }

                    if (_hoveredItem.Items.Count > 0)
                    {
                        MeasureNewRegionSize(_hoveredItem);
                        AddStateFlags(UIStateFlags.InvalidLayout);

                        _visibleItems.Add(new VisibleItem(Vector2.Zero, _hoveredItem));
                        _hoveredItem = null;
                    }
                }
            }
        }

        public void Handle(ref readonly SDL_Event @event)
        {
            if (_isCleanedUp)
                return;

            if ((@event.Type == SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP || @event.Type == SDL_EventType.SDL_EVENT_MOUSE_MOTION) && (uint)@event.button.windowID == _window.WindowId)
            {
                Vector2 hit = new Vector2(@event.motion.x, @event.motion.y) + _window.Position.AsVector2();

                for (int i = _visibleItems.Count; i >= 0; --i)
                {
                    ROList<ContextMenuBase> items;
                    Boundaries boundaries;

                    if (i == 0)
                    {
                        items = _asset.Items;
                        boundaries = new Boundaries(_originPosition, _originPosition + _baseSize);
                    }
                    else
                    {
                        VisibleItem item = _visibleItems[i - 1];

                        items = item.Item.Items;
                        boundaries = new Boundaries(item.Position, item.Position + item.Item.TotalSize);
                    }

                    boundaries = Boundaries.Grow(boundaries, -Vector2.One);

                    if (boundaries.IsWithin(hit))
                    {
                        hit.Y = @event.motion.y + 1.0f;
                        for (int j = 0; j < items.Count; ++j)
                        {
                            ContextMenuBase item = items[j];
                            if (j == items.Count - 1 || items[j + 1].VerticalOffset >= hit.Y)
                            {
                                bool isAlreadyVisible = false;
                                for (int k = 0; k < _visibleItems.Count; k++)
                                {
                                    if (_visibleItems[k].Item == item)
                                    {
                                        isAlreadyVisible = true;
                                        _hoveredItem = null;
                                        break;
                                    }
                                }

                                if (!isAlreadyVisible)
                                {
                                    if (item != _hoveredItem)
                                        _hoverTimeStart = DateTime.Now;
                                    _hoveredItem = item as ContextMenuItem;
                                    _hoveredItemIndex = i - 1;
                                }

                                if (@event.Type == SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP && @event.button.Button == SDLButton.SDL_BUTTON_LEFT)
                                {
                                    if (item == _hoveredItem)
                                        _hoverTimeStart = DateTime.MinValue;

                                    if (item is not ContextMenuItem menuItem || menuItem.Items.Count == 0)
                                    {
                                        if (_listener.OnItemPressed(item))
                                            Close();
                                    }
                                }

                                return;
                            }
                        }

                        break;
                    }
                }

                _hoveredItem = null;
                
                if (@event.Type == SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP && @event.button.Button == SDLButton.SDL_BUTTON_LEFT)
                    Close();
            }
            else if (@event.Type == SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE && (uint)@event.motion.windowID == _window.WindowId)
            {
                _hoveredItem = null;
                _hoveredItemIndex = -1;
            }
        }

        public void Close()
        {
            UIManager.Instance.ContextMenu.CloseCurrentContextMenu();
        }

        public void AddStateFlags(UIStateFlags flags) => _stateFlags |= flags;
        public void RemoveStateFlags(UIStateFlags flags) => _stateFlags &= ~flags;

        public Rect HostMetrics => new Rect(Int2.Zero, _window.ClientSize);
        public Rect ContentMetrics => new Rect(Int2.Zero, _window.ClientSize);

        public Boundaries InvalidVisualRegion => _invalidVisualRegion;

        public Window? HostWindow => _window;
        public RHITexture? HostTexture => _texture;

        public UIStateFlags InvalidationFlags => _stateFlags;

        private static readonly Color s_normalColor = Color.FromHex("242424");
        private static readonly Color s_hoveredColor = Color.FromHex("6d6d6d");
        private static readonly Color s_strokeColor = Color.FromHex("1b1b1b");

        private readonly record struct VisibleItem(Vector2 Position, ContextMenuItem Item);
    }
}
