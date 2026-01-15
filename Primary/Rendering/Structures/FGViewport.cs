using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Primary.Rendering.Structures
{
    public record struct FGViewport(float TopLeftX, float TopLeftY, float Width, float Height, float MinDepth = 0.0f, float MaxDepth = 1.0f) : IEquatable<FGViewport>
    {
        public bool Equals(FGViewport other)
        {
            return Vector128.EqualsAll(AsVector128(ref this), AsVector128(ref other)) && MinDepth == other.MinDepth && MaxDepth == other.MaxDepth;
        }

        private static Vector128<float> AsVector128(ref FGViewport viewport)
        {
            Vector128<float> vector;
            Unsafe.SkipInit(out vector);
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<float>, byte>(ref vector), viewport);
            return vector;
        }
    }
}
