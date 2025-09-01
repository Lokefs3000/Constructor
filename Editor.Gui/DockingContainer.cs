using CommunityToolkit.HighPerformance;
using Editor.Gui.Elements;
using Editor.Gui.Events;
using Editor.Gui.Graphics;
using Primary.Common;
using Primary.Rendering;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using RHI = Primary.RHI;

namespace Editor.Gui
{
    public sealed class DockingContainer : IDisposable
    {
        private Window? _window;

        private DockingContainer? _containgDock;
        private DockSpace _containedDockSpace;

        private List<DockingContainer> _dockedContainers;
        private List<EditorWindow> _tabbedWindows;

        private int _activeTabbedWindow;

        private RHI.RenderTarget? _renderTarget;

        private Vector2 _position;
        private Vector2 _size;

        private Boundaries _elementSpace;

        private bool _isHoveringResize = false;

        internal DockingContainer(Window? window = null)
        {
            _window = window;

            _containgDock = null;
            _containedDockSpace = DockSpace.None;

            _dockedContainers = new List<DockingContainer>();
            _tabbedWindows = new List<EditorWindow>();

            _activeTabbedWindow = -1;

            _renderTarget = null;

            _position = Vector2.Zero;
            _size = window?.ClientSize ?? Vector2.Zero;

            _elementSpace = Boundaries.Zero;

            if (window != null)
            {
                window.WindowResized += WindowResizeCallback;
            }

            RecomputeDockingSpace();
        }

        private void WindowResizeCallback(Vector2 newSize)
        {
            if (_size != newSize)
            {
                _size = newSize;

                RecomputeDockingSpace();
                RecreateRenderTarget();
            }
        }

        public void Dispose()
        {
            if (_window != null)
                _window.WindowResized -= WindowResizeCallback;

            _renderTarget?.Dispose();
            _renderTarget = null;
        }

        internal void DrawBackgroundVisuals(GuiCommandBuffer commandBuffer)
        {
            Vector2 elementSize = _elementSpace.Size;

            commandBuffer.FillRectangle(Vector2.Zero, elementSize, GuiColors.Background);

            if (_containgDock != null)
            {
                switch (_containedDockSpace)
                {
                    case DockSpace.Left:
                        {
                            commandBuffer.FillRectangle(new Vector2(elementSize.X - 1.0f, 0.0f), elementSize, GuiColors.TabBackground);
                            break;
                        }
                    case DockSpace.Right:
                        {
                            commandBuffer.FillRectangle(Vector2.Zero, new Vector2(1.0f, elementSize.Y), GuiColors.TabBackground);
                            break;
                        }
                    case DockSpace.Top:
                        {
                            commandBuffer.FillRectangle(new Vector2(0.0f, elementSize.Y - 1.0f), elementSize, GuiColors.TabBackground);
                            break;
                        }
                    case DockSpace.Bottom:
                        {
                            commandBuffer.FillRectangle(Vector2.Zero, new Vector2(elementSize.X, 1.0f), GuiColors.TabBackground);
                            break;
                        }
                }
            }

            if (_tabbedWindows.Count > 0)
            {
                commandBuffer.FillRectangle(Vector2.Zero, new Vector2(elementSize.X, 18.0f), GuiColors.TabBackground);

                float xOffset = 0.0f;
                int overflowIndex = -1;

                for (int i = 0; i < _tabbedWindows.Count; i++)
                {
                    float textWidth = EditorGuiManager.Instance.DefaultFont.CalculateTextWidth(_tabbedWindows[i].Title, 14.0f);
                    float tabWidth = textWidth + 8.0f;

                    if (xOffset + tabWidth > elementSize.X - 8.0f)
                    {
                        overflowIndex = i;
                        break;
                    }

                    commandBuffer.FillRectangleRounded(new Vector2(xOffset, 0.0f), new Vector2(xOffset + tabWidth, 18.0f), GuiColors.HeaderForeground, 4.0f, CornerRoundingFlags.Top);

                    if (_activeTabbedWindow == i)
                    {
                        commandBuffer.FillRectangleRounded(new Vector2(xOffset, 0.0f), new Vector2(xOffset + tabWidth, 4.0f), GuiColors.Active, 4.0f, CornerRoundingFlags.Top);
                        commandBuffer.FillRectangleRounded(new Vector2(xOffset, 1.0f), new Vector2(xOffset + tabWidth, 5.0f), GuiColors.HeaderForeground, 4.0f, CornerRoundingFlags.Top);
                    }

                    commandBuffer.Text(new Vector2(xOffset + tabWidth * 0.5f - textWidth * 0.5f, 1.0f), _tabbedWindows[i].Title, 14.0f, GuiColors.Text);

                    xOffset += tabWidth;
                }

                if (overflowIndex >= 0 || true)
                {
                    commandBuffer.FillRectangle(new Vector2(elementSize.X - 14.0f, 4.0f), new Vector2(elementSize.X - 4.0f, 14.0f), Color.White);
                }
            }
        }

        internal bool ProcessEvent(ref readonly UIEvent @event)
        {
            if (UIDragManager.CurrentDragContext == this)
            {
                if (@event.Type == UIEventType.MouseButtonUp)
                {
                    UIEventManager.SetCursor(UICursorType.Default);
                    UIEventManager.SetGlobalEvents(this, false);
                    UIDragManager.CancelDrag(this);
                }

                return true;
            }

            if (_containgDock != null)
            {
                if (@event.Type == UIEventType.MouseMotion || @event.Type == UIEventType.MouseButtonDown)
                {
                    bool isHovered = _containedDockSpace switch
                    {
                        DockSpace.Left => @event.MouseHit.X >= _elementSpace.Size.X - 4.0f,
                        DockSpace.Right => @event.MouseHit.X <= 4.0f,
                        DockSpace.Top => @event.MouseHit.Y >= _elementSpace.Size.Y - 4.0f,
                        DockSpace.Bottom => @event.MouseHit.X <= 4.0f,
                        _ => false
                    };

                    _isHoveringResize = isHovered;
                    UIEventManager.SetCursor(isHovered ? _containedDockSpace switch
                    {
                        DockSpace.Left => UICursorType.WResize,
                        DockSpace.Right => UICursorType.EResize,
                        DockSpace.Top => UICursorType.SResize,
                        DockSpace.Bottom => UICursorType.NResize,
                        _ => UICursorType.Default,
                    } : UICursorType.Default);

                    if (isHovered)
                    {
                        if (@event.Type == UIEventType.MouseButtonDown)
                        {
                            Vector2 startSize = _size;
                            UIDragManager.SetupDrag(this, (x) =>
                            {
                                UIEventManager.SetGlobalEvents(this, true);

                                switch (_containedDockSpace)
                                {
                                    case DockSpace.Left:
                                        {
                                            _size.X = MathF.Max(startSize.X + x.MouseOffset.X, 50.0f);
                                            
                                            _containgDock!.RecomputeDockingSpace();
                                            RecomputeDockingSpace();
                                            break;
                                        }
                                    case DockSpace.Right:
                                        {
                                            break;
                                        }
                                    case DockSpace.Top:
                                        {
                                            break;
                                        }
                                    case DockSpace.Bottom:
                                        {
                                            break;
                                        }
                                    default:
                                        {
                                            UIEventManager.SetCursor(UICursorType.Default);
                                            UIEventManager.SetGlobalEvents(this, false);
                                            UIDragManager.CancelDrag(this);
                                            break;
                                        }
                                }
                            }, 0.0f);
                        }
                    }
                    else
                        UIDragManager.CancelDrag(this);

                    return true;
                }
                else if (@event.Type == UIEventType.MouseLeave || @event.Type == UIEventType.MouseButtonUp)
                {
                    if (_isHoveringResize)
                    {
                        UIEventManager.SetCursor(UICursorType.Default);
                        UIEventManager.SetGlobalEvents(this, false);
                        UIDragManager.CancelDrag(this);
                    }

                    _isHoveringResize = false;
                }
            }

            return false;
        }

        internal void RecomputeDockingSpace()
        {
            _elementSpace = new Boundaries(Vector2.Zero, _size);

            /*if (_containgDock != null)
            {
                switch (_containedDockSpace)
                {
                    case DockSpace.Left: _elementSpace.Minimum.X += 1.0f; break;
                    case DockSpace.Right: _elementSpace.Maximum.X -= 1.0f; break;
                    case DockSpace.Top: _elementSpace.Minimum.Y += 1.0f; break;
                    case DockSpace.Bottom: _elementSpace.Maximum.Y -= 1.0f; break;
                }
            }*/

            for (int i = 0; i < _dockedContainers.Count; i++)
            {
                DockingContainer container = _dockedContainers[i];
                switch (container._containedDockSpace)
                {
                    case DockSpace.Left:
                        {
                            container._position = _elementSpace.Minimum;
                            container.Size = new Vector2(container._size.X, _elementSpace.Size.Y);

                            _elementSpace.Minimum.X += container._size.X;
                            break;
                        }
                    case DockSpace.Right:
                        {
                            container._position = new Vector2(_elementSpace.Maximum.X - container._size.X, _elementSpace.Minimum.Y);
                            container.Size = new Vector2(container._size.X, _elementSpace.Size.Y);

                            _elementSpace.Maximum.X -= container._size.X;
                            break;
                        }
                    case DockSpace.Top:
                        {
                            container._position = _elementSpace.Minimum;
                            container.Size = new Vector2(_elementSpace.Size.X, container._size.Y);

                            _elementSpace.Minimum.Y += container._size.Y;
                            break;
                        }
                    case DockSpace.Bottom:
                        {
                            container._position = new Vector2(_elementSpace.Minimum.X, _elementSpace.Maximum.Y - container._size.Y);
                            container.Size = new Vector2(_elementSpace.Size.X, container._size.Y);

                            _elementSpace.Maximum.Y -= container._size.Y;
                            break;
                        }
                }
            }

            if (_renderTarget == null || _renderTarget.Description.Dimensions.AsVector2() != _elementSpace.Size)
            {
                RecreateRenderTarget();
            }

            //if (_tabbedWindows.Count > 0)
            //{
            //    _elementSpace.Minimum.Y += 18.0f;
            //}

            Vector2 size = _elementSpace.Size;
            for (int i = 0; i < _tabbedWindows.Count; i++)
            {
                _tabbedWindows[i].SetNewLayoutSpace(size);
            }
        }

        internal void DockContainer(DockingContainer container, DockSpace space)
        {
            Debug.Assert(container._window == null);

            if (!_dockedContainers.Contains(container))
            {
                container._window = _window;
                container._containgDock = this;
                container._containedDockSpace = space;

                _dockedContainers.Add(container);
            }
        }

        internal void UndockContainer(DockingContainer container)
        {
            Debug.Assert(container._window == _window);

            int index = _dockedContainers.IndexOf(container);
            if (index >= 0)
            {
                container._window = null;
                container._containgDock = null;
                container._containedDockSpace = DockSpace.None;

                _dockedContainers.RemoveAt(index);
            }
        }

        internal void DockWindowAsTab(EditorWindow window)
        {
            if (!_tabbedWindows.Contains(window))
            {
                window.SetNewDockingContainer(this);

                _tabbedWindows.Add(window);
                if (_tabbedWindows.Count == 1)
                    _activeTabbedWindow = 0;
            }
        }

        internal void UndockWindowFromTab(EditorWindow window)
        {
            int index = _tabbedWindows.IndexOf(window);
            if (index >= 0)
            {
                window.SetNewDockingContainer(null);

                _tabbedWindows.RemoveAt(index);
                if (_tabbedWindows.Count == 0)
                    _activeTabbedWindow = -1;
                else
                    _activeTabbedWindow = Math.Min(_activeTabbedWindow, _tabbedWindows.Count - 1);
            }
        }

        private void RecreateRenderTarget()
        {
            _renderTarget?.Dispose();
            _renderTarget = null;

            Vector2 size = _elementSpace.Size;
            if (size.X + size.Y > 0.0f)
            {
                _renderTarget = RenderingManager.Device.CreateRenderTarget(new RHI.RenderTargetDescription
                {
                    ColorFormat = RHI.RenderTargetFormat.RGB10A2un,
                    DepthFormat = RHI.DepthStencilFormat.Undefined,
                    Dimensions = new Size((int)size.X, (int)size.Y),
                    ShaderVisibility = RHI.RenderTargetVisiblity.Color
                });
            }
        }

        public Vector2 Position => _position;
        public Vector2 Size
        {
            get => _size;
            internal set
            {
                bool isDiff = _size != value;
                _size = value;

                //if (isDiff)
                //    RecreateRenderTarget();
            }
        }

        internal DockingContainer? OwningContainer => _containgDock;
        internal DockSpace OwningDockSpace => _containedDockSpace;

        internal Boundaries ElementSpace => _elementSpace;

        internal RHI.RenderTarget? RenderTarget => _renderTarget;
        internal Window? Window => _window;

        internal Span<DockingContainer> DockedContainers => _dockedContainers.AsSpan();

        internal EditorWindow? FocusedEditorWindow => _activeTabbedWindow == -1 ? null : _tabbedWindows[_activeTabbedWindow];

        private readonly record struct WindowData(DockableElement Root);
    }

    public enum DockSpace : byte
    {
        None = 0,

        Tab,
        Left,
        Right,
        Top,
        Bottom
    }
}
