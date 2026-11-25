using System;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxShape
    {
        /// <inheritdoc cref="PhysXInterop.PxShape_setGeometry(PxGeometry*)" />
        public void setGeometry(PxGeometry* geometry)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxShape_setGeometry(@this, geometry);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getGeometry()" />
        public PxGeometryHolder getGeometry()
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getGeometry(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getActor()" />
        public PxRigidActor* getActor()
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getActor(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_setLocalPose(PxTransform)" />
        public void setLocalPose(PxTransform pose)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxShape_setLocalPose(@this, pose);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getLocalPose()" />
        public PxTransform getLocalPose()
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getLocalPose(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_setSimulationFilterData(PxFilterData)" />
        public void setSimulationFilterData(PxFilterData data)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxShape_setSimulationFilterData(@this, data);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getSimulationFilterData()" />
        public PxFilterData getSimulationFilterData()
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getSimulationFilterData(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_setQueryFilterData(PxFilterData)" />
        public void setQueryFilterData(PxFilterData data)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxShape_setQueryFilterData(@this, data);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getQueryFilterData()" />
        public PxFilterData getQueryFilterData()
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getQueryFilterData(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_setMaterials(PxMaterial**ushort)" />
        public void setMaterials(PxMaterial** materials, ushort materialCount)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxShape_setMaterials(@this, materials, materialCount);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_setDeformableSurfaceMaterials(PxDeformableSurfaceMaterial**ushort)" />
        public void setDeformableSurfaceMaterials(PxDeformableSurfaceMaterial** materials, ushort materialCount)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxShape_setDeformableSurfaceMaterials(@this, materials, materialCount);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_setDeformableVolumeMaterials(PxDeformableVolumeMaterial**ushort)" />
        public void setDeformableVolumeMaterials(PxDeformableVolumeMaterial** materials, ushort materialCount)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxShape_setDeformableVolumeMaterials(@this, materials, materialCount);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getNbMaterials()" />
        public ushort getNbMaterials()
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getNbMaterials(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getMaterials(PxMaterial**uintuint)" />
        public uint getMaterials(PxMaterial** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getMaterials(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getDeformableSurfaceMaterials(PxDeformableSurfaceMaterial**uintuint)" />
        public uint getDeformableSurfaceMaterials(PxDeformableSurfaceMaterial** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getDeformableSurfaceMaterials(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getDeformableVolumeMaterials(PxDeformableVolumeMaterial**uintuint)" />
        public uint getDeformableVolumeMaterials(PxDeformableVolumeMaterial** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getDeformableVolumeMaterials(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getMaterialFromInternalFaceIndex(uint)" />
        public PxBaseMaterial* getMaterialFromInternalFaceIndex(uint faceIndex)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getMaterialFromInternalFaceIndex(@this, faceIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_setContactOffset(float)" />
        public void setContactOffset(float contactOffset)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxShape_setContactOffset(@this, contactOffset);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getContactOffset()" />
        public float getContactOffset()
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getContactOffset(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_setRestOffset(float)" />
        public void setRestOffset(float restOffset)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxShape_setRestOffset(@this, restOffset);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getRestOffset()" />
        public float getRestOffset()
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getRestOffset(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_setDensityForFLuid(float)" />
        public void setDensityForFLuid(float densityForFluid)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxShape_setDensityForFLuid(@this, densityForFluid);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getDensityForFluid()" />
        public float getDensityForFluid()
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getDensityForFluid(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_setTorsionalPatchRadius(float)" />
        public void setTorsionalPatchRadius(float radius)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxShape_setTorsionalPatchRadius(@this, radius);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getTorsionalPatchRadius()" />
        public float getTorsionalPatchRadius()
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getTorsionalPatchRadius(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_setMinTorsionalPatchRadius(float)" />
        public void setMinTorsionalPatchRadius(float radius)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxShape_setMinTorsionalPatchRadius(@this, radius);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getMinTorsionalPatchRadius()" />
        public float getMinTorsionalPatchRadius()
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getMinTorsionalPatchRadius(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getGPUIndex()" />
        public PxShapeGPUIndex getGPUIndex()
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getGPUIndex(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_setFlag(PxShapeFlagbool)" />
        public void setFlag(PxShapeFlag flag, bool value)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxShape_setFlag(@this, flag, value);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_setFlags(PxShapeFlag)" />
        public void setFlags(PxShapeFlag inFlags)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxShape_setFlags(@this, inFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getFlags()" />
        public PxShapeFlag getFlags()
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getFlags(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_isExclusive()" />
        public bool isExclusive()
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_isExclusive(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_setName(byte*)" />
        public void setName(byte* name)
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxShape_setName(@this, name);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getName()" />
        public byte* getName()
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getName(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxShape_getConcreteTypeName()" />
        public byte* getConcreteTypeName()
        {
            PxShape* @this = (PxShape*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxShape_getConcreteTypeName(@this);
        }

       // public static implicit operator PxRefCounted*(PxShape* value) => (PxRefCounted*)value;
       // public static explicit operator PxShape*(PxRefCounted* value) => (PxShape*)value;

        /// <inheritdoc cref="PhysXInterop.PxRefCounted_release()" />
        public void release()
        {
            PxRefCounted* @this = (PxRefCounted*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxRefCounted_release(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRefCounted_getReferenceCount()" />
        public uint getReferenceCount()
        {
            PxRefCounted* @this = (PxRefCounted*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxRefCounted_getReferenceCount(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxRefCounted_acquireReference()" />
        public void acquireReference()
        {
            PxRefCounted* @this = (PxRefCounted*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxRefCounted_acquireReference(@this);
        }

       // public static implicit operator PxBase*(PxRefCounted* value) => (PxBase*)value;
       // public static explicit operator PxRefCounted*(PxBase* value) => (PxRefCounted*)value;

        /// <inheritdoc cref="PhysXInterop.PxBase_getConcreteType()" />
        public PxType getConcreteType()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxBase_getConcreteType(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_setBaseFlag(PxBaseFlagbool)" />
        public void setBaseFlag(PxBaseFlag flag, bool value)
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxBase_setBaseFlag(@this, flag, value);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_setBaseFlags(PxBaseFlag)" />
        public void setBaseFlags(PxBaseFlag inFlags)
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            PhysXInterop.PxBase_setBaseFlags(@this, inFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_getBaseFlags()" />
        public PxBaseFlag getBaseFlags()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxBase_getBaseFlags(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxBase_isReleasable()" />
        public bool isReleasable()
        {
            PxBase* @this = (PxBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxShape>(ref this));
            return PhysXInterop.PxBase_isReleasable(@this);
        }
    }
}
