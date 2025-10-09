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
        public static readonly CVar<bool> DbgShowRegions = new CVar<bool>(true);
        public static readonly CVar<bool> DbgShowTrees = new CVar<bool>(true);
    }
}
