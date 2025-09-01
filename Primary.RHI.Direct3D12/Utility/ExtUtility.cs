using SharpGen.Runtime;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace Primary.RHI.Direct3D12.Utility
{
    internal static class ExtUtility
    {
        public static Result QueryInterface<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this ComObject self, out T? value) where T : ComObject
        {
            Result r = self.QueryInterface(typeof(T).GetTypeInfo().GUID, out var parentPtr);
            value = MarshallingHelpers.FromPointer<T>(parentPtr);
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CpuDescriptorHandle NewOffseted(this CpuDescriptorHandle self, int offsetScaledByIncrementSize)
        {
            return new CpuDescriptorHandle(self, offsetScaledByIncrementSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CpuDescriptorHandle NewOffseted(this CpuDescriptorHandle self, int offsetInDescriptors, uint descriptorIncrementSize)
        {
            return new CpuDescriptorHandle(self, offsetInDescriptors, descriptorIncrementSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GpuDescriptorHandle NewOffseted(this GpuDescriptorHandle self, int offsetScaledByIncrementSize)
        {
            return new GpuDescriptorHandle(self, offsetScaledByIncrementSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GpuDescriptorHandle NewOffseted(this GpuDescriptorHandle self, int offsetInDescriptors, uint descriptorIncrementSize)
        {
            return new GpuDescriptorHandle(self, offsetInDescriptors, descriptorIncrementSize);
        }
    }
}
