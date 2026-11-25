using System;

namespace Interop.PhysX
{
    public enum PxGeometryType : int
    {
        eSPHERE = 0,
        ePLANE = 1,
        eCAPSULE = 2,
        eBOX = 3,
        eCONVEXCORE = 4,
        eCONVEXMESH = 5,
        ePARTICLESYSTEM = 6,
        eTETRAHEDRONMESH = 7,
        eTRIANGLEMESH = 8,
        eHEIGHTFIELD = 9,
        eCUSTOM = 10,
        eGEOMETRY_COUNT = 11
    }
}
