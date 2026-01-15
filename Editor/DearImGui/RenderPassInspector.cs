using CommunityToolkit.HighPerformance;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Rendering;
using Primary.Rendering.Pass;
using Primary.Rendering.Recording;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Editor.DearImGui
{
    internal sealed class RenderPassInspector : IDearImGuiWindow, IDisposable
    {
        private List<FrameGraphPass> _capturedPasses;

        private int _activePassId;
        private int _commandIndexInspector;

        private bool _disposedValue;

        public RenderPassInspector()
        {
            _capturedPasses = new List<FrameGraphPass>();

            _activePassId = -1;
            _commandIndexInspector = -1;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

                }

                _disposedValue = true;
            }
        }

        ~RenderPassInspector()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Render()
        {
            RenderingManager renderer = Editor.GlobalSingleton.RenderingManager;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

            bool shouldDrawWindow = ImGui.Begin("Frame graph inspector"u8);

            ImGui.PopStyleVar();

            uint dockspaceId = ImGui.GetID("FGI_DockSpace"u8);

            ReadOnlySpan<byte> leftWindowId = "FGI_Left"u8;
            ReadOnlySpan<byte> rightWindowId = "FGI_Right"u8;

            ImGuiWindowClass windowClass = new ImGuiWindowClass
            {
                ClassId = dockspaceId,
                DockingAllowUnclassed = 0
            };

            if (ImGuiP.DockBuilderGetNode(dockspaceId).IsNull)
            {
                ImGuiP.DockBuilderAddNode(dockspaceId, (ImGuiDockNodeFlags)(
                    ImGuiDockNodeFlagsPrivate.Space |
                    ImGuiDockNodeFlagsPrivate.NoWindowMenuButton |
                    ImGuiDockNodeFlagsPrivate.NoCloseButton));
                ImGuiP.DockBuilderSetNodeSize(dockspaceId, Vector2.Max(ImGui.GetContentRegionAvail(), Vector2.One));

                uint leftDockId = 0, rightDockId = 0;
                unsafe
                {
                    ImGuiP.DockBuilderSplitNode(dockspaceId, ImGuiDir.Left, 0.5f, &leftDockId, &rightDockId);

                    ImGuiDockNodePtr leftNode = ImGuiP.DockBuilderGetNode(leftDockId);
                    ImGuiDockNodePtr rightNode = ImGuiP.DockBuilderGetNode(rightDockId);

                    leftNode.LocalFlags |= (ImGuiDockNodeFlags)(ImGuiDockNodeFlagsPrivate.NoTabBar /*| ImGuiDockNodeFlags.NoDockingSplitMe*/ | ImGuiDockNodeFlagsPrivate.NoDockingOverMe);
                    rightNode.LocalFlags |= (ImGuiDockNodeFlags)(ImGuiDockNodeFlagsPrivate.NoTabBar /*| ImGuiDockNodeFlags.NoDockingSplitMe*/ | ImGuiDockNodeFlagsPrivate.NoDockingOverMe);
                }

                ImGuiP.DockBuilderDockWindow(leftWindowId, leftDockId);
                ImGuiP.DockBuilderDockWindow(rightWindowId, rightDockId);
            }

            ImGui.DockSpace(dockspaceId, ImGui.GetContentRegionAvail(), shouldDrawWindow ? ImGuiDockNodeFlags.NoDockingSplit : ImGuiDockNodeFlags.KeepAliveOnly, ref windowClass);
            ImGui.End();

            ImGuiContextPtr context = ImGui.GetCurrentContext();

            if (ImGui.Begin(leftWindowId, ImGuiWindowFlags.HorizontalScrollbar))
            {
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                Vector2 avail = ImGui.GetContentRegionAvail();

                RenderPassManager passManager = renderer.RenderPassManager;
                foreach (ref readonly RenderPassDescription desc in passManager.CurrentPasses)
                {
                    if (desc.Function != null)
                    {
                        int id = desc.Name.GetDjb2HashCode();
                        if (DrawSelectableTab(desc.Name, desc.Type, _activePassId == id))
                            _activePassId = id;
                    }
                }

                unsafe bool DrawSelectableTab(string passName, RenderPassType type, bool isActive)
                {
                    string miliString = "0.00ms";

                    Vector2 msSize = ImGui.CalcTextSize(miliString);

                    Vector2 cursor = ImGui.GetCursorScreenPos();
                    ImRect bb = new ImRect(cursor, cursor + new Vector2(MathF.Max(avail.X, msSize.X + context.Style.FramePadding.X * 2.0f), context.FontSize + context.Style.FramePadding.Y * 2.0f));

                    uint id = ImGui.GetID(passName.Length == 0 ? "##" : passName);

                    Vector4 clipRect = new Vector4(bb.Min.X, bb.Min.Y, bb.Max.X - msSize.X - context.Style.FramePadding.X * 2.0f, bb.Max.Y);
                    float middleDiff = bb.Max.Y - bb.Min.Y;

                    bool hovered = false, held = false;
                    bool pressed = ImGuiP.ButtonBehavior(bb, id, ref hovered, ref held);

                    drawList.AddRectFilled(bb.Min, bb.Max, new Color32(context.Style.Colors[(int)(held ? ImGuiCol.ButtonActive : (hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button))]).ABGR);
                    drawList.AddTriangleFilled(new Vector2(bb.Min.X + 1.0f, (int)float.Lerp(bb.Min.Y, bb.Max.Y, 0.5f)), new Vector2(bb.Min.X + 1.0f, bb.Max.Y), new Vector2(bb.Min.X + middleDiff * 0.5f + 1.0f, bb.Max.Y), type == RenderPassType.Compute ? 0x40ff0000u : 0x400080ffu);
                    drawList.AddText(new Vector2(bb.Max.X - msSize.X - context.Style.FramePadding.X, bb.Min.Y + context.Style.FramePadding.Y), 0xffffffff, miliString);
                    drawList.AddText(context.Font.Handle, context.FontSize, bb.Min + context.Style.FramePadding, 0xffffffff, passName, ref clipRect);

                    if (isActive)
                    {
                        drawList.AddRect(bb.Min - new Vector2(2.0f), bb.Max + new Vector2(2.0f), 0xff2070a0, 1.0f);
                        bb.Max.Y += 1.0f;
                    }

                    ImGuiP.ItemAdd(bb, id);
                    ImGuiP.ItemSize(bb);

                    return pressed;
                }
            }
            ImGui.End();

            if (ImGui.Begin(rightWindowId))
            {
                if (_activePassId != -1)
                {
                    RenderPassManager passManager = renderer.RenderPassManager;

                    int index = 0;
                    foreach (ref readonly RenderPassDescription desc in passManager.CurrentPasses)
                    {
                        if (desc.Function != null && desc.Name.GetDjb2HashCode() == _activePassId)
                        {
                            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                            Vector2 cursorPos = ImGui.GetCursorScreenPos();
                            Vector2 avail = ImGui.GetContentRegionAvail();

                            FrameGraphCommands commands = passManager.Commands[index];

                            CommandRecorder recorder = commands.Recorder;

                            int steps = Math.Min((int)MathF.Round(recorder.TotalCommandCount / 10.0f + 2), 10);

                            float lineY = 10.0f + context.FontSize + context.Style.FramePadding.Y * 2.0f + 1.0f;
                            float stepSize = float.Truncate(avail.X / (steps - 1));

                            for (int i = 0; i < steps; i++)
                            {
                                float t = i / (float)(steps - 1);
                                int value = (int)(t * recorder.TotalCommandCount);

                                Vector2 min = new Vector2(t * (avail.X - 1.0f) + cursorPos.X, cursorPos.Y);
                                drawList.AddRectFilled(min + new Vector2(0.0f, context.FontSize + 2.0f), min + new Vector2(1.0f, context.FontSize + 10.0f), 0xffffffff);

                                string text = value.ToString();

                                if (i == 0)
                                {
                                    drawList.AddText(min, 0xffffffff, text);

                                    float x = i * stepSize;
                                    drawList.AddRectFilled(cursorPos + new Vector2(x + 1.0f, lineY), cursorPos + new Vector2(x + stepSize - 1.0f, 48.0f + lineY), 0x10ffffff);
                                }
                                else if (i == steps - 1)
                                    drawList.AddText(min - new Vector2(ImGui.CalcTextSize(text).X, 0.0f), 0xffffffff, text);
                                else
                                {
                                    drawList.AddText(min - new Vector2(ImGui.CalcTextSize(text).X * 0.5f, 0.0f), 0xffffffff, text);
                                    drawList.AddRectFilled(new Vector2(min.X, cursorPos.Y + 10.0f + context.FontSize + context.Style.FramePadding.Y * 2.0f), new Vector2(min.X, cursorPos.Y + 58.0f + context.FontSize + context.Style.FramePadding.Y * 2.0f), 0xffffffff);

                                    float x = i * stepSize;
                                    drawList.AddRectFilled(cursorPos + new Vector2(x - 1.0f, lineY), cursorPos + new Vector2(x + stepSize - 1.0f, 48.0f + lineY), 0x10ffffff);
                                    drawList.AddRectFilled(cursorPos + new Vector2(x, lineY), cursorPos + new Vector2(x - 1.0f, 48.0f + lineY), 0x15ffffff);
                                }
                            }

                            drawList.AddRectFilled(cursorPos + new Vector2(0.0f, context.FontSize + 9.0f), cursorPos + new Vector2(avail.X, context.FontSize + 10.0f), 0xffffffff);
                            drawList.AddRect(cursorPos + new Vector2(0.0f, lineY - 1.0f), cursorPos + new Vector2(avail.X, lineY + 49.0f), new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR);

                            ImGui.Dummy(new Vector2(82.0f));

                            CommandInterface @interface = new CommandInterface(context, drawList, cursorPos + new Vector2(1.0f, lineY), new Vector2(avail.X - 2.0f, 48.0f), recorder.TotalCommandCount);

                            int offset = 0;
                            while (offset < recorder.BufferSize)
                            {
                                RecCommandType type = recorder.GetCommandTypeAtOffset(offset);
                                offset += Unsafe.SizeOf<RecCommandType>();

                                if (type == RecCommandType.Dummy)
                                    break;

                                if (!s_commandTypeCallbacks.TryGetValue(type, out CommandType cmdType))
                                {
                                    //TODO: implement actual error
                                    ImGui.Text("TEMPLATE BAD COMMAND TYPE: " + type.ToString());
                                    break;
                                }
                                else
                                    cmdType.Callback(recorder, ref offset, ref @interface);

                                offset += cmdType.StructSize;
                            }

                            break;
                        }

                        index++;
                    }
                }
            }
            ImGui.End();
        }

        private static FrozenDictionary<RecCommandType, CommandType> s_commandTypeCallbacks = new Dictionary<RecCommandType, CommandType>
        {
            { RecCommandType.SetRenderTarget, new CommandType(Unsafe.SizeOf<UCSetRenderTarget>(), (recorder, ref offset, ref @interface) =>
            {
                UCSetRenderTarget cmd = recorder.GetCommandAtOffset<UCSetRenderTarget>(offset);
                if (@interface.AddCommand(RecCommandContextType.StateChange, "SetRenderTarget"u8))
                {
                    @interface.AddInformation("Slot:"u8, cmd.Slot);
                    @interface.AddInformation("Render target:"u8, cmd.RenderTarget);
                }
            }) },
            { RecCommandType.SetDepthStencil, new CommandType(Unsafe.SizeOf<UCSetDepthStencil>(), (recorder, ref offset, ref @interface) =>
            {
                UCSetDepthStencil cmd = recorder.GetCommandAtOffset<UCSetDepthStencil>(offset);
                if (@interface.AddCommand(RecCommandContextType.StateChange, "SetDepthStencil"u8))
                {
                    @interface.AddInformation("Depth stencil:"u8, cmd.DepthStencil);
                }
            }) },
            { RecCommandType.ClearRenderTarget, new CommandType(Unsafe.SizeOf<UCClearRenderTarget>(), (recorder, ref offset, ref @interface) =>
            {
                UCClearRenderTarget cmd = recorder.GetCommandAtOffset<UCClearRenderTarget>(offset);
                if (@interface.AddCommand(RecCommandContextType.Modification, "ClearRenderTarget"u8))
                {
                    @interface.AddInformation("Render target:"u8, cmd.RenderTarget);
                    @interface.AddInformation("Rect:"u8, cmd.Rect.HasValue ? cmd.Rect.Value.ToString() : "null");
                }
            }) },
            { RecCommandType.ClearDepthStencil, new CommandType(Unsafe.SizeOf<UCClearDepthStencil>(), (recorder, ref offset, ref @interface) =>
            {
                UCClearDepthStencil cmd = recorder.GetCommandAtOffset<UCClearDepthStencil>(offset);
                if (@interface.AddCommand(RecCommandContextType.Modification, "ClearDepthStencil"u8))
                {
                    @interface.AddInformation("Depth stencil:"u8, cmd.DepthStencil);
                    @interface.AddInformation("Clear flags:"u8, cmd.ClearFlags.ToString());
                    @interface.AddInformation("Rect:"u8, cmd.Rect.HasValue ? cmd.Rect.Value.ToString() : "null");
                }
            }) },
            { RecCommandType.ClearRenderTargetCustom, new CommandType(Unsafe.SizeOf<UCClearRenderTargetCustom>(), (recorder, ref offset, ref @interface) =>
            {
                UCClearRenderTargetCustom cmd = recorder.GetCommandAtOffset<UCClearRenderTargetCustom>(offset);
                if (@interface.AddCommand(RecCommandContextType.Modification, "ClearRenderTarget"u8))
                {
                    @interface.AddInformation("Render target:"u8, cmd.RenderTarget);
                    @interface.AddInformation("Rect:"u8, cmd.Rect.HasValue ? cmd.Rect.Value.ToString() : "null");
                    @interface.AddInformation("Color:"u8, cmd.Color);
                }
            }) },
            { RecCommandType.ClearDepthStencilCustom, new CommandType(Unsafe.SizeOf<UCClearDepthStencilCustom>(), (recorder, ref offset, ref @interface) =>
            {
                UCClearDepthStencilCustom cmd = recorder.GetCommandAtOffset<UCClearDepthStencilCustom>(offset);
                if (@interface.AddCommand(RecCommandContextType.Modification, "ClearDepthStencil"u8))
                {
                    @interface.AddInformation("Depth stencil:"u8, cmd.DepthStencil);
                    @interface.AddInformation("Clear flags:"u8, cmd.ClearFlags.ToString());
                    @interface.AddInformation("Rect:"u8, cmd.Rect.HasValue ? cmd.Rect.Value.ToString() : "null");
                    @interface.AddInformation("Depth:"u8, cmd.Depth);
                    @interface.AddInformation("Stencil:"u8, cmd.Stencil);
                }
            }) },
            { RecCommandType.SetViewport, new CommandType(Unsafe.SizeOf<UCSetViewport>(), (recorder, ref offset, ref @interface) =>
            {
                UCSetViewport cmd = recorder.GetCommandAtOffset<UCSetViewport>(offset);
                if (@interface.AddCommand(RecCommandContextType.StateChange, "SetViewport"u8))
                {
                    @interface.AddInformation("Render target:"u8, cmd.Slot);
                    @interface.AddInformation("Viewport:"u8, cmd.Viewport.HasValue ? cmd.Viewport.Value.ToString() : "null");
                }
            }) },
            { RecCommandType.SetScissor, new CommandType(Unsafe.SizeOf<UCSetScissor>(), (recorder, ref offset, ref @interface) =>
            {
                UCSetScissor cmd = recorder.GetCommandAtOffset<UCSetScissor>(offset);
                if (@interface.AddCommand(RecCommandContextType.StateChange, "SetScissor"u8))
                {
                    @interface.AddInformation("Render target:"u8, cmd.Slot);
                    @interface.AddInformation("Viewport:"u8, cmd.Scissor.HasValue ? cmd.Scissor.Value.ToString() : "null");
                }
            }) },
            { RecCommandType.SetStencilReference, new CommandType(Unsafe.SizeOf<UCSetStencilRef>(), (recorder, ref offset, ref @interface) =>
            {
                UCSetStencilRef cmd = recorder.GetCommandAtOffset<UCSetStencilRef>(offset);
                if (@interface.AddCommand(RecCommandContextType.StateChange, "SetStencilRef"u8))
                {
                    @interface.AddInformation("Stencil ref:"u8, cmd.StencilRef);
                }
            }) },
            { RecCommandType.SetBuffer, new CommandType(Unsafe.SizeOf<UCSetBuffer>(), (recorder, ref offset, ref @interface) =>
            {
                UCSetBuffer cmd = recorder.GetCommandAtOffset<UCSetBuffer>(offset);
                if (@interface.AddCommand(RecCommandContextType.StateChange, "SetBuffer"u8))
                {
                    @interface.AddInformation("Buffer:"u8, cmd.Buffer);
                    @interface.AddInformation("Location:"u8, cmd.Location.ToString());
                    @interface.AddInformation("Stride:"u8, cmd.Stride);
                }
            }) },
            { RecCommandType.SetProperties, new CommandType(Unsafe.SizeOf<UCSetProperties>(), (recorder, ref offset, ref @interface) =>
            {
                UCSetProperties cmd = recorder.GetCommandAtOffset<UCSetProperties>(offset);
                offset += Unsafe.SizeOf<UnmanagedPropertyData>() * cmd.ResourceCount + cmd.DataBlockSize;

                if (@interface.AddCommand(RecCommandContextType.StateChange, "SetProperties"u8))
                {

                }
            }) },
            { RecCommandType.UploadBuffer, new CommandType(Unsafe.SizeOf<UCUploadBuffer>(), (recorder, ref offset, ref @interface) =>
            {
                UCUploadBuffer cmd = recorder.GetCommandAtOffset<UCUploadBuffer>(offset);
                if (@interface.AddCommand(RecCommandContextType.Modification, "UploadBuffer"u8))
                {
                    @interface.AddInformation("Data pointer:"u8, cmd.DataPointer);
                    @interface.AddInformation("Data size:"u8, cmd.DataSize);
                }
            }) },
            { RecCommandType.UploadTexture, new CommandType(Unsafe.SizeOf<UCUploadTexture>(), (recorder, ref offset, ref @interface) =>
            {
                UCUploadTexture cmd = recorder.GetCommandAtOffset<UCUploadTexture>(offset);
                if (@interface.AddCommand(RecCommandContextType.Modification, "UploadTexture"u8))
                {
                    @interface.AddInformation("Buffer:"u8, cmd.Texture);
                    @interface.AddInformation("Destination box:"u8, cmd.DestinationBox.HasValue ? cmd.DestinationBox.Value.ToString() : "null");
                    @interface.AddInformation("Subresource index:"u8, cmd.SubresourceIndex);
                    @interface.AddInformation("Data pointer:"u8, cmd.DataPointer);
                    @interface.AddInformation("Data size:"u8, cmd.DataSize);
                }
            }) },
            { RecCommandType.DrawInstanced, new CommandType(Unsafe.SizeOf<UCDrawInstanced>(), (recorder, ref offset, ref @interface) =>
            {
                UCDrawInstanced cmd = recorder.GetCommandAtOffset<UCDrawInstanced>(offset);
                if (@interface.AddCommand(RecCommandContextType.Execution, "DrawInstanced"u8))
                {
                    @interface.AddInformation("Vertex count:"u8, cmd.VertexCount);
                    @interface.AddInformation("Instance count:"u8, cmd.InstanceCount);
                    @interface.AddInformation("Start vertex:"u8, cmd.StartVertex);
                    @interface.AddInformation("Start instance:"u8, cmd.StartInstance);
                }
            }) },
            { RecCommandType.DrawIndexedInstanced, new CommandType(Unsafe.SizeOf<UCDrawIndexedInstanced>(), (recorder, ref offset, ref @interface) =>
            {
                UCDrawIndexedInstanced cmd = recorder.GetCommandAtOffset<UCDrawIndexedInstanced>(offset);
                if (@interface.AddCommand(RecCommandContextType.Execution, "DrawIndexedInstanced"u8))
                {
                    @interface.AddInformation("Vertex count:"u8, cmd.IndexCount);
                    @interface.AddInformation("Instance count:"u8, cmd.InstanceCount);
                    @interface.AddInformation("Start index:"u8, cmd.StartIndex);
                    @interface.AddInformation("Base vertex:"u8, cmd.BaseVertex);
                    @interface.AddInformation("Start instance:"u8, cmd.StartInstance);
                }
            }) },
            { RecCommandType.SetPipeline, new CommandType(Unsafe.SizeOf<UCSetPipeline>(), (recorder, ref offset, ref @interface) =>
            {
                UCSetPipeline cmd = recorder.GetCommandAtOffset<UCSetPipeline>(offset);
                if (@interface.AddCommand(RecCommandContextType.StateChange, "SetPipeline"u8))
                {
                    @interface.AddInformation("Pipeline:"u8, cmd.Pipeline);
                }
            }) },
        }.ToFrozenDictionary();

        private readonly record struct CommandType(int StructSize, CommandCallbackDelegate Callback);
        private delegate void CommandCallbackDelegate(CommandRecorder recorder, ref int offset, ref CommandInterface @interface);

        private record struct CommandInterface(ImGuiContextPtr Context, ImDrawListPtr DrawList, Vector2 Position, Vector2 Avail, float Divisor)
        {
            private readonly float _width = 1.0f / Divisor * Avail.X - 1.0f;
            private int _index = 0;

            public unsafe bool AddCommand(RecCommandContextType type, ReadOnlySpan<byte> name)
            {
                ImRect bb = new ImRect(Position, Position + new Vector2(_width, Avail.Y));

                DrawList.AddRectFilled(bb.Min, bb.Max, type switch
                {
                    RecCommandContextType.StateChange => 0xffff0000,
                    RecCommandContextType.Execution => 0xff0000ff,
                    RecCommandContextType.Modification => 0xff00a000,
                    _ => 0xffff00ff
                });

                DrawList.AddRect(bb.Min, bb.Max, type switch
                {
                    RecCommandContextType.StateChange => 0xff800000,
                    RecCommandContextType.Execution => 0xff000080,
                    RecCommandContextType.Modification => 0xff008000,
                    _ => 0xff800080
                });

                DrawList.AddText(Context.Font.Handle, Context.FontSize, bb.Min + Context.Style.FramePadding, 0xffffffff, name, ref Unsafe.As<ImRect, Vector4>(ref bb));

                Position += new Vector2(_width + 1.0f, 0.0f);
                _index++;

                return false;
            }

            public void AddInformation(ReadOnlySpan<byte> name, int value)
            {

            }

            public void AddInformation(ReadOnlySpan<byte> name, float value)
            {

            }

            public void AddInformation(ReadOnlySpan<byte> name, string value)
            {

            }

            public void AddInformation(ReadOnlySpan<byte> name, Color value)
            {

            }
        }

        private readonly record struct FrameGraphPass(string Name, float Time, int TotalCommandCount, nint RawData, int DataLength) : IDisposable
        {
            public unsafe void Dispose()
            {
                if (RawData != nint.Zero)
                    NativeMemory.Free(RawData.ToPointer());
            }

            public nint GetPointerAtOffset(int offset)
            {
                Debug.Assert(offset < DataLength);
                return RawData + offset;
            }

            public unsafe RecCommandType GetCommandTypeAtOffset(int offset)
            {
                Debug.Assert(offset + Unsafe.SizeOf<RecCommandType>() < DataLength);
                return Unsafe.ReadUnaligned<RecCommandType>((RawData + offset).ToPointer());
            }

            public unsafe T GetCommandAtOffset<T>(int offset) where T : unmanaged
            {
                Debug.Assert(offset + Unsafe.SizeOf<RecCommandType>() < DataLength);
                return Unsafe.ReadUnaligned<T>((RawData + offset).ToPointer());
            }
        }
    }
}
