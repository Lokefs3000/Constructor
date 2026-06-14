using Primary.Memory.Native;
using Primary.RHI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TerraFX.Interop.Windows;

namespace Primary.RHI
{
    public static partial class Extensions
    {
        /// <inheritdoc cref="RHIDevice.CreateBuffer(in RHIBufferDescription, ArrayPtr{byte}, string?)"/>
        public static unsafe RHIBuffer? CreateBuffer<T>(this RHIDevice self, in RHIBufferDescription description, Span<T> rawData, [CallerMemberName] string? debugName = "") where T : unmanaged
        {
            fixed (T* ptr = rawData)
            {
                return self.CreateBuffer(description, new ArrayPtr<byte>((byte*)ptr, rawData.Length * Unsafe.SizeOf<T>()), debugName);
            }
        }

        /// <inheritdoc cref="RHIDevice.CreateTexture(in RHITextureDescription, Span{ArrayPtr{byte}}, string?)" />
        public static unsafe RHITexture? CreateTexture<T>(this RHIDevice self, in RHITextureDescription description, Span<T> planeSlice, [CallerMemberName] string? debugName = "") where T : unmanaged
        {
            fixed (T* ptr = planeSlice)
            {
                ArrayPtr<byte> arrayPtr = new ArrayPtr<byte>((byte*)ptr, planeSlice.Length * Unsafe.SizeOf<T>());
                return self.CreateTexture(description, new Span<ArrayPtr<byte>>(ref arrayPtr), debugName);
            }
        }
    }
}
