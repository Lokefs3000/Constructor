using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Components;
using Primary.Scenes;

namespace VoxelizationDemo.Editor.Tools
{
    internal sealed class EntityToolTransform
    {
        private readonly SceneEntity _entity;

        private bool _hasStoredDefaults;
        private Vector3 _storedPosition;
        private Quaternion _storedRotation;
        private Vector3 _storedScale;

        public EntityToolTransform(SceneEntity entity)
        {
            _entity = entity;

            _hasStoredDefaults = false;
        }

        private void StoreDefaults()
        {
            ref Transform transform = ref _entity.GetComponent<Transform>();

            _hasStoredDefaults = true;
            _storedPosition = transform.Position;
            _storedRotation = transform.Rotation;
            _storedScale = transform.Scale;
        }

        public void DiscardUpdates()
        {
            if (_hasStoredDefaults)
            {
                ref Transform transform = ref _entity.GetComponent<Transform>();
                transform.Position = _storedPosition;
                transform.Rotation = _storedRotation;
                transform.Scale = _storedScale;

                _hasStoredDefaults = false;
            }
        }

        public void ConfirmUpdates()
        {
            _hasStoredDefaults = false;
        }

        public void UpdateTranslation(Vector3 delta)
        {
            if (!_hasStoredDefaults)
                StoreDefaults();

            ref Transform transform = ref _entity.GetComponent<Transform>();
            if (!_entity.Parent.IsNull && !_entity.Parent.IsSceneRoot)
            {
                ref WorldTransform parentWorld = ref _entity.Parent.GetComponent<WorldTransform>();
                if (!Unsafe.IsNullRef(in parentWorld))
                    delta = Vector3.Transform(delta, parentWorld.Transformation);
            }

            transform.Position = _storedPosition + delta;
        }

        public void UpdateRotation(Quaternion delta)
        {
            if (!_hasStoredDefaults)
                StoreDefaults();

            ref Transform transform = ref _entity.GetComponent<Transform>();
            if (!_entity.Parent.IsNull && !_entity.Parent.IsSceneRoot)
            {
                ref WorldTransform parentWorld = ref _entity.Parent.GetComponent<WorldTransform>();
                if (!Unsafe.IsNullRef(in parentWorld))
                    delta *= Quaternion.CreateFromRotationMatrix(parentWorld.Transformation);
            }

            transform.Rotation = _storedRotation * delta;
        }

        public void UpdateScale(Vector3 delta)
        {
            if (!_hasStoredDefaults)
                StoreDefaults();

            ref Transform transform = ref _entity.GetComponent<Transform>();
            if (!_entity.Parent.IsNull && !_entity.Parent.IsSceneRoot)
            {
                ref WorldTransform parentWorld = ref _entity.Parent.GetComponent<WorldTransform>();
                if (!Unsafe.IsNullRef(in parentWorld))
                    delta *= Vector3.SquareRoot(new Vector3(
                        parentWorld.Transformation.X.AsVector3().LengthSquared(),
                        parentWorld.Transformation.Y.AsVector3().LengthSquared(),
                        parentWorld.Transformation.Z.AsVector3().LengthSquared()));
            }

            transform.Scale = _storedScale + delta;
        }

        public Vector3 Position => _entity.GetComponent<WorldTransform>().Position;

        public Matrix4x4 LocalModel => _entity.GetComponent<LocalTransform>().Transformation;
        public Matrix4x4 WorldModel => _entity.GetComponent<WorldTransform>().Transformation;

        public SceneEntity Entity => _entity;
    }
}
