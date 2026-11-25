using Editor.Assets.Types;
using Editor.Geo.Clipboard;
using Editor.Geo.History;
using Editor.Geo.Selection;
using Editor.Geometry;
using Editor.Gui.View;
using Editor.History;
using Editor.Interaction;
using Editor.IO;
using Editor.UI.Elements.Tree;
using Editor.UI.Menu;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Text;

namespace Editor.Geo.Menu
{
    internal record class GroupContextMenuListener(GeoHierchyWindow HierchyWindow, BrushGroup Group) : IContextMenuListener
    {
        public bool OnItemPressed(ContextMenuBase item)
        {
            if (item.Id == "AddBrushCamera")
            {
                EditorCamera camera = EditorCamera.Instance;

                Vector3 position = camera.Position + camera.Forward * 4.0f;
                if (ToolManager.IsSnappingActive)
                    position = Vector3.Round(position / ToolManager.SnapScale) * ToolManager.SnapScale;

                Brush brush = Group.CreateBrush();

                Span<Vector3> vertices = brush.Vertices;
                for (int i = 0; i < vertices.Length; ++i)
                {
                    vertices[i] += position;
                }

                HistoryManager.AddStep(new AddBrushStep(brush, position));
            }
            else if (item.Id == "AddBrushCenter")
            {
                Brush brush = Group.CreateBrush();
                HistoryManager.AddStep(new AddBrushStep(brush, Vector3.Zero));
            }
            else if (item.Id == "NewGroup")
            {
                BrushGroup group = Group.Scene.CreateGroup($"New Group {Group.Scene.Groups.Count}");
                HistoryManager.AddStep(new AddGroupStep(group));
            }
            else if (item.Id == "Cut")
            {

            }
            else if (item.Id == "Copy")
            {
                GeoSelectionGroup selectionGroup = EditorRuntime.GlobalSingleton.GeoSceneManager.SelectionGroup;
                EditorClipboard.Set(new BrushClipboardData(selectionGroup.Selection.Keys));
            }
            else if (item.Id == "Paste")
            {
                BrushClipboardData? clipboardData = EditorClipboard.Get<BrushClipboardData>();
                if (clipboardData != null)
                {
                    Brush[] brushes = new Brush[clipboardData.Vertices.Length / 8];

                    GeoSelectionGroup selectionGroup = EditorRuntime.GlobalSingleton.GeoSceneManager.SelectionGroup;
                    selectionGroup.DeselectAll();

                    for (int i = 0; i < brushes.Length; i++)
                    {
                        Brush brush = Group.CreateBrush();
                        clipboardData.Vertices.Slice(i * 8, 8).CopyTo(brush.Vertices);

                        brushes[i] = brush;
                        selectionGroup.Select(brush);
                    }

                    HistoryManager.AddStep(new PasteBrushesStep(Group, brushes.ToImmutableArray()));
                }
            }
            else if (item.Id == "Delete")
            {

            }

            return true;
        }
    }
}
