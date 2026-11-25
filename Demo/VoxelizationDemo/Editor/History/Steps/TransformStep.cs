using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Primary.Components;
using Primary.Scenes;
using VoxelizationDemo.Editor.History.Steps;
using VoxelizationDemo.Editor.Tracking;

namespace VoxelizationDemo.Editor.History.Steps
{
    public sealed class TransformStep : IHistoryStep
    {
        private readonly Guid _entityUuid;

        private readonly Vector3 _prevPosition;
        private readonly Quaternion _prevRotation;
        private readonly Vector3 _prevScale;

        private readonly Vector3? _nextPosition;
        private readonly Quaternion? _nextRotation;
        private readonly Vector3? _nextScale;

        public TransformStep(SceneEntity entity, Vector3? position, Quaternion? rotation, Vector3? scale)
        {
            ref Transform transform = ref entity.GetComponent<Transform>();

            _entityUuid = EntityTracker.GetUuid(entity);

            _prevPosition = transform.Position;
            _prevRotation = transform.Rotation;
            _prevScale = transform.Scale;

            _nextPosition = position;
            _nextRotation = rotation;
            _nextScale = scale;
        }

        public void Undo()
        {
            SceneEntity entity = EntityTracker.GetEntity(_entityUuid);
            if (!entity.IsNull)
            {
                ref Transform transform = ref entity.GetComponent<Transform>();
                transform.Position = _prevPosition;
                transform.Rotation = _prevRotation;
                transform.Scale = _prevScale;
            }
        }

        public void Redo()
        {
            SceneEntity entity = EntityTracker.GetEntity(_entityUuid);
            if (!entity.IsNull)
            {
                ref Transform transform = ref entity.GetComponent<Transform>();
                transform.Position = _nextPosition.GetValueOrDefault(transform.Position);
                transform.Rotation = _nextRotation.GetValueOrDefault(transform.Rotation);
                transform.Scale = _nextScale.GetValueOrDefault(transform.Scale);
            }
        }
    }
}

namespace VoxelizationDemo.Editor
{
    public static partial class UndoHistory
    {
        public static void RecordTransform(SceneEntity entity, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null)
        {
            if (position.HasValue || rotation.HasValue || scale.HasValue)
            {
                Record(new TransformStep(entity, position, rotation, scale));
            }
        }
    }
}
