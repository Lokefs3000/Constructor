using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Diagnostics;
using EditorUI.Assets;
using EditorUI.Built;
using EditorUI.Common;
using EditorUI.Input;
using EditorUI.Text;
using EditorUI.Visual;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Utility;

namespace EditorUI.Dock
{
    public sealed class WindowDock : DockBase, IInteractable
    {
        private readonly DockManager _dockManager;
        private readonly WindowManager _windowManager;

        private DockBase? _parentDock;

        private DockingSide _dockingSide;
        private int _dockingSpace;

        private Rect _dockRect;
        private Rect _windowRect;

        private List<WindowBase> _windows;
        private List<DockBase> _docks;

        private int _activeWindow;

        private int _dockingSpaceDragStart;

        private IAssetProvider<FontFamily>? _fontFamily;
        private FontStyle _fontStyle;
        private FontWeight _fontWeight;

        private float _fontSize;

        private UIColor _backgroundColor;
        private UIColor _tabColor;
        private UIColor _textColor;

        private UIColor _activeTabColor;

        private ushort _strokeWidth;
        private UIColor _strokeColor;

        private Vector4 _tabCornerRadius;

        internal WindowDock(DockFlags flags, DockManager dockManager, WindowManager windowManager) : base(flags)
        {
            _dockManager = dockManager;
            _windowManager = windowManager;

            _parentDock = null;

            _dockingSide = DockingSide.Left;
            _dockingSpace = WindowDock.TabHeight;

            _dockRect = Rect.Zero;
            _windowRect = Rect.Zero;

            _windows = new List<WindowBase>();
            _docks = new List<DockBase>();

            _activeWindow = -1;

            _dockingSpaceDragStart = -1;

            _fontFamily = null;
            _fontStyle = FontStyle.Normal;
            _fontWeight = FontWeight.Normal;

            _fontSize = 16.0f;

            _backgroundColor = Color.White;
            _tabColor = Color.White;
            _textColor = Color.White;

            _activeTabColor = Color.White;

            _tabCornerRadius = Vector4.Zero;
        }

        private void DestroySelf()
        {
            Guard.IsEmpty(_windows);

            if (_parentDock != null)
            {
                int i = 0;
                while (_docks.Count > i)
                {
                    DockBase dock = _docks[i];
                    if (!dock.TryDockInto(_parentDock, dock.Side))
                        ++i;
                }

                _docks.Clear();

                _parentDock = null;
            }

            _parentHost?.UnregisterDock(this);
            _parentHost = null;
        }

        public override bool TryDockInto(DockBase newDock, DockingSide side)
        {
            // fail because we can't be parented to ourselves
            if (newDock == this)
                return false;

            // succeed because this is already valid
            if (newDock == _parentDock)
                return true;

            _parentDock?.TryRemoveDockChild(this);

            if (newDock.TryAddDockChild(this))
            {
                _parentDock = newDock;
                _dockingSide = side;

                if (newDock.Host != _parentHost)
                {
                    _parentHost?.UnregisterDock(this);
                    newDock.Host?.RegisterNewDock(this);

                    AddStateFlags(StateFlags.SelfInvalidStyle);
                }

                AddStateFlags(StateFlags.SelfInvalidLayout);
                return true;
            }
            else
            {
                _parentHost?.UnregisterDock(this);

                _parentDock = null;
                _parentHost = null;
            }

            return false;
        }

        public override bool TryFloat(Int2 targetSize)
        {
            throw new NotImplementedException();
        }

        public override bool TryAddWindow(WindowBase window)
        {
            if (_windows.Count > 0 && _dockFlags.HasFlag(DockFlags.SingleWindow))
                return false;

            if (_windows.AddUnique(window))
            {
                if (_activeWindow == -1)
                    TryFocusWindow(window);

                AddStateFlags(StateFlags.SelfInvalidLayout);
            }

            return true;
        }

        public override bool TryRemoveWindow(WindowBase window)
        {
            int windowIndex = _windows.IndexOf(window);
            if (windowIndex != -1)
            {
                if (_activeWindow == windowIndex)
                {
                    if (_windows.Count == 1)
                        TryFocusWindow(null);
                    else
                        TryFocusWindow(_windows[_activeWindow]);
                }

                _windows.RemoveAt(windowIndex);

                // if (_windows.Count == 0)
                // {
                //     DestroySelf();
                // }

                AddStateFlags(StateFlags.SelfInvalidLayout);
                return true;
            }

            return false;
        }

        public override bool TryFocusWindow(WindowBase? window)
        {
            if (window == null)
            {
                if (_activeWindow != -1)
                {
                    _windowManager.TrySetWindowFocus(null);
                    return true;
                }

                return false;
            }

            int windowIndex = _windows.IndexOf(window);
            if (windowIndex != -1)
            {
                if (_activeWindow != windowIndex)
                {
                    _windowManager.TrySetWindowFocus(_windows[windowIndex]);
                }

                _activeWindow = windowIndex;
                return true;
            }

            return false;
        }

        protected internal override void UpdateData()
        {
            if (_activeWindow != -1 && _windows.Count > 0)
            {
                WindowBase window = _windows[_activeWindow];
                window.UpdateData();

                StateFlags windowStateFlags = window.StateFlags;
                if (windowStateFlags != StateFlags.None)
                {
                    AddStateFlags(windowStateFlags.RemoveFlags(StateFlags.This));
                }
            }
        }

        protected internal override void RecalculateLayout(Rect dockRect)
        {
            _dockRect = dockRect;
            _windowRect = _dockFlags.HasFlag(DockFlags.SingleWindow) ? dockRect : Rect.OffsetMin(dockRect, 0, TabHeight + _strokeWidth);
        }

        protected internal override void PaintVisual(ref readonly PainterContext painter)
        {
            if (_dockFlags.HasFlag(DockFlags.SingleWindow))
                return;

            Boundaries tabLineBoundaries = new Boundaries(_dockRect.Position.AsVector2(), _dockRect.Position.AsVector2() + new Vector2(_dockRect.Width, TabHeight + _strokeWidth));

            if (_backgroundColor.IsVisible)
                painter.AddRectangle(tabLineBoundaries, new Paint(_backgroundColor));

            if (_windows.Count > 0)
            {
                if (_textColor.IsVisible && _fontFamily != null && _fontFamily.Value != null && _fontFamily.IsReadyToUse)
                {
                    TextManager textManager = UIManager.Instance.TextManager;
                    TextBuilder textBuilder = new TextBuilder(200.0f - _fontSize - 4.0f, TextWrapMode.Ellipsis, TextAlignment.BottomLeft, AllowRichText: false);
                    BuiltTextBuilder builtTextBuilder = BuiltTextBuilder.Build(in textBuilder);

                    FontStyleData fontStyleData = _fontFamily.Value!.GetFontStyle(_fontStyle, _fontWeight);
                    float positionX = tabLineBoundaries.Minimum.X;

                    float selectionLineStartX = 0.0f;
                    float selectionLineEndX = 0.0f;

                    float closeSize = _fontSize * 0.75f;
                    float closeMinY = tabLineBoundaries.Minimum.Y + TabHeight * 0.5f - closeSize * 0.5f;
                    float closeMaxY = tabLineBoundaries.Minimum.Y + TabHeight * 0.5f + closeSize * 0.5f;

                    for (int i = 0; i < _windows.Count; i++)
                    {
                        WindowBase windowBase = _windows[i];
                        TextShapingData shapingData = textManager.ShapeText(windowBase.GetType().Name, _fontSize, builtTextBuilder, fontStyleData);

                        float totalWidth = shapingData.TotalSize.X + 8.0f + _fontSize + 4.0f;
                        Boundaries tabBoundaries = new Boundaries(new Vector2(positionX, tabLineBoundaries.Minimum.Y), new Vector2(positionX + totalWidth, tabLineBoundaries.Maximum.Y));

                        if (_activeWindow == i)
                        {
                            if (_activeTabColor.IsVisible)
                            {
                                painter.AddRectangle(tabBoundaries, new Paint(_activeTabColor, _strokeColor, _strokeWidth, StrokePosition.Inside), _tabCornerRadius);

                                selectionLineStartX = positionX;
                                selectionLineEndX = positionX + totalWidth;
                            }
                        }
                        else
                        {
                            if (_tabColor.IsVisible)
                                painter.AddRectangle(tabBoundaries, new Paint(_tabColor), _tabCornerRadius);
                        }

                        painter.AddText(new Vector2(positionX + 4.0f, tabLineBoundaries.Maximum.Y - 6.0f), shapingData, tabBoundaries.Size, new Paint(_textColor));

                        float closePositionBase = positionX + shapingData.TotalSize.X + 8.0f + (_fontSize - closeSize) * 0.5f;
                        CrossLineBuffer lineBuffer = new CrossLineBuffer
                        {
                            Point0 = new Vector2(closePositionBase, closeMinY),
                            Point1 = new Vector2(closePositionBase + closeSize, closeMaxY),
                            Point2 = new Vector2(closePositionBase + closeSize, closeMinY),
                            Point3 = new Vector2(closePositionBase, closeMaxY)
                        };

                        painter.AddLines(MemoryMarshal.CreateReadOnlySpan(ref lineBuffer.Point0, 4), new Paint(_textColor), thickness: 1.5f);

                        positionX += totalWidth + 1.0f;
                    }

                    if (selectionLineStartX != selectionLineEndX)
                    {
                        float y = tabLineBoundaries.Maximum.Y - _strokeWidth;

                        if (selectionLineStartX > tabLineBoundaries.Minimum.X)
                            painter.AddLine(new Vector2(tabLineBoundaries.Minimum.X, y), new Vector2(selectionLineStartX, y), new Paint(_strokeColor), _strokeWidth);
                        if (selectionLineEndX < tabLineBoundaries.Maximum.X)
                            painter.AddLine(new Vector2(selectionLineEndX, y), new Vector2(tabLineBoundaries.Maximum.X, y), new Paint(_strokeColor), _strokeWidth);
                    }
                }
                else
                {

                }
            }
        }

        public bool HandleEventSelf(ref readonly UIInputEvent inputEvent)
        {
            switch (inputEvent.EventType)
            {
                case UIInputEventType.MouseWheel:
                    {
                        int nextWindowIndex = Math.Clamp((int)(-inputEvent.Mouse.Delta.Y + _activeWindow), 0, _windows.Count - 1);
                        if (nextWindowIndex != _activeWindow)
                        {
                            TryFocusWindow(_windows[nextWindowIndex]);
                        }

                        return true;
                    }

                case UIInputEventType.DragBegin:
                    {
                        if (inputEvent.Drag.Button == MouseButton.Left)
                        {
                            _dockingSpaceDragStart = _dockingSpace;
                        }

                        return true;
                    }
                case UIInputEventType.DragUpdate:
                case UIInputEventType.DragEnd:
                    {
                        if (inputEvent.Drag.Button == MouseButton.Left && _dockingSpaceDragStart != -1)
                        {
                            int newDockingSpace = _dockingSpaceDragStart - (int)((_dockingSide == DockingSide.Left || _dockingSide == DockingSide.Right) ? inputEvent.Drag.Delta.X : inputEvent.Drag.Delta.Y);
                            
                            if (newDockingSpace != _dockingSpace)
                                Space = newDockingSpace;

                            if (inputEvent.EventType == UIInputEventType.DragEnd)
                                _dockingSpaceDragStart = -1;
                        }

                        return true;
                    }
            }

            return false;
        }

        protected internal override bool TrySetDockHost(DockHost? newHost)
        {
            if (_parentHost == newHost)
                return true;

            _parentHost?.UnregisterDock(this);
            _parentHost = newHost;

            return true;
        }

        protected internal override bool TryAddDockChild(DockBase child)
        {
            // this dock cannot be docked into
            if (_dockFlags.HasFlag(DockFlags.NoDocking))
                return false;

            if (child == this)
                return false;

            if (_docks.AddUnique(child))
                AddStateFlags(StateFlags.SelfInvalidLayout);

            return true;
        }

        protected internal override bool TryRemoveDockChild(DockBase child)
        {
            if (child == this)
                return false;

            if (_docks.Remove(child))
            {
                AddStateFlags(StateFlags.SelfInvalidLayout);
                return true;
            }

            return false;
        }

        protected internal override void TryUpdateWindowFocus(WindowBase window)
        {
            if (_windows.Contains(window))
            {
                _parentHost?.FocusDockHost();
            }
        }

        public override void AddStateFlags(StateFlags flags)
        {
            base.AddStateFlags(flags);

            if (_parentDock != null)
                _parentDock.AddStateFlags(flags);
        }

        public IInteractable GetInteractable(Vector2 point) => this;

        public IInteractionShape? Shape => null;
        public WidgetInputState InputState => WidgetInputState.Sink;

        public override Rect DockRect => _dockRect;
        public override Rect WindowRect => _windowRect;

        public override DockBase? Parent => _parentDock;

        public override DockingSide Side
        {
            get => _dockingSide;
            set
            {
                if (_dockingSide != value)
                {
                    _dockingSide = value;
                    AddStateFlags(StateFlags.SelfInvalidLayout);
                }
            }
        }
        public override int Space
        {
            get => _dockingSpace;
            set
            {
                int clampedSpace = Math.Max(value, WindowDock.TabHeight);
                if (_dockingSpace != clampedSpace)
                {
                    _dockingSpace = clampedSpace;
                    AddStateFlags(StateFlags.SelfInvalidLayout);
                }
            }
        }

        public override ROList<WindowBase> Windows => _windows;
        public override int ActiveWindow => _activeWindow;

        public override ROList<DockBase> Docks => _docks;

        #region Serializable
        [Styled(nameof(_fontFamily))] public IAssetProvider<FontFamily>? FontFamily { get => _fontFamily; set => SetStyledField(value); }
        [Styled(nameof(_fontStyle))] public FontStyle FontStyle { get => _fontStyle; set => SetStyledField(value); }
        [Styled(nameof(_fontWeight))] public FontWeight FontWeight { get => _fontWeight; set => SetStyledField(value); }

        [Styled(nameof(_fontSize))] public float FontSize { get => _fontSize; set => SetStyledField(value); }

        [Styled(nameof(_backgroundColor))] public UIColor BackgroundColor { get => _backgroundColor; set => SetStyledField(value); }
        [Styled(nameof(_tabColor))] public UIColor TabColor { get => _tabColor; set => SetStyledField(value); }
        [Styled(nameof(_textColor))] public UIColor TextColor { get => _textColor; set => SetStyledField(value); }

        [Styled(nameof(_activeTabColor))] public UIColor ActiveTabColor { get => _activeTabColor; set => SetStyledField(value); }

        [Styled(nameof(_strokeWidth))] public ushort StrokeWidth { get => _strokeWidth; set => SetStyledField(value); }
        [Styled(nameof(_strokeColor))] public UIColor StrokeColor { get => _strokeColor; set => SetStyledField(value); }

        [Styled(nameof(_tabCornerRadius))] public Vector4 TabCornerRadius { get => _tabCornerRadius; set => SetStyledField(value); }
        #endregion

        public const int TabHeight = 24;

        private record struct CrossLineBuffer
        {
            public Vector2 Point0;
            public Vector2 Point1;
            public Vector2 Point2;
            public Vector2 Point3;
        }
    }
}
