using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Primary.Components;
using Primary.Components.Behaviour;
using Primary.Scenes.Entities;

namespace Primary.Worlds.Volume
{
    public abstract class WorldVolume : EntityBehaviour, ITransformUpdateListener
    {
        private bool _isGlobal;
        private VolumeShape _shape;

        private float _blendDistance;
        private float _weight;
        private int _priority;

        private VolumeShapeData _shapeData;

        private CRef<WorldTransform> _transform;

        public WorldVolume()
        {
            _isGlobal = false;
            _shape = VolumeShape.Box;

            _blendDistance = 0.0f;
            _weight = 1.0f;
            _priority = 0;

            _shapeData = new VolumeShapeData(Vector3.One);
        }

        protected internal override void OnEarlyInit()
        {
            _transform = Entity.GetComponentAsCRef<WorldTransform>();
        }

        void ITransformUpdateListener.OnTransformUpdate()
        {
            
        }

        public bool IsGlobal { get => _isGlobal; set => _isGlobal = value; }
        public VolumeShape Shape { get => _shape; set => _shape = value; }

        public float BlendDistance { get => _blendDistance; set => _blendDistance = value; }
        public float Weight { get => _weight; set => _weight = value; }
        public int Priority { get => _priority; set => _priority = value; }

        public Vector3 BoxExtents { get => _shapeData.Extents; set => _shapeData.Extents = value; }
        public float SphereRadius { get => _shapeData.Radius; set => _shapeData.Radius = value; }

        [StructLayout(LayoutKind.Explicit)]
        private struct VolumeShapeData
        {
            [FieldOffset(0)] public Vector3 Extents;
            [FieldOffset(0)] public float Radius;

            public VolumeShapeData(Vector3 extents) => Extents = extents;
            public VolumeShapeData(float radius) => Radius = radius;
        }
    }

    public enum VolumeShape : byte
    {
        Box,
        Sphere
    }
}
