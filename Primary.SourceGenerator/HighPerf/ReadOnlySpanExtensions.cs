using System;
using System.Runtime.CompilerServices;
#if NETSTANDARD
using System.Runtime.InteropServices;
#endif
using CommunityToolkit.HighPerformance.Helpers.Internals;

namespace CommunityToolkit.HighPerformance;

public static class ReadOnlySpanExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T DangerousGetReference<T>(this ReadOnlySpan<T> span)
    {
#if NET6_0_OR_GREATER
        return ref Unsafe.AsRef(in text.GetPinnableReference());
#else
        return ref MemoryMarshal.GetReference(span);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T DangerousGetReferenceAt<T>(this ReadOnlySpan<T> span, int i)
    {
#if NET6_0_OR_GREATER
        ref char r0 = ref Unsafe.AsRef(in text.GetPinnableReference());
#else
        ref T r0 = ref MemoryMarshal.GetReference(span);
#endif
        ref T ri = ref Unsafe.Add(ref r0, (nint)(uint)i);

        return ref ri;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetDjb2HashCode<T>(this ReadOnlySpan<T> span) where T : notnull
    {
        ref T r0 = ref span.DangerousGetReference();
        nint length = (nint)(uint)span.Length;

        return SpanHelper.GetDjb2HashCode(ref r0, length);
    }
}