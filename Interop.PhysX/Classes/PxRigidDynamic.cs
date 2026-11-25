using System;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxRigidDynamic
    {
        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_setKinematicTarget(PxTransform*)" />
        public void setKinematicTarget(PxTransform* destination)
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxRigidDynamic_setKinematicTarget(@this, destination);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_getKinematicTarget(PxTransform*)" />
        public bool getKinematicTarget(PxTransform* target)
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidDynamic_getKinematicTarget(@this, target);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_isSleeping()" />
        public bool isSleeping()
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidDynamic_isSleeping(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_setSleepThreshold(float)" />
        public void setSleepThreshold(float threshold)
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxRigidDynamic_setSleepThreshold(@this, threshold);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_getSleepThreshold()" />
        public float getSleepThreshold()
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidDynamic_getSleepThreshold(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_setStabilizationThreshold(float)" />
        public void setStabilizationThreshold(float threshold)
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxRigidDynamic_setStabilizationThreshold(@this, threshold);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_getStabilizationThreshold()" />
        public float getStabilizationThreshold()
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidDynamic_getStabilizationThreshold(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_setWakeCounter(float)" />
        public void setWakeCounter(float wakeCounterValue)
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxRigidDynamic_setWakeCounter(@this, wakeCounterValue);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_getWakeCounter()" />
        public float getWakeCounter()
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidDynamic_getWakeCounter(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_wakeUp()" />
        public void wakeUp()
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxRigidDynamic_wakeUp(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_putToSleep()" />
        public void putToSleep()
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxRigidDynamic_putToSleep(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_getRigidDynamicLockFlags()" />
        public PxRigidDynamicLockFlag getRigidDynamicLockFlags()
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidDynamic_getRigidDynamicLockFlags(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_setRigidDynamicLockFlag(PxRigidDynamicLockFlagbool)" />
        public void setRigidDynamicLockFlag(PxRigidDynamicLockFlag flag, bool value)
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxRigidDynamic_setRigidDynamicLockFlag(@this, flag, value);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_setRigidDynamicLockFlags(PxRigidDynamicLockFlag)" />
        public void setRigidDynamicLockFlags(PxRigidDynamicLockFlag flags)
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxRigidDynamic_setRigidDynamicLockFlags(@this, flags);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_getLinearVelocity()" />
        public PxVec3 getLinearVelocity()
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidDynamic_getLinearVelocity(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_setLinearVelocity(PxVec3*bool)" />
        public void setLinearVelocity(PxVec3* linVel, bool autowake = true)
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxRigidDynamic_setLinearVelocity(@this, linVel, autowake);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_getAngularVelocity()" />
        public PxVec3 getAngularVelocity()
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidDynamic_getAngularVelocity(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_setAngularVelocity(PxVec3*bool)" />
        public void setAngularVelocity(PxVec3* angVel, bool autowake = true)
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxRigidDynamic_setAngularVelocity(@this, angVel, autowake);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_setSolverIterationCounts(uintuint)" />
        public void setSolverIterationCounts(uint minPositionIters, uint minVelocityIters = 1)
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxRigidDynamic_setSolverIterationCounts(@this, minPositionIters, minVelocityIters);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_getSolverIterationCounts(uint*uint*)" />
        public void getSolverIterationCounts(uint* minPositionIters, uint* minVelocityIters)
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxRigidDynamic_getSolverIterationCounts(@this, minPositionIters, minVelocityIters);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_getContactReportThreshold()" />
        public float getContactReportThreshold()
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidDynamic_getContactReportThreshold(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_setContactReportThreshold(float)" />
        public void setContactReportThreshold(float threshold)
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxRigidDynamic_setContactReportThreshold(@this, threshold);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_getGPUIndex()" />
        public PxRigidDynamicGPUIndex getGPUIndex()
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidDynamic_getGPUIndex(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidDynamic_getConcreteTypeName()" />
        public byte* getConcreteTypeName()
        {
            PxRigidDynamic* @this = (PxRigidDynamic*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidDynamic_getConcreteTypeName(@this);
        }

       // public static implicit operator PxRigidActor*(PxRigidDynamic* value) => (PxRigidActor*)value;
       // public static explicit operator PxRigidDynamic*(PxRigidActor* value) => (PxRigidDynamic*)value;

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_release()" />
        public void release()
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxRigidActor_release(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_getInternalActorIndex()" />
        public uint getInternalActorIndex()
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidActor_getInternalActorIndex(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_getGlobalPose()" />
        public PxTransform getGlobalPose()
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidActor_getGlobalPose(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_setGlobalPose(PxTransform*bool)" />
        public void setGlobalPose(PxTransform* pose, bool autowake = true)
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxRigidActor_setGlobalPose(@this, pose, autowake);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_attachShape(PxShape*)" />
        public bool attachShape(PxShape* shape)
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidActor_attachShape(@this, shape);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_detachShape(PxShape*bool)" />
        public void detachShape(PxShape* shape, bool wakeOnLostTouch = true)
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxRigidActor_detachShape(@this, shape, wakeOnLostTouch);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_getNbShapes()" />
        public uint getNbShapes()
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidActor_getNbShapes(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_getShapes(PxShape**uintuint)" />
        public uint getShapes(PxShape** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidActor_getShapes(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_getNbConstraints()" />
        public uint getNbConstraints()
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidActor_getNbConstraints(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRigidActor_getConstraints(PxConstraint**uintuint)" />
        public uint getConstraints(PxConstraint** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxRigidActor* @this = (PxRigidActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxRigidActor_getConstraints(@this, userBuffer, bufferSize, startIndex);
        }

       // public static implicit operator PxActor*(PxRigidActor* value) => (PxActor*)value;
       // public static explicit operator PxRigidActor*(PxActor* value) => (PxRigidActor*)value;

        /// <inheritdoc cref="PhysXInterop.PxActor_getType()" />
        public PxActorType getType()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxActor_getType(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getScene()" />
        public PxScene* getScene()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxActor_getScene(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setName(byte*)" />
        public void setName(byte* name)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxActor_setName(@this, name);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getName()" />
        public byte* getName()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxActor_getName(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getWorldBounds(float)" />
        public PxBounds3 getWorldBounds(float inflation = 1f)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxActor_getWorldBounds(@this, inflation);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setActorFlag(PxActorFlagbool)" />
        public void setActorFlag(PxActorFlag flag, bool value)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxActor_setActorFlag(@this, flag, value);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setActorFlags(PxActorFlag)" />
        public void setActorFlags(PxActorFlag inFlags)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxActor_setActorFlags(@this, inFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getActorFlags()" />
        public PxActorFlag getActorFlags()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxActor_getActorFlags(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setDominanceGroup(PxDominanceGroup)" />
        public void setDominanceGroup(PxDominanceGroup dominanceGroup)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxActor_setDominanceGroup(@this, dominanceGroup);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getDominanceGroup()" />
        public PxDominanceGroup getDominanceGroup()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxActor_getDominanceGroup(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setOwnerClient(PxClientID)" />
        public void setOwnerClient(PxClientID inClient)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxActor_setOwnerClient(@this, inClient);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getOwnerClient()" />
        public PxClientID getOwnerClient()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxActor_getOwnerClient(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getAggregate()" />
        public PxAggregate* getAggregate()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxActor_getAggregate(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setEnviormentID(uint)" />
        public bool setEnviormentID(uint envID)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxActor_setEnviormentID(@this, envID);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getEnviormentID()" />
        public uint getEnviormentID()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxActor_getEnviormentID(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_userData_set(void*)" />
        public void userData_set(void* userData)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxActor_userData_set(@this, userData);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_userData_get()" />
        public void* userData_get()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxActor_userData_get(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_userData_ref()" />
        public void** userData_ref()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxActor_userData_ref(@this);
        }

       // public static implicit operator PxBase*(PxActor* value) => (PxBase*)value;
       // public static explicit operator PxActor*(PxBase* value) => (PxActor*)value;

        /// <inheritdoc cref="PhysXInterop.PxBase_getConcreteType()" />
        public PxType getConcreteType()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxBase_getConcreteType(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_setBaseFlag(PxBaseFlagbool)" />
        public void setBaseFlag(PxBaseFlag flag, bool value)
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxBase_setBaseFlag(@this, flag, value);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_setBaseFlags(PxBaseFlag)" />
        public void setBaseFlags(PxBaseFlag inFlags)
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            PhysXInterop.PxBase_setBaseFlags(@this, inFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_getBaseFlags()" />
        public PxBaseFlag getBaseFlags()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxBase_getBaseFlags(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_isReleasable()" />
        public bool isReleasable()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxRigidDynamic>(ref this));
            return PhysXInterop.PxBase_isReleasable(@this);
        }
    }
}
