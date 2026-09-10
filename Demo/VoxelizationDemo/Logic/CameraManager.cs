using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Primary.Components;
using Primary.Input;
using Primary.Input.Bindings;
using Primary.Input.Controls;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Scenes;
using Primary.Timing;
using Primary.Windowing;

namespace VoxelizationDemo.Logic
{
    public sealed class CameraManager
    {
        private readonly InputScheme _scheme;

        private readonly InputAction _forwardBackwardAction;
        private readonly InputAction _leftRightAction;
        private readonly InputAction _upDownAction;

        private readonly InputAction _speedupAction;

        private SceneEntity _entity;

        private Vector2 _rotation;

        private Vector3 _position;
        private Quaternion _viewQuat;

        private Matrix4x4 _projection;
        private Matrix4x4 _view;

        private Matrix4x4 _invProjection;
        private Matrix4x4 _invView;

        private Vector2 _clientSize;
        private Vector2 _halfClientSize;

        private Vector2 _mouseViewport;

        internal CameraManager()
        {
            _scheme = new InputScheme() { Name = "Camera" };
            {
                _forwardBackwardAction = _scheme.AddAction();
                _forwardBackwardAction.Control = new AxisControl();

                _leftRightAction = _scheme.AddAction();
                _leftRightAction.Control = new AxisControl();

                _upDownAction = _scheme.AddAction();
                _upDownAction.Control = new AxisControl();

                _speedupAction = _scheme.AddAction();
                _speedupAction.Control = new ButtonControl();

                {
                    Composite1D ws = _forwardBackwardAction.AddBinding<Composite1D>();
                    ws.Name = "W/S";

                    Binding wBinding = ws.AddPositiveBinding();
                    wBinding.Name = "W";
                    wBinding.Path = "<Keyboard>/W";

                    Binding sBinding = ws.AddNegativeBinding();
                    sBinding.Name = "S";
                    sBinding.Path = "<Keyboard>/S";
                }

                {
                    Composite1D ad = _leftRightAction.AddBinding<Composite1D>();
                    ad.Name = "A/D";

                    Binding wBinding = ad.AddPositiveBinding();
                    wBinding.Name = "A";
                    wBinding.Path = "<Keyboard>/A";

                    Binding sBinding = ad.AddNegativeBinding();
                    sBinding.Name = "D";
                    sBinding.Path = "<Keyboard>/D";
                }

                {
                    Composite1D eq = _upDownAction.AddBinding<Composite1D>();
                    eq.Name = "E/Q";

                    Binding wBinding = eq.AddPositiveBinding();
                    wBinding.Name = "E";
                    wBinding.Path = "<Keyboard>/E";

                    Binding sBinding = eq.AddNegativeBinding();
                    sBinding.Name = "Q";
                    sBinding.Path = "<Keyboard>/Q";
                }

                {
                    Binding binding = _speedupAction.AddBinding<Binding>();
                    binding.Name = "Shift";
                    binding.Path = "<Keyboard>/LeftShift";
                }
            }

            _entity = SceneEntity.Null;

            _rotation = Vector2.Zero;

            _position = Vector3.Zero;
            _viewQuat = Quaternion.Identity;

            InputSystem.AddScheme(_scheme);
        }

        internal void SetupWithinScene(Scene scene)
        {
            _entity = scene.CreateEntity(SceneEntity.Null);
            _entity.AddComponent<Camera>();
        }

        internal void UpdateCamera()
        {
            ref WorldTransform worldTransform = ref _entity.GetComponent<WorldTransform>();

            Vector3 directionVector = new Vector3(_leftRightAction.Value.ValueSingle, _upDownAction.Value.ValueSingle, _forwardBackwardAction.Value.ValueSingle);
            if (directionVector != Vector3.Zero)
            {
                float movementSpeed = (_speedupAction.Value.ValueBoolean ? 35.0f : 3.0f) * Time.DeltaTime;

                directionVector = Vector3.Normalize(directionVector);
                directionVector = worldTransform.RightVector * directionVector.X + worldTransform.UpVector * directionVector.Y + worldTransform.ForwardVector * directionVector.Z;

                _entity.GetComponent<Transform>().Position += directionVector * movementSpeed;
            }

            if (InputSystem.Pointer.IsButtonHeld(MouseButton.Right))
            {
                Vector2 delta = Vector2.Shuffle(InputSystem.Pointer.MouseDelta, 1, 0) * 0.25f;
                if (delta != Vector2.Zero)
                {
                    _rotation.X = (_rotation.X + delta.X) % 360.0f;
                    _rotation.Y = (_rotation.Y - delta.Y) % 360.0f;

                    Vector2 rad = Vector2.DegreesToRadians(_rotation);

                    _viewQuat = Quaternion.CreateFromYawPitchRoll(rad.Y, rad.X, 0.0f);
                    _entity.GetComponent<Transform>().Rotation = _viewQuat;
                }
            }

            if (worldTransform.UpdateIndex == Time.FrameIndex - 1)
            {
                ref CameraProjectionData projectionData = ref _entity.GetComponent<CameraProjectionData>();

                _projection = projectionData.ProjectionMatrix;
                _view = projectionData.ViewMatrix;

                Matrix4x4.Invert(_projection, out _invProjection);
                Matrix4x4.Invert(_view, out _invView);
            }

            _position = worldTransform.Position;
            _mouseViewport = InputSystem.Pointer.MousePosition / WindowManager.Instance.PrimaryWindow!.ClientSize.AsVector2() * 2.0f - Vector2.One;

            _clientSize = WindowManager.Instance.PrimaryWindow!.ClientSize.AsVector2();
            _halfClientSize = _clientSize * 0.5f;
        }

        public (Vector2 Screen, bool IsBehindViewer) ProjectWorldToScreen(Vector3 world)
        {
            Vector4 projected = Vector4.Transform(new Vector4(world, 1.0f), _view * _projection);
            Vector3 screenSpace = (projected / projected.W).AsVector3();
            Vector2 output = (new Vector2(screenSpace.X, -screenSpace.Y) + Vector2.One) * _halfClientSize;

            return (output, projected.Z < 0.0f);
        }

        public Ray ProjectViewportToRayInverse(Vector2 viewport)
        {
            Vector3 clipspace = new Vector3(viewport.X, -viewport.Y, -1.0f);
            Vector3 viewspace = Vector3.Transform(clipspace, _invProjection);
            Vector3 worldspace = Vector3.Transform(viewspace, _invView);

            Vector3 direction = Vector3.Normalize(_position - worldspace);
            return new Ray(worldspace + direction, direction);
        }

        public Ray ProjectViewportToRay(Vector2 viewport)
        {
            Vector3 clipspace = new Vector3(viewport.X, -viewport.Y, -1.0f);
            Vector3 viewspace = Vector3.Transform(clipspace, _invProjection);
            Vector3 worldspace = Vector3.Transform(viewspace, _invView);

            Vector3 direction = Vector3.Normalize(worldspace - _position);
            return new Ray(worldspace + direction, direction);
        }

        internal SceneEntity Entity => _entity;

        public Vector3 Position => _position;
        public Quaternion ViewQuaternion => _viewQuat;

        public Vector2 MouseViewport => _mouseViewport;

        public Vector2 ClientSize => _clientSize;
        public Vector2 HalfClientSize => _halfClientSize;
    }
}
