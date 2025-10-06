using Arch.Core;
using CommunityToolkit.HighPerformance;
using Editor.DearImGui;
using Editor.Interaction.Controls;
using Primary.Components;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Scenes;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Editor.Interaction.Tools
{
    public sealed class TranslateTool : ITool
    {
        private ToolManager _tools;

        private bool _hasBegunDragging;
        private float _startDist;
        private Vector3 _axisLock;
        private DragAxis _dragAxis;
        private Quaternion _baseRotation;

        private Vector3 _lastDragDelta;

        private bool _isHoveringAxis;
        private Vector3 _hoveredAxis;

        private List<StoredData> _storedPositions;

        internal TranslateTool(ToolManager toolManager)
        {
            _tools = toolManager;

            _storedPositions = new List<StoredData>();
        }

        public void Selected()
        {
            _storedPositions.Clear();

            IControlTool control = _tools.ActiveControlTool;
            foreach (ref readonly IToolTransform @base in control.Transforms)
            {
                _storedPositions.Add(new StoredData(@base, @base.WorldMatrix.Translation));
            }

            control.NewTransformSelected += Event_NewTransformSelected;
            control.OldTransformDeselected += Event_OldTransformDeselected;

            _isHoveringAxis = false;
            _hoveredAxis = Vector3.Zero;

            //InputManager.AddLayout()
        }

        public void Deselected()
        {
            _storedPositions.Clear();

            IControlTool control = _tools.ActiveControlTool;
            control.NewTransformSelected -= Event_NewTransformSelected;
            control.OldTransformDeselected -= Event_OldTransformDeselected;
        }

        public void Reset()
        {
            Deselected();
            Selected();
        }

        private void Event_NewTransformSelected(IToolTransform transform)
        {
            _storedPositions.Add(new StoredData(transform, transform.WorldMatrix.Translation));
        }

        private void Event_OldTransformDeselected(IToolTransform transform)
        {
            int idx = _storedPositions.FindIndex((x) => x.Selected == transform);
            if (idx != -1)
            {
                _storedPositions.RemoveAt(idx);
            }
        }

        public void Update()
        {
            if (!_hasBegunDragging)
                CheckForDragBegin();
            else
                HandleActiveDrag();
        }

        private void CheckForDragBegin()
        {
            SceneView sceneView = Editor.GlobalSingleton.SceneView;
            if (!sceneView.IsViewActive)
                return;

            Vector3 absoluteMin = Vector3.PositiveInfinity;
            Vector3 absoluteMax = Vector3.NegativeInfinity;

            Quaternion baseQuat = Quaternion.Identity;
            Matrix4x4 baseMatrix = Matrix4x4.Identity;
            bool isFirstQuat = false;

            Span<StoredData> span = _storedPositions.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                ref StoredData data = ref span[i];

                data.BasePosition = data.Selected.WorldMatrix.Translation;

                absoluteMin = Vector3.Min(absoluteMin, data.BasePosition);
                absoluteMax = Vector3.Max(absoluteMax, data.BasePosition);

                if (!isFirstQuat)
                {
                    isFirstQuat = Matrix4x4.Decompose(data.Selected.WorldMatrix, out _, out baseQuat, out _);
                }
            }

            Vector3 origin = Vector3.Lerp(absoluteMin, absoluteMax, 0.5f);
            Matrix4x4 lookMatrix = Matrix4x4.CreateFromQuaternion(baseQuat) * Matrix4x4.CreateTranslation(origin);

            Vector3 cameraPos = sceneView.CameraTranslation;
            Matrix4x4 vp = sceneView.ViewMatrix * sceneView.ProjectionMatrix;

            Vector3 relative = Vector3.Transform(cameraPos - origin, Matrix4x4.Transpose(lookMatrix));

            bool negX = relative.X < 0.0f;
            bool negY = relative.Y < 0.0f;
            bool negZ = relative.Z < 0.0f;

            float scale = 1.0f; //MathF.Max(MathF.Min(Vector3.Distance(origin, cameraPos) * 0.1f, 1.5f), 0.25f);

            float shortLength = 3.0f * scale;
            float longLength = 3.5f * scale;

            float startLength = scale * 0.75f;

            Vector2 clientSize = sceneView.OutputClientSize * 0.5f;

            Vector2 mouseHit = Editor.GlobalSingleton.SceneView.LocalMouseHit;
            mouseHit = new Vector2(mouseHit.X - clientSize.X, clientSize.Y - mouseHit.Y);

            DragAxis axis = DragAxis.None;

            _isHoveringAxis = false;

            //x axis
            {
                float xValue = (negX ? -shortLength : longLength);
                float yValue = (negY ? -0.25f : 0.25f) * scale;
                float zValue = (negZ ? -0.25f : 0.25f) * scale;

                Vector3 hit1 = Vector3.Transform(new Vector3(negX ? -startLength : startLength, 0.0f, 0.0f), lookMatrix);
                Vector3 hit2 = Vector3.Transform(new Vector3(negX ? -shortLength : shortLength, yValue, 0.0f), lookMatrix);
                Vector3 hit3 = Vector3.Transform(new Vector3(negX ? -longLength : longLength, 0.0f, 0.0f), lookMatrix);
                Vector3 hit4 = Vector3.Transform(new Vector3(negX ? -shortLength : shortLength, 0.0f, zValue), lookMatrix);

                bool hovered = ScreenRectDetection(vp, clientSize, mouseHit, hit1, hit2, hit3, hit4);

                if (hovered)
                {
                    _isHoveringAxis = true;
                    _hoveredAxis = Vector3.Transform(Vector3.UnitX, baseQuat);

                    axis = DragAxis.X;

                    goto HasFoundAxis;
                }
            }

            //y axis
            {
                float xValue = (negX ? -0.25f : 0.25f) * scale;
                float yValue = (negY ? -shortLength : longLength);
                float zValue = (negZ ? -0.25f : 0.25f) * scale;

                Vector3 hit1 = Vector3.Transform(new Vector3(0.0f, negY ? -startLength : startLength, 0.0f), lookMatrix);
                Vector3 hit2 = Vector3.Transform(new Vector3(xValue, negY ? -shortLength : shortLength, 0.0f), lookMatrix);
                Vector3 hit3 = Vector3.Transform(new Vector3(0.0f, negY ? -longLength : longLength, 0.0f), lookMatrix);
                Vector3 hit4 = Vector3.Transform(new Vector3(0.0f, negY ? -shortLength : shortLength, zValue), lookMatrix);

                bool hovered = ScreenRectDetection(vp, clientSize, mouseHit, hit1, hit2, hit3, hit4);

                if (hovered)
                {
                    _isHoveringAxis = true;
                    _hoveredAxis = Vector3.Transform(Vector3.UnitY, baseQuat);

                    axis = DragAxis.Y;

                    goto HasFoundAxis;
                }
            }

            //z axis
            {
                float xValue = (negX ? -0.25f : 0.25f) * scale;
                float yValue = (negY ? -0.25f : 0.25f) * scale;
                float zValue = (negZ ? -shortLength : longLength);

                Vector3 hit1 = Vector3.Transform(new Vector3(0.0f, 0.0f, negZ ? -startLength : startLength), lookMatrix);
                Vector3 hit2 = Vector3.Transform(new Vector3(xValue, 0.0f, negZ ? -shortLength : shortLength), lookMatrix);
                Vector3 hit3 = Vector3.Transform(new Vector3(0.0f, 0.0f, negZ ? -longLength : longLength), lookMatrix);
                Vector3 hit4 = Vector3.Transform(new Vector3(0.0f, yValue, negZ ? -shortLength : shortLength), lookMatrix);

                bool hovered = ScreenRectDetection(vp, clientSize, mouseHit, hit1, hit2, hit3, hit4);

                if (hovered)
                {
                    _isHoveringAxis = true;
                    _hoveredAxis = Vector3.Transform(Vector3.UnitZ, baseQuat);

                    axis = DragAxis.Z;

                    goto HasFoundAxis;
                }
            }

            //Ray ray = ExMath.ViewportToWorld(sceneView.ProjectionMatrix, sceneView.ViewMatrix, sceneView.RelativeMouseHit);
            //float dist = InfinitePlane.Intersect(new InfinitePlane(origin, planeNormal), ray);
            //if (dist >= 0.0f)
            //{
            //    Log.Information("{x}", ray.AtDistance(dist));
            //}

            return;

        HasFoundAxis:

            Ray ray = ExMath.ViewportToWorld(sceneView.ProjectionMatrix, sceneView.ViewMatrix, sceneView.RelativeMouseHit);
            float dist = GetPlaneDistance(axis, origin, ray, baseQuat, Vector3.Normalize(_hoveredAxis));

            if (float.IsRealNumber(dist))
            {
                if (InputSystem.Pointer.IsButtonPressed(MouseButton.Left))
                {
                    _hasBegunDragging = true;
                    _startDist = dist;
                    _axisLock = Vector3.Normalize(_hoveredAxis);
                    _dragAxis = axis;
                    _baseRotation = baseQuat;
                }
            }
            else
            {
                _isHoveringAxis = false;
            }
        }

        private void HandleActiveDrag()
        {
            SceneView sceneView = Editor.GlobalSingleton.SceneView;
            if (!InputSystem.Pointer.IsButtonHeld(MouseButton.Left) || !sceneView.IsViewVisible)
                _hasBegunDragging = false;

            Vector3 absoluteMin = Vector3.Zero;
            Vector3 absoluteMax = Vector3.Zero;

            Span<StoredData> span = _storedPositions.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                ref StoredData data = ref span[i];

                absoluteMin = Vector3.Min(absoluteMin, data.BasePosition);
                absoluteMax = Vector3.Max(absoluteMax, data.BasePosition);
            }

            Vector3 origin = Vector3.Lerp(absoluteMin, absoluteMax, 0.5f);

            Vector3 cameraPos = sceneView.CameraTranslation;
            Matrix4x4 vp = sceneView.ViewMatrix * sceneView.ProjectionMatrix;

            Ray ray = ExMath.ViewportToWorld(sceneView.ProjectionMatrix, sceneView.ViewMatrix, sceneView.RelativeMouseHit);
            float dist = GetPlaneDistance(_dragAxis, origin, ray, _baseRotation, _axisLock);

            if (dist != 0.0f)
            {
                Vector3 delta = _axisLock * (dist - _startDist);
                if (delta != Vector3.Zero && ToolManager.IsSnappingActive)
                    delta = Vector3.Round(delta / ToolManager.SnapScale) * ToolManager.SnapScale;

                if (_lastDragDelta != delta)
                {
                    _lastDragDelta = Vector3.Zero;

                    for (int i = 0; i < span.Length; i++)
                    {
                        ref StoredData data = ref span[i];
                        data.Selected.SetWorldTransform(data.BasePosition + delta, delta);

                        if (!_hasBegunDragging)
                        {
                            data.Selected.CommitTransform();
                        }
                    }
                }
            }
        }

        private static bool ScreenRectDetection(Matrix4x4 matrix, Vector2 clientSize, Vector2 hit, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            Vector4 proj1 = Vector4.Transform(p1, matrix); ;
            Vector4 proj2 = Vector4.Transform(p2, matrix); ;
            Vector4 proj3 = Vector4.Transform(p3, matrix); ;
            Vector4 proj4 = Vector4.Transform(p4, matrix); ;

            Vector256<float> combined = Vector256.Create(proj1.X, proj1.Y, proj2.X, proj2.Y, proj3.X, proj3.Y, proj4.X, proj4.Y);

            combined /= Vector256.Create(proj1.W, proj1.W, proj2.W, proj2.W, proj3.W, proj3.W, proj4.W, proj4.W);
            combined *= Vector256.Create(clientSize.X, clientSize.Y, clientSize.X, clientSize.Y, clientSize.X, clientSize.Y, clientSize.X, clientSize.Y);

            Vector2 ab1 = new Vector2(combined.GetElement(0), combined.GetElement(1));
            Vector2 ab2 = new Vector2(combined.GetElement(2), combined.GetElement(3));
            Vector2 ab3 = new Vector2(combined.GetElement(4), combined.GetElement(5));
            Vector2 ab4 = new Vector2(combined.GetElement(6), combined.GetElement(7));

            return IsWithinTri(hit, ab1, ab2, ab4) || IsWithinTri(hit, ab2, ab3, ab4);
        }

        private static bool IsWithinTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
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

        public bool IsInteracting => _isHoveringAxis || _hasBegunDragging;
        public bool IsActive => _hasBegunDragging;

        private static Vector3 FindNearestOnInfiniteLine(Vector3 origin, Vector3 direction, Vector3 point)
        {
            Vector3 lhs = point - origin;

            float dotP = Vector3.Dot(lhs, direction);
            return origin + direction * dotP;
        }

        private static float GetPlaneDistance(DragAxis axis, Vector3 origin, Ray ray, Quaternion rotation, Vector3 vectorAxis)
        {
            float dist1;
            float dist2;

            switch (axis)
            {
                case DragAxis.X:
                    {
                        dist1 = InfinitePlane.Intersect(new InfinitePlane(origin, Vector3.Transform(Vector3.UnitY, rotation)), ray);
                        dist2 = InfinitePlane.Intersect(new InfinitePlane(origin, Vector3.Transform(Vector3.UnitZ, rotation)), ray);
                        break;
                    }
                case DragAxis.Y:
                    {
                        dist1 = InfinitePlane.Intersect(new InfinitePlane(origin, Vector3.Transform(Vector3.UnitX, rotation)), ray);
                        dist2 = InfinitePlane.Intersect(new InfinitePlane(origin, Vector3.Transform(Vector3.UnitZ, rotation)), ray);
                        break;
                    }
                case DragAxis.Z:
                    {
                        dist1 = InfinitePlane.Intersect(new InfinitePlane(origin, Vector3.Transform(Vector3.UnitY, rotation)), ray);
                        dist2 = InfinitePlane.Intersect(new InfinitePlane(origin, Vector3.Transform(Vector3.UnitX, rotation)), ray);
                        break;
                    }
                default: return float.MinValue;
            }

            if (dist1 < 0.0f && dist2 < 0.0f)
                return 0.0f;

            float globalDist = float.MinValue;
            if (dist1 < 0.0f)
                globalDist = dist2;
            else if (dist2 < 0.0f)
                globalDist = dist1;
            else
                globalDist = MathF.Min(dist1, dist2);

            Vector3 rayAt = ray.AtDistance(globalDist);
            Vector3 result = rayAt - origin;
            float length = result.Length();

            return Vector3.Dot(vectorAxis, rayAt);
        }

        private record struct StoredData(IToolTransform Selected, Vector3 BasePosition);

        private enum DragAxis : byte
        {
            None = 0,

            X,
            Y,
            Z
        }
    }
}
