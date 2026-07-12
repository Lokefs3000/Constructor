using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using PrimaryEditor.Assets.Utility;
using Tomlyn.Serialization;

namespace PrimaryEditor.Processors.Shader
{
    public sealed class ShaderConfiguration
    {
        [TomlIgnore]
        public AssetId DefaultId { get; set; } = AssetId.Invalid;
        [TomlIgnore]
        public IAssetIdProvider? IdProvider { get; set; } = null;

        [TomlIgnore]
        public string[] IncludeDirectories { get; set; } = [];

        [TomlRequired, TomlPropertyName("primitive_topology")]
        public SBCPrimitiveTopology TopologyType { get; set; } = default;
        [TomlRequired, TomlPropertyName("rasterizer")]
        public Rasterizer RasterizerInfo { get; set; } = default;
        [TomlRequired, TomlPropertyName("depth_stencil")]
        public DepthStencil DepthStencilInfo { get; set; } = default;
        [TomlRequired, TomlPropertyName("blend")]
        public Blend BlendInfo { get; set; } = default;

        public struct Rasterizer
        {
            public SBCFillMode FillMode { get; set; }
            public SBCCullMode CullMode { get; set; }
            public bool FrontCounterClockwise { get; set; }
            public int DepthBias { get; set; }
            public float DepthBiasClamp { get; set; }
            public float SlopeScaledDepthBias { get; set; }
            public bool DepthClipEnable { get; set; }
            public bool ConservativeRaster { get; set; }
        }

        public struct DepthStencil
        {
            public bool DepthEnable { get; set; }
            [TomlPropertyName("depth_write_mask")]
            public SBCDepthWriteMask WriteMask { get; set; }
            public SBCComparisonFunc DepthFunc { get; set; }
            public bool StencilEnable { get; set; }
            public byte StencilReadMask { get; set; }
            public byte StencilWriteMask { get; set; }
            public DepthStencilFace FrontFace { get; set; }
            public DepthStencilFace BackFace { get; set; }
        }

        public struct DepthStencilFace
        {
            public SBCStencilOp Fail { get; set; }
            public SBCStencilOp DepthFail { get; set; }
            public SBCStencilOp Pass { get; set; }
            public SBCComparisonFunc Func { get; set; }
        }

        public struct Blend
        {
            public bool AlphaToCoverageEnable { get; set; }
            public bool IndependentBlendEnable { get; set; }
            [TomlPropertyName("rtblends")]
            public RenderTargetBlend[] Blends { get; set; }
        }

        public struct RenderTargetBlend
        {
            public bool BlendEnable { get; set; }
            [TomlPropertyName("src_blend")]
            public SBCBlendSource Source { get; set; }
            [TomlPropertyName("dst_blend")]
            public SBCBlendSource Destination { get; set; }
            [TomlPropertyName("blend_op")]
            public SBCBlendOp Operation { get; set; }
            [TomlPropertyName("src_blend_alpha")]
            public SBCBlendSource SourceAlpha { get; set; }
            [TomlPropertyName("dst_blend_alpha")]
            public SBCBlendSource DestinationAlpha { get; set; }
            [TomlPropertyName("blend_op_alpha")]
            public SBCBlendOp OperationAlpha { get; set; }
            [TomlPropertyName("render_target_write_mask")]
            public byte WriteMask { get; set; }
        }
    }
}
