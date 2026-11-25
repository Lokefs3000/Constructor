using System;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxActor
    {
        /// <inheritdoc cref="PhysXInterop.PxActor_release()" />
        public void release()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            PhysXInterop.PxActor_release(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getType()" />
        public PxActorType getType()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            return PhysXInterop.PxActor_getType(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getScene()" />
        public PxScene* getScene()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            return PhysXInterop.PxActor_getScene(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setName(byte*)" />
        public void setName(byte* name)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            PhysXInterop.PxActor_setName(@this, name);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getName()" />
        public byte* getName()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            return PhysXInterop.PxActor_getName(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getWorldBounds(float)" />
        public PxBounds3 getWorldBounds(float inflation = 1f)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            return PhysXInterop.PxActor_getWorldBounds(@this, inflation);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setActorFlag(PxActorFlagbool)" />
        public void setActorFlag(PxActorFlag flag, bool value)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            PhysXInterop.PxActor_setActorFlag(@this, flag, value);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setActorFlags(PxActorFlag)" />
        public void setActorFlags(PxActorFlag inFlags)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            PhysXInterop.PxActor_setActorFlags(@this, inFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getActorFlags()" />
        public PxActorFlag getActorFlags()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            return PhysXInterop.PxActor_getActorFlags(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setDominanceGroup(PxDominanceGroup)" />
        public void setDominanceGroup(PxDominanceGroup dominanceGroup)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            PhysXInterop.PxActor_setDominanceGroup(@this, dominanceGroup);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getDominanceGroup()" />
        public PxDominanceGroup getDominanceGroup()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            return PhysXInterop.PxActor_getDominanceGroup(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setOwnerClient(PxClientID)" />
        public void setOwnerClient(PxClientID inClient)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            PhysXInterop.PxActor_setOwnerClient(@this, inClient);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getOwnerClient()" />
        public PxClientID getOwnerClient()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            return PhysXInterop.PxActor_getOwnerClient(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getAggregate()" />
        public PxAggregate* getAggregate()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            return PhysXInterop.PxActor_getAggregate(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_setEnviormentID(uint)" />
        public bool setEnviormentID(uint envID)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            return PhysXInterop.PxActor_setEnviormentID(@this, envID);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_getEnviormentID()" />
        public uint getEnviormentID()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            return PhysXInterop.PxActor_getEnviormentID(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_userData_set(void*)" />
        public void userData_set(void* userData)
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            PhysXInterop.PxActor_userData_set(@this, userData);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_userData_get()" />
        public void* userData_get()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            return PhysXInterop.PxActor_userData_get(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxActor_userData_ref()" />
        public void** userData_ref()
        {
            PxActor* @this = (PxActor*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            return PhysXInterop.PxActor_userData_ref(@this);
        }

       // public static implicit operator PxBase*(PxActor* value) => (PxBase*)value;
       // public static explicit operator PxActor*(PxBase* value) => (PxActor*)value;

        /// <inheritdoc cref="PhysXInterop.PxBase_getConcreteTypeName()" />
        public byte* getConcreteTypeName()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            return PhysXInterop.PxBase_getConcreteTypeName(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_getConcreteType()" />
        public PxType getConcreteType()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            return PhysXInterop.PxBase_getConcreteType(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_setBaseFlag(PxBaseFlagbool)" />
        public void setBaseFlag(PxBaseFlag flag, bool value)
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            PhysXInterop.PxBase_setBaseFlag(@this, flag, value);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_setBaseFlags(PxBaseFlag)" />
        public void setBaseFlags(PxBaseFlag inFlags)
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            PhysXInterop.PxBase_setBaseFlags(@this, inFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_getBaseFlags()" />
        public PxBaseFlag getBaseFlags()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            return PhysXInterop.PxBase_getBaseFlags(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_isReleasable()" />
        public bool isReleasable()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxActor>(ref this));
            return PhysXInterop.PxBase_isReleasable(@this);
        }
    }
}
