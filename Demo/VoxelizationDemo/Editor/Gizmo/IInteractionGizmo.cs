using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Primary.Collections;
using Primary.Mathematics;
using VoxelizationDemo.Logic;

namespace VoxelizationDemo.Editor.Gizmo
{
    internal interface IInteractionGizmo
    {
        public void GenerateVertices(Frustrum frustrum, Vector3 center, ref RentedList<InteractionGizmoVertex> vertices, ref RentedList<ushort> indices, out Matrix4x4 model);

        public void UpdateScreenData(CameraManager camera, Vector2 mousePos);

        public bool TryStartDrag();
        public bool TryEndDrag();

        public bool IsEnabled { get; set; }
    }
}
