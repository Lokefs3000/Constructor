using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Primary.Collections;
using Primary.Common;
using Primary.Input;
using Primary.Mathematics;
using SharpGen.Runtime;
using VoxelizationDemo.Core;
using VoxelizationDemo.Logic;

namespace VoxelizationDemo.Editor.Gizmo
{
    internal sealed class TranslateGizmo : IInteractionGizmo
    {
        private bool _isEnabled;

        private Vector3 _origin;
        private Quaternion _rotation;

        private TranslateGizmoHit _currentHit;
        private Ray _currentRay;
        private Plane _currentPlane;

        private Vector3 _startWorldHit;
        private Vector3 _lastWorldHit;

        private TranslateGizmoHit _hoverHit;

        public TranslateGizmo()
        {
            _isEnabled = true;

            _origin = Vector3.Zero;
            _rotation = Quaternion.Identity;

            _currentHit = TranslateGizmoHit.None;

            _hoverHit = TranslateGizmoHit.None;
        }

        public void GenerateVertices(Frustrum frustrum, Vector3 center, ref RentedList<InteractionGizmoVertex> vertices, ref RentedList<ushort> indices, out Matrix4x4 model)
        {
            model = Matrix4x4.Identity;

            float scale = Vector3.Distance(_currentHit != TranslateGizmoHit.None ? (_origin - (_lastWorldHit - _startWorldHit)) : _origin, center) * 0.075f;
            {
                Vector3 min = Vector3.Transform(new Vector3(-2.0f * scale), _rotation) + _origin;
                Vector3 max = Vector3.Transform(new Vector3(2.0f * scale), _rotation) + _origin;

                if (!frustrum.Intersects(new AABB(min, max)))
                    return;
            }

            model *= Matrix4x4.CreateFromQuaternion(_rotation);
            model *= Matrix4x4.CreateTranslation(_origin);

            TranslateGizmoHit hit = _currentHit == TranslateGizmoHit.None ? _hoverHit : _currentHit;

            Color axisXColor = hit == TranslateGizmoHit.X ? Color.Yellow : s_xAxisColor;
            Color axisYColor = hit == TranslateGizmoHit.Y ? Color.Yellow : s_yAxisColor;
            Color axisZColor = hit == TranslateGizmoHit.Z ? Color.Yellow : s_zAxisColor;

            Color planeXColor = hit == (TranslateGizmoHit.Y | TranslateGizmoHit.Z) ? Color.Yellow : s_xAxisColor;
            Color planeYColor = hit == (TranslateGizmoHit.X | TranslateGizmoHit.Z) ? Color.Yellow : s_yAxisColor;
            Color planeZColor = hit == (TranslateGizmoHit.X | TranslateGizmoHit.Y) ? Color.Yellow : s_zAxisColor;

            Vector3 unitX = Vector3.UnitX * scale;
            Vector3 unitY = Vector3.UnitY * scale;
            Vector3 unitZ = Vector3.UnitZ * scale;

            float lineOffset = 0.4f * scale;
            float lineHeight = 1.3f * scale;

            float coneOffset = 1.7f * scale;
            float coneHeight = 0.5f * scale;

            float planeOffset = 0.8f;

            if (_currentHit != TranslateGizmoHit.None)
            {
                switch (_currentHit)
                {
                    case TranslateGizmoHit.X: GenerateLine(unitZ, unitY, Vector3.UnitX, 0.0f, _startWorldHit.X - _lastWorldHit.X, s_xAxisColor.Darken(0.1f), ref vertices, ref indices); break;
                    case TranslateGizmoHit.Y: GenerateLine(unitX, unitY, Vector3.UnitY, 0.0f, _startWorldHit.Y - _lastWorldHit.Y, s_yAxisColor.Darken(0.1f), ref vertices, ref indices); break;
                    case TranslateGizmoHit.Z: GenerateLine(unitX, unitY, Vector3.UnitZ, 0.0f, _startWorldHit.Z - _lastWorldHit.Z, s_zAxisColor.Darken(0.1f), ref vertices, ref indices); break;
                }
            }

            GenerateLine(unitZ, unitY, Vector3.UnitX, lineOffset, lineHeight, axisXColor, ref vertices, ref indices);
            GenerateCone(unitZ, unitY, Vector3.UnitX, coneOffset, coneHeight, axisXColor, ref vertices, ref indices);
            if (_currentHit == TranslateGizmoHit.None || _currentHit == TranslateGizmoHit.PlaneX)
                GeneratePlane(unitY, unitZ, planeOffset, planeXColor, ref vertices, ref indices);

            GenerateLine(unitX, unitZ, Vector3.UnitY, lineOffset, lineHeight, axisYColor, ref vertices, ref indices);
            GenerateCone(unitX, unitZ, Vector3.UnitY, coneOffset, coneHeight, axisYColor, ref vertices, ref indices);
            if (_currentHit == TranslateGizmoHit.None || _currentHit == TranslateGizmoHit.PlaneY)
                GeneratePlane(unitZ, unitX, planeOffset, planeYColor, ref vertices, ref indices);

            GenerateLine(unitX, unitY, Vector3.UnitZ, lineOffset, lineHeight, axisZColor, ref vertices, ref indices);
            GenerateCone(unitX, unitY, Vector3.UnitZ, coneOffset, coneHeight, axisZColor, ref vertices, ref indices);
            if (_currentHit == TranslateGizmoHit.None || _currentHit == TranslateGizmoHit.PlaneZ)
                GeneratePlane(unitX, unitY, planeOffset, planeZColor, ref vertices, ref indices);

            static void GenerateLine(Vector3 x, Vector3 y, Vector3 normal, float offset, float height, Color color, ref RentedList<InteractionGizmoVertex> vertices, ref RentedList<ushort> indices)
            {
                x *= 0.02f;
                y *= 0.02f;

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
            static void GenerateCone(Vector3 x, Vector3 y, Vector3 normal, float offset, float height, Color color, ref RentedList<InteractionGizmoVertex> vertices, ref RentedList<ushort> indices)
            {
                x *= 0.1f;
                y *= 0.1f;

                Vector3 basePos = normal * offset;

                ushort topVertexIndex = (ushort)vertices.Count;
                vertices.Add(new InteractionGizmoVertex(normal * (offset + height), color));

                float angleStep = 1.0f / (24 - 1) * float.Pi * 2.0f;
                for (int i = 0; i < 24; ++i)
                {
                    float angle = i * angleStep;
                    Vector3 v = x * MathF.Sin(angle) + y * MathF.Cos(angle);

                    if (i > 0)
                    {
                        ushort vertexIndex = (ushort)vertices.Count;
                        indices.Add(topVertexIndex);
                        indices.Add(vertexIndex);
                        indices.Add((ushort)(vertexIndex - 1));
                    }

                    vertices.Add(new InteractionGizmoVertex(v + basePos, color));
                }
            }
            static void GeneratePlane(Vector3 x, Vector3 y, float offset, Color color, ref RentedList<InteractionGizmoVertex> vertices, ref RentedList<ushort> indices)
            {
                Vector3 origin = (x + y) * offset;

                x *= 0.35f;
                y *= 0.35f;

                Vector3 absCenter = (origin + origin + x + y) * 0.5f;

                Vector3 v0 = origin;
                Vector3 v1 = origin + x;
                Vector3 v2 = origin + x + y;
                Vector3 v3 = origin + y;

                Color darkened = new Color(new Color(color).AsVector4() * 0.5f);

                int baseIndex = vertices.Count;

                /*
                 
                 0------1
                 | 4--5 |
                 | |  | |
                 | 7--6 |
                 3------2
                 
                 */

                vertices.Add(new InteractionGizmoVertex(v0, color));
                vertices.Add(new InteractionGizmoVertex(v1, color));
                vertices.Add(new InteractionGizmoVertex(v2, color));
                vertices.Add(new InteractionGizmoVertex(v3, color));

                vertices.Add(new InteractionGizmoVertex(Vector3.Lerp(v0, absCenter, 0.175f), color));
                vertices.Add(new InteractionGizmoVertex(Vector3.Lerp(v1, absCenter, 0.175f), color));
                vertices.Add(new InteractionGizmoVertex(Vector3.Lerp(v2, absCenter, 0.175f), color));
                vertices.Add(new InteractionGizmoVertex(Vector3.Lerp(v3, absCenter, 0.175f), color));

                vertices.Add(new InteractionGizmoVertex(vertices[baseIndex + 4].Position, darkened));
                vertices.Add(new InteractionGizmoVertex(vertices[baseIndex + 5].Position, darkened));
                vertices.Add(new InteractionGizmoVertex(vertices[baseIndex + 6].Position, darkened));
                vertices.Add(new InteractionGizmoVertex(vertices[baseIndex + 7].Position, darkened));

                indices.Add((ushort)(baseIndex + 8));
                indices.Add((ushort)(baseIndex + 10));
                indices.Add((ushort)(baseIndex + 9));
                indices.Add((ushort)(baseIndex + 8));
                indices.Add((ushort)(baseIndex + 11));
                indices.Add((ushort)(baseIndex + 10));

                indices.Add((ushort)baseIndex);
                indices.Add((ushort)(baseIndex + 1));
                indices.Add((ushort)(baseIndex + 4));
                indices.Add((ushort)(baseIndex + 1));
                indices.Add((ushort)(baseIndex + 4));
                indices.Add((ushort)(baseIndex + 5));

                indices.Add((ushort)baseIndex);
                indices.Add((ushort)(baseIndex + 3));
                indices.Add((ushort)(baseIndex + 4));
                indices.Add((ushort)(baseIndex + 3));
                indices.Add((ushort)(baseIndex + 4));
                indices.Add((ushort)(baseIndex + 7));

                indices.Add((ushort)(baseIndex + 3));
                indices.Add((ushort)(baseIndex + 2));
                indices.Add((ushort)(baseIndex + 7));
                indices.Add((ushort)(baseIndex + 2));
                indices.Add((ushort)(baseIndex + 7));
                indices.Add((ushort)(baseIndex + 6));

                indices.Add((ushort)(baseIndex + 2));
                indices.Add((ushort)(baseIndex + 1));
                indices.Add((ushort)(baseIndex + 6));
                indices.Add((ushort)(baseIndex + 1));
                indices.Add((ushort)(baseIndex + 6));
                indices.Add((ushort)(baseIndex + 5));
            }
        }

        public void UpdateScreenData(CameraManager camera, Vector2 mousePos)
        {
            float scale = Vector3.Distance(_origin, camera.Position) * 0.075f;

            const float MaxScreenMarginDistance = 12.0f;

            float maxDistance = MaxScreenMarginDistance * (camera.ClientSize.X / 900.0f);
            float maxSqrDistance = (MaxScreenMarginDistance * MaxScreenMarginDistance) * (camera.ClientSize.X / 900.0f);

            Ray ray = camera.ProjectViewportToRayInverse(camera.MouseViewport);

            Vector3 normalX = Vector3.Transform(Vector3.UnitX, _rotation);
            Vector3 normalY = Vector3.Transform(Vector3.UnitY, _rotation);
            Vector3 normalZ = Vector3.Transform(Vector3.UnitZ, _rotation);

            Vector3 planeNormalX = normalY + normalZ;
            Vector3 planeNormalY = normalX + normalZ;
            Vector3 planeNormalZ = normalX + normalY;

            float lineMin = scale * 0.4f;
            float coneMax = scale * 1.7f;

            float planeMin = scale * 0.8f;
            float planeMax = scale * (0.8f + 0.35f);

            Ray tempRay;

            Vector3 axisXPos = (tempRay = new Ray(_origin + normalX * lineMin, normalX)).AtDistance(Math.Clamp(tempRay.FindClosest(ray), 0.0f, coneMax));
            Vector3 axisYPos = (tempRay = new Ray(_origin + normalY * lineMin, normalY)).AtDistance(Math.Clamp(tempRay.FindClosest(ray), 0.0f, coneMax));
            Vector3 axisZPos = (tempRay = new Ray(_origin + normalZ * lineMin, normalZ)).AtDistance(Math.Clamp(tempRay.FindClosest(ray), 0.0f, coneMax));

            Vector2 tempScreenPos;
            bool tempIsBehindViewer;

            float axisXDist = ((tempScreenPos, tempIsBehindViewer) = camera.ProjectWorldToScreen(axisXPos)).tempIsBehindViewer ? -1.0f : Vector2.DistanceSquared(tempScreenPos, mousePos);
            float axisYDist = ((tempScreenPos, tempIsBehindViewer) = camera.ProjectWorldToScreen(axisYPos)).tempIsBehindViewer ? -1.0f : Vector2.DistanceSquared(tempScreenPos, mousePos);
            float axisZDist = ((tempScreenPos, tempIsBehindViewer) = camera.ProjectWorldToScreen(axisZPos)).tempIsBehindViewer ? -1.0f : Vector2.DistanceSquared(tempScreenPos, mousePos);

            float planeXDist = GetMouseDistanceFromPlane(camera, mousePos, _origin + planeNormalX * planeMin, _origin + planeNormalX * planeMax, maxDistance);
            float planeYDist = GetMouseDistanceFromPlane(camera, mousePos, _origin + planeNormalY * planeMin, _origin + planeNormalY * planeMax, maxDistance);
            float planeZDist = GetMouseDistanceFromPlane(camera, mousePos, _origin + planeNormalZ * planeMin, _origin + planeNormalZ * planeMax, maxDistance);

            float minPlaneDist = MathF.Min(MathF.Min(planeXDist, planeYDist), planeZDist);
            float minAxisDist = MathF.Min(MathF.Min(axisXDist, axisYDist), axisZDist);

            if (minPlaneDist < minAxisDist)
            {
                if (planeXDist < planeYDist && planeXDist < planeZDist)
                {
                    if (planeXDist > maxSqrDistance)
                        _hoverHit = TranslateGizmoHit.None;
                    else
                        _hoverHit = TranslateGizmoHit.Y | TranslateGizmoHit.Z;
                }
                else if (planeYDist < planeZDist)
                {
                    if (planeYDist > maxSqrDistance)
                        _hoverHit = TranslateGizmoHit.None;
                    else
                        _hoverHit = TranslateGizmoHit.X | TranslateGizmoHit.Z;
                }
                else
                {
                    if (planeZDist > maxSqrDistance)
                        _hoverHit = TranslateGizmoHit.None;
                    else
                        _hoverHit = TranslateGizmoHit.X | TranslateGizmoHit.Y;
                }
            }
            else
            {
                if (axisXDist < axisYDist && axisXDist < axisZDist)
                {
                    if (axisXDist > maxSqrDistance)
                        _hoverHit = TranslateGizmoHit.None;
                    else
                        _hoverHit = TranslateGizmoHit.X;
                }
                else if (axisYDist < axisZDist)
                {
                    if (axisYDist > maxSqrDistance)
                        _hoverHit = TranslateGizmoHit.None;
                    else
                        _hoverHit = TranslateGizmoHit.Y;
                }
                else
                {
                    if (axisZDist > maxSqrDistance)
                        _hoverHit = TranslateGizmoHit.None;
                    else
                        _hoverHit = TranslateGizmoHit.Z;
                }
            }

            static float GetMouseDistanceFromPlane(CameraManager camera, Vector2 mousePos, Vector3 min, Vector3 max, float outerMargin)
            {
                (Vector2 minScreen, bool minBehindViewer) = camera.ProjectWorldToScreen(min);
                (Vector2 maxScreen, bool maxBehindViewer) = camera.ProjectWorldToScreen(max);

                if (minBehindViewer && maxBehindViewer)
                    return -1.0f;

                (minScreen, maxScreen) = (Vector2.Min(minScreen, maxScreen), Vector2.Max(minScreen, maxScreen));

                Boundaries boundaries = new Boundaries(minScreen, maxScreen);
                return boundaries.IsWithin(mousePos) ? 0.0f : Vector2.DistanceSquared(Boundaries.OnEdge(boundaries, mousePos), mousePos) * 0.75f;
            }
        }

        public bool TryStartDrag()
        {
            if (_hoverHit == TranslateGizmoHit.None || !_isEnabled || _currentHit != TranslateGizmoHit.None)
                return false;

            CameraManager camera = VoxelRuntime.Instance.CameraManager;

            _currentHit = _hoverHit;

            switch (_currentHit)
            {
                case TranslateGizmoHit.X: _currentRay = new Ray(_origin, Vector3.Transform(Vector3.UnitX, _rotation)); break;
                case TranslateGizmoHit.Y: _currentRay = new Ray(_origin, Vector3.Transform(Vector3.UnitY, _rotation)); break;
                case TranslateGizmoHit.Z: _currentRay = new Ray(_origin, Vector3.Transform(Vector3.UnitZ, _rotation)); break;

                case TranslateGizmoHit.PlaneX: _currentPlane = CreatePlane(_origin, Vector3.Transform(Vector3.UnitX, _rotation)); break;
                case TranslateGizmoHit.PlaneY: _currentPlane = CreatePlane(_origin, Vector3.Transform(Vector3.UnitY, _rotation)); break;
                case TranslateGizmoHit.PlaneZ: _currentPlane = CreatePlane(_origin, Vector3.Transform(Vector3.UnitZ, _rotation)); break;
            }

            Ray cameraRay = camera.ProjectViewportToRayInverse(camera.MouseViewport);

            switch (_currentHit)
            {
                case TranslateGizmoHit.X:
                case TranslateGizmoHit.Y:
                case TranslateGizmoHit.Z: _lastWorldHit = _currentRay.AtDistance(_currentRay.FindClosest(cameraRay)); break;

                case TranslateGizmoHit.PlaneX:
                case TranslateGizmoHit.PlaneY:
                case TranslateGizmoHit.PlaneZ: _lastWorldHit = cameraRay.AtDistance(IntersectPlane(_currentPlane, cameraRay)); break;
            }

            _startWorldHit = _lastWorldHit;

            return true;

            static Plane CreatePlane(Vector3 origin, Vector3 normal) => Plane.Create(normal, -Vector3.Dot(normal, origin));
        }

        public bool TryEndDrag()
        {
            if (_currentHit == TranslateGizmoHit.None)
                return false;

            _currentHit = TranslateGizmoHit.None;
            return true;
        }

        public bool TryUpdateDrag(out Vector3 delta)
        {
            CameraManager camera = VoxelRuntime.Instance.CameraManager;

            Ray cameraRay = camera.ProjectViewportToRayInverse(camera.MouseViewport);
            Vector3 worldHit = _lastWorldHit;

            switch (_currentHit)
            {
                case TranslateGizmoHit.X:
                case TranslateGizmoHit.Y:
                case TranslateGizmoHit.Z: worldHit = _currentRay.AtDistance(_currentRay.FindClosest(cameraRay)); break;

                case TranslateGizmoHit.PlaneX:
                case TranslateGizmoHit.PlaneY:
                case TranslateGizmoHit.PlaneZ: worldHit = cameraRay.AtDistance(IntersectPlane(_currentPlane, cameraRay)); break;
            }

            if (worldHit != _lastWorldHit)
            {
                delta = worldHit - _startWorldHit;
                _lastWorldHit = worldHit;

                return true;
            }

            delta = Vector3.Zero;
            return false;
        }

        public Vector3 Origin { get => _origin; set => _origin = value; }
        public Quaternion Rotation { get => _rotation; set => _rotation = value; }

        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }

        private static readonly Color s_xAxisColor = new Color32(0xff443ce6, true).FlipRGB().ToColor();
        private static readonly Color s_yAxisColor = new Color32(0xff3ce683, true).FlipRGB().ToColor();
        private static readonly Color s_zAxisColor = new Color32(0xffe6553c, true).FlipRGB().ToColor();

        private static float IntersectPlane(Plane plane, Ray ray)
        {
            float t = -(Vector3.Dot(plane.Normal, ray.Origin) + plane.D) / Vector3.Dot(plane.Normal, ray.Direction);
            return t;
        }
    }

    public enum TranslateGizmoHit : byte
    {
        None = 0,

        X = 1 << 0,
        Y = 1 << 1,
        Z = 1 << 2,

        PlaneX = Y | Z,
        PlaneY = X | Z,
        PlaneZ = X | Y
    }
}
