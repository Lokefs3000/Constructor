using Arch.Core;
using Editor.Components;
using Editor.Gui.View;
using Editor.Rendering;
using Primary;
using Primary.Common;
using Primary.Components;
using Primary.Profiling;
using Primary.Scenes;
using Primary.Systems;
using Schedulers;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.Systems
{
    public record struct GizmoSystem : ISystem, IForEach<GizmoTriangleComponent>
    {
        private Vector3 _nearPosition;

        public void Schedule(World world, JobScheduler scheduler)
        {
            using (new ProfilingScope("Gizmo"))
            {
                _nearPosition = EditorCamera.Instance.Position;

                SceneManager manager = Engine.GlobalSingleton.SceneManager;
                manager.World.InlineQuery<GizmoSystem, GizmoTriangleComponent>(s_query, ref this);
            }
        }

        public void Update(ref GizmoTriangleComponent tri)
        {
            if (!Unsafe.IsNullRef(in tri))
            {
                float dist = Vector3.Distance(tri.PointA, _nearPosition);
                if (dist < 10.0f)
                    Gizmos.DrawWireTriangle(tri.PointA, tri.PointB, tri.PointC, tri.Color);
                else
                    Gizmos.DrawWireTriangle(tri.PointA, tri.PointB, tri.PointC, new Color(tri.Color.R, tri.Color.G, tri.Color.B, 0.025f));
            }
        }

        private static readonly QueryDescription s_query = new QueryDescription().WithAny<GizmoTriangleComponent>();

        public ref readonly QueryDescription Description => ref s_query;
        public bool SystemNeedsFullExecutionTime => true;
    }
}
