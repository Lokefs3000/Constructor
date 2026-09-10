using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Gizmo;

namespace VoxelizationDemo.Editor.Tools
{
    internal sealed class TranslateTool : Tool
    {
        private readonly TranslateGizmo _gizmo;

        private Vector3 _gizmoStartOrigin;

        public TranslateTool()
        {
            _gizmo = EditorManager.Instance.InteractionGizmoManager.CreateTranslation();
            _gizmo.IsEnabled = false;
        }

        public override void ActiveSelf()
        {
            base.ActiveSelf();
        }

        public override void DeactivateSelf()
        {
            base.DeactivateSelf();
            _gizmo.IsEnabled = false;
        }

        public override void Update()
        {
            if (_isBusy)
            {
                if (_gizmo.TryUpdateDrag(out Vector3 delta))
                {
                    _gizmo.Origin = _gizmoStartOrigin + delta;

                    ToolManager manager = EditorManager.Instance.ToolManager;
                    foreach (EntityToolTransform transform in manager.Transforms)
                    {
                        transform.UpdateTranslation(delta);
                    }
                }
            }
            else
            {
                ToolManager manager = EditorManager.Instance.ToolManager;
                if (manager.Transforms.Count == 0)
                {
                    _gizmo.IsEnabled = false;
                }
                else
                {
                    _gizmo.IsEnabled = true;

                    switch (manager.OriginMode)
                    {
                        case ToolOriginMode.Centered:
                            {
                                Vector3 currentMin = default;
                                Vector3 currentMax = default;

                                bool isTransformFirst = true;
                                bool hasFoundRotation = false;

                                foreach (EntityToolTransform transform in manager.Transforms)
                                {
                                    Vector3 position = transform.Position;
                                    if (isTransformFirst)
                                    {
                                        currentMin = position;
                                        currentMax = position;

                                        isTransformFirst = false;
                                    }
                                    else
                                    {
                                        currentMin = Vector3.Min(currentMin, position);
                                        currentMax = Vector3.Max(currentMax, position);
                                    }

                                    if (!hasFoundRotation)
                                    {
                                        Matrix4x4 matrix = manager.RelativeMode == ToolRelativeMode.Local ? transform.LocalModel : transform.WorldModel;
                                        hasFoundRotation = Matrix4x4.Decompose(matrix, out _, out Quaternion quaternion, out _);

                                        if (hasFoundRotation)
                                            _gizmo.Rotation = quaternion;
                                    }
                                }

                                _gizmo.Origin = (currentMin + currentMax) * 0.5f;
                                break;
                            }
                        case ToolOriginMode.First:
                            {
                                EntityToolTransform transform = manager.Transforms[0];
                                _gizmo.Origin = transform.Position;

                                Matrix4x4 matrix = manager.RelativeMode == ToolRelativeMode.Local ? transform.LocalModel : transform.WorldModel;
                                if (Matrix4x4.Decompose(matrix, out _, out Quaternion quaternion, out _))
                                    _gizmo.Rotation = quaternion;
                                else
                                    _gizmo.Rotation = Quaternion.Identity;

                                break;
                            }
                        case ToolOriginMode.Last:
                            {
                                EntityToolTransform transform = manager.Transforms[^1];
                                _gizmo.Origin = transform.Position;

                                Matrix4x4 matrix = manager.RelativeMode == ToolRelativeMode.Local ? transform.LocalModel : transform.WorldModel;
                                if (Matrix4x4.Decompose(matrix, out _, out Quaternion quaternion, out _))
                                    _gizmo.Rotation = quaternion;
                                else
                                    _gizmo.Rotation = Quaternion.Identity;

                                break;
                            }
                    }
                }
            }
        }

        public override void Finish()
        {
            Debug.Assert(_isBusy);

            ToolManager manager = EditorManager.Instance.ToolManager;
            foreach (EntityToolTransform transform in manager.Transforms)
            {
                transform.ConfirmUpdates();
            }

            _gizmo.TryEndDrag();
            _isBusy = false;
        }

        public override void Cancel()
        {
            Debug.Assert(_isBusy);

            ToolManager manager = EditorManager.Instance.ToolManager;
            foreach (EntityToolTransform transform in manager.Transforms)
            {
                transform.DiscardUpdates();
            }

            _gizmo.TryEndDrag();
            _isBusy = false;
        }

        public override void TryUseTool()
        {
            if (_gizmo.TryStartDrag())
            {
                _isBusy = true;
                _gizmoStartOrigin = _gizmo.Origin;
            }
        }
    }
}
