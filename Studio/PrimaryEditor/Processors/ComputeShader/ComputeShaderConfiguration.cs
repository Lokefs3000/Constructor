using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using PrimaryEditor.Assets.Utility;
using Tomlyn.Serialization;

namespace PrimaryEditor.Processors.ComputeShader
{
    public sealed class ComputeShaderConfiguration
    {
        [TomlIgnore]
        public AssetId DefaultId { get; set; } = AssetId.Invalid;
        [TomlIgnore]
        public IAssetIdProvider? IdProvider { get; set; } = null;

        [TomlIgnore]
        public string[] IncludeDirectories { get; set; } = [];
    }
}
