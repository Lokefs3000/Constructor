using Editor.Assets.Types;
using Editor.DearImGui;
using Editor.Geometry;
using Editor.Geometry.Shapes;
using Editor.Interaction;
using Editor.Interaction.Tools;
using Editor.Rendering;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.GeoEdit
{
    internal sealed class PlaceTool : IGeoTool
    {
        private readonly GeoEditorView _editorView;

        private bool _isDragValid;
        private bool _isPlacingBrush;
        private Vector3 _startPlacement;
        private Vector3 _startPlacementNormal;

        private GeoBrush? _temporaryBrush;
        private IGeoShape? _temporaryShape;
        private CachedGeoShape? _computedShape;
        private GeoShapePickResult? _brushPickResult;

        internal PlaceTool(GeoEditorView editorView)
        {
            _editorView = editorView;
        }

        private void ClearData(bool clearSelection)
        {
            if (clearSelection)
            {
                SelectionManager.DeselectMultiple((x) =>
                {
                    if (x is SelectedGeoBrush geoBrush)
                        return geoBrush.Brush == _temporaryBrush;
                    else if (x is SelectedGeoShapeBase geoShapeBase)
                        return geoShapeBase.Brush == _temporaryBrush;

                    return false;
                });
            }

            _isDragValid = false;
            _isPlacingBrush = false;
            _startPlacement = Vector3.Zero;
            _startPlacementNormal = Vector3.Zero;

            _temporaryBrush = null;
            _temporaryShape = null;
            _computedShape = null;
            _brushPickResult = null;
        }

        public void ConnectEvents()
        {
            ClearData(true);

            SceneView view = Editor.GlobalSingleton.SceneView;
            view.MouseDown += Event_MouseDown;
            view.MouseUp += Event_MouseUp;
            view.MouseMoved += Event_MouseMoved;
            view.KeyDown += Event_KeyDown;
        }

        public void DisconnectEvents()
        {
            if (_isPlacingBrush || _temporaryShape != null)
                _editorView.UnlockCurrentScene();

            ClearData(true);

            SceneView view = Editor.GlobalSingleton.SceneView;
            view.MouseDown -= Event_MouseDown;
            view.MouseUp -= Event_MouseUp;
            view.MouseMoved -= Event_MouseMoved;
            view.KeyDown -= Event_KeyDown;
        }

        public void Update()
        {

        }

        public void Render(ref readonly GeoToolRenderInterface @interface)
        {
            SceneView sceneView = Editor.GlobalSingleton.SceneView;
            if (_temporaryShape == null)
            {
                if (_isPlacingBrush)
                {
                    Ray ray = sceneView.CameraMouseRay;
                    float hitDist = InfinitePlane.Intersect(new InfinitePlane(_startPlacement, _startPlacementNormal), ray);

                    if (hitDist >= 0.0f)
                    {
                        Vector3 hit = ray.AtDistance(hitDist);
                        if (ToolManager.IsSnappingActive)
                            hit = Vector3.Round(hit / ToolManager.SnapScale) * ToolManager.SnapScale;

                        Vector3 min = Vector3.Min(_startPlacement, hit);
                        Vector3 max = Vector3.Max(_startPlacement, hit);

                        DrawShape(in @interface, _editorView.Shape, new AABB(min, max));
                    }
                }
            }
            else
            {
                _temporaryShape.ForceDirty();
                GeoShapePickResult result = _brushPickResult.GetValueOrDefault(new GeoShapePickResult(Vector3.Zero, Vector3.Zero, -1));

                SelectedGeoBoxShape? selected = SelectionManager.FindSelected<SelectedGeoBoxShape>((x) => x.Shape == _temporaryShape);
                
                GeoMesh mesh = _temporaryShape.GenerateMesh();
                //_computedShape = GeoGenerator.Transform(mesh, _temporaryBrush!.Transform, false);

                Vector3 pivotOffset = _temporaryBrush!.Transform.Position + Vector3.Lerp(mesh.Boundaries.Minimum, mesh.Boundaries.Maximum, _temporaryBrush!.Transform.Origin);

                for (int i = 0; i < mesh.Faces.Length; i++)
                {
                    ref GeoFace face = ref mesh.Faces[i];
                    if (face.Type == GeoFaceType.Triangle)
                    {
                        Vector3 v0 = mesh.Vertices[face.Triangle.Point0.Index] + pivotOffset;
                        Vector3 v1 = mesh.Vertices[face.Triangle.Point1.Index] + pivotOffset;
                        Vector3 v2 = mesh.Vertices[face.Triangle.Point2.Index] + pivotOffset;

                        uint color;
                        {
                            Vector3 normal = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));
                            float dot = Vector3.Dot(normal, Vector3.Normalize(sceneView.CameraTranslation));
                            color = new Color32(new Vector3(dot * 0.5f + 0.5f), 0.5f).ARGB;

                            if (i == result.FaceIndex || i == (selected?.FaceIndex ?? -1))
                            {
                                color = (color & ~0x0000ffffu);
                            }
                        }

                        @interface.AddTriangle(v0, v1, v2, color);
                    }
                    else
                    {
                        Vector3 v0 = mesh.Vertices[face.Quad.Point0.Index] + pivotOffset;
                        Vector3 v1 = mesh.Vertices[face.Quad.Point1.Index] + pivotOffset;
                        Vector3 v2 = mesh.Vertices[face.Quad.Point2.Index] + pivotOffset;
                        Vector3 v3 = mesh.Vertices[face.Quad.Point3.Index] + pivotOffset;

                        uint color;
                        {
                            Vector3 normal = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));
                            color = new Color32(new Vector3(1.0f * 0.5f + 0.5f), 0.5f).ARGB;

                            if (i == result.FaceIndex || i == (selected?.FaceIndex ?? -1))
                            {
                                color = (color & ~0x0000ffffu);
                            }
                        }

                        @interface.AddTriangle(v3, v2, v0, color);
                        @interface.AddTriangle(v1, v3, v0, color);
                    }
                }

                DrawShape(in @interface, _editorView.Shape, new AABB(mesh.Boundaries.Minimum + pivotOffset, mesh.Boundaries.Maximum + pivotOffset));
            }

            void DrawShape(ref readonly GeoToolRenderInterface @interface, GeoEditorView.PlaceShape shape, AABB minmax)
            {
                const uint LineColor = 0xffffa080;

                Vector3 min = minmax.Minimum;
                Vector3 max = minmax.Maximum;

                switch (shape)
                {
                    case GeoEditorView.PlaceShape.Box:
                        {
                            @interface.AddLine(min, new Vector3(max.X, min.Y, min.Z), LineColor);
                            @interface.AddLine(new Vector3(min.X, min.Y, max.Z), new Vector3(max.X, min.Y, max.Z), LineColor);
                            @interface.AddLine(min, new Vector3(min.X, min.Y, max.Z), LineColor);
                            @interface.AddLine(new Vector3(max.X, min.Y, min.Z), new Vector3(max.X, min.Y, max.Z), LineColor);

                            @interface.AddLine(new Vector3(min.X, max.Y, min.Z), new Vector3(max.X, max.Y, min.Z), LineColor);
                            @interface.AddLine(new Vector3(min.X, max.Y, max.Z), new Vector3(max.X, max.Y, max.Z), LineColor);
                            @interface.AddLine(new Vector3(min.X, max.Y, min.Z), new Vector3(min.X, max.Y, max.Z), LineColor);
                            @interface.AddLine(new Vector3(max.X, max.Y, min.Z), new Vector3(max.X, max.Y, max.Z), LineColor);

                            @interface.AddLine(new Vector3(min.X, min.Y, min.Z), new Vector3(min.X, max.Y, min.Z), LineColor);
                            @interface.AddLine(new Vector3(min.X, min.Y, max.Z), new Vector3(min.X, max.Y, max.Z), LineColor);
                            @interface.AddLine(new Vector3(max.X, min.Y, min.Z), new Vector3(max.X, max.Y, min.Z), LineColor);
                            @interface.AddLine(new Vector3(max.X, min.Y, max.Z), new Vector3(max.X, max.Y, max.Z), LineColor);

                            break;
                        }
                }
            }
        }

        private void Event_MouseDown(ImGuiMouseButton button)
        {
            if (button == ImGuiMouseButton.Left)
            {
                if (_temporaryShape == null)
                {
                    ClearData(true);

                    _isDragValid = _editorView.LastPickResult.HasValue;
                    if (_editorView.LastPickResult.HasValue)
                    {
                        GeoPickResult result = _editorView.LastPickResult.Value;

                        _startPlacement = result.Position;
                        _startPlacementNormal = result.Normal;
                    }
                }
                else
                {
                    if (_brushPickResult.HasValue)
                    {
                        GeoShapePickResult result = _brushPickResult.Value;

                        GeoMesh mesh = _temporaryShape.GenerateMesh();
                        ref GeoFace face = ref mesh.Faces[result.FaceIndex];

                        if (face.ShapeFaceIndex != ushort.MaxValue)
                        {
                            SelectionManager.Select(new SelectedGeoBoxShape(_temporaryBrush!, face.ShapeFaceIndex), SelectionMode.Multi);
                        }
                    }
                }
            }
        }

        private void Event_MouseUp(ImGuiMouseButton button)
        {
            if (_temporaryShape == null && button == ImGuiMouseButton.Left)
            {
                Ray ray = Editor.GlobalSingleton.SceneView.CameraMouseRay;
                float hitDist = InfinitePlane.Intersect(new InfinitePlane(_startPlacement, _startPlacementNormal), ray);

                if (hitDist > 0.0f)
                {
                    Vector3 hit = ray.AtDistance(hitDist);
                    if (ToolManager.IsSnappingActive)
                        hit = Vector3.Round(hit / ToolManager.SnapScale) * ToolManager.SnapScale;

                    Vector3 min = Vector3.Min(_startPlacement, hit);
                    Vector3 max = Vector3.Max(_startPlacement, hit);

                    Vector3 delta = max - min;

                    IGeoShape shape = _editorView.Shape switch
                    {
                        GeoEditorView.PlaceShape => new GeoBoxShape()
                        {
                            Extents = delta + _startPlacementNormal
                        }
                    };

                    ClearData(true);

                    _temporaryBrush = new GeoBrush(new GeoTransform(min, Quaternion.Identity, Vector3.Zero), shape);
                    _temporaryShape = shape;
                }
                else
                {
                    if (_isPlacingBrush)
                        _editorView.UnlockCurrentScene();

                    ClearData(true);
                }
            }
        }

        private void Event_MouseMoved(Vector2 screen, Vector2 viewport)
        {
            SceneView view = Editor.GlobalSingleton.SceneView;
            if (_temporaryShape == null)
            {
                if (_isDragValid && !_isPlacingBrush)
                {
                    if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                    {
                        _isPlacingBrush = true;
                        _editorView.LockCurrentScene();
                    }
                }
            }
            else
            {
                ITool tool = Editor.GlobalSingleton.ToolManager.ToolObject;
                if (!tool.IsInteracting && GeoPicker.RaycastFaceWithinShape(view.CameraMouseRay, _temporaryShape, _temporaryBrush!.Transform, out GeoShapePickResult result))
                {
                    _brushPickResult = result;
                }
                else
                {
                    _brushPickResult = null;
                }
            }
        }

        private void Event_KeyDown()
        {
            if (_temporaryShape != null && _editorView.ActiveScene != null)
            {
                if (InputSystem.Keyboard.IsKeyDown(KeyCode.Return))
                {
                    GeoSceneAsset asset = _editorView.ActiveScene;
                    if (asset.BrushScene != null)
                    {
                        asset.BrushScene.AddBrush(_temporaryBrush!);

                        SelectionManager.Select(new SelectedGeoBrush(_temporaryBrush!), SelectionMode.Multi);

                        ClearData(false);
                        _editorView.UnlockCurrentScene();
                    }
                }
            }
        }
    }
}
