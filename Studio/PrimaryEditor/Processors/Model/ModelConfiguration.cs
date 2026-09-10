using System;
using System.Collections.Generic;
using System.Text;
using Tomlyn.Serialization;

namespace PrimaryEditor.Processors.Model
{
    public sealed class ModelConfiguration
    {
        public ModelIndexStrideMode IndexStrideMode { get; set; } = ModelIndexStrideMode.Automatic;
        public ModelUVMask UVChannels { get; set; } = ModelUVMask.All;
    }

    public enum ModelIndexStrideMode : byte
    {
        Automatic = 0,
        HalfPrecision,
        FullPrecision
    }

    public enum ModelUVMask : byte
    {
        UV0 = 1 << 0,
        UV1 = 1 << 1,
        UV2 = 1 << 2,
        UV3 = 1 << 3,
        UV4 = 1 << 4,
        UV5 = 1 << 5,
        UV6 = 1 << 6,
        UV7 = 1 << 7,

        None = 0x00,
        All = 0xff
    }
}
