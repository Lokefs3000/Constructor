using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Primary.Collections;
using Primary.Common;
using Primary.Mathematics;
using Primary.Rendering.Diagnostics;
using Primary.Timing;
using VoxelizationDemo.Core;
using VoxelizationDemo.Logic;

namespace VoxelizationDemo.Editor.Gizmo
{
    internal sealed class RotateGizmo : IInteractionGizmo
    {
        private bool _isEnabled;

        private Vector3 _origin;
        private Quaternion _rotation;

        private TranslateGizmoHit _hoverHit;
        private TranslateGizmoHit _currentHit;

        private Quaternion _startQuaternion;
        private Quaternion _lastHitQuaternion;

        private Vector3 _rotationOrigin;
        private Vector3 _rotationAxis;

        private Quaternion _rotationQuaternion;
        private Quaternion _inverseRotationQuat;

        public RotateGizmo()
        {
            _isEnabled = true;

            _origin = Vector3.Zero;
            _rotation = Quaternion.Identity;

            _hoverHit = TranslateGizmoHit.None;
            _currentHit = TranslateGizmoHit.None;

            _startQuaternion = Quaternion.Identity;
            _lastHitQuaternion = Quaternion.Identity;

            _rotationOrigin = Vector3.Zero;
            _rotationAxis = Vector3.Zero;

            _rotationQuaternion = Quaternion.Identity;
            _inverseRotationQuat = Quaternion.Identity;
        }

        public void GenerateVertices(Frustrum frustrum, Vector3 center, ref RentedList<InteractionGizmoVertex> vertices, ref RentedList<ushort> indices, out Matrix4x4 model)
        {
            model = Matrix4x4.Identity;

            float scale = Vector3.Distance(_origin, center) * 0.075f * 1.4f;
            {
                Vector3 min = Vector3.Transform(new Vector3(-2.0f * scale), _rotation) + _origin;
                Vector3 max = Vector3.Transform(new Vector3(2.0f * scale), _rotation) + _origin;

                if (!frustrum.Intersects(new AABB(min, max)))
                    return;
            }

            model *= Matrix4x4.CreateFromQuaternion(_rotation);
            model *= Matrix4x4.CreateTranslation(_origin);

            Color axisXColor = s_xAxisColor;
            Color axisYColor = s_yAxisColor;
            Color axisZColor = s_zAxisColor;

            Color outerColor = new Color(0.775f);
            Color innerColor = new Color(0.775f, 0.15f);

            Vector3 unitX = Vector3.UnitX * scale;
            Vector3 unitY = Vector3.UnitY * scale;
            Vector3 unitZ = Vector3.UnitZ * scale;

            Quaternion invQuat = Quaternion.Inverse(_rotation);

            Vector2 directionX = Vector2.Normalize(Vector3.Shuffle(Vector3.Transform(center - (_origin + Vector3.UnitX), invQuat), 2, 1, 0).AsVector2());
            Vector2 directionY = Vector2.Normalize(Vector3.Shuffle(Vector3.Transform(center - (_origin + Vector3.UnitY), invQuat), 0, 2, 0).AsVector2());
            Vector2 directionZ = Vector2.Normalize(Vector3.Shuffle(Vector3.Transform(center - (_origin + Vector3.UnitZ), invQuat), 0, 1, 0).AsVector2());

            Vector3 towardsCenterVector = Vector3.Transform(Vector3.Normalize(center - _origin), invQuat);
            Vector3 lookRightVector = Vector3.Normalize(Vector3.Cross(towardsCenterVector, Vector3.UnitY));
            Vector3 lookUpVector = Vector3.Normalize(Vector3.Cross(towardsCenterVector, lookRightVector));

            float radiansX = -ExMath.GetAngleTowards(Vector2.Zero, directionX);
            float radiansY = -ExMath.GetAngleTowards(Vector2.Zero, directionY);
            float radiansZ = -ExMath.GetAngleTowards(Vector2.Zero, directionZ);

            float ringWidth = 0.04f;
            float ringThickness = ringWidth * scale;

            float outerRingWidth = 0.025f;
            float outerRingThickness = ringWidth * scale;
            float outerRingMargin = 1.5f * scale;

            if (_currentHit == TranslateGizmoHit.None)
            {
                switch (_hoverHit)
                {
                    case TranslateGizmoHit.X: axisXColor = Color.Yellow; break;
                    case TranslateGizmoHit.Y: axisYColor = Color.Yellow; break;
                    case TranslateGizmoHit.Z: axisZColor = Color.Yellow; break;
                    case TranslateGizmoHit.Axis: outerColor = new Color(0.9f, outerColor.A); break;
                }

                GenerateRing(unitZ, unitY, Vector3.UnitX, ringWidth, ringThickness, radiansX, float.Pi, axisXColor, ref vertices, ref indices);
                GenerateRing(unitX, unitZ, Vector3.UnitY, ringWidth, ringThickness, radiansY, float.Pi, axisYColor, ref vertices, ref indices);
                GenerateRing(unitX, unitY, Vector3.UnitZ, ringWidth, ringThickness, radiansZ, float.Pi, axisZColor, ref vertices, ref indices);

                GenerateRing(lookRightVector * outerRingMargin, lookUpVector * outerRingMargin, Vector3.UnitX, outerRingWidth, outerRingThickness, 0.0f, float.Pi * 2.0f, outerColor, ref vertices, ref indices);

                if (_hoverHit == TranslateGizmoHit.All)
                    GenerateCircle(lookRightVector, lookUpVector, scale, 0.0f, float.Pi * 2.0f, innerColor, ref vertices, ref indices);
            }
            else if (_currentHit != TranslateGizmoHit.All)
            {
                Quaternion startQuaternion = _startQuaternion;
                Quaternion quaternion = _lastHitQuaternion;

                Vector3 normalRightVector = Vector3.Transform(Vector3.Normalize(Vector3.Cross(_rotationAxis, Vector3.UnitY)), quaternion);
                Vector3 normalUpVector = Vector3.Normalize(Vector3.Cross(_rotationAxis, normalRightVector));

                Vector3 startRightVector = Vector3.Transform(Vector3.Normalize(Vector3.Cross(_rotationAxis, Vector3.UnitY)), startQuaternion);
                Vector3 startUpVector = Vector3.Normalize(Vector3.Cross(_rotationAxis, startRightVector));

                switch (_currentHit)
                {
                    case TranslateGizmoHit.X:
                        {
                            // GenerateCircle(unitZ, unitY, 1.0f, _startWorldAngle, _lastWorldAngle - _startWorldAngle, new Color(axisXColor) { A = 0.15f }, ref vertices, ref indices);
                            // GenerateLine(startRightVector * scale, _rotationAxis * scale, startUpVector, 0.0f, scale, ringWidth * 0.3f, axisXColor.Darken(0.4f), ref vertices, ref indices);
                            // GenerateLine(normalRightVector * scale, _rotationAxis * scale, normalUpVector, 0.0f, scale, ringWidth * 0.5f, s_xAxisColor, ref vertices, ref indices);
                            GenerateRing(unitZ, unitY, Vector3.UnitX, ringWidth, ringThickness, 0.0f, float.Pi * 2.0f, axisXColor, ref vertices, ref indices);
                            break;
                        }
                    case TranslateGizmoHit.Y:
                        {
                            GenerateRing(unitX, unitZ, Vector3.UnitY, ringWidth, ringThickness, 0.0f, float.Pi * 2.0f, axisYColor, ref vertices, ref indices);
                            break;
                        }
                    case TranslateGizmoHit.Z:
                        {
                            GenerateRing(unitX, unitY, Vector3.UnitZ, ringWidth, ringThickness, 0.0f, float.Pi * 2.0f, axisZColor, ref vertices, ref indices);
                            break;
                        }
                    case TranslateGizmoHit.Axis:
                        {
                            GenerateRing(lookRightVector * outerRingMargin, lookUpVector * outerRingMargin, Vector3.UnitX, outerRingWidth, outerRingThickness, 0.0f, float.Pi * 2.0f, outerColor, ref vertices, ref indices);
                            break;
                        }
                }
            }

            static void GenerateRing(Vector3 x, Vector3 y, Vector3 normal, float thickness, float width, float direction, float arcEnd, Color color, ref RentedList<InteractionGizmoVertex> vertices, ref RentedList<ushort> indices)
            {
                normal *= width * 0.5f;

                float innerScale = 1.0f - thickness;
                float halfInnerScale = 1.0f - thickness * 0.5f;

                int numSteps = (int)MathF.Ceiling(arcEnd / float.Pi * 24.0f);

                float angleStep = 1.0f / (numSteps - 1) * arcEnd;
                for (int i = 0; i < numSteps; ++i)
                {
                    float angle = i * angleStep + direction;

                    Vector3 outer = x * MathF.Sin(angle) + y * MathF.Cos(angle);
                    Vector3 middle = outer * halfInnerScale;
                    Vector3 inner = outer * innerScale;

                    if (i > 0)
                    {
                        ushort baseIndex = (ushort)(vertices.Count - 4);
                        {
                            indices.Add((ushort)baseIndex);
                            indices.Add((ushort)(baseIndex + 4));
                            indices.Add((ushort)(baseIndex + 2));
                            indices.Add((ushort)(baseIndex + 4));
                            indices.Add((ushort)(baseIndex + 2));
                            indices.Add((ushort)(baseIndex + 6));

                            indices.Add((ushort)baseIndex);
                            indices.Add((ushort)(baseIndex + 4));
                            indices.Add((ushort)(baseIndex + 3));
                            indices.Add((ushort)(baseIndex + 4));
                            indices.Add((ushort)(baseIndex + 3));
                            indices.Add((ushort)(baseIndex + 7));
                        }

                        {
                            indices.Add((ushort)(baseIndex + 1));
                            indices.Add((ushort)(baseIndex + 5));
                            indices.Add((ushort)(baseIndex + 2));
                            indices.Add((ushort)(baseIndex + 5));
                            indices.Add((ushort)(baseIndex + 2));
                            indices.Add((ushort)(baseIndex + 6));

                            indices.Add((ushort)(baseIndex + 1));
                            indices.Add((ushort)(baseIndex + 5));
                            indices.Add((ushort)(baseIndex + 3));
                            indices.Add((ushort)(baseIndex + 5));
                            indices.Add((ushort)(baseIndex + 3));
                            indices.Add((ushort)(baseIndex + 7));
                        }
                    }

                    // up, down, right, left
                    vertices.Add(new InteractionGizmoVertex(outer, color));
                    vertices.Add(new InteractionGizmoVertex(inner, color));
                    vertices.Add(new InteractionGizmoVertex(middle + normal, color));
                    vertices.Add(new InteractionGizmoVertex(middle - normal, color));
                }
            }

            static void GenerateCircle(Vector3 x, Vector3 y, float radius, float direction, float arcEnd, Color color, ref RentedList<InteractionGizmoVertex> vertices, ref RentedList<ushort> indices)
            {
                x *= radius;
                y *= radius;

                int numSteps = (int)MathF.Ceiling(arcEnd / float.Pi * 24.0f);

                ushort baseCenterIndex = (ushort)vertices.Count;
                vertices.Add(new InteractionGizmoVertex(Vector3.Zero, color));

                float angleStep = 1.0f / (numSteps - 1) * arcEnd;
                for (int i = 0; i < numSteps; ++i)
                {
                    float angle = i * angleStep + direction;

                    Vector3 outer = x * MathF.Sin(angle) + y * MathF.Cos(angle);

                    if (i > 0)
                    {
                        ushort vertexIndex = (ushort)vertices.Count;
                        indices.Add(baseCenterIndex);
                        indices.Add(vertexIndex);
                        indices.Add((ushort)(vertexIndex - 1));
                    }

                    // up, down, right, left
                    vertices.Add(new InteractionGizmoVertex(outer, color));
                }
            }

            static void GenerateLine(Vector3 x, Vector3 y, Vector3 normal, float offset, float height, float radius, Color color, ref RentedList<InteractionGizmoVertex> vertices, ref RentedList<ushort> indices)
            {
                x *= radius;
                y *= radius;

                Vector3 basePos = normal * offset;
                Vector3 heightOffset = normal * height;

                Vector3 up = basePos + y;
                Vector3 down = basePos - y;
                Vector3 right = basePos + x;
                Vector3 left = basePos - x;

                int baseIndex = vertices.Count;

                vertices.Add(new InteractionGizmoVertex(up, color));
                vertices.Add(new InteractionGizmoVertex(down, color));
                vertices.Add(new InteractionGizmoVertex(right, color));
                vertices.Add(new InteractionGizmoVertex(left, color));

                vertices.Add(new InteractionGizmoVertex(up + heightOffset, color));
                vertices.Add(new InteractionGizmoVertex(down + heightOffset, color));
                vertices.Add(new InteractionGizmoVertex(right + heightOffset, color));
                vertices.Add(new InteractionGizmoVertex(left + heightOffset, color));

                {
                    indices.Add((ushort)baseIndex);
                    indices.Add((ushort)(baseIndex + 4));
                    indices.Add((ushort)(baseIndex + 2));
                    indices.Add((ushort)(baseIndex + 4));
                    indices.Add((ushort)(baseIndex + 2));
                    indices.Add((ushort)(baseIndex + 6));

                    indices.Add((ushort)baseIndex);
                    indices.Add((ushort)(baseIndex + 4));
                    indices.Add((ushort)(baseIndex + 3));
                    indices.Add((ushort)(baseIndex + 4));
                    indices.Add((ushort)(baseIndex + 3));
                    indices.Add((ushort)(baseIndex + 7));
                }

                {
                    indices.Add((ushort)(baseIndex + 1));
                    indices.Add((ushort)(baseIndex + 5));
                    indices.Add((ushort)(baseIndex + 2));
                    indices.Add((ushort)(baseIndex + 5));
                    indices.Add((ushort)(baseIndex + 2));
                    indices.Add((ushort)(baseIndex + 6));

                    indices.Add((ushort)(baseIndex + 1));
                    indices.Add((ushort)(baseIndex + 5));
                    indices.Add((ushort)(baseIndex + 3));
                    indices.Add((ushort)(baseIndex + 5));
                    indices.Add((ushort)(baseIndex + 3));
                    indices.Add((ushort)(baseIndex + 7));
                }
            }
        }

        public void UpdateScreenData(CameraManager camera, Vector2 mousePos)
        {
            const float MaxScreenMarginDistance = 12.0f;

            float scale = Vector3.Distance(_origin, camera.Position) * 0.075f * 1.4f;

            (Vector2 screenCenterPos, bool isBehindViewer) = camera.ProjectWorldToScreen(_origin);
            if (isBehindViewer)
            {
                _hoverHit = TranslateGizmoHit.None;
                return;
            }

            Ray ray = camera.ProjectViewportToRayInverse(camera.MouseViewport);

            Vector3 axisX = Vector3.Transform(Vector3.UnitX, _rotation);
            Vector3 axisY = Vector3.Transform(Vector3.UnitY, _rotation);
            Vector3 axisZ = Vector3.Transform(Vector3.UnitZ, _rotation);

            Quaternion inverseRotation = Quaternion.Inverse(_rotation);

            float distX = GetAxisDistanceOnScreen(axisX, Vector3.UnitX, 1, 2);
            float distY = GetAxisDistanceOnScreen(axisY, Vector3.UnitY, 2, 0);
            float distZ = GetAxisDistanceOnScreen(axisZ, Vector3.UnitZ, 1, 0);

            float smallestAxisDist = MathF.Min(MathF.Min(distX, distY), distZ);
            if (smallestAxisDist <= MaxScreenMarginDistance)
            {
                if (distX < distY && distX < distZ)
                    _hoverHit = TranslateGizmoHit.X;
                else if (distY < distZ)
                    _hoverHit = TranslateGizmoHit.Y;
                else
                    _hoverHit = TranslateGizmoHit.Z;
            }
            else
            {
                const float RingWidth = 0.04f;

                Vector3 towardsCenterVector = Vector3.Normalize(camera.Position - _origin);
                Vector3 lookRightVector = Vector3.Normalize(Vector3.Cross(towardsCenterVector, Vector3.UnitY));

                float outerRingThickness = RingWidth * scale;
                float outerRingMargin = 1.5f * scale;

                (Vector2 screenPos, isBehindViewer) = camera.ProjectWorldToScreen(_origin + lookRightVector * (outerRingMargin - outerRingThickness * 0.5f));
                float outerCircleRadius = isBehindViewer ? float.PositiveInfinity : Vector2.Distance(screenCenterPos, screenPos);

                float mouseDistFromCenter = Vector2.Distance(mousePos, screenCenterPos);
                if (mouseDistFromCenter <= outerCircleRadius + MaxScreenMarginDistance)
                {
                    _hoverHit = mouseDistFromCenter < outerCircleRadius - MaxScreenMarginDistance ? TranslateGizmoHit.All : TranslateGizmoHit.Axis;
                }
                else
                {
                    _hoverHit = TranslateGizmoHit.None;
                }
            }

            float GetAxisDistanceOnScreen(Vector3 axis, Vector3 staticAxis, byte shuffleX, byte shuffleY)
            {
                Vector3 planeHit = ray.AtDistance(InfinitePlane.Intersect(new InfinitePlane(_origin, axis), ray));
                Vector3 originHit = Vector3.Normalize(planeHit - _origin);
                Vector2 flattenedHit = Vector3.Shuffle(Vector3.Transform(originHit, inverseRotation), shuffleX, shuffleY, 0).AsVector2();

                Vector2 cameraDir = Vector2.Normalize(Vector3.Shuffle(Vector3.Transform(camera.Position - (_origin + staticAxis), inverseRotation), shuffleY, shuffleX, 0).AsVector2());
                float cameraRadians = -ExMath.GetAngleTowards(Vector2.Zero, cameraDir) + float.Pi * 0.5f;

                (float camSin, float camCos) = MathF.SinCos(-cameraRadians);
                Vector2 rotatedHit = new Vector2(flattenedHit.X * camCos - flattenedHit.Y * camSin, flattenedHit.X * camSin + flattenedHit.Y * camCos);

                float hitRadians = Math.Clamp(ExMath.GetAngleTowards(Vector2.Zero, rotatedHit), -float.Pi * 0.5f, float.Pi * 0.5f) + cameraRadians;

                Vector3 hitCircleWorld = Vector3.Zero;
                hitCircleWorld[shuffleX] = MathF.Cos(hitRadians);
                hitCircleWorld[shuffleY] = MathF.Sin(hitRadians);
                hitCircleWorld = Vector3.Transform(hitCircleWorld, _rotation) * scale;

                (Vector2 screenPos, bool isBehindViewer) = camera.ProjectWorldToScreen(hitCircleWorld + _origin);
                ScreenGizmos.DrawWireCircle(screenPos, 16.0f, new Color(staticAxis.AsVector4().WithElement(3, 1.0f)));

                return isBehindViewer ? float.PositiveInfinity : Vector2.Distance(screenPos, mousePos);
            }
        }

        public bool TryStartDrag()
        {
            if (_hoverHit == TranslateGizmoHit.None || !_isEnabled || _currentHit != TranslateGizmoHit.None)
                return false;

            CameraManager camera = VoxelRuntime.Instance.CameraManager;

            _currentHit = _hoverHit;

            _rotationOrigin = _origin;
            switch (_currentHit)
            {
                case TranslateGizmoHit.X: _rotationAxis = Vector3.Transform(Vector3.UnitX, _rotation); break;
                case TranslateGizmoHit.Y: _rotationAxis = Vector3.Transform(Vector3.UnitY, _rotation); break;
                case TranslateGizmoHit.Z: _rotationAxis = Vector3.Transform(Vector3.UnitZ, _rotation); break;
                case TranslateGizmoHit.Axis: _rotationAxis = Vector3.Normalize(camera.Position - _origin); break;
                case TranslateGizmoHit.All: _rotationAxis = camera.ForwardVector; break;
            }

            _rotationQuaternion = _rotation;
            _inverseRotationQuat = Quaternion.Inverse(_rotationQuaternion);

            Ray cameraRay = camera.ProjectViewportToRayInverse(camera.MouseViewport);
            _startQuaternion = IsSingleAxis(_currentHit) ? ProjectForAxisRadians(cameraRay) : ProjectForAngledQuat(cameraRay);
            _lastHitQuaternion = _startQuaternion;

            return true;
        }

        public bool TryEndDrag()
        {
            if (_currentHit == TranslateGizmoHit.None)
                return false;

            _currentHit = TranslateGizmoHit.None;
            return true;
        }

        public bool TryUpdateDrag(out Quaternion delta)
        {
            if (_currentHit == TranslateGizmoHit.None)
            {
                delta = Quaternion.Identity;
                return false;
            }

            CameraManager camera = VoxelRuntime.Instance.CameraManager;

            Ray cameraRay = camera.ProjectViewportToRayInverse(camera.MouseViewport);
            Quaternion worldRot = IsSingleAxis(_currentHit) ? ProjectForAxisRadians(cameraRay) : ProjectForAngledQuat(cameraRay);

            if (worldRot != _lastHitQuaternion)
            {
                delta = worldRot * Quaternion.Conjugate(_startQuaternion);
                _lastHitQuaternion = worldRot;

                return true;
            }

            delta = Quaternion.Identity;
            return false;
        }

        private Quaternion ProjectForAxisRadians(Ray ray)
        {
            Debug.Assert(_currentHit == TranslateGizmoHit.X || _currentHit == TranslateGizmoHit.Y || _currentHit == TranslateGizmoHit.Z);

            (byte shuffleX, byte shuffleY) = _currentHit switch
            {
                TranslateGizmoHit.X => ((byte)1, (byte)2),
                TranslateGizmoHit.Y => ((byte)2, (byte)0),
                TranslateGizmoHit.Z => ((byte)0, (byte)1),
                _ => ((byte)0, (byte)0)
            };

            Vector3 planeHit = ray.AtDistance(InfinitePlane.Intersect(new InfinitePlane(_rotationOrigin, _rotationAxis), ray));
            Vector3 originHit = Vector3.Normalize(planeHit - _rotationOrigin);
            Vector2 flattenedHit = Vector3.Shuffle(Vector3.Transform(originHit, _inverseRotationQuat), shuffleX, shuffleY, 0).AsVector2();

            float hitRadians = ExMath.GetAngleTowards(Vector2.Zero, flattenedHit);
            return _currentHit switch
            {
                TranslateGizmoHit.X => _rotationQuaternion * Quaternion.CreateFromYawPitchRoll(0.0f, hitRadians, 0.0f),
                TranslateGizmoHit.Y => _rotationQuaternion * Quaternion.CreateFromYawPitchRoll(hitRadians, 0.0f, 0.0f),
                TranslateGizmoHit.Z => _rotationQuaternion * Quaternion.CreateFromYawPitchRoll(0.0f, 0.0f, hitRadians),
                _ => Quaternion.Zero
            };
        }

        private Quaternion ProjectForAngledQuat(Ray ray)
        {
            Debug.Assert(_currentHit == TranslateGizmoHit.Axis || _currentHit == TranslateGizmoHit.All);

            if (_currentHit == TranslateGizmoHit.Axis)
            {
                Vector3 planeHit = ray.AtDistance(InfinitePlane.Intersect(new InfinitePlane(_rotationOrigin, _rotationAxis), ray));
                Vector3 forward = Vector3.Normalize(planeHit - _rotationOrigin);

                Vector3 rightVector = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
                Vector3 upVector = Vector3.Normalize(Vector3.Cross(forward, rightVector));

                return Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateLookTo(Vector3.Zero, forward, upVector));
            }
            else
            {
                Vector3 planeHit = ray.AtDistance(InfinitePlane.Intersect(new InfinitePlane(_rotationOrigin, _rotationAxis), ray));
                return Quaternion.Identity;
            }
        }

        public Vector3 Origin { get => _origin; set => _origin = value; }
        public Quaternion Rotation { get => _rotation; set => _rotation = value; }

        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }

        private static readonly Color s_xAxisColor = new Color32(0xff443ce6, true).FlipRGB().ToColor();
        private static readonly Color s_yAxisColor = new Color32(0xff3ce683, true).FlipRGB().ToColor();
        private static readonly Color s_zAxisColor = new Color32(0xffe6553c, true).FlipRGB().ToColor();

        private static bool IsSingleAxis(TranslateGizmoHit hit) => hit == TranslateGizmoHit.X || hit == TranslateGizmoHit.Y || hit == TranslateGizmoHit.Z;
    }
}
