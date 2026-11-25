using System;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxPBDMaterial
    {
       // public static implicit operator PxBaseMaterial*(PxPBDMaterial* value) => (PxBaseMaterial*)value;
       // public static explicit operator PxPBDMaterial*(PxBaseMaterial* value) => (PxPBDMaterial*)value;

       // public static implicit operator PxRefCounted*(PxBaseMaterial* value) => (PxRefCounted*)value;
       // public static explicit operator PxBaseMaterial*(PxRefCounted* value) => (PxBaseMaterial*)value;

        /// <inheritdoc cref="PhysXInterop.PxRefCounted_release()" />
        public void release()
        {
            PxRefCounted* @this = (PxRefCounted*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPBDMaterial>(ref this));
            PhysXInterop.PxRefCounted_release(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRefCounted_getReferenceCount()" />
        public uint getReferenceCount()
        {
            PxRefCounted* @this = (PxRefCounted*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPBDMaterial>(ref this));
            return PhysXInterop.PxRefCounted_getReferenceCount(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRefCounted_acquireReference()" />
        public void acquireReference()
        {
            PxRefCounted* @this = (PxRefCounted*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPBDMaterial>(ref this));
            PhysXInterop.PxRefCounted_acquireReference(@this);
        }

       // public static implicit operator PxBase*(PxRefCounted* value) => (PxBase*)value;
       // public static explicit operator PxRefCounted*(PxBase* value) => (PxRefCounted*)value;

        /// <inheritdoc cref="PhysXInterop.PxBase_getConcreteTypeName()" />
        public byte* getConcreteTypeName()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPBDMaterial>(ref this));
            return PhysXInterop.PxBase_getConcreteTypeName(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_getConcreteType()" />
        public PxType getConcreteType()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPBDMaterial>(ref this));
            return PhysXInterop.PxBase_getConcreteType(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_setBaseFlag(PxBaseFlagbool)" />
        public void setBaseFlag(PxBaseFlag flag, bool value)
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPBDMaterial>(ref this));
            PhysXInterop.PxBase_setBaseFlag(@this, flag, value);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_setBaseFlags(PxBaseFlag)" />
        public void setBaseFlags(PxBaseFlag inFlags)
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPBDMaterial>(ref this));
            PhysXInterop.PxBase_setBaseFlags(@this, inFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_getBaseFlags()" />
        public PxBaseFlag getBaseFlags()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPBDMaterial>(ref this));
            return PhysXInterop.PxBase_getBaseFlags(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_isReleasable()" />
        public bool isReleasable()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPBDMaterial>(ref this));
            return PhysXInterop.PxBase_isReleasable(@this);
        }
    }
}
