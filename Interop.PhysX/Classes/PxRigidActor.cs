using System;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxRigidActor
    {
        /// <inheritdoc cref="PhysXInterop.PxRigidActor_release()" />
        public void release()
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            PhysXInterop.PxRigidActor_release(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_getInternalActorIndex()" />
        public uint getInternalActorIndex()
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxRigidActor_getInternalActorIndex(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_getGlobalPose()" />
        public PxTransform getGlobalPose()
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxRigidActor_getGlobalPose(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_setGlobalPose(PxTransform*bool)" />
        public void setGlobalPose(PxTransform* pose, bool autowake = true)
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            PhysXInterop.PxRigidActor_setGlobalPose(@this, pose, autowake);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_attachShape(PxShape*)" />
        public bool attachShape(PxShape* shape)
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxRigidActor_attachShape(@this, shape);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_detachShape(PxShape*bool)" />
        public void detachShape(PxShape* shape, bool wakeOnLostTouch = true)
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            PhysXInterop.PxRigidActor_detachShape(@this, shape, wakeOnLostTouch);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_getNbShapes()" />
        public uint getNbShapes()
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxRigidActor_getNbShapes(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_getShapes(PxShape**uintuint)" />
        public uint getShapes(PxShape** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxRigidActor_getShapes(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_getNbConstraints()" />
        public uint getNbConstraints()
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxRigidActor_getNbConstraints(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_getConstraints(PxConstraint**uintuint)" />
        public uint getConstraints(PxConstraint** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxRigidActor_getConstraints(@this, userBuffer, bufferSize, startIndex);
        }

       // public static implicit operator PxActor*(PxRigidActor* value) => (PxActor*)value;
       // public static explicit operator PxRigidActor*(PxActor* value) => (PxRigidActor*)value;

        /// <inheritdoc cref="PhysXInterop.PxActor_getType()" />
        public PxActorType getType()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxActor_getType(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getScene()" />
        public PxScene* getScene()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxActor_getScene(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setName(byte*)" />
        public void setName(byte* name)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            PhysXInterop.PxActor_setName(@this, name);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getName()" />
        public byte* getName()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxActor_getName(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getWorldBounds(float)" />
        public PxBounds3 getWorldBounds(float inflation = 1f)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxActor_getWorldBounds(@this, inflation);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setActorFlag(PxActorFlagbool)" />
        public void setActorFlag(PxActorFlag flag, bool value)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            PhysXInterop.PxActor_setActorFlag(@this, flag, value);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setActorFlags(PxActorFlag)" />
        public void setActorFlags(PxActorFlag inFlags)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            PhysXInterop.PxActor_setActorFlags(@this, inFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getActorFlags()" />
        public PxActorFlag getActorFlags()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxActor_getActorFlags(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setDominanceGroup(PxDominanceGroup)" />
        public void setDominanceGroup(PxDominanceGroup dominanceGroup)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            PhysXInterop.PxActor_setDominanceGroup(@this, dominanceGroup);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getDominanceGroup()" />
        public PxDominanceGroup getDominanceGroup()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxActor_getDominanceGroup(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setOwnerClient(PxClientID)" />
        public void setOwnerClient(PxClientID inClient)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            PhysXInterop.PxActor_setOwnerClient(@this, inClient);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getOwnerClient()" />
        public PxClientID getOwnerClient()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxActor_getOwnerClient(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getAggregate()" />
        public PxAggregate* getAggregate()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxActor_getAggregate(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setEnviormentID(uint)" />
        public bool setEnviormentID(uint envID)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxActor_setEnviormentID(@this, envID);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getEnviormentID()" />
        public uint getEnviormentID()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxActor_getEnviormentID(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_userData_set(void*)" />
        public void userData_set(void* userData)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            PhysXInterop.PxActor_userData_set(@this, userData);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_userData_get()" />
        public void* userData_get()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxActor_userData_get(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_userData_ref()" />
        public void** userData_ref()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxActor_userData_ref(@this);
        }

       // public static implicit operator PxBase*(PxActor* value) => (PxBase*)value;
       // public static explicit operator PxActor*(PxBase* value) => (PxActor*)value;

        /// <inheritdoc cref="PhysXInterop.PxBase_getConcreteTypeName()" />
        public byte* getConcreteTypeName()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxBase_getConcreteTypeName(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_getConcreteType()" />
        public PxType getConcreteType()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxBase_getConcreteType(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_setBaseFlag(PxBaseFlagbool)" />
        public void setBaseFlag(PxBaseFlag flag, bool value)
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            PhysXInterop.PxBase_setBaseFlag(@this, flag, value);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_setBaseFlags(PxBaseFlag)" />
        public void setBaseFlags(PxBaseFlag inFlags)
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            PhysXInterop.PxBase_setBaseFlags(@this, inFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_getBaseFlags()" />
        public PxBaseFlag getBaseFlags()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxBase_getBaseFlags(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_isReleasable()" />
        public bool isReleasable()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidActor>(ref this));
            return PhysXInterop.PxBase_isReleasable(@this);
        }
    }
}
