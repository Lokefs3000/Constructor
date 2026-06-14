using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.R2.ForwardPlus.Statistics
{
    public sealed class RenderPathStatistics
    {
        public DrawStatistics Draw;

        internal RenderPathStatistics()
        {
            Draw = new DrawStatistics();
        }

        internal void ClearTransientData()
        {
            Draw.ClearTransientData();
        }
    }
}
