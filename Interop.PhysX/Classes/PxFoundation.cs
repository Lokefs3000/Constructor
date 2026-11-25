using System;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxFoundation
    {
        /// <inheritdoc cref="PhysXInterop.PxFoundation_release()" />
        public void release()
        {
            PxFoundation* @this = (PxFoundation*)Unsafe.AsPointer(ref Unsafe.AsRef<PxFoundation>(ref this));
            PhysXInterop.PxFoundation_release(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxFoundation_getErrorCallback()" />
        public PxErrorCallback* getErrorCallback()
        {
            PxFoundation* @this = (PxFoundation*)Unsafe.AsPointer(ref Unsafe.AsRef<PxFoundation>(ref this));
            return PhysXInterop.PxFoundation_getErrorCallback(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxFoundation_setErrorLevel(PxErrorCode)" />
        public void setErrorLevel(PxErrorCode mask = PxErrorCode.eMASK_ALL)
        {
            PxFoundation* @this = (PxFoundation*)Unsafe.AsPointer(ref Unsafe.AsRef<PxFoundation>(ref this));
            PhysXInterop.PxFoundation_setErrorLevel(@this, mask);
        }

        /// <inheritdoc cref="PhysXInterop.PxFoundation_getErrorLevel()" />
        public PxErrorCode getErrorLevel()
        {
            PxFoundation* @this = (PxFoundation*)Unsafe.AsPointer(ref Unsafe.AsRef<PxFoundation>(ref this));
            return PhysXInterop.PxFoundation_getErrorLevel(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxFoundation_getAllocatorCallback()" />
        public PxAllocatorCallback* getAllocatorCallback()
        {
            PxFoundation* @this = (PxFoundation*)Unsafe.AsPointer(ref Unsafe.AsRef<PxFoundation>(ref this));
            return PhysXInterop.PxFoundation_getAllocatorCallback(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxFoundation_getReportAllocationNames()" />
        public bool getReportAllocationNames()
        {
            PxFoundation* @this = (PxFoundation*)Unsafe.AsPointer(ref Unsafe.AsRef<PxFoundation>(ref this));
            return PhysXInterop.PxFoundation_getReportAllocationNames(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxFoundation_setReportAllocationNames(bool)" />
        public void setReportAllocationNames(bool value)
        {
            PxFoundation* @this = (PxFoundation*)Unsafe.AsPointer(ref Unsafe.AsRef<PxFoundation>(ref this));
            PhysXInterop.PxFoundation_setReportAllocationNames(@this, value);
        }

        /// <inheritdoc cref="PhysXInterop.PxFoundation_registerAllocationListener(PxAllocationListener*)" />
        public void registerAllocationListener(PxAllocationListener* listener)
        {
            PxFoundation* @this = (PxFoundation*)Unsafe.AsPointer(ref Unsafe.AsRef<PxFoundation>(ref this));
            PhysXInterop.PxFoundation_registerAllocationListener(@this, listener);
        }

        /// <inheritdoc cref="PhysXInterop.PxFoundation_deregisterAllocationListener(PxAllocationListener*)" />
        public void deregisterAllocationListener(PxAllocationListener* listener)
        {
            PxFoundation* @this = (PxFoundation*)Unsafe.AsPointer(ref Unsafe.AsRef<PxFoundation>(ref this));
            PhysXInterop.PxFoundation_deregisterAllocationListener(@this, listener);
        }

        /// <inheritdoc cref="PhysXInterop.PxFoundation_registerErrorCallback(PxErrorCallback*)" />
        public void registerErrorCallback(PxErrorCallback* callback)
        {
            PxFoundation* @this = (PxFoundation*)Unsafe.AsPointer(ref Unsafe.AsRef<PxFoundation>(ref this));
            PhysXInterop.PxFoundation_registerErrorCallback(@this, callback);
        }

        /// <inheritdoc cref="PhysXInterop.PxFoundation_deregisterErrorCallback(PxErrorCallback*)" />
        public void deregisterErrorCallback(PxErrorCallback* callback)
        {
            PxFoundation* @this = (PxFoundation*)Unsafe.AsPointer(ref Unsafe.AsRef<PxFoundation>(ref this));
            PhysXInterop.PxFoundation_deregisterErrorCallback(@this, callback);
        }
    }
}
