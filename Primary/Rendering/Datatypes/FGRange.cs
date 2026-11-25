using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Datatypes
{
    public struct FGRange
    {
        public int Mininmum;
        public int Maximum;

        public FGRange(int min, int max)
        {
            Mininmum = min;
            Maximum = max;
        }

        public FGRange(int length)
        {
            Mininmum = 0;
            Maximum = length;
        }

        public FGRange(FGRange range)
        {
            this = range;
        }

        public int Length => Maximum - Mininmum;
    }
}
