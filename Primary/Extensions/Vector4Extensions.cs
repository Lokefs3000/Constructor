using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Primary.Extensions
{
    public static partial class Vector4Extensions
    {
        extension (Vector4 vector)
        {
            public Vector2 GetLower()
            {
                return Unsafe.As<Vector4, Vector2>(ref vector);
            }

            public Vector2 GetUpper()
            {
                return Unsafe.Add(ref Unsafe.As<Vector4, Vector2>(ref vector), 1);
            }
        }
    }
}
