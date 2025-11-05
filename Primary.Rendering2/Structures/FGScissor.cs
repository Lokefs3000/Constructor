using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Structures
{
    public record struct FGRect(int Left, int Top, int Right, int Bottom)
    {
        public int Width { get => Right - Left; set => Right = Left + value; }
        public int Height { get => Bottom - Top; set => Bottom = Top + value; }

        public static bool Intersects(FGRect a, FGRect b)
        {
            return !Vector128.LessThanAny(
                Vector128.Create(a.Left, b.Left, b.Bottom, a.Bottom),
                Vector128.Create(b.Right, a.Right, a.Top, b.Top));
        }
    }
}
