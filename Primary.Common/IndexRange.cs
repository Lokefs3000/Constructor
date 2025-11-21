using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Common
{
    public struct IndexRange
    {
        public int Start;
        public int End;

        public IndexRange(int start, int end)
        {
            Start = start;
            End = end;
        }
        
        public IndexRange(int length)
        {
            Start = 0;
            End = length;
        }
    }
}
