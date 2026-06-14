using Editor.Gui.View;
using Editor.Gui.Windows;
using Editor.Interaction.Controls;
using Editor.Rendering;
using Editor.Rendering.Tools;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Rendering;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Editor.Interaction.Tools
{
    internal class TranslateTool : ITool
    {
        private bool _isActive;
        private bool _isAnyTransformActive;

        private Vector3 _toolOrigin;
        private Quaternion _toolQuat;

        private bool _isDragActive;

        private ToolHandleAxis _activeAxis;
        private Ray _axisRay;
        private Vector3 _hitStartPosition;

        private Vector3 _lastHitPosition;
        private Vector3 _dragDelta;

        private List<StoredTransformData> _activeTransforms;

        public TranslateTool()
        {
            _activeTransforms = new List<StoredTransformData>();
        }

        public void Selected(ToolManager tools)
        {
            _isActive = true;
            _isAnyTransformActive = false;
        }

        public void Deselected(ToolManager tools)
        {
            _isActive = false;
            _isAnyTransformActive = false;

            _isDragActive = false;
            _activeTransforms.Clear();
        }

        public void Update(ToolManager tools)
        {
            ROList<ToolTransformData> transforms = tools.Transforms;
            if (transforms.Count == 0)
            {
                _isDragActive = false;
                _isAnyTransformActive = false;
                return;
            }

            if (_isDragActive)
                HandleDragLogic(transforms);
            else
                HandlePickingLogic(transforms);
        }

        private void HandleDragLogic(ROList<ToolTransformData> transforms)
        {
            EditorCamera camera = EditorCamera.Instance;
            EditorView view = EditorView.Instance;

            Ray ray = camera.ProjectToRay(view.MousePosition);
            Vector3 hit = _axisRay.AtDistance(_axisRay.FindClosest(ray));
            if (hit != _lastHitPosition)
            {
                Vector3 delta = hit - _hitStartPosition;
                if (ToolManager.IsSnappingActive)
                    delta = Vector3.Round(delta / ToolManager.SnapScale) * ToolManager.SnapScale;

                foreach (StoredTransformData transformData in _activeTransforms)
                {
                    transformData.Transform.SetWorldTransform(hit, delta);
                }

                _lastHitPosition = hit;
                _dragDelta = delta;
            }

            if (!view.IsButtonHeld(MouseButton.Left))
            {
                foreach (StoredTransformData transformData in _activeTransforms)
                {
                    transformData.Transform.CommitTransform();
                }

                _activeTransforms.Clear();
                _isDragActive = false;
            }
        }

        private void HandlePickingLogic(ROList<ToolTransformData> transforms)
        {
            Vector3 center = Vector3.Zero;
            Quaternion quat = Quaternion.Identity;

            {
                Vector3 min = Vector3.PositiveInfinity;
                Vector3 max = Vector3.NegativeInfinity;

                bool isFirst = true;

                foreach (ToolTransformData transformData in transforms)
                {
                    IToolTransform transform = transformData.Transform;
                    if (!transform.IsActive)
                        continue;

                    if (Matrix4x4.Decompose(transform.WorldMatrix, out _, out Quaternion transformQuat, out Vector3 transformPos))
                    {
                        if (isFirst)
                        {
                            min = transformPos;
                            max = transformPos;

                            quat = transformQuat;

                            isFirst = false;
                        }
                        else
                        {
                            min = Vector3.Min(min, transformPos);
                            max = Vector3.Max(max, transformPos);
                        }
                    }
                }

                _isAnyTransformActive = !isFirst;
                if (isFirst)
                    return;

                center = Vector3.Lerp(min, max, 0.5f);
            }

            _toolOrigin = center;
            _toolQuat = quat;

            _activeAxis = unchecked((ToolHandleAxis)(-1));

            EditorCamera camera = EditorCamera.Instance;
            EditorView view = EditorView.Instance;

            float handleScale = Math.Clamp(Vector3.Distance(camera.Position, _toolOrigin) * 0.15f, 0.25f, 2.0f);

            const float ArrowWidth = 0.3f;

            (Vector2 screenOrigin, bool isBehindViewer) = camera.ProjectToScreen(_toolOrigin);
            if (isBehindViewer)
                return;

            Vector3 xAxis = Vector3.Transform(Vector3.UnitX, quat) * handleScale;
            Vector3 yAxis = Vector3.Transform(Vector3.UnitY, quat) * handleScale;
            Vector3 zAxis = Vector3.Transform(Vector3.UnitZ, quat) * handleScale;

            Vector3 xArrowAxis = xAxis * ArrowWidth;
            Vector3 yArrowAxis = yAxis * ArrowWidth;
            Vector3 zArrowAxis = zAxis * ArrowWidth;

            ProjectAndHandleAxis(camera, view, ToolHandleAxis.X, view.MousePosition, screenOrigin, xAxis, yArrowAxis, zArrowAxis, false, false, out Vector2 extentX);
            ProjectAndHandleAxis(camera, view, ToolHandleAxis.Y, view.MousePosition, screenOrigin, yAxis, -xArrowAxis, zArrowAxis, false, false, out Vector2 extentY);
            ProjectAndHandleAxis(camera, view, ToolHandleAxis.Z, view.MousePosition, screenOrigin, zAxis, xArrowAxis, -yArrowAxis, false, false, out Vector2 extentZ);

            Vector2 screenMin = Vector2.Min(Vector2.Min(extentX, extentY), extentZ);
            Vector2 screenMax = Vector2.Max(Vector2.Max(extentX, extentY), extentZ);

            if (screenMin.X > camera.ClientSize.X || screenMin.Y > camera.ClientSize.Y || screenMax.X < 0.0f || screenMax.Y < 0.0f)
            {
                _activeAxis = unchecked((ToolHandleAxis)(-1));
                _isDragActive = false;
                return;
            }

            if (_isDragActive)
            {
                foreach (ToolTransformData transformData in transforms)
                {
                    _activeTransforms.Add(new StoredTransformData(transformData.Transform, transformData.Transform.Position));
                }
            }
        }

        private void ProjectAndHandleAxis(EditorCamera camera, EditorView view, ToolHandleAxis axis, Vector2 point, Vector2 screenOrigin, Vector3 direction, Vector3 axis1, Vector3 axis2, bool flipAxis1, bool flipAxis2, out Vector2 extent)
        {
            const float ArrowStep = 0.7f;

            Vector3 camPosition = camera.Position - _toolOrigin;

            bool isBeside = Vector3.Dot(direction, camPosition) < 0.0f;
            bool isBelow = flipAxis1 ? (Vector3.Dot(axis1, camPosition) >= 0.0f) : (Vector3.Dot(axis1, camPosition) < 0.0f);
            bool isBehind = flipAxis2 ? (Vector3.Dot(axis2, camPosition) >= 0.0f) : (Vector3.Dot(axis2, camPosition) < 0.0f);

            if (isBeside)
                direction = -direction;
            if (isBelow)
                axis1 = -axis1;
            if (isBehind)
                axis2 = -axis2;

            Vector3 partialOrigin = Vector3.Lerp(_toolOrigin, _toolOrigin + direction, ArrowStep);

            Vector2 end = camera.ProjectToScreen(_toolOrigin + direction * 1.15f).Position;
            Vector2 axis1End = camera.ProjectToScreen(partialOrigin + axis1).Position;
            Vector2 axis2End = camera.ProjectToScreen(partialOrigin + axis2).Position;

            ScreenGizmos.DrawWireTriangle(axis1End, axis2End, screenOrigin);
            ScreenGizmos.DrawWireTriangle(axis1End, axis2End, end);

            if (IsWithinTriangle(point, axis1End, axis2End, screenOrigin) || IsWithinTriangle(point, axis1End, axis2End, end))
            {
                _activeAxis = axis;

                if (view.IsButtonPressed(MouseButton.Left))
                {
                    Ray ray = camera.ProjectToRay(view.MousePosition);

                    Quaternion quat = axis switch
                    {
                        ToolHandleAxis.X => Quaternion.CreateFromYawPitchRoll(0.0f, 0.0f, float.DegreesToRadians(90.0f)),
                        ToolHandleAxis.Y => Quaternion.CreateFromYawPitchRoll(0.0f, float.DegreesToRadians(90.0f), 0.0f),
                        ToolHandleAxis.Z => Quaternion.CreateFromYawPitchRoll(0.0f, float.DegreesToRadians(90.0f), 0.0f),
                        _ => throw new NotImplementedException()
                    };
                    Vector3 normal = Vector3.Normalize(Vector3.Transform(direction, quat));

                    Vector3 axisNormal = Vector3.Normalize(-Vector3.Abs(direction));

                    _axisRay = new Ray(_toolOrigin, axisNormal);
                    _hitStartPosition = _axisRay.AtDistance(_axisRay.FindClosest(ray));

                    _lastHitPosition = _hitStartPosition;
                    _dragDelta = Vector3.Zero;

                    _isDragActive = true;
                }
            }

            extent = end;
        }

        public bool Render(ToolManager tools, ToolDrawData drawData)
        {
            if (!_isAnyTransformActive)
                return false;

            drawData.HandleType = ToolHandleType.Translate;

            drawData.HandleOrigin = _toolOrigin;
            drawData.HandleRotation = _toolQuat;

            if (_activeAxis != unchecked((ToolHandleAxis)(-1)))
            {
                drawData.HandleAxisColors[(int)_activeAxis] = Color.Yellow;
            }

            if (_isDragActive)
                drawData.HandleOrigin += _dragDelta;

            return true;
        }

        private void OldTransformDeselected(IToolTransform transform)
        {
            for (int i = 0; i < _activeTransforms.Count; ++i)
            {
                if (_activeTransforms[i].Transform == transform)
                {
                    _activeTransforms.RemoveAt(i);
                    break;
                }
            }
        }

        public bool IsActive => _isActive;
        public bool IsInteracting => _isDragActive;

        private static bool IsWithinTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);

            bool hasNeg = Vector128.LessThanAny(Vector128.Create(d1, d2, d3, 0.0f), Vector128.CreateScalar(0.0f));
            bool hasPos = Vector128.GreaterThanAny(Vector128.Create(d1, d2, d3, 0.0f), Vector128.CreateScalar(0.0f));

            return !(hasNeg && hasPos);

            static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
            {
                Vector128<float> temp =
                    Vector128.Create(p1.X, p2.Y, p2.X, p1.Y) -
                    Vector128.Create(p3.X, p3.Y, p3.X, p3.Y);
                return temp.GetElement(0) * temp.GetElement(1) - temp.GetElement(2) * temp.GetElement(3);
            }
        }

        private readonly record struct StoredTransformData(IToolTransform Transform, Vector3 Position);
    }
}
