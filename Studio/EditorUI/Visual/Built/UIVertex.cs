using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using Primary.Common;

namespace EditorUI.Visual.Built
{
    public readonly record struct UIVertex(UIVector2 Position, UIVector2 UV, UIVector2 UV2, Color Tint, uint DataOffset);
    public readonly record struct UIVector2(float X, float Y)
    {
        public static UIVector2 Zero => new UIVector2(0.0f, 0.0f);
        public static UIVector2 UnitX => new UIVector2(1.0f, 0.0f);
        public static UIVector2 UnitY => new UIVector2(0.0f, 1.0f);
        public static UIVector2 One => new UIVector2(1.0f, 1.0f);

        public static implicit operator UIVector2(Vector2 v) => Unsafe.ReadUnaligned<UIVector2>(ref Unsafe.As<Vector2, byte>(ref v));
        public static implicit operator UIVector2(Vector64<float> v) => Unsafe.ReadUnaligned<UIVector2>(ref Unsafe.As<Vector64<float>, byte>(ref v));
    }
}
