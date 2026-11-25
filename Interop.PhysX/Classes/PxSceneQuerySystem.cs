using System;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxSceneQuerySystem
    {
       // public static implicit operator PxSceneQuerySystemBase*(PxSceneQuerySystem* value) => (PxSceneQuerySystemBase*)value;
       // public static explicit operator PxSceneQuerySystem*(PxSceneQuerySystemBase* value) => (PxSceneQuerySystem*)value;

        /// <inheritdoc cref="PhysXInterop.PxSceneQuerySystemBase_setDynamicTreeRebuildRateHint(uint)" />
        public void setDynamicTreeRebuildRateHint(uint dynamicTreeRebuildRateHint)
        {
            PxSceneQuerySystemBase* @this = (PxSceneQuerySystemBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxSceneQuerySystem>(ref this));
            PhysXInterop.PxSceneQuerySystemBase_setDynamicTreeRebuildRateHint(@this, dynamicTreeRebuildRateHint);
        }

        /// <inheritdoc cref="PhysXInterop.PxSceneQuerySystemBase_getDynamicTreeRebuildRateHint()" />
        public uint getDynamicTreeRebuildRateHint()
        {
            PxSceneQuerySystemBase* @this = (PxSceneQuerySystemBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxSceneQuerySystem>(ref this));
            return PhysXInterop.PxSceneQuerySystemBase_getDynamicTreeRebuildRateHint(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxSceneQuerySystemBase_forceRebuildDynamicTree(uint)" />
        public void forceRebuildDynamicTree(uint prunerIndex)
        {
            PxSceneQuerySystemBase* @this = (PxSceneQuerySystemBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxSceneQuerySystem>(ref this));
            PhysXInterop.PxSceneQuerySystemBase_forceRebuildDynamicTree(@this, prunerIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxSceneQuerySystemBase_setUpdateMode(PxSceneQueryUpdateMode)" />
        public void setUpdateMode(PxSceneQueryUpdateMode updateMode)
        {
            PxSceneQuerySystemBase* @this = (PxSceneQuerySystemBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxSceneQuerySystem>(ref this));
            PhysXInterop.PxSceneQuerySystemBase_setUpdateMode(@this, updateMode);
        }

        /// <inheritdoc cref="PhysXInterop.PxSceneQuerySystemBase_getUpdateMode()" />
        public PxSceneQueryUpdateMode getUpdateMode()
        {
            PxSceneQuerySystemBase* @this = (PxSceneQuerySystemBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxSceneQuerySystem>(ref this));
            return PhysXInterop.PxSceneQuerySystemBase_getUpdateMode(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxSceneQuerySystemBase_getStaticTimestamp()" />
        public uint getStaticTimestamp()
        {
            PxSceneQuerySystemBase* @this = (PxSceneQuerySystemBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxSceneQuerySystem>(ref this));
            return PhysXInterop.PxSceneQuerySystemBase_getStaticTimestamp(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxSceneQuerySystemBase_flushUpdates()" />
        public void flushUpdates()
        {
            PxSceneQuerySystemBase* @this = (PxSceneQuerySystemBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxSceneQuerySystem>(ref this));
            PhysXInterop.PxSceneQuerySystemBase_flushUpdates(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxSceneQuerySystemBase_raycast(PxVec3*, PxVec3*, float, PxRaycastCallback*, PxHitFlag, PxQueryFilterData*, PxQueryFilterCallback*, PxQueryCache*, PxGeometryQueryFlag, )" />
        public bool raycast(PxVec3* origin, PxVec3* unitDir, float distance, PxRaycastCallback* hitCall, PxHitFlag hitFlags = PxHitFlag.eDEFAULT, PxQueryFilterData* filterData = null, PxQueryFilterCallback* filterCall = null, PxQueryCache* cache = null, PxGeometryQueryFlag queryFlags = PxGeometryQueryFlag.eSIMD_GUARD)
        {
            PxSceneQuerySystemBase* @this = (PxSceneQuerySystemBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxSceneQuerySystem>(ref this));
            return PhysXInterop.PxSceneQuerySystemBase_raycast(@this, origin, unitDir, distance, hitCall, hitFlags, filterData, filterCall, cache, queryFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxSceneQuerySystemBase_sweep(PxGeometry*, PxTransform*, PxVec3*, float, PxSweepCallback*, PxHitFlag, PxQueryFilterData*, PxQueryFilterCallback*, PxQueryCache*, float, PxGeometryQueryFlag, )" />
        public bool sweep(PxGeometry* geometry, PxTransform* pose, PxVec3* unitDir, float distance, PxSweepCallback* hitCall, PxHitFlag hitFlags = PxHitFlag.eDEFAULT, PxQueryFilterData* filterData = null, PxQueryFilterCallback* filterCall = null, PxQueryCache* cache = null, float inflation = 0f, PxGeometryQueryFlag queryFlags = PxGeometryQueryFlag.eSIMD_GUARD)
        {
            PxSceneQuerySystemBase* @this = (PxSceneQuerySystemBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxSceneQuerySystem>(ref this));
            return PhysXInterop.PxSceneQuerySystemBase_sweep(@this, geometry, pose, unitDir, distance, hitCall, hitFlags, filterData, filterCall, cache, inflation, queryFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxSceneQuerySystemBase_overlap(PxGeometry*PxTransform*PxOverlapCallback*PxQueryFilterData*PxQueryFilterCallback*PxQueryCache*PxGeometryQueryFlag)" />
        public bool overlap(PxGeometry* geometry, PxTransform* pose, PxOverlapCallback* hitCall, PxQueryFilterData* filterData = null, PxQueryFilterCallback* filterCall = null, PxQueryCache* cache = null, PxGeometryQueryFlag queryFlags = PxGeometryQueryFlag.eSIMD_GUARD)
        {
            PxSceneQuerySystemBase* @this = (PxSceneQuerySystemBase*)Unsafe.AsPointer(ref Unsafe.AsRef<PxSceneQuerySystem>(ref this));
            return PhysXInterop.PxSceneQuerySystemBase_overlap(@this, geometry, pose, hitCall, filterData, filterCall, cache, queryFlags);
        }
    }
}
