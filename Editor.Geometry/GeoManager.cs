using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geometry
{
    public sealed class GeoManager
    {
        private static ILogger? s_logger;

        internal static ILogger? Logger => s_logger;
    }
}
