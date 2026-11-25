using System;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxMaterial
    {
        /// <inheritdoc cref="PhysXInterop.PxMaterial_setDynamicFriction(float)" />
        public void setDynamicFriction(float coef)
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            PhysXInterop.PxMaterial_setDynamicFriction(@this, coef);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_getDynamicFriction()" />
        public float getDynamicFriction()
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            return PhysXInterop.PxMaterial_getDynamicFriction(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_setStaticFriction(float)" />
        public void setStaticFriction(float coef)
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            PhysXInterop.PxMaterial_setStaticFriction(@this, coef);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_getStaticFriction()" />
        public float getStaticFriction()
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            return PhysXInterop.PxMaterial_getStaticFriction(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_setRestitution(float)" />
        public void setRestitution(float rest)
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            PhysXInterop.PxMaterial_setRestitution(@this, rest);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_getRestitution()" />
        public float getRestitution()
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            return PhysXInterop.PxMaterial_getRestitution(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_setDamping(float)" />
        public void setDamping(float damping)
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            PhysXInterop.PxMaterial_setDamping(@this, damping);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_getDamping()" />
        public float getDamping()
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            return PhysXInterop.PxMaterial_getDamping(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_setFlag(PxMaterialFlagbool)" />
        public void setFlag(PxMaterialFlag flag, bool b)
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            PhysXInterop.PxMaterial_setFlag(@this, flag, b);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_setFlags(PxMaterialFlag)" />
        public void setFlags(PxMaterialFlag flags)
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            PhysXInterop.PxMaterial_setFlags(@this, flags);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_getFlags()" />
        public PxMaterialFlag getFlags()
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            return PhysXInterop.PxMaterial_getFlags(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_setFrictionCombineMode(PxCombineMode)" />
        public void setFrictionCombineMode(PxCombineMode combMode)
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            PhysXInterop.PxMaterial_setFrictionCombineMode(@this, combMode);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_getFrictionCombineMode()" />
        public PxCombineMode getFrictionCombineMode()
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            return PhysXInterop.PxMaterial_getFrictionCombineMode(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_setRestitutionCombineMode(PxCombineMode)" />
        public void setRestitutionCombineMode(PxCombineMode combMode)
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            PhysXInterop.PxMaterial_setRestitutionCombineMode(@this, combMode);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_getRestitutionCombineMode()" />
        public PxCombineMode getRestitutionCombineMode()
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            return PhysXInterop.PxMaterial_getRestitutionCombineMode(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_setDampingCombineMode(PxCombineMode)" />
        public void setDampingCombineMode(PxCombineMode combMode)
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            PhysXInterop.PxMaterial_setDampingCombineMode(@this, combMode);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_getDampingCombineMode()" />
        public PxCombineMode getDampingCombineMode()
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            return PhysXInterop.PxMaterial_getDampingCombineMode(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxMaterial_getConcreteTypeName()" />
        public byte* getConcreteTypeName()
        {
            PxMaterial* @this = (PxMaterial*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            return PhysXInterop.PxMaterial_getConcreteTypeName(@this);
        }

       // public static implicit operator PxBaseMaterial*(PxMaterial* value) => (PxBaseMaterial*)value;
       // public static explicit operator PxMaterial*(PxBaseMaterial* value) => (PxMaterial*)value;

       // public static implicit operator PxRefCounted*(PxBaseMaterial* value) => (PxRefCounted*)value;
       // public static explicit operator PxBaseMaterial*(PxRefCounted* value) => (PxBaseMaterial*)value;

        /// <inheritdoc cref="PhysXInterop.PxRefCounted_release()" />
        public void release()
        {
            PxRefCounted* @this = (PxRefCounted*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            PhysXInterop.PxRefCounted_release(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRefCounted_getReferenceCount()" />
        public uint getReferenceCount()
        {
            PxRefCounted* @this = (PxRefCounted*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            return PhysXInterop.PxRefCounted_getReferenceCount(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRefCounted_acquireReference()" />
        public void acquireReference()
        {
            PxRefCounted* @this = (PxRefCounted*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            PhysXInterop.PxRefCounted_acquireReference(@this);
        }

       // public static implicit operator PxBase*(PxRefCounted* value) => (PxBase*)value;
       // public static explicit operator PxRefCounted*(PxBase* value) => (PxRefCounted*)value;

        /// <inheritdoc cref="PhysXInterop.PxBase_getConcreteType()" />
        public PxType getConcreteType()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            return PhysXInterop.PxBase_getConcreteType(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_setBaseFlag(PxBaseFlagbool)" />
        public void setBaseFlag(PxBaseFlag flag, bool value)
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            PhysXInterop.PxBase_setBaseFlag(@this, flag, value);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_setBaseFlags(PxBaseFlag)" />
        public void setBaseFlags(PxBaseFlag inFlags)
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            PhysXInterop.PxBase_setBaseFlags(@this, inFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_getBaseFlags()" />
        public PxBaseFlag getBaseFlags()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            return PhysXInterop.PxBase_getBaseFlags(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_isReleasable()" />
        public bool isReleasable()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxMaterial>(ref this));
            return PhysXInterop.PxBase_isReleasable(@this);
        }
    }
}
