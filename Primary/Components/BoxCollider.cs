using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Interop.PhysX;
using Primary.Physics;
using Primary.Scenes.Entities;

namespace Primary.Components
{
    public sealed unsafe class BoxCollider : Collider
    {
        private RigidObject _currentOwner;

        private bool _isLocked;
        private PxShape* _shape;

        private Vector3 _halfExtents;

        protected internal override void OnInit()
        {
            PhysicsManager manager = Engine.GlobalSingleton.PhysicsManager;

            _currentOwner = manager.FindCurrentOwner(Entity);

            _isLocked = manager.Config.ShareStaticShapes && Entity.HasComponent<StaticTag>();

            PxBoxGeometry geometry = new PxBoxGeometry
            {
                @base = new PxGeometry { type = PxGeometryType.eBOX },
                halfExtents = Unsafe.As<Vector3, PxVec3>(ref _halfExtents)
            };

            _shape = manager.PhysXPhysics->createShape(
                (PxGeometry*)&geometry,
                manager.PhysXDefaultMat,
                _isLocked,
                PhysXEnumCast.GetShapeFlags(_colliderUsage) | PxShapeFlag.eVISUALIZATION);

            if (_shape == null)
            {
                EngLog.Physics.Error("Failed to create box collider on entity '{e}'", Entity);
            }
            else
            {
                Vector3 centerOffset = _centerOffset;
                Quaternion identity = Quaternion.Identity;

                _shape->setLocalPose(new PxTransform
                {
                    p =
                    Unsafe.As<Vector3, PxVec3>(ref centerOffset),
                    q = Unsafe.As<Quaternion, PxQuat>(ref identity)
                });

                _currentOwner.Attach(this);
            }
        }

        protected internal override void OnDestroy()
        {
            _currentOwner.Detach(this);

            if (_shape != null)
            {
                _shape->release();
                _shape = null;
            }
        }

        protected internal override void OnDisabled()
        {
            _currentOwner.Detach(this);
        }

        protected internal override void OnEnabled()
        {
            _currentOwner.Attach(this);
        }
    }
}
