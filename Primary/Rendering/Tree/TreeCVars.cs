using Primary.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering.Tree
{
    [CommandClassNamespace("rtree")]
    internal static class TreeCVars
    {
        [CVarRange(1, 4)]
        public static CVar<int> MaxWorkers = new CVar<int>(4);
    }
}
