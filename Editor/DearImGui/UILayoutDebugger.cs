using CircularBuffer;
using CommunityToolkit.HighPerformance;
using Editor.DearImGui.LayoutDbg;
using Editor.UI;
using Editor.UI.Debugging;
using Editor.UI.Elements;
using Editor.UI.Interaction;
using Editor.UI.Visual;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Mathematics;
using Primary.Threading;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Editor.DearImGui
{
    internal sealed class UILayoutDebugger : IDearImGuiWindow
    {
        private UIWindow? _targetWindow;
        private UIElement? _selectedElement;

        private UIElement? _dragDropElement;

        private UIElement? _hoveredElement;

        private bool _drawElementBounds;

        public void Render()
        {
            if (ImGui.GetDragDropPayload().IsNull)
                _dragDropElement = null;

            if (ImGui.Begin("UI layout debugger"u8, ImGuiWindowFlags.MenuBar))
            {
                Editor editor = Editor.GlobalSingleton;
                UIManager manager = editor.UIManager;

                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("View"u8))
                    {
                        if (ImGui.MenuItem("Boundaries"u8, _drawElementBounds))
                            _drawElementBounds = !_drawElementBounds;

                        ImGui.EndMenu();
                    }

                    if (ImGui.MenuItem($"\"{_targetWindow?.WindowTitle ?? "No window"}\""))
                    {
                        editor.DearImGuiWindowManager.OpenPopup<SelectWindowPopup>((x) =>
                        {
                            if (x.TargetWindow != null)
                            {
                                _targetWindow = x.TargetWindow;
                                _selectedElement = null;
                            }
                        });
                    }

                    if (_targetWindow != null)
                    {
                        if (ImGui.MenuItem("Recorder"u8))
                        {
                            editor.DearImGuiWindowManager.OpenPopup(new RecorderViewPopup(_targetWindow));
                        }

                        if (ImGui.MenuItem("Interaction"u8))
                        {
                            editor.DearImGuiWindowManager.OpenPopup(new InteractionViewPopup(_targetWindow));
                        }

                        if (ImGui.MenuItem("Rendering"u8))
                        {
                            editor.DearImGuiWindowManager.OpenPopup(new RenderingViewPopup(_targetWindow));
                        }
                    }

                    ImGui.EndMenuBar();
                }

                Vector2 avail = ImGui.GetContentRegionAvail();
                ImGuiStylePtr style = ImGui.GetStyle();

                if (_targetWindow != null)
                {
                    if (ImGui.BeginChild("##HR"u8, new Vector2(0.0f, avail.Y * 0.65f - style.FramePadding.Y), ImGuiChildFlags.Borders))
                    {
                        DrawLayoutHierchy(_targetWindow.RootElement);
                    }
                    ImGui.EndChild();
                }

                if (_selectedElement != null)
                {
                    if (ImGui.BeginChild("##IE"u8, new Vector2(0.0f, avail.Y * 0.35f), ImGuiChildFlags.Borders))
                    {
                        Type? type = _selectedElement.GetType();
                        do
                        {
                            if (s_inspectors.TryGetValue(type, out IElementInspector? inspector))
                                inspector.Inspect(_selectedElement);
                            else
                                ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), $"No inspector for: {type.Name}");
                        } while ((type = type.BaseType) != null && type != typeof(object));
                    }
                    ImGui.EndChild();
                }
            }
            ImGui.End();

            if (_targetWindow != null)
            {
                Vector2 baseOffset = _targetWindow.ParentHost!.TabbedClientBounds.Minimum + _targetWindow.ParentHost!.ClientOffset;

                if (_drawElementBounds && _targetWindow != null)
                {
                    ImDrawListPtr drawList = ImGui.GetBackgroundDrawList();
                    RecursiveDraw(_targetWindow.RootElement);

                    void RecursiveDraw(UIElement element)
                    {
                        Boundaries bounds = Boundaries.Offset(element.Transform.RenderCoordinates, baseOffset);
                        drawList.AddRect(bounds.Minimum, bounds.Maximum, 0x8000ffff);

                        foreach (UIElement child in element.Children)
                        {
                            RecursiveDraw(child);
                        }
                    }
                }

                if (_hoveredElement != null)
                {
                    ImDrawListPtr drawList = ImGui.GetBackgroundDrawList();
                    Boundaries renderBounds = Boundaries.Offset(_hoveredElement.Transform.RenderCoordinates, baseOffset);

                    drawList.AddRect(renderBounds.Minimum, renderBounds.Maximum, 0xff0000ff);

                    _hoveredElement = null;
                }

                if (_selectedElement != null)
                {
                    ImDrawListPtr drawList = ImGui.GetBackgroundDrawList();
                    Boundaries renderBounds = Boundaries.Offset(_selectedElement.Transform.RenderCoordinates, baseOffset);

                    drawList.AddRect(renderBounds.Minimum - new Vector2(8.0f), renderBounds.Maximum + new Vector2(8.0f), 0xffff0000);

                    if (_selectedElement.WindowOwner != null && !renderBounds.IsIntersecting(new Boundaries(Vector2.Zero, _selectedElement.WindowOwner.ClientSize)))
                    {
                        Boundaries within = new Boundaries(new Vector2(8.0f), _selectedElement.WindowOwner.ClientSize - new Vector2(8.0f));

                        Vector2 closest = Boundaries.OnEdge(within, renderBounds.Center);
                        float angle = ExMath.GetAngleTowards(within.Center, closest);
                        Vector2 forward = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

                        drawList.AddLine(closest, closest - forward * 48.0f, 0xffff0000);
                    }
                    else
                        drawList.AddRect(renderBounds.Minimum, renderBounds.Maximum, 0xff0000ff);

                    if (_selectedElement is UIFrame frame && frame.StrokeWeight > 0.0f && frame.StrokePosition != UIStrokePosition.Center)
                    {
                        Boundaries newBounds = frame.StrokePosition switch
                        {
                            UIStrokePosition.Inside => new Boundaries(renderBounds.Minimum + new Vector2(frame.StrokeWeight), renderBounds.Maximum - new Vector2(frame.StrokeWeight)),
                            UIStrokePosition.Outside => new Boundaries(renderBounds.Minimum - new Vector2(frame.StrokeWeight), renderBounds.Maximum + new Vector2(frame.StrokeWeight)),
                            _ => throw new NotImplementedException(),
                        };

                        drawList.AddRect(newBounds.Minimum, newBounds.Maximum, 0xff00ff00);
                    }

                    drawList.AddText(renderBounds.Minimum, 0xffffffff, _selectedElement.GetType().Name);
                    drawList.AddText(renderBounds.Minimum + new Vector2(0.0f, 12.0f), 0xffffffff, $"RelPos: {_selectedElement.Transform.RelativePosition}");
                    drawList.AddText(renderBounds.Minimum + new Vector2(0.0f, 24.0f), 0xffffffff, $"RealSize: {_selectedElement.Transform.RealSize}");
                }
            }
        }

        private void DrawLayoutHierchy(UIElement element)
        {
            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow;
            if (element.Children.Count == 0)
                flags |= ImGuiTreeNodeFlags.Leaf;

            ImGui.PushID(element.GetHashCode());
            bool treeValue = ImGui.TreeNodeEx(element.GetType().Name, flags);

            if (ImGui.IsItemHovered())
            {
                _hoveredElement = element;

                if (element.Parent != null && element.Parent.Children.Count > 0)
                {
                    ImGuiIOPtr io = ImGui.GetIO();
                    if (io.KeyCtrl && io.MouseWheel != 0.0f)
                    {
                        int idx = 0;
                        for (; idx < element.Parent.Children.Count; ++idx)
                        {
                            if (element.Parent.Children[idx] == element)
                                break;
                        }

                        if (io.MouseWheel < 0.0f)
                        {
                            if (idx < element.Parent.Children.Count + 1)
                                ThreadHelper.ExecuteOnMainThread(() => element.Parent.MoveChild(element, idx + 1));
                        }
                        else
                        {
                            if (idx > 0)
                                ThreadHelper.ExecuteOnMainThread(() => element.Parent.MoveChild(element, idx - 1));
                        }
                    }
                }
            }

            if (ImGui.IsItemClicked())
            {
                _selectedElement = element;
            }

            if (element is not UISplitPanel)
            {
                if (ImGui.BeginDragDropTarget())
                {
                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("UIELEM"u8);
                    if (!payload.IsNull)
                    {
                        _dragDropElement?.Parent = element;
                        _dragDropElement = null;
                    }

                    ImGui.EndDragDropTarget();
                }
                else if (element.Parent != null && ImGui.BeginDragDropSource())
                {
                    _dragDropElement = element;

                    unsafe
                    {
                        ImGui.SetDragDropPayload("UIELEM"u8, null, 0);
                    }

                    ImGui.Text(element.GetType().Name);
                    ImGui.TextColored(new Vector4(1.0f, 1.0f, 1.0f, 0.5f), element.GetType().Name);

                    ImGui.EndDragDropSource();
                }
            }

            if (ImGui.BeginPopupContextItem())
            {
                if (element.Parent != null && element.Parent.Children.Count > 0)
                {
                    int idx = 0;
                    for (; idx < element.Parent.Children.Count; ++idx)
                    {
                        if (element.Parent.Children[idx] == element)
                            break;
                    }

                    if (idx > 0 && idx < element.Parent.Children.Count - 1)
                    {
                        if (ImGui.BeginMenu("Move"))
                        {
                            if (ImGui.MenuItem("Move up"u8, "Ctrl+WheelUp"u8))
                                ThreadHelper.ExecuteOnMainThread(() => element.Parent.MoveChild(element, idx - 1));
                            if (ImGui.MenuItem("Move down"u8, "Ctrl+WheelDown"u8))
                                ThreadHelper.ExecuteOnMainThread(() => element.Parent.MoveChild(element, idx + 1));

                            ImGui.EndMenu();
                        }
                    }
                    else if (idx == 0)
                    {
                        if (ImGui.MenuItem("Move down"u8, "Ctrl+WheelDown"u8))
                        {
                            ThreadHelper.ExecuteOnMainThread(() => element.Parent.MoveChild(element, idx + 1));
                        }
                    }
                    else if (idx == element.Parent.Children.Count - 1)
                    {
                        if (ImGui.MenuItem("Move up"u8, "Ctrl+WheelUp"u8))
                        {
                            ThreadHelper.ExecuteOnMainThread(() => element.Parent.MoveChild(element, idx - 1));
                        }
                    }
                }

                if (ImGui.BeginMenu("Add"u8))
                {
                    if (ImGui.MenuItem("Element"u8)) new UIElement { Parent = element };
                    if (ImGui.MenuItem("Frame"u8)) new UIFrame { Parent = element };
                    if (ImGui.MenuItem("Button"u8)) new UIButton { Parent = element };
                    if (ImGui.MenuItem("Label"u8)) new UILabel { Parent = element };
                    if (ImGui.MenuItem("Image"u8)) new UIImage { Parent = element };
                    if (ImGui.MenuItem("Canvas"u8)) new UICanvas { Parent = element };
                    if (ImGui.MenuItem("Split container"u8)) new UISplitContainer { Parent = element };

                    ImGui.EndMenu();
                }

                if (element is UISplitContainer or UISplitPanel)
                {
                    UISplitContainer splitContainer;
                    if (element is UISplitPanel splitPanel)
                    {
                        splitContainer = (UISplitContainer)splitPanel.Parent!;
                    }
                    else
                    {
                        splitContainer = (UISplitContainer)element;
                        splitPanel = null;
                    }

                    ImGui.Separator();

                    if (ImGui.BeginMenu("Balance splits"u8))
                    {
                        if (ImGui.MenuItem("Surface"u8))
                            splitContainer.AutoBalanceSplits(splitPanel);
                        if (ImGui.MenuItem("All"u8))
                            splitContainer.AutoBalanceSplits(splitPanel, true);

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Add split"u8))
                    {
                        if (ImGui.MenuItem("Horizontal -"u8))
                            splitContainer.AddSplit(splitPanel, UISplitDirection.Horizontal);
                        if (ImGui.MenuItem("Vertical |"u8))
                            splitContainer.AddSplit(splitPanel, UISplitDirection.Vertical);

                        ImGui.EndMenu();
                    }
                }

                ImGui.Separator();

                if (ImGui.MenuItem("Invalidate layout"u8))
                    element.InvalidateSelf(UIInvalidationFlags.Layout);
                if (ImGui.MenuItem("Invalidate visual"u8))
                    element.InvalidateSelf(UIInvalidationFlags.Visual);

                ImGui.EndPopup();
            }

            if (treeValue)
            {
                foreach (UIElement child in element.Children)
                {
                    DrawLayoutHierchy(child);
                }

                ImGui.TreePop();
            }
            ImGui.PopID();
        }

        private static FrozenDictionary<Type, IElementInspector> s_inspectors = new Dictionary<Type, IElementInspector>
        {
            { typeof(UIElement), new ElementInspector() },
            { typeof(UIFrame), new FrameInspector() },
            { typeof(UIButton), new ButtonInspector() },
            { typeof(UICanvas), new CanvasInspector() },
            { typeof(UIImage), new ImageInspector() },
            { typeof(UILabel), new LabelInspector() },
            { typeof(UISplitContainer), new SplitContainerInspector() },
            { typeof(UISplitPanel), new SplitPanelInspector() },
        }.ToFrozenDictionary();

        private sealed class SelectWindowPopup : IDearImGuiPopup
        {
            private UIWindow? _targetWindow;
            private bool _hasSelected;

            public void OpenPopup() => ImGui.OpenPopup("Select window"u8);

            public void Render(ref bool WindowOpen)
            {
                if (ImGui.BeginPopupModal("Select window"u8, ref WindowOpen))
                {
                    if (ImGui.BeginChild("VIEW"u8, ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.AlwaysAutoResize))
                    {
                        Editor editor = Editor.GlobalSingleton;
                        UIManager manager = editor.UIManager;

                        foreach (UIDockHost host in manager.ActiveHosts)
                        {
                            ImGui.PushID(host.UniqueDockHostId);
                            if (ImGui.TreeNodeEx("Dock host"u8, ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                foreach (UIWindow window in host.TabbedWindows)
                                {
                                    ImGui.PushID(window.UniqueWindowId);
                                    if (ImGui.Selectable($"{window.WindowTitle}({window.GetType().Name})", _targetWindow == window))
                                        _targetWindow = window;
                                    ImGui.PopID();
                                }

                                ImGui.TreePop();
                            }

                            ImGui.PopID();
                        }
                    }
                    ImGui.EndChild();

                    if (_targetWindow != null)
                    {
                        if (ImGui.Button("Select"u8))
                        {
                            _hasSelected = true;
                            ImGui.CloseCurrentPopup();
                        }
                    }

                    ImGui.EndPopup();
                }
            }

            public UIWindow? TargetWindow => _hasSelected ? _targetWindow : null;

            public DearImGuiPopupFlags Flags => DearImGuiPopupFlags.None;
        }
        private sealed class RecorderViewPopup : IDearImGuiPopup
        {
            private readonly UIWindow _targetWindow;
            private readonly LayoutRecorder _recorder;

            internal RecorderViewPopup(UIWindow targetWindow)
            {
                _targetWindow = targetWindow;
                _recorder = new LayoutRecorder();

                Debug.Assert(targetWindow.LayoutRecorder == null);
                targetWindow.LayoutRecorder = _recorder;
            }

            public void Render(ref bool windowOpen)
            {
                if (ImGui.Begin("Layout recorder"u8, ref windowOpen, ImGuiWindowFlags.MenuBar))
                {
                    if (ImGui.BeginMenuBar())
                    {
                        if (ImGui.MenuItem("Relayout tree"))
                        {
                            _targetWindow.InvalidateTree(UIInvalidationFlags.Layout);
                        }

                        ImGui.EndMenuBar();
                    }
                }
                ImGui.End();

                if (!windowOpen)
                {
                    _recorder.Dispose();
                    _targetWindow.LayoutRecorder = null;
                }
            }

            public void OpenPopup() { }

            public DearImGuiPopupFlags Flags => DearImGuiPopupFlags.Unique;
        }
        private sealed class InteractionViewPopup : IDearImGuiPopup
        {
            private readonly UIWindow _targetWindow;

            private CircularBuffer<FiredEventData> _firedEvents;
            private int _inspectingEvent;

            private bool _autoUpdate;

            internal InteractionViewPopup(UIWindow targetWindow)
            {
                _targetWindow = targetWindow;

                _firedEvents = new CircularBuffer<FiredEventData>(128);
                _inspectingEvent = -1;

                _autoUpdate = true;

                targetWindow.ParentHost!.InteractionManager.EventFired += EventFiredCallback;
            }

            private void EventFiredCallback(UIElement element, Ref<UIEvent> eventDataRef)
            {
                _firedEvents.PushFront(new FiredEventData(element, eventDataRef.Value));

                if (_autoUpdate)
                    _inspectingEvent = 0;
                else if (_inspectingEvent != -1 && ++_inspectingEvent >= _firedEvents.Capacity)
                    _inspectingEvent = -1;
            }

            public void Render(ref bool windowOpen)
            {
                if (ImGui.Begin("Interaction viewer"u8, ref windowOpen, ImGuiWindowFlags.MenuBar))
                {
                    if (ImGui.BeginMenuBar())
                    {
                        if (ImGui.MenuItem("Auto update"u8, _autoUpdate))
                        {
                            _autoUpdate = !_autoUpdate;
                            if (_autoUpdate)
                                _inspectingEvent = 0;
                        }

                        ImGui.EndMenuBar();
                    }

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4.0f));
                    bool beginChild = ImGui.BeginChild("##EVENTS"u8, new Vector2(0.0f, ImGui.GetContentRegionAvail().Y - 100.0f), ImGuiChildFlags.Borders);
                    ImGui.PopStyleVar();

                    if (beginChild)
                    {
                        if (ImGui.BeginTable("TABLE"u8, 2, ImGuiTableFlags.BordersInner))
                        {
                            int i = 0;
                            foreach (FiredEventData eventData in _firedEvents)
                            {
                                ImGui.PushID(i);

                                ImGui.TableNextRow();

                                ImGui.TableNextColumn();
                                if (ImGui.Selectable(eventData.Element.Id ?? eventData.Element.GetType().Name, i == _inspectingEvent, ImGuiSelectableFlags.SpanAllColumns))
                                {
                                    _inspectingEvent = i;
                                }

                                ImGui.TableNextColumn();
                                ImGui.Text(eventData.Data.Type.ToString());

                                ImGui.PopID();
                                ++i;
                            }

                            ImGui.EndTable();
                        }
                    }
                    ImGui.EndChild();

                    if (ImGui.BeginChild("##OVERVIEW", new Vector2(0.0f, -1.0f), ImGuiChildFlags.Borders))
                    {
                        if (_inspectingEvent != -1)
                        {
                            FiredEventData eventData = _firedEvents[_inspectingEvent];
                            UIEvent @event = eventData.Data;

                            ImDrawListPtr drawList = ImGui.GetBackgroundDrawList();

                            Boundaries bounds = Boundaries.Offset(eventData.Element.Transform.RenderCoordinates, _targetWindow.ParentHost!.ClientOffset);
                            drawList.AddRect(bounds.Minimum, bounds.Maximum, 0xffffff00);

                            if (eventData.Element.Id != null)
                                ImGui.TextUnformatted($"{eventData.Element.Id} ({eventData.Element.GetType().Name})");
                            else
                                ImGui.TextUnformatted(eventData.Element.GetType().Name);
                            ImGui.TextUnformatted(@event.Type.ToString());

                            ImGui.Indent();

                            switch (@event.Type)
                            {
                                case UIEventType.MouseMotion:
                                    {
                                        UIMouseEvent data = @event.Mouse;
                                        ImGui.TextUnformatted($"Position: {data.Position}");
                                        ImGui.TextUnformatted($"Delta: {data.Delta}");

                                        drawList.AddCircle(data.Position - data.Delta, 4.0f, 0x80ff0000);
                                        drawList.AddCircle(data.Position, 4.0f, 0xffff0000);
                                        break;
                                    }
                                case UIEventType.MouseWheel:
                                    {
                                        UIMouseEvent data = @event.Mouse;
                                        ImGui.TextUnformatted($"Position: {data.Position}");
                                        ImGui.TextUnformatted($"Delta: {data.Delta}");

                                        drawList.AddCircle(data.Position, 4.0f, 0xffff0000);
                                        break;
                                    }
                                case UIEventType.MouseEnter:
                                    {
                                        UIMouseEvent data = @event.Mouse;
                                        ImGui.TextUnformatted($"Position: {data.Position}");

                                        drawList.AddCircle(data.Position, 4.0f, 0xffff0000);
                                        break;
                                    }
                                case UIEventType.MouseLeave:
                                    {
                                        UIMouseEvent data = @event.Mouse;
                                        ImGui.TextUnformatted($"Position: {data.Position}");

                                        drawList.AddCircle(data.Position, 4.0f, 0xffff0000);
                                        break;
                                    }
                                case UIEventType.MouseButtonDown:
                                    {
                                        UIMouseEvent data = @event.Mouse;
                                        ImGui.TextUnformatted($"Position: {data.Position}");
                                        ImGui.TextUnformatted($"Button: {data.Button}");

                                        drawList.AddCircle(data.Position, 4.0f, 0xffff0000);
                                        break;
                                    }
                                case UIEventType.MouseButtonUp:
                                    {
                                        UIMouseEvent data = @event.Mouse;
                                        ImGui.TextUnformatted($"Position: {data.Position}");
                                        ImGui.TextUnformatted($"Button: {data.Button}");

                                        drawList.AddCircle(data.Position, 4.0f, 0xffff0000);
                                        break;
                                    }
                            }

                            ImGui.Unindent();
                        }
                    }
                    ImGui.EndChild();
                }
                ImGui.End();

                if (!windowOpen)
                {
                    _targetWindow.ParentHost!.InteractionManager.EventFired -= EventFiredCallback;
                }
            }

            public void OpenPopup() { }

            public DearImGuiPopupFlags Flags => DearImGuiPopupFlags.Unique;

            private readonly record struct FiredEventData(UIElement Element, UIEvent Data);
        }
        private sealed class RenderingViewPopup : IDearImGuiPopup
        {
            private readonly UIWindow _targetWindow;

            private UICommandBuffer _heldCommandBuffer;
            private UIBakedCommandBuffer _heldBakedCommandBuffer;

            private int _focusedCommandIndex;

            private bool _showSorted;
            private bool _viewOutput;

            internal RenderingViewPopup(UIWindow targetWindow)
            {
                _targetWindow = targetWindow;

                UIRenderer renderer = Editor.GlobalSingleton.UIManager.Renderer;
                _heldCommandBuffer = new UICommandBuffer(renderer);
                _heldBakedCommandBuffer = new UIBakedCommandBuffer();

                _focusedCommandIndex = -1;

                _showSorted = true;
                _viewOutput = false;

                renderer.CommandBufferFinished += CommandBufferFinishedCallback;
                renderer.CommandBufferBaked += CommandBufferBakedCallback;
            }

            private void CommandBufferFinishedCallback(UIWindow window, UICommandBuffer commandBuffer)
            {
                if (!_viewOutput && window == _targetWindow)
                {
                    commandBuffer.CopyInternalsTo(_heldCommandBuffer);
                }
            }

            private void CommandBufferBakedCallback(UIWindow window, UIBakedCommandBuffer commandBuffer)
            {
                if (_viewOutput && window == _targetWindow)
                {
                    commandBuffer.CopyInternalsTo(_heldBakedCommandBuffer);
                }
            }

            public void Render(ref bool windowOpen)
            {
                if (ImGui.Begin("Rendering view"u8, ref windowOpen, ImGuiWindowFlags.MenuBar))
                {
                    if (ImGui.BeginMenuBar())
                    {
                        if (ImGui.MenuItem("Show sorted"u8, _showSorted))
                        {
                            _showSorted = !_showSorted;
                        }

                        if (ImGui.MenuItem("View output"u8, _viewOutput))
                        {
                            _focusedCommandIndex = -1;

                            if (_viewOutput)
                            {
                                _heldBakedCommandBuffer.ClearInternalData();
                                _viewOutput = false;
                            }
                            else
                            {
                                _heldCommandBuffer.ClearCommands(Boundaries.Zero);
                                _viewOutput = true;
                            }
                        }

                        ImGui.EndMenuBar();
                    }

                    ImDrawListPtr drawList = ImGui.GetBackgroundDrawList();

                    Vector2 baseOffset = _targetWindow.ParentHost!.ClientOffset;
                    Boundaries bounds = Boundaries.Offset(_heldCommandBuffer.DrawBoundaries, baseOffset);
                    drawList.AddRect(bounds.Minimum, bounds.Maximum, 0xffffff00);

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4.0f));
                    bool beginChild = ImGui.BeginChild("##COMMANDS"u8, new Vector2(0.0f, ImGui.GetContentRegionAvail().Y * 0.3f), ImGuiChildFlags.Borders);
                    ImGui.PopStyleVar();

                    if (beginChild)
                    {
                        if (!_viewOutput)
                        {
                            if (ImGui.BeginTable("##RAW"u8, 3, ImGuiTableFlags.BordersInner))
                            {
                                using RentedArray<UIDrawCommand> commands = RentedArray<UIDrawCommand>.Rent(_heldCommandBuffer.Commands.Count);
                                _heldCommandBuffer.CopyCommandsTo(commands.Span);

                                commands.Span.Sort(UIBakedCommandBuffer.DrawCommandComparer.Default);

                                int i = 0;
                                foreach (UIDrawCommand command in commands.Span)
                                {
                                    ImGui.PushID(command.ZIndex << 16 | command.CommandId);

                                    ImGui.TableNextRow();

                                    ImGui.TableNextColumn();
                                    if (ImGui.Selectable(command.Type.ToString(), _focusedCommandIndex == command.CommandId, ImGuiSelectableFlags.SpanAllColumns))
                                    {
                                        _focusedCommandIndex = command.CommandId;
                                    }

                                    if (ImGui.IsItemHovered())
                                    {
                                        switch (command.Type)
                                        {
                                            case UIDrawType.Rectangle:
                                                {
                                                    bounds = Boundaries.Offset(command.Rectangle.DrawBounds, baseOffset);
                                                    drawList.AddRect(bounds.Minimum, bounds.Maximum, 0xff0000ff);
                                                    break;
                                                }
                                            case UIDrawType.Triangle:
                                                {
                                                    drawList.AddTriangle(command.Triangle.A + baseOffset, command.Triangle.B + baseOffset, command.Triangle.C + baseOffset, 0xff0000ff);
                                                    break;
                                                }
                                            case UIDrawType.Circle:
                                                {
                                                    drawList.AddCircle(command.Circle.Center + baseOffset, command.Circle.Radius, 0xff0000ff);
                                                    if (command.Circle.InfillRadius > 0.0f)
                                                        drawList.AddCircle(command.Circle.Center + baseOffset, command.Circle.Radius * (command.Circle.InfillRadius / command.Circle.Radius), 0xff0000ff);

                                                    break;
                                                }
                                        }
                                    }

                                    ImGui.TableNextColumn();
                                    ImGui.Text(command.ZIndex.ToString());

                                    ImGui.TableNextColumn();
                                    ImGui.Text(command.CommandId.ToString());

                                    ImGui.PopID();

                                    ++i;
                                }

                                ImGui.EndTable();
                            }
                        }
                        else
                        {
                            if (ImGui.BeginTable("##RENDER"u8, 3, ImGuiTableFlags.BordersInner))
                            {
                                int i = 0;
                                foreach (UIDrawSection section in _heldBakedCommandBuffer.Sections)
                                {
                                    ImGui.PushID(i);

                                    ImGui.TableNextRow();

                                    ImGui.TableNextColumn();
                                    if (ImGui.Selectable(section.Type.ToString(), _focusedCommandIndex == i, ImGuiSelectableFlags.SpanAllColumns))
                                    {
                                        _focusedCommandIndex = i;
                                    }

                                    ImGui.TableNextColumn();
                                    if (section.Aux != null)
                                        ImGui.Text(section.Aux.GetType().Name);
                                    else
                                        ImGui.Text("null"u8);

                                    ImGui.PopID();

                                    ++i;
                                }

                                ImGui.EndTable();
                            }
                        }
                    }
                    ImGui.EndChild();

                    if (ImGui.BeginChild("##OVERVIEW"u8, new Vector2(0.0f, -1.0f), ImGuiChildFlags.Borders))
                    {
                        if (_focusedCommandIndex != -1)
                        {
                            if (!_viewOutput)
                            {
                                if (_focusedCommandIndex > _heldCommandBuffer.Commands.Count)
                                {
                                    _focusedCommandIndex = -1;
                                }
                                else
                                {
                                    UIDrawCommand command = _heldCommandBuffer.Commands[_focusedCommandIndex];

                                    ImGui.TextUnformatted($"CommandId: {command.CommandId}");
                                    ImGui.TextUnformatted($"ZIndex: {command.ZIndex}");

                                    ImGui.TextUnformatted($"{command.Type}:");
                                    ImGui.Indent();

                                    switch (command.Type)
                                    {
                                        case UIDrawType.Rectangle:
                                            {
                                                ImGui.TextUnformatted($"DrawBounds: {command.Rectangle.DrawBounds}");
                                                ImGui.TextUnformatted($"Color: {command.Rectangle.Color}");

                                                ImGui.TextUnformatted($"InfillWidth: {command.Rectangle.InfillWidth}");

                                                ImGui.TextUnformatted($"CornersToRound: {command.Rectangle.CornersToRound}");
                                                ImGui.TextUnformatted($"Rounding: {command.Rectangle.Rounding}");

                                                bounds = Boundaries.Offset(command.Rectangle.DrawBounds, baseOffset);
                                                drawList.AddRect(bounds.Minimum, bounds.Maximum, 0xff00ffff);
                                                break;
                                            }
                                        case UIDrawType.Triangle:
                                            {
                                                ImGui.TextUnformatted($"A: {command.Triangle.A}");
                                                ImGui.TextUnformatted($"B: {command.Triangle.B}");
                                                ImGui.TextUnformatted($"C: {command.Triangle.C}");
                                                ImGui.TextUnformatted($"Color: {command.Triangle.Color}");

                                                ImGui.TextUnformatted($"InfillWidth: {command.Triangle.InfillWidth}");

                                                ImGui.TextUnformatted($"Rounding: {command.Triangle.Rounding}");

                                                drawList.AddTriangle(command.Triangle.A + baseOffset, command.Triangle.B + baseOffset, command.Triangle.C + baseOffset, 0xff00ffff);
                                                break;
                                            }
                                        case UIDrawType.Circle:
                                            {
                                                ImGui.TextUnformatted($"Center: {command.Circle.Center}");
                                                ImGui.TextUnformatted($"Radius: {command.Circle.Radius}");
                                                ImGui.TextUnformatted($"Color: {command.Circle.Color}");

                                                ImGui.TextUnformatted($"InfillRadius: {command.Circle.InfillRadius}");

                                                drawList.AddCircle(command.Circle.Center + baseOffset, command.Circle.Radius, 0xff00ffff);
                                                if (command.Circle.InfillRadius > 0.0f)
                                                    drawList.AddCircle(command.Circle.Center + baseOffset, command.Circle.Radius * (command.Circle.InfillRadius / command.Circle.Radius), 0xff00ffff);

                                                break;
                                            }
                                    }

                                    ImGui.Unindent();
                                }
                            }
                            else
                            {
                                if (_focusedCommandIndex > _heldBakedCommandBuffer.Sections.Length)
                                {
                                    _focusedCommandIndex = -1;
                                }
                                else
                                {
                                    UIDrawSection section = _heldBakedCommandBuffer.Sections[_focusedCommandIndex];
                                
                                    ImGui.TextUnformatted($"Type: {section.Type}");

                                    ImGui.TextUnformatted($"Index count: {section.IndexCount}");
                                    ImGui.TextUnformatted($"Index offset: {section.IndexOffset}");
                                    ImGui.TextUnformatted($"Base vertex: {section.BaseVertex}");

                                    ImGui.TextUnformatted($"Auxillary: {section.Aux}");
                                }
                            }
                        }
                    }
                    ImGui.EndChild();
                }
                ImGui.End();

                if (!windowOpen)
                {
                    UIRenderer renderer = UIManager.Instance.Renderer;
                    renderer.CommandBufferFinished -= CommandBufferFinishedCallback;
                    renderer.CommandBufferBaked -= CommandBufferBakedCallback;

                    _heldCommandBuffer.Dispose();
                }
            }

            public void OpenPopup() { }

            public DearImGuiPopupFlags Flags => DearImGuiPopupFlags.Unique;
        }
    }
}
