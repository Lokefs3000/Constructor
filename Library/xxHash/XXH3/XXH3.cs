using System;
using System.Collections.Generic;
using System.Text;

namespace xxHash.XXH3
{
    public static unsafe class XXH3
    {
        public static XXH3State* CreateState()
        {

        }

        public static XXHErrorCode FreeState(XXH3State* state)
        {

        }

        public static void CopyState(XXH3State* dstState, XXH3State* srcState)
        {

        }

        #region 64-bit
        public static ulong Hash64(void* input, ulong length)
        {

        }

        public static ulong HashWithSeed64(void* input, ulong length, ulong seed)
        {

        }

        public static ulong HashWithSecret64(void* data, ulong length, void* secret, ulong secretSize)
        {

        }

        public static XXHErrorCode Reset64(XXH3State* state)
        {

        }

        public static XXHErrorCode ResetWithSeed64(XXH3State* state, ulong seed)
        {

        }

        public static XXHErrorCode ResetWithSecret64(XXH3State* state, void* secret, ulong secretSize)
        {

        }

        public static XXHErrorCode Update64(XXH3State* state, void* input, ulong length)
        {

        }

        public static ulong Digest64(XXH3State* state)
        {

        }

        public static ulong HashWithSecretAndSeed64(void* data, ulong len, void* secret, ulong secretSize, ulong seed)
        {

        }

        public static XXHErrorCode ResetWithSecretAndSeed64(XXH3State* state, void* secret, ulong secretSize, ulong seed64)
        {

        }
        #endregion
        #region 128-bit
        public static XXH128Hash Hash128(void* input, ulong length)
        {

        }

        public static XXH128Hash HashWithSeed128(void* input, ulong length, ulong seed)
        {

        }

        public static XXH128Hash HashWithSecret128(void* data, ulong length, void* secret, ulong secretSize)
        {

        }

        public static XXHErrorCode Reset128(XXH3State* state)
        {

        }

        public static XXHErrorCode ResetWithSeed128(XXH3State* state, ulong seed)
        {

        }

        public static XXHErrorCode ResetWithSecret128(XXH3State* state, void* secret, ulong secretSize)
        {

        }

        public static XXHErrorCode Update128(XXH3State* state, void* input, ulong length)
        {

        }

        public static XXH128Hash Digest64(XXH3State* state)
        {

        }

        public static int IsEqual128(XXH128Hash h1, XXH128Hash h2)
        {

        }

        public static int Compare128(void* h128_1, void* h128_2)
        {

        }

        public static void CanonicalFromHash128(XXH128Canonical* dst, XXH128Hash hash)
        {

        }

        public static XXH128Hash HashFromCanonical128(XXH128Canonical* src)
        {

        }

        public static XXH128Hash XXH128(void* data, ulong len, ulong seed)
        {

        }

        public static XXH128Hash HashWithSecretAndSeed128(void* data, ulong len, void* secret, ulong secretSize, ulong seed)
        {

        }

        public static XXHErrorCode ResetWithSecretAndSeed128(XXH3State* state, void* secret, ulong secretSize, ulong seed64)
        {

        }
        #endregion

        public static XXHErrorCode GenerateSecret(void* secretBuffer, ulong secretSize, ulong customSeed, ulong customSeedSize)
        {

        }

        public static void GenerateSecretFromSeed(void* secretBuffer, ulong seed)
        {

        }

        public static void InitState(ref XXH3State state)
        {
            state.Seed = 0;
            state.ExtSecret = null;
        }

        public const int InternalBufferSize = 256;
        public const int SecretDefaultSize = 192;
        public const int MidSizeMax = 240;
    }
}
