using CommunityToolkit.HighPerformance;
using Editor.LegacyGui.Data;
using Editor.LegacyGui.Managers;
using Editor.Rendering.Gui;
using Primary.Common;
using Primary.Rendering;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using static Primary.Rendering.Window;
using Boundaries = Editor.LegacyGui.Data.Boundaries;
using RHI = Primary.RHI;

namespace Editor.LegacyGui.Elements
{
    internal sealed class DockSpace : Element, IDisposable
    {
        private List<DockedDockSpaceData> _docks;
        private List<WindowTabData> _tabs;

        private Vector2 _position;
        private Vector2 _size;

        private DockPosition _currentDockPosition;

        private Vector2 _dockSpace;

        private int _draggingTabActive;
        private Vector2 _tabHitPosition;

        private bool _isDockDragActive;
        private Vector2 _windowPositionStart;
        private Vector2 _dockDragMouseOrigin;

        private DockSpace? _dockSpaceAttempting;
        private DockPosition _dockSpacePosition;

        private Window? _rootWindow;

        private Window? _window;
        private RHI.SwapChain? _swapChain;

        private RHI.RenderTarget _renderTarget;

        public DockSpace(bool createFloating)
        {
            _docks = new List<DockedDockSpaceData>();
            _tabs = new List<WindowTabData>();

            _position = new Vector2(200.0f);
            _size = new Vector2(300.0f);

            _currentDockPosition = DockPosition.Tabbed;

            _dockSpace = _size;

            _draggingTabActive = -1;

            if (createFloating)
            {
                _window = Editor.GlobalSingleton.WindowManager.CreateWindow("FLOATING_DOCKSPACE", _size, CreateWindowFlags.Borderless | CreateWindowFlags.Resizable);
                _swapChain = Editor.GlobalSingleton.RenderingManager.SwapChainCache.GetOrAddDefault(_window);

                _position = Vector2.Zero;

                _window.WindowResized += (x) => ChangeSize(x);
                _window.WindowMoved += (x) => ChangePosition(x);

                _window.HitTest = WindowHitTest;
            }
            else
            {
                _window = null;
                _swapChain = null;
            }

            _rootWindow = _window;

            _renderTarget = RenderingManager.Device.CreateRenderTarget(new RHI.RenderTargetDescription
            {
                ColorFormat = RHI.RenderTargetFormat.RGB10A2un,
                DepthFormat = RHI.DepthStencilFormat.Undefined,
                Dimensions = new Size((int)_size.X, (int)_size.Y),
                ShaderVisibility = RHI.RenderTargetVisiblity.Color
            });

            //EditorGuiManager.Instance.NewWindowFocused += NewWindowFocused;

            RecalculateLayout();
        }

        private void NewWindowFocused(UIWindow? obj)
        {
            EditorGuiManager.Instance.DockingManager.SetDockSpaceNewFocus(this);
        }

        public void Dispose()
        {
            if (_window != null)
            {
                Editor.GlobalSingleton.RenderingManager.SwapChainCache.DestroySwapChain(_window);
                Editor.GlobalSingleton.WindowManager.DestroyWindow(_window);

                _swapChain = null;
                _window = null;
            }

            _rootWindow = null;
            _renderTarget?.Dispose();

            _renderTarget = null;
        }

        protected override void Destroyed()
        {
            Dispose();
        }

        public override void DrawVisual(GuiCommandBuffer commandBuffer)
        {
            commandBuffer.DrawSolidRect(Vector2.Zero, new Vector2(_size.X, 24.0f), EdGuiColors.TabBackground);
            commandBuffer.DrawSolidRect(new Vector2(0.0f, 24.0f), new Vector2(_size.X, 26.0f), EdGuiColors.HeaderForeground);

            float tabMiddleY = 20.0f;
            float xPosition = 0.0f;
            for (int i = 0; i < _tabs.Count; i++)
            {
                WindowTabData data = _tabs[i];
                if (data.Window != null)
                {
                    Vector2 cursor = new Vector2(data.Bounds.Minimum.X + xPosition, 0.0f);
                    if (_draggingTabActive == i)
                        cursor.X = _tabHitPosition.X;

                    commandBuffer.DrawSolidRect(new Vector2(cursor.X, 0.0f), new Vector2(cursor.X + data.TabWidth, 24.0f), EdGuiColors.HeaderForeground);
                    if (data.Window.IsFocused)
                        commandBuffer.DrawSolidRect(new Vector2(cursor.X, 0.0f), new Vector2(cursor.X + data.TabWidth, 2.0f), EdGuiColors.Active);

                    commandBuffer.DrawRegularText(cursor + new Vector2(data.TabWidth - data.TabWidth * 0.5f - data.TextWidth * 0.5f, tabMiddleY), data.Window.Title, 18.0f, Color.White);

                    xPosition += data.TabWidth + 2.0f;
                }
            }

            if (_dockSpaceAttempting != null)
            {
                Vector2 halfSize = _size * 0.5f;

                {
                    commandBuffer.DrawSolidRect(new Vector2(halfSize.X - 30.0f, 10.0f), new Vector2(halfSize.X + 30.0f, 50.0f), _dockSpacePosition == DockPosition.OuterTop ? EdGuiColors.Active : EdGuiColors.ActiveBackground);
                    commandBuffer.DrawSolidRect(new Vector2(halfSize.X - 30.0f, _size.Y - 10.0f), new Vector2(halfSize.X + 30.0f, _size.Y - 50.0f), _dockSpacePosition == DockPosition.OuterBottom ? EdGuiColors.Active : EdGuiColors.ActiveBackground);

                    commandBuffer.DrawSolidRect(new Vector2(10.0f, halfSize.Y - 20.0f), new Vector2(70.0f, halfSize.Y + 20.0f), _dockSpacePosition == DockPosition.OuterLeft ? EdGuiColors.Active : EdGuiColors.ActiveBackground);
                    commandBuffer.DrawSolidRect(new Vector2(_size.X - 10.0f, halfSize.Y - 20.0f), new Vector2(_size.X - 70.0f, halfSize.Y + 20.0f), _dockSpacePosition == DockPosition.OuterRight ? EdGuiColors.Active : EdGuiColors.ActiveBackground);
                }

                {
                    commandBuffer.DrawSolidRect(halfSize + new Vector2(-30.0f, -30.0f), halfSize + new Vector2(30.0f, -70.0f), _dockSpacePosition == DockPosition.Top ? EdGuiColors.Active : EdGuiColors.ActiveBackground);
                    commandBuffer.DrawSolidRect(halfSize + new Vector2(-30.0f, 30.0f), halfSize + new Vector2(30.0f, 70.0f), _dockSpacePosition == DockPosition.Bottom ? EdGuiColors.Active : EdGuiColors.ActiveBackground);

                    commandBuffer.DrawSolidRect(halfSize + new Vector2(-40.0f, -20.0f), halfSize + new Vector2(-100.0f, 20.0f), _dockSpacePosition == DockPosition.Left ? EdGuiColors.Active : EdGuiColors.ActiveBackground);
                    commandBuffer.DrawSolidRect(halfSize + new Vector2(40.0f, -20.0f), halfSize + new Vector2(100.0f, 20.0f), _dockSpacePosition == DockPosition.Right ? EdGuiColors.Active : EdGuiColors.ActiveBackground);

                    commandBuffer.DrawSolidRect(halfSize + new Vector2(-30.0f, -20.0f), halfSize + new Vector2(30.0f, 20.0f), _dockSpacePosition == DockPosition.Tabbed ? EdGuiColors.Active : EdGuiColors.ActiveBackground);
                }
            }

            if (_draggingTabActive != -1)
            {
                float xPos = 0.0f;
                int maxIndex = 0;

                float checkX = MathF.Max(_tabHitPosition.X + _tabs[_draggingTabActive].TabWidth * 0.5f, 0.0f);

                for (int i = 0; i < _tabs.Count; i++)
                {
                    WindowTabData data = _tabs[i];
                    if (data.Window != null)
                    {
                        float xNext = xPos;

                        if (checkX > xNext)
                            commandBuffer.DrawSolidRect(new Vector2(xNext - 0.5f, 0.0f), new Vector2(xNext + 0.5f, 24.0f), new Color(0.0f, 1.0f, 0.0f));
                        else
                            commandBuffer.DrawSolidRect(new Vector2(xNext - 0.5f, 0.0f), new Vector2(xNext + 0.5f, 24.0f), new Color(1.0f, 0.0f, 0.0f));
                    }

                    xPos += data.TabWidth + 2.0f;
                }

                commandBuffer.DrawSolidRect(new Vector2(checkX - 0.5f, 0.0f), new Vector2(checkX + 0.5f, 24.0f), new Color(0.0f, 0.0f, 1.0f));
            }

            //debug
            //commandBuffer.DrawSolidRect(Vector2.Zero, Vector2.Zero + _size, new Color(0.0f, 1.0f, 1.0f, 0.1f));

            if (_tabs.Count > 0)
                commandBuffer.DrawSolidRect(_tabs[0].Bounds.Minimum, _tabs[0].Bounds.Maximum, new Color(1.0f, 0.0f, 0.0f, 0.1f));
        }

        internal void UpdateDockingData(DockSpace? dockSpace, DockPosition position)
        {
            _dockSpaceAttempting = dockSpace;
            _dockSpacePosition = position;
        }

        public override bool HandleEvent(ref readonly UIEvent @event)
        {
            if (@event.Type == UIEventType.MouseButtonDown)
            {
                if (@event.Mouse.Button == UIMouseButton.Left && @event.Hit.Y <= 32.0f)
                {
                    if (@event.Hit.Y <= 26.0f)
                    {
                        float xPos = 0.0f;
                        for (int i = 0; i < _tabs.Count; i++)
                        {
                            WindowTabData data = _tabs[i];
                            if (data.Window != null)
                            {
                                if (@event.Hit.X >= xPos && @event.Hit.X <= xPos + data.TabWidth)
                                {
                                    Vector2 hitStart = @event.Hit;
                                    float xIndexStart = xPos;

                                    GuiDragManager.TryStartDrag(this, _window!, @event.Hit, (x) =>
                                    {
                                        EditorGuiManager.Instance.EventManager.SetExclusiveWindow(_window);

                                        _tabHitPosition = x.Position - new Vector2(x.Start.X - xIndexStart, 0.0f);
                                        WindowDragCallback(data, x);
                                    });

                                    EditorGuiManager.Instance.SwitchWindowFocus(data.Window);
                                    return true;
                                }

                                xPos += data.TabWidth;
                            }
                        }

                        ForceMoveOperation(null, Vector2.Zero);
                        return true;
                    }
                }
            }
            else if (@event.Type == UIEventType.MouseButtonUp)
            {
                if (@event.Mouse.Button == UIMouseButton.Left)
                {
                    GuiDragManager.CancelDrag(this);
                    _draggingTabActive = -1;

                    return true;
                }
            }

            return false;
        }

        private HitTestResult WindowHitTest(Window window, Vector2 point)
        {
            if (_draggingTabActive != -1)
                return HitTestResult.Normal;

            if (point.Y < 6.0f)
                return point.X < 6.0f ? HitTestResult.ResizeTopLeft : (point.X > window.ClientSize.X - 6.0f ? HitTestResult.ResizeTopRight : HitTestResult.ResizeTop);
            else if (point.Y > window.ClientSize.Y - 6.0f)
                return point.X < 6.0f ? HitTestResult.ResizeBottomLeft : (point.X > window.ClientSize.X - 6.0f ? HitTestResult.ResizeBottomRight : HitTestResult.ResizeBottom);
            else if (point.X < 6.0f)
                return HitTestResult.ResizeLeft;
            else if (point.X > window.ClientSize.X - 6.0f)
                return HitTestResult.ResizeRight;

            if (point.Y <= 26.0f)
            {
                float xPos = 0.0f;
                for (int i = 0; i < _tabs.Count; i++)
                {
                    WindowTabData data = _tabs[i];
                    if (data.Window != null)
                    {
                        if (point.X >= xPos && point.X <= xPos + data.TabWidth)
                        {
                            return HitTestResult.Normal;
                        }

                        xPos += data.TabWidth;
                    }
                }

                //return HitTestResult.Draggable;
            }

            return HitTestResult.Normal;
        }

        private void WindowDragCallback(WindowTabData dragging, GuiDragData x)
        {
            if (x.IsEnding)
            {
                _draggingTabActive = -1;
                EditorGuiManager.Instance.EventManager.SetExclusiveWindow(null);
                return;
            }

            if (x.Position.Y < -6.0f || x.Position.Y > 30.0f || x.Position.X < -6.0f || x.Position.X > _window!.ClientSize.X + 36.0f)
            {
                Vector2 offset = x.Offset;

                if (_tabs.Count == 1)
                {
                    GuiDragManager.CancelDrag(this);
                    ForceMoveOperation(dragging.Window!, offset);
                    return;
                }

                DockSpace? newDockSpace = EditorGuiManager.Instance.DockingManager.SplitWindowIntoEmptyDock(this, dragging.Window!);

                GuiDragManager.CancelDrag(this);
                (newDockSpace ?? this).ForceMoveOperation(dragging.Window!, offset);
            }

            int indexOf = _tabs.FindIndex((x) => x.Window == dragging.Window);
            if (indexOf == -1)
            {
                GuiDragManager.CancelDrag(this);
                return;
            }

            float checkX = MathF.Max(_tabHitPosition.X + _tabs[indexOf].TabWidth * 0.5f, 0.0f);

            int maxIndex = CheckForTabDragIndex(checkX);

            Log.Information("{x}", maxIndex);

            if (indexOf != maxIndex)
            {
                dragging = _tabs[indexOf];

                _tabs.Remove(dragging);
                _tabs.Insert(maxIndex, dragging);

                if (CheckForTabDragIndex(checkX) == maxIndex)
                    indexOf = maxIndex;
            }

            _draggingTabActive = indexOf;
        }

        private int CheckForTabDragIndex(float checkX)
        {
            float xPos = 0.0f;
            int maxIndex = 0;

            for (int i = 0; i < _tabs.Count; i++)
            {
                WindowTabData data = _tabs[i];
                if (data.Window != null)
                {
                    float xNext = xPos;

                    if (checkX > xNext)
                        maxIndex = i;
                    else
                        break;
                }

                xPos += data.TabWidth + 2.0f;
            }

            return maxIndex;
        }

        private void WindowMoveCallback(GuiDragData x)
        {
            if (x.IsEnding)
            {
                _isDockDragActive = false;

                EditorGuiManager.Instance.EventManager.SetExclusiveWindow(null);
                EditorGuiManager.Instance.DockingManager.CommitAttemptedDock(this);
                return;
            }

            if (_parent != null || _window == null)
            {
                GuiDragManager.CancelDrag(this);
                return;
            }

            if (_window != null)
            {
                _window.Position = x.Offset + _windowPositionStart;

                EditorGuiManager.Instance.DockingManager.UpdateDockPosition(this, _window.Position);
            }
        }

        public override void InvalidateLayout()
        {
            if (!_layoutInvalid)
            {
                _layoutInvalid = true;
                RecalculateLayout();
            }
        }

        public override bool RecalculateLayout()
        {
            Vector2 offset = Vector2.Zero;
            _dockSpace = _size;

            for (int i = 0; i < _docks.Count; i++)
            {
                DockedDockSpaceData dock = _docks[i];
                if (dock.Element != null)
                {
                    DockSpace space = dock.Element;

                    Vector2 size = Vector2.Zero;

                    switch (dock.Position)
                    {
                        case DockPosition.Left:
                            {
                                space._position = Vector2.Zero;
                                size = new Vector2(space._size.X, _size.Y);

                                offset.X += size.X;
                                break;
                            }
                        case DockPosition.Right:
                            {
                                space._position = new Vector2(_dockSpace.X - space._size.X, 0.0f);
                                size = new Vector2(space._size.X, _size.Y);

                                _dockSpace.X -= size.X;
                                break;
                            }
                        case DockPosition.Top:
                            {
                                space._position = Vector2.Zero;
                                size = new Vector2(_size.X, space._size.X);

                                offset.Y += size.Y;
                                break;
                            }
                        case DockPosition.Bottom:
                            {
                                space._position = new Vector2(0.0f, _dockSpace.Y - space._size.Y);
                                size = new Vector2(_size.X, space._size.Y);

                                _dockSpace.Y -= size.Y;
                                break;
                            }
                    }

                    dock.Element.ChangeSize(size);
                    dock.Element.InvalidateLayout();
                }
            }

            _dockSpace -= offset;

            GuiFont font = EditorGuiManager.Instance.DefaultFont;

            Span<WindowTabData> tabs = _tabs.AsSpan();
            for (int i = 0; i < tabs.Length; i++)
            {
                ref WindowTabData dockData = ref tabs[i];
                if (dockData.Window != null)
                {
                    dockData.Bounds = new Boundaries(offset + new Vector2(0.0f, 26.0f), _dockSpace);
                    dockData.Window.SetBoundaries(dockData.Bounds);

                    dockData.TextWidth = font.CalculateTextWidth(dockData.Window.Title, 18.0f);
                    dockData.TabWidth = dockData.TextWidth + 14.0f;
                }
            }

            _layoutInvalid = false;
            return true;
        }

        internal void DockNewWindow(UIWindow window)
        {
            window.CurrentDockSpace?.RemoveDockedWindow(window);

            if (!_tabs.Exists((x) => x.Window == window))
            {
                _tabs.Add(new WindowTabData
                {
                    Window = window
                });

                InvalidateLayout();
            }
        }

        internal void DockDockSpace(DockSpace dockSpace, DockPosition position)
        {
            if (dockSpace.Parent == this || _docks.Exists((x) => x.Element == dockSpace) || Parent == dockSpace)
                return;

            dockSpace.Parent?.RemoveDockedDockSpace(dockSpace);

            if (position >= DockPosition.OuterLeft)
            {
                _docks.Insert(0, new DockedDockSpaceData
                {
                    Position = position,
                    Element = dockSpace
                });

                position -= 4;
            }
            else
            {
                _docks.Add(new DockedDockSpaceData
                {
                    Position = position,
                    Element = dockSpace
                });
            }

            dockSpace.Parent = this;
            dockSpace._currentDockPosition = position;

            InvalidateLayout();
        }

        internal void RemoveDockedWindow(UIWindow window)
        {
            int index = _tabs.FindIndex((x) => x.Window == window);
            if (index > -1)
                _tabs.RemoveAt(index);
        }

        internal void RemoveDockedDockSpace(DockSpace dockSpace)
        {
            int space = (int)(dockSpace._currentDockPosition - 1);

            int index = _docks.FindIndex((x) => x.Element == dockSpace);
            if (index > -1)
            {
                _docks.RemoveAt(index);

                dockSpace.Parent = null;
            }
        }

        internal void SetFloating(bool isFloating)
        {
            if (isFloating)
            {
                if (_window == null)
                {
                    _window = Editor.GlobalSingleton.WindowManager.CreateWindow("FLOATING_DOCKSPACE", _size, CreateWindowFlags.Borderless | CreateWindowFlags.Resizable);
                    _swapChain = Editor.GlobalSingleton.RenderingManager.SwapChainCache.GetOrAddDefault(_window);

                    _position = _window.Position;

                    _window.WindowResized += (x) => ChangeSize(x);
                    _window.WindowMoved += (x) => ChangePosition(x);

                    _window.HitTest = WindowHitTest;
                }
                else
                {
                    _window.ClientSize = _size;
                    _window.Position = _position;
                }
            }
            else
            {
                if (_window != null)
                {
                    Editor.GlobalSingleton.RenderingManager.SwapChainCache.DestroySwapChain(_window);
                    Editor.GlobalSingleton.WindowManager.DestroyWindow(_window);

                    _swapChain = null;
                    _window = null;
                }
            }

            if (_renderTarget == null)
            {
                _renderTarget = RenderingManager.Device.CreateRenderTarget(new RHI.RenderTargetDescription
                {
                    ColorFormat = RHI.RenderTargetFormat.RGB10A2un,
                    DepthFormat = RHI.DepthStencilFormat.Undefined,
                    Dimensions = new Size((int)_size.X, (int)_size.Y),
                    ShaderVisibility = RHI.RenderTargetVisiblity.Color
                });
            }
        }

        internal void ClearRemainingDockData()
        {
            for (int i = 0; i < _docks.Count; i++)
            {
                throw new NotImplementedException();
                _docks[i].Element?.ClearRemainingDockData();
            }

            _docks.Clear();
            _tabs.Clear();

            Dispose();
        }

        internal void ForceMoveOperation(UIWindow? window, Vector2 dragOffset)
        {
            if (_parent != null)
                return;

            Debug.Assert(_window != null);

            _isDockDragActive = true;
            _windowPositionStart = _window.Position;
            _dockDragMouseOrigin = EditorGuiManager.Instance.EventManager.GetMousePosition(_window!) - dragOffset;

            EditorGuiManager.Instance.EventManager.SetExclusiveWindow(_window);
            GuiDragManager.ForceStartDrag(this, _window, _dockDragMouseOrigin, WindowMoveCallback);
        }

        protected override void SetParent(Element? newParent)
        {
            if (newParent is DockSpace || newParent == null)
            {
                base.SetParent(newParent);

                _rootWindow = ((DockSpace?)_parent)?.RootWindow;
            }
        }

        private void ChangeSize(Vector2 newSize)
        {
            if (_size == newSize)
                return;

            Size size = new Size((int)newSize.X, (int)newSize.Y);
            if (_renderTarget.Description.Dimensions != size)
            {
                _renderTarget?.Dispose();
                _renderTarget = RenderingManager.Device.CreateRenderTarget(new RHI.RenderTargetDescription
                {
                    ColorFormat = RHI.RenderTargetFormat.RGB10A2un,
                    DepthFormat = RHI.DepthStencilFormat.Undefined,
                    Dimensions = size,
                    ShaderVisibility = RHI.RenderTargetVisiblity.Color
                });
            }

            _swapChain?.Resize(newSize);

            _size = newSize;
            RecalculateLayout();
        }

        private void ChangePosition(Vector2 newPosition)
        {
            if (Position == newPosition)
                return;

            if (_window != null)
            {
                _window.Position = newPosition;
                _position = Vector2.Zero;
            }
            else
            {
                _position = newPosition;
                RecalculateLayout();
            }
        }

        public Vector2 Position
        {
            get => _window == null ? _position : _window.Position; private set
            {
                if (_window != null && _window.Position != value)
                {
                    _window.Position = value;
                }
            }
        }

        public Vector2 LocalPosition => _window == null ? _position : Vector2.Zero;
        public Vector2 LocalSize => _dockSpace;

        public Vector2 Size => _size;

        internal new DockSpace? Parent { get => (DockSpace?)_parent; set => SetParent(value); }

        internal RHI.RenderTarget RenderTarget => _renderTarget;

        internal Window? Window => _window;
        internal RHI.SwapChain? SwapChain => _swapChain;

        internal Window? RootWindow => _parent != null ? _rootWindow : _window;

        internal Span<WindowTabData> Tabs => _tabs.AsSpan();

        internal record struct DockedDockSpaceData
        {
            public DockPosition Position;
            public DockSpace? Element;
        }

        internal record struct WindowTabData
        {
            public UIWindow? Window;
            public Boundaries Bounds;

            public float TabWidth;
            public float TextWidth;
        }
    }

    public enum DockPosition : byte
    {
        None = 0,

        Tabbed,

        Left,
        Right,
        Top,
        Bottom,

        OuterLeft,
        OuterRight,
        OuterTop,
        OuterBottom
    }
}
