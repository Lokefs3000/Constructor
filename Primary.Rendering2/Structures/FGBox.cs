using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Structures
{
    public readonly record struct FGBox(int X, int Y, int Z, int Width, int Height, int Depth)
    {
    }
}
