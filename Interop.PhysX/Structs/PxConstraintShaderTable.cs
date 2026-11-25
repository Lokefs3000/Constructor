using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxConstraintShaderTable
    {
        public PxConstraintSolverPrep* solverPrep;
        public PxConstraintVisualize* visualize;
        public PxConstraintFlag flag;
    }
}
