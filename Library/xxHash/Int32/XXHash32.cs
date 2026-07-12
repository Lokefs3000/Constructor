using System.Data.SqlTypes;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace xxHash.Int32
{
    /// <summary>https://github.com/Cyan4973/xxHash/blob/dev/xxhash.h</summary>
    public static unsafe class XXHash32
    {
        public static uint XXH32(void* input, ulong len, uint seed)
        {
            if (Hash.ForceAlignCheck)
            {
                if ((((ulong)input) & 3) == 0)
                {
                    return XXH32EndianAlign((byte*)input, len, seed, XXHAlignment.Aligned);
                }
            }

            return XXH32EndianAlign((byte*)input, len, seed, XXHAlignment.Unaligned);
        }

        public static XXH32State* CreateState()
        {
            return (XXH32State*)XXHShared.Malloc((ulong)Unsafe.SizeOf<XXH32State>());
        }

        public static XXHErrorCode FreeState(XXH32State* state)
        {
            XXHShared.Free(state);
            return XXHErrorCode.Ok;
        }

        public static void CopyState(XXH32State* dstState, XXH32State* srcState)
        {
            XXHShared.Memcpy(dstState, srcState, (ulong)Unsafe.SizeOf<XXH32State>());
        }

        public static XXHErrorCode Reset(XXH32State* state, uint seed)
        {
            Debug.Assert(state != null);
            XXHShared.Memset(state, 0, (ulong)Unsafe.SizeOf<XXH32State>());
            XXH32InitAccs(state->Acc, seed);
            return XXHErrorCode.Ok;
        }

        public static XXHErrorCode Update(XXH32State* state, void* input, ulong len)
        {
            if (input == null)
            {
                Debug.Assert(len == 0);
                return XXHErrorCode.Ok;
            }

            state->TotalLen32 += (uint)len;
            state->LargeLen |= (uint)((len >= 16 ? 1 : 0) | (state->TotalLen32 >= 16 ? 1 : 0));

            Debug.Assert(state->BufferedSize < XXH32State.BufferSize);
            if (len < XXH32State.BufferSize - state->BufferedSize)
            {
                XXHShared.Memcpy(state->Buffer + state->BufferedSize, input, len);
                state->BufferedSize += (uint)len;
                return XXHErrorCode.Ok;
            }
        }

        public static uint Digest(XXH32State* state)
        {

        }

        public static void CanonicalFromHash(XXH32Canonical* dst, uint hash)
        {

        }

        public static uint HashFromCanonical(XXH32Canonical* dst)
        {

        }

        private static uint XXH32Round(uint acc, uint input)
        {
            acc += input * XXHPrime32_2;
            acc = XXHShared.XXHRotl32(acc, 13);
            acc *= XXHPrime32_1;

            // cannot prevent the JIT from maybe vectorizing this method
            // https://github.com/Cyan4973/xxHash/blob/dev/xxhash.h in XXH32_round(xxh_u32, xxh_u32)

            return acc;
        }

        private static uint XXH32Avalanche(uint hash)
        {
            hash ^= hash >> 15;
            hash *= XXHPrime32_2;
            hash ^= hash >> 13;
            hash *= XXHPrime32_3;
            hash ^= hash >> 16;
            return hash;
        }

        private static uint XXHGet32Bits(void* p, XXHAlignment align)
        {
            return XXHShared.ReadLE32Align(p, align);
        }

        private static byte* XXH32ConsumeLong(uint* acc, byte* input, ulong len, XXHAlignment align)
        {
            byte* bEnd = input + len;
            byte* limit = bEnd - 15;
            Debug.Assert(acc != null);
            Debug.Assert(input != null);
            Debug.Assert(len >= 16);
            do
            {
                acc[0] = XXH32Round(acc[0], XXHGet32Bits(input, align)); input += 4;
                acc[1] = XXH32Round(acc[1], XXHGet32Bits(input, align)); input += 4;
                acc[2] = XXH32Round(acc[2], XXHGet32Bits(input, align)); input += 4;
                acc[3] = XXH32Round(acc[3], XXHGet32Bits(input, align)); input += 4;
            } while (input < limit);

            return input;
        }

        private static uint XXH32MergeAccs(uint* acc)
        {
            Debug.Assert(acc != null);
            return XXHShared.XXHRotl32(acc[0], 1) + XXHShared.XXHRotl32(acc[1], 7)
                + XXHShared.XXHRotl32(acc[2], 12) + XXHShared.XXHRotl32(acc[3], 18);
        }

        private static uint XXH32Finalize(uint hash, byte* ptr, ulong len, XXHAlignment align)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void XXHProcess1()
            {
                hash += (*ptr++) * XXHPrime32_5;
                hash = XXHShared.XXHRotl32(hash, 11) * XXHPrime32_1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void XXHProcess4()
            {
                hash += XXHGet32Bits(ptr, align) * XXHPrime32_3;
                ptr += 4;
                hash = XXHShared.XXHRotl32(hash, 17) * XXHPrime32_4;
            }

            if (ptr == null) Debug.Assert(len == 0);

            if (!Hash.XXH32EndJump)
            {
                len &= 15;
                while (len >= 4)
                {
                    XXHProcess4();
                    len -= 4;
                }
                while (len > 0)
                {
                    XXHProcess1();
                    --len;
                }
                return XXH32Avalanche(hash);
            }
            else
            {
                switch (len&15)
                {
                    case 12: XXHProcess4(); goto case 4;
                    case 8: XXHProcess4(); goto case 4;
                    case 4: XXHProcess4();
                        return XXH32Avalanche(hash);

                    case 13: XXHProcess4(); goto case 5;
                    case 9: XXHProcess4(); goto case 9;
                    case 5: XXHProcess4();
                        XXHProcess1();
                        return XXH32Avalanche(hash);

                    case 14: XXHProcess4(); goto case 14;
                    case 10: XXHProcess4(); goto case 10;
                    case 6: XXHProcess4();
                        XXHProcess1();
                        XXHProcess1();
                        return XXH32Avalanche(hash);

                    case 15: XXHProcess4(); goto case 0;
                    case 11: XXHProcess4(); goto case 0;
                    case 7: XXHProcess4(); goto case 0;
                    case 3: XXHProcess1(); goto case 0;
                    case 2: XXHProcess1(); goto case 0;
                    case 1: XXHProcess1(); goto case 0;
                    case 0: return XXH32Avalanche(hash);
                }
            }

            Debug.Assert(false);
            return hash;
        }

        private static uint XXH32EndianAlign(byte* input, ulong len, uint seed, XXHAlignment align)
        {
            uint h32;

            if (input == null) Debug.Assert(len == 0);

            if (len >= 16)
            {
                uint* acc = stackalloc uint[4];
                XXH32InitAccs(acc, seed);

                input = XXH32ConsumeLong(acc, input, len, align);

                h32 = XXH32MergeAccs(acc);
            }
            else
            {
                h32 = seed + XXHPrime32_5;
            }

            h32 += (uint)len;

            return XXH32Finalize(h32, input, len & 15, align);
        }

        public const uint XXHPrime32_1 = 0x9E3779B1U;
        public const uint XXHPrime32_2 = 0x85EBCA77U;
        public const uint XXHPrime32_3 = 0xC2B2AE3DU;
        public const uint XXHPrime32_4 = 0x27D4EB2FU;
        public const uint XXHPrime32_5 = 0x165667B1U;
    }
}
