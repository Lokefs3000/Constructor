using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Primary.Rendering.Structures
{
    public record struct FGViewport(float TopLeftX, float TopLeftY, float Width, float Height, float MinDepth = 0.0f, float MaxDepth = 1.0f) : IEquatable<FGViewport>
    {
        public bool Equals(FGViewport other)
        {
            return Vector128.EqualsAll(AsVector128(), other.AsVector128()) && MinDepth == other.MinDepth && MaxDepth == other.MaxDepth;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private Vector128<float> AsVector128()
        {
            return Unsafe.ReadUnaligned<Vector128<float>>(ref Unsafe.As<FGViewport, byte>(ref this));
            //fixed (FGViewport* ptr = &viewport)
            //{
            //    return Vector128.Load((float*)ptr);
            //}
        }
    }
}
