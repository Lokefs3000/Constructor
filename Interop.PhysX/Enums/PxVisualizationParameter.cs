using System;

namespace Interop.PhysX
{
    public enum PxVisualizationParameter : int
    {
        eSCALE = 0,
        eWORLD_AXES = 1,
        eBODY_AXES = 2,
        eBODY_MASS_AXES = 3,
        eBODY_LIN_VELOCITY = 4,
        eBODY_ANG_VELOCITY = 5,
        eCONTACT_POINT = 6,
        eCONTACT_NORMAL = 7,
        eCONTACT_ERROR = 8,
        eCONTACT_IMPULSE = 9,
        eFRICTION_POINT = 10,
        eFRICTION_NORMAL = 11,
        eFRICTION_IMPULSE = 12,
        eACTOR_AXES = 13,
        eCOLLISION_AABBS = 14,
        eCOLLISION_SHAPES = 15,
        eCOLLISION_AXES = 16,
        eCOLLISION_COMPOUNDS = 17,
        eCOLLISION_FNORMALS = 18,
        eCOLLISION_EDGES = 19,
        eCOLLISION_STATIC = 20,
        eCOLLISION_DYNAMIC = 21,
        eJOINT_LOCAL_FRAMES = 22,
        eJOINT_LIMITS = 23,
        eCULL_BOX = 24,
        eMBP_REGIONS = 25,
        eSIMULATION_MESH = 26,
        eSDF = 27,
        eNUM_VALUES = 28,
        eFORCE_DWORD = 2147483647
    }
}
