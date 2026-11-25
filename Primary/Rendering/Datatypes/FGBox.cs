using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Mathematics;

namespace Primary.Rendering.Datatypes
{
    public struct FGBox
    {
        public int X1;
        public int Y1;
        public int Z1;

        public int X2;
        public int Y2;
        public int Z2;
    
        public FGBox(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            X1 = x1;
            Y1 = y1;
            Z1 = z1;
            X2 = x2;
            Y2 = y2;
            Z2 = z2;
        }

        public FGBox(int left, int top, int right, int bottom)
        {
            X1 = left;
            Y1 = top;
            Z1 = 0;
            X2 = right;
            Y2 = bottom;
            Z2 = 1;
        }

        public FGBox(Int3 min, Int3 max)
        {
            Unsafe.As<FGBox, Int3>(ref this) = min;
            Unsafe.Add(ref Unsafe.As<FGBox, Int3>(ref this), 1) = max;
        }

        public FGBox(FGBox box)
        {
            this = box;
        }

        public Int3 Size => new Int3(X2, Y2, Z2) - new Int3(X1, Y1, Z1);
    }
}
