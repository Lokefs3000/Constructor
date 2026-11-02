using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using CommunityToolkit.HighPerformance;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Rendering;
using Primary.Rendering.Memory;
using TerraFX.Interop.Windows;
using VoxelizationDemo.Core;

namespace VoxelizationDemo.Editor.Windows
{
    internal sealed class RenderInspector : IEditorWindow
    {
        private readonly List<StoredUploadInfo> _uploadInfos;
        private int _uploadResourceSize;

        private float _uploadTabViewOffset;
        private float _uploadTabViewZoom;

        internal RenderInspector()
        {
            _uploadInfos = new List<StoredUploadInfo>();

            _uploadTabViewOffset = 1.0f;
            _uploadTabViewZoom = 1.0f;
        }

        public bool UpdateAndRender()
        {
            bool isOpen = true;
            if (ImGui.Begin("Render inspector"u8, ref isOpen))
            {
                if (ImGui.BeginTabBar("###TABS"u8))
                {
                    if (ImGui.BeginTabItem("Uploads"u8))
                    {
                        DoUploadsTab();
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                VoxelRuntime.Instance.RenderingManager.InspectRenderPlan(OnInspectRenderPlan);
            }
            ImGui.End();

            return isOpen;
        }

        private void OnInspectRenderPlan(CompiledRenderPlan renderPlan)
        {
            _uploadInfos.Clear();

            _uploadResourceSize = renderPlan.RequiredUploadMemory;

            FrameGraph frameGraph = VoxelRuntime.Instance.RenderingManager.FrameGraph;
            foreach (DataUploadRequest uploadRequest in frameGraph.UploadManager.UploadRequests)
            {
                _uploadInfos.Add(new StoredUploadInfo(
                    frameGraph.ResourceManager.GetName(uploadRequest.Resource),
                    uploadRequest.UploadBaseOffset,
                    uploadRequest.ReservationSize,
                    uploadRequest.UploadSize));
            }

            _uploadInfos.Sort(static (x, y) => x.ByteOffsetInHeap.CompareTo(y.ByteOffsetInHeap));
        }

        private unsafe void DoUploadsTab()
        {
            ImGuiContextPtr context = EditorManager.Instance.ContextManager.ImGuiCtx;

            {
                ImGuiWindowPtr window = context.CurrentWindow;
                Vector2 availSize = ImGui.GetContentRegionAvail();

                float offset = (_uploadTabViewOffset / _uploadTabViewZoom - 1.0f) * availSize.X;
                float startCursorX = window.DC.CursorPos.X;

                availSize.X /= _uploadTabViewZoom;
                window.DC.CursorPos.X += offset;

                string uploadText = FileUtility.FormatSize(_uploadResourceSize, "F2");

                ImDrawListPtr drawList = window.DrawList;
                drawList.AddText(window.DC.CursorPos, 0xffffffff, "0kb"u8);
                drawList.AddText(window.DC.CursorPos + new Vector2(availSize.X - ImGui.CalcTextSize(uploadText).X, 0.0f), 0xffffffff, uploadText);

                window.DC.CursorPos.Y += context.FontSize + context.Style.ItemSpacing.Y;
                for (int i = 0; i < 10; ++i)
                {
                    Vector2 baseCursor = new Vector2(window.DC.CursorPos.X + 1.0f + (availSize.X - 2.0f) * (i / 9.0f), window.DC.CursorPos.Y);
                    if (i != 0 && i != 9)
                        baseCursor.Y += context.FontSize * 0.7f;

                    drawList.AddRectFilled(baseCursor, new Vector2(baseCursor.X + 1.0f, window.DC.CursorPos.Y + context.FontSize), 0xff808080);
                }

                window.DC.CursorPos.X = startCursorX;
                window.DC.CursorPos.Y += context.FontSize;
            }

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
            bool drawChild = ImGui.BeginChild("##BLOCKS"u8, ImGuiChildFlags.Borders);
            ImGui.PopStyleVar();

            if (drawChild)
            {
                ImGuiWindowPtr window = context.CurrentWindow;
                ImDrawListPtr drawList = window.DrawList;

                Vector2 availSize = ImGui.GetContentRegionAvail();

                double offset = (_uploadTabViewOffset / _uploadTabViewZoom - 1.0f) * _uploadResourceSize;
                double multiplier = 1.0 / _uploadResourceSize * availSize.X * _uploadTabViewZoom;

                for (int i = 0; i < _uploadInfos.Count; ++i)
                {
                    StoredUploadInfo uploadInfo = _uploadInfos[i];

                    float startX = (float)((uploadInfo.ByteOffsetInHeap + offset) * multiplier);
                    float endX = (float)(startX + uploadInfo.ReservedSize * multiplier);

                    uint color = (uint)(uploadInfo.ResourceName.GetDjb2HashCode() | 0xff000000);

                    Vector2 rectMin = new Vector2(window.DC.CursorPos.X + startX, window.DC.CursorPos.Y);
                    Vector2 rectMax = new Vector2(window.DC.CursorPos.X + endX, window.DC.CursorPos.Y + context.FontSize + 8.0f);

                    drawList.AddRectFilled(rectMin, rectMax, color);
                    drawList.AddRectFilled(rectMin.WithElement(1, rectMax.Y - 2.0f), rectMax.WithElement(0, float.Lerp(rectMin.X, rectMax.X, (float)(uploadInfo.UploadSize / (double)uploadInfo.ReservedSize))), 0xff7addeb);

                    if (endX - startX > 12.0f)
                    {
                        ImGuiP.RenderTextEllipsis(drawList, rectMin, rectMax, rectMax.X, uploadInfo.ResourceName, (byte*)null, (Vector2*)null);
                    }
                }

                ImGui.EndChild();
            }
        }

        private readonly record struct StoredUploadInfo(string ResourceName, int ByteOffsetInHeap, int ReservedSize, int UploadSize);
    }
}
