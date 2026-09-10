using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Primary.Collections;
using Primary.Input;
using Primary.Mathematics;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Gizmo;

namespace VoxelizationDemo.Editor
{
    internal sealed class InteractionGizmoManager
    {
        private readonly List<IInteractionGizmo> _gizmos;

        internal InteractionGizmoManager()
        {
            _gizmos = new List<IInteractionGizmo>();
        }

        internal void UpdateGizmos()
        {
            foreach (IInteractionGizmo gizmo in _gizmos)
            {
                gizmo.UpdateScreenData(VoxelRuntime.Instance.CameraManager, InputSystem.Pointer.MousePosition);
            }
        }

        internal void GetGizmoPolygons(Frustrum frustrum, Vector3 center, ref RentedList<InteractionGizmoDraw> draws, ref RentedList<InteractionGizmoVertex> vertices, ref RentedList<ushort> indices)
        {
            foreach (IInteractionGizmo gizmo in _gizmos)
            {
                if (gizmo.IsEnabled)
                {
                    int startVtxCount = vertices.Count;
                    int startIdxCount = indices.Count;

                    gizmo.GenerateVertices(frustrum, center, ref vertices, ref indices, out Matrix4x4 model);

                    int indexCount = indices.Count - startIdxCount;
                    if (indexCount > 0)
                    {
                        draws.Add(new InteractionGizmoDraw(model, startVtxCount, indexCount, startIdxCount));
                    }
                }
            }
        }

        internal TranslateGizmo CreateTranslation()
        {
            TranslateGizmo gizmo = new TranslateGizmo();
            _gizmos.Add(gizmo);

            return gizmo;
        }
    }

    internal readonly record struct InteractionGizmoDraw(Matrix4x4 Model, int BaseVertex, int IndexCount, int IndexOffset);
}
