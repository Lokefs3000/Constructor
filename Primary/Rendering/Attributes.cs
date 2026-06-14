using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RenderPassSetupAttribute : Attribute
    {
        public RenderPassRunContext RunContext { init; get; } = RenderPassRunContext.PerCamera;
    }

    public enum RenderPassRunContext : byte
    {
        PerCamera,
        PerWindow
    }
}
