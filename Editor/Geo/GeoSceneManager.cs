using Editor.Assets.Types;
using Editor.Geo.Selection;
using Editor.Geo.UI;
using Editor.Geometry;
using Editor.Gui.View;
using Editor.Gui.Windows;
using Editor.Interaction;
using Editor.Rendering;
using Editor.UI;
using Primary.Collections;
using Primary.Common;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Profiling;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.Geo
{
    public sealed class GeoSceneManager
    {
        private HashSet<(GeoSceneAsset, GeoScene)> _invalidScenes;
        private Lock _lock;

        private GeoSelectionGroup _selectionGroup;

        private ToolsViewSnippet? _toolsViewSnippet;

        internal GeoSceneManager()
        {
            _invalidScenes = new HashSet<(GeoSceneAsset, GeoScene)>();
            _lock = new Lock();

            _selectionGroup = new GeoSelectionGroup();

            _toolsViewSnippet = null;
        }

        internal void UpdateData()
        {
            using (new ProfilingScope("GeoManagerUpdate"))
            {
                if (_toolsViewSnippet?.IsEditVertexActive ?? false)
                {
                    HandleVertexPicking();
                }
            }
        }

        private void HandleVertexPicking()
        {
            EditorView editorView = EditorView.Instance;
            EditorCamera camera = EditorCamera.Instance;

            if (!editorView.IsButtonReleased(MouseButton.Left))
                return;

            using (new ProfilingScope("PickVertices"))
            {
                float relativeScreenSize = Math.Min(editorView.ViewSize.X, editorView.ViewSize.Y) * 0.075f;

                using RentedList<PotentialVertexData> points = new RentedList<PotentialVertexData>();

                foreach (var (brush, selection) in _selectionGroup.Selection)
                {
                    Span<Vector3> vertices = brush.Vertices;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        (Vector2 screen, bool isBehindViewer) = camera.ProjectToScreen(vertices[i]);
                        if (isBehindViewer)
                            continue;

                        float dist = Vector3.Distance(vertices[i], camera.Position);
                        if (Vector2.Distance(screen, editorView.MousePosition) <= relativeScreenSize / dist)
                        {
                            points.Add(new PotentialVertexData(screen, dist, brush, (BrushVertexIndex)i, selection.IsVertexSelected((BrushVertexIndex)i)));
                        }
                    }
                }

                if (!points.IsEmpty)
                {
                    points.AsSpan().Sort(static (x, y) => x.Distance.CompareTo(y.Distance));

                    foreach (PotentialVertexData vertexData in points)
                    {
                        Boundaries screenBounds = Boundaries.Grow(new Boundaries(vertexData.Screen, vertexData.Screen), new Vector2(relativeScreenSize / vertexData.Distance));

                        if (screenBounds.IsWithin(editorView.MousePosition))
                        {
                            Gizmos.DrawWireSphere(vertexData.Brush.Vertices[(int)vertexData.VertexIndex], 0.1f);

                            if (vertexData.IsSelected)
                                _selectionGroup.Deselect(vertexData.Brush, vertexData.VertexIndex);
                            else
                                _selectionGroup.Select(vertexData.Brush, vertexData.VertexIndex);
                            break;
                        }
                    }
                }
            }
        }

        internal void AddInvalidScene(GeoSceneAsset asset, GeoScene scene)
        {
            lock (_lock)
            {
                _invalidScenes.Add((asset, scene));
            }
        }

        internal void RemoveInvalidScene(GeoSceneAsset asset, GeoScene scene)
        {
            lock (_lock)
            {
                _invalidScenes.Remove((asset, scene));
            }
        }

        internal void SetNewFocus(SceneEntity entity, GeoSceneAsset asset, UIDockHost? hostToOpenIn = null)
        {
            EditorRuntime runtime = EditorRuntime.GlobalSingleton;
            foreach (var kvp in runtime.UIManager.WindowManager.Active)
            {
                if (kvp.Value is GeoHierchyWindow window)
                {
                    if (window.TargetAsset == asset)
                    {
                        window.TargetEntity = entity;
                        window.Focus();
                    }
                    else if (window.TargetAsset == null)
                    {
                        window.TargetAsset = asset;
                        window.TargetEntity = entity;
                        window.Focus();
                    }

                    return;
                }
            }

            if (_toolsViewSnippet == null)
            {
                EditorViewWindow editorView = runtime.UIManager.FindWindow<EditorViewWindow>()!;
                _toolsViewSnippet = editorView.AddViewSnippet<ToolsViewSnippet>("Editor/UI/Snippets/Geo/EditorViewTools.snippet");
            }

            GeoHierchyWindow newWindow = runtime.UIManager.OpenWindow<GeoHierchyWindow>(hostToOpenIn, "Editor/UI/Geo/GeoHierchy.layout");

            newWindow.TargetEntity = entity;
            newWindow.TargetAsset = asset;

            newWindow.Focus();
        }

        internal HashSet<(GeoSceneAsset, GeoScene)> InvalidScenes => _invalidScenes;
        internal Lock Lock => _lock;

        internal GeoSelectionGroup SelectionGroup => _selectionGroup;

        internal ToolsViewSnippet? ToolsSnippet => _toolsViewSnippet;

        private readonly record struct PotentialVertexData(Vector2 Screen, float Distance, Brush Brush, BrushVertexIndex VertexIndex, bool IsSelected);
    }
}
