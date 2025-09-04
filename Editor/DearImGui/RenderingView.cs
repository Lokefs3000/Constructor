using Hexa.NET.ImGui;
using Primary;
using Primary.Common;
using Primary.Rendering;
using Primary.Rendering.Forward.Managers;
using Primary.Scenes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Editor.DearImGui
{
    internal sealed class RenderingView
    {
        private IViewComponent[] _components;

        internal RenderingView()
        {
            _components = [
                new GarbageCollectorVC(),
                new LightManagerVC()
                ];
        }

        internal void Render()
        {
            Vector2 cursor = new Vector2(20.0f, 100.0f);
            foreach (IViewComponent component in _components)
            {
                component.Render(ref cursor);
                cursor.Y += 24.0f;
            }
        }

        private static void DrawHeader(ImDrawListPtr drawList, ref Vector2 cursor, string text)
        {
            ImGuiStylePtr style = ImGui.GetStyle();
            Vector2 textSize = ImGui.CalcTextSize(text);

            drawList.AddLine(cursor, cursor + new Vector2(12.0f, 0.0f), 0xffffffff);
            drawList.AddText(cursor + new Vector2(12.0f + style.FramePadding.X, -6.5f), 0xffffffff, text);
            drawList.AddLine(cursor + new Vector2(12.0f + style.FramePadding.X * 2.0f + textSize.X, 0.0f), cursor + new Vector2(200.0f, 0.0f), 0xffffffff);

            cursor.Y += 8.0f;
        }

        private static void DrawText(ImDrawListPtr drawList, ref Vector2 cursor, string text, string value)
        {
            drawList.AddText(cursor + new Vector2(8.0f, 0.0f), 0xffffffff, text);
            drawList.AddText(cursor + new Vector2(150.0f, 0.0f), 0xffffffff, value);

            cursor.Y += 15.0f;
        }

        internal interface IViewComponent
        {
            public void Render(ref Vector2 cursor);
        }

        private class GarbageCollectorVC : IViewComponent
        {
            private long _prevUsage;

            public void Render(ref Vector2 cursor)
            {
                ImDrawListPtr drawList = ImGui.GetForegroundDrawList();

                LightManager lightManager = Unsafe.As<ForwardRenderPath>(Editor.GlobalSingleton.RenderingManager.RenderPath).Lights;

                GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();
                long memUsage = GC.GetTotalMemory(false);

                long growth = memUsage - (memUsage < _prevUsage ? (memUsage + _prevUsage) : _prevUsage);
                _prevUsage = memUsage;

                DrawHeader(drawList, ref cursor, "Garbage collector");
                DrawText(drawList, ref cursor, "Managed:", $"{FileUtility.FormatSize(memUsage, "F4", CultureInfo.InvariantCulture)}/{FileUtility.FormatSize(memoryInfo.HeapSizeBytes, "F4", CultureInfo.InvariantCulture)} ({FileUtility.FormatSize(growth, "F2", CultureInfo.InvariantCulture)}/s)");
                DrawText(drawList, ref cursor, "Objects:", $"f:{memoryInfo.FinalizationPendingCount} p:{memoryInfo.PinnedObjectsCount}");
                DrawText(drawList, ref cursor, "Bytes:", $"f:{FileUtility.FormatSize(memoryInfo.FragmentedBytes, "N", CultureInfo.InvariantCulture)} t:{FileUtility.FormatSize(memoryInfo.TotalAvailableMemoryBytes, "N", CultureInfo.InvariantCulture)} c:{FileUtility.FormatSize(memoryInfo.TotalCommittedBytes, "N", CultureInfo.InvariantCulture)}");
                DrawText(drawList, ref cursor, "Recent:", memoryInfo.Generation == 2 ? $"gen{memoryInfo.Generation} !!!" : $"gen{memoryInfo.Generation}");
            }
        }

        private class LightManagerVC : IViewComponent
        {
            public void Render(ref Vector2 cursor)
            {
                ImDrawListPtr drawList = ImGui.GetForegroundDrawList();

                LightManager lightManager = Unsafe.As<ForwardRenderPath>(Editor.GlobalSingleton.RenderingManager.RenderPath).Lights;

                DrawHeader(drawList, ref cursor, "Light manager");
                DrawText(drawList, ref cursor, "Light count:", $"{lightManager.LightListCount}/{lightManager.LightListCapacity} ({(lightManager.LightListCount / (float)lightManager.LightListCapacity).ToString("P", CultureInfo.InvariantCulture)})");
                DrawText(drawList, ref cursor, "Pending lights:", lightManager.PendingLightUpdateCount.ToString());
            }
        }
    }
}
