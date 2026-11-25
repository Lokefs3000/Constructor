using System;
using System.Collections.Generic;
using System.Text;
using Primary.Console;

namespace Primary.Physics
{
    [ConsoleName("phys")]
    public sealed class PhysicsConfig
    {
        [ConsoleName("shareStaticShapes")]
        public bool ShareStaticShapes = true;

        public static PhysicsConfig Instance => Engine.GlobalSingleton.PhysicsManager.Config;
    }
}
