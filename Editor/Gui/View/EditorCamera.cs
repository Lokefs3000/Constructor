using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Timing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.Gui.View
{
    public sealed class EditorCamera
    {
        private Vector3 _position;
        private Vector2 _eulerAngles;

        private Vector3 _forward;
        private Vector3 _right;
        private Vector3 _up;

        private Matrix4x4 _transform;
        private Matrix4x4 _viewMatrix;
        private Matrix4x4 _projectionMatrix;

        private Matrix4x4 _vpMatrix;
        private Matrix4x4 _invertedVpMatrix;

        private Vector2 _clientSize;

        private bool _isViewDirty;
        private bool _isProjectionDirty;

        internal EditorCamera()
        {
            _position = Vector3.Zero;
            _eulerAngles = Vector2.Zero;

            _transform = Matrix4x4.Identity;
            _viewMatrix = Matrix4x4.Identity;
            _projectionMatrix = Matrix4x4.Identity;

            _vpMatrix = Matrix4x4.Identity;

            _clientSize = Vector2.Zero;

            _isViewDirty = true;
            _isProjectionDirty = true;
        }

        internal void UpdateVectors()
        {
            float speed = Time.DeltaTime * 10.0f;

            if (InputSystem.Keyboard.IsKeyDown(KeyCode.W))
            {
                _position += _forward * speed;
                _isViewDirty = true;
            }

            if (InputSystem.Keyboard.IsKeyDown(KeyCode.S))
            {
                _position -= _forward * speed;
                _isViewDirty = true;
            }

            if (InputSystem.Keyboard.IsKeyDown(KeyCode.A))
            {
                _position -= _right * speed;
                _isViewDirty = true;
            }

            if (InputSystem.Keyboard.IsKeyDown(KeyCode.D))
            {
                _position += _right * speed;
                _isViewDirty = true;
            }

            if (InputSystem.Keyboard.IsKeyDown(KeyCode.E))
            {
                _position += _up * speed;
                _isViewDirty = true;
            }

            if (InputSystem.Keyboard.IsKeyDown(KeyCode.Q))
            {
                _position -= _up * speed;
                _isViewDirty = true;
            }

            if (_isViewDirty)
            {
                _eulerAngles.X %= 360.0f;
                _eulerAngles.Y %= 360.0f;

                Vector2 rad = Vector2.DegreesToRadians(_eulerAngles);
                Quaternion quat = Quaternion.CreateFromYawPitchRoll(rad.Y, rad.X, 0.0f);

                _forward = Vector3.Transform(Vector3.UnitZ, quat);
                _right = -Vector3.Transform(Vector3.UnitX, quat);
                _up = Vector3.Transform(Vector3.UnitY, quat);

                _transform = Matrix4x4.CreateFromQuaternion(quat) * Matrix4x4.CreateTranslation(_position);
                _viewMatrix = Matrix4x4.CreateLookTo(_position, _forward, _up);
            }

            if (_isProjectionDirty)
            {
                _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(float.DegreesToRadians(70.0f), _clientSize.X / _clientSize.Y, 0.02f, 1000.0f);
            }

            if (_isViewDirty || _isProjectionDirty)
            {
                _vpMatrix = _viewMatrix * _projectionMatrix;

                Matrix4x4.Invert(_vpMatrix, out _invertedVpMatrix);
            }

            _isViewDirty = false;
            _isProjectionDirty = false;
        }

        public (Vector2 Position, bool IsBehindViewer) ProjectToScreen(Vector3 position)
        {
            return ProjectionMath.WorldToScreen(position, _vpMatrix, _clientSize);
        }

        public Ray ProjectToRay(Vector2 position)
        {
            Vector2 viewport = (position / _clientSize) * 2.0f - Vector2.One;
            return ProjectionMath.ViewportToRay(new Vector2(viewport.X, -viewport.Y), _invertedVpMatrix, _position);
        }

        public Vector3 Position { get => _position; set { _position = value; _isViewDirty = true; } }
        public Vector2 EulerAngles { get => _eulerAngles; set { _eulerAngles = value; _isViewDirty = true; } }

        public Vector3 Forward => _forward;
        public Vector3 Right => _right;
        public Vector3 Up => _up;

        public Matrix4x4 Transform => _transform;
        public Matrix4x4 ViewMatrix => _viewMatrix;
        public Matrix4x4 ProjectionMatrix => _projectionMatrix;

        public Matrix4x4 VPMatrix => _vpMatrix;

        public Vector2 ClientSize { get => _clientSize; set { _clientSize = value; _isProjectionDirty = true; } }

        public static EditorCamera Instance => EditorRuntime.GlobalSingleton.EditorRenderManager.Camera;
    }
}
