using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Primary.Components;
using Primary.Components.Behaviour;
using Primary.Scenes.Entities;

namespace Primary.Worlds.Volume
{
    [ComponentRequirements(typeof(WorldTransform))]
    public abstract class WorldVolume : EntityBehaviour, ITransformUpdateListener
    {
        private bool _isGlobal;
        private VolumeShape _shape;

        private float _blendDistance;
        private float _weight;
        private int _priority;

        private Vector3 _boxExtents;
        private float _sphereRadius;

        [IgnoreDataMember]
        private CRef<WorldTransform> _transform;

        public WorldVolume()
        {
            _isGlobal = false;
            _shape = VolumeShape.Box;

            _blendDistance = 0.0f;
            _weight = 1.0f;
            _priority = 0;

            _boxExtents = Vector3.One;
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

        public Vector3 BoxExtents { get => _boxExtents; set => _boxExtents = value; }
        public float SphereRadius { get => _sphereRadius; set => _sphereRadius = value; }
    }

    public enum VolumeShape : byte
    {
        Box,
        Sphere
    }
}
