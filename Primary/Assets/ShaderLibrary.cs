using CommunityToolkit.HighPerformance;
using Primary.Rendering;
using Primary.Utility;
using Serilog;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Primary.Assets
{
    public sealed class ShaderLibrary : IDisposable
    {
        private List<IShaderSubLibrary> _packages;
        private bool disposedValue;

        internal ShaderLibrary()
        {
            _packages = new List<IShaderSubLibrary>();
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    for (int i = 0; i < _packages.Count; i++)
                    {
                        _packages[i].Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void AddSubLibrary(IShaderSubLibrary library)
        {
            _packages.Add(library);
        }

        public void RemoveSubLibrary(IShaderSubLibrary library)
        {
            _packages.Remove(library);
        }

        public bool ReadGraphicsShader(string path, out RHI.GraphicsPipelineDescription desc, out RHI.GraphicsPipelineBytecode bytecode, out ShaderVariable[] variables)
        {
            Unsafe.SkipInit(out desc);
            Unsafe.SkipInit(out bytecode);
            Unsafe.SkipInit(out variables);

            byte[]? raw = null;

            for (int i = 0; i < _packages.Count; i++)
            {
                raw = _packages[i].ReadFromLibrary(path);
                if (raw != null)
                    break;
            }

            if (raw == null)
            {
                Log.Error("Failed to find graphics shader at path: \"{path}\"!", path);
                return false;
            }

            using (Stream stream = raw.AsMemory().AsStream())
            {
                using BinaryReader br = new BinaryReader(stream);

                if (br.ReadUInt32() != HeaderId)
                {
                    Log.Error("[g:{path}]: Invalid header id present!", path);
                    return false;
                }

                if (br.ReadUInt32() != HeaderVersion)
                {
                    Log.Error("[g:{path}]: Invalid header version present!", path);
                    return false;
                }

                string internalFilePath = br.ReadString();
                if (internalFilePath != path)
                {
                    Log.Error("[g:{path}]: Different shader paths present!", path);
                    return false;
                }

                {
                    desc = new RHI.GraphicsPipelineDescription
                    {
                        FillMode = (RHI.FillMode)br.ReadByte(),
                        CullMode = (RHI.CullMode)br.ReadByte(),
                        FrontCounterClockwise = br.ReadBoolean(),
                        DepthBias = br.ReadInt32(),
                        DepthBiasClamp = br.ReadSingle(),
                        SlopeScaledDepthBias = br.ReadSingle(),
                        DepthClipEnable = br.ReadBoolean(),
                        ConservativeRaster = br.ReadBoolean(),
                        DepthEnable = br.ReadBoolean(),
                        DepthWriteMask = (RHI.DepthWriteMask)br.ReadByte(),
                        DepthFunc = (RHI.ComparisonFunc)br.ReadByte(),
                        StencilEnable = br.ReadBoolean(),
                        StencilReadMask = br.ReadByte(),
                        StencilWriteMask = br.ReadByte(),
                        PrimitiveTopology = (RHI.PrimitiveTopologyType)br.ReadByte(),
                        FrontFace = new RHI.StencilFace
                        {
                            StencilFailOp = (RHI.StencilOp)br.ReadByte(),
                            StencilDepthFailOp = (RHI.StencilOp)br.ReadByte(),
                            StencilPassOp = (RHI.StencilOp)br.ReadByte(),
                            StencilFunc = (RHI.ComparisonFunc)br.ReadByte()
                        },
                        BackFace = new RHI.StencilFace
                        {
                            StencilFailOp = (RHI.StencilOp)br.ReadByte(),
                            StencilDepthFailOp = (RHI.StencilOp)br.ReadByte(),
                            StencilPassOp = (RHI.StencilOp)br.ReadByte(),
                            StencilFunc = (RHI.ComparisonFunc)br.ReadByte()
                        },
                        AlphaToCoverageEnable = br.ReadBoolean(),
                        IndependentBlendEnable = br.ReadBoolean(),
                        LogicOpEnable = br.ReadBoolean(),
                        LogicOp = (RHI.LogicOp)br.ReadByte(),
                        ExpectedConstantsSize = br.ReadByte()
                    };

                    {
                        int blendsCount = Math.Min((int)br.ReadByte(), 8);
                        if (blendsCount == 0)
                        {
                            desc.Blends = Array.Empty<RHI.BlendDescription>();
                        }
                        else
                        {
                            desc.Blends = new RHI.BlendDescription[blendsCount];
                            for (int i = 0; i < blendsCount; i++)
                            {
                                desc.Blends[i] = br.Read<RHI.BlendDescription>();
                            }
                        }
                    }

                    {
                        int inputElementsCount = br.ReadByte();
                        if (inputElementsCount == 0)
                        {
                            desc.InputElements = Array.Empty<RHI.InputElementDescription>();
                        }
                        else
                        {
                            desc.InputElements = new RHI.InputElementDescription[inputElementsCount];
                            for (int i = 0; i < inputElementsCount; i++)
                            {
                                desc.InputElements[i] = br.Read<RHI.InputElementDescription>();
                            }
                        }
                    }

                    {
                        int boundResourcesCount = br.ReadByte();
                        if (boundResourcesCount == 0)
                        {
                            desc.BoundResources = Array.Empty<RHI.BoundResourceDescription>();
                        }
                        else
                        {
                            desc.BoundResources = new RHI.BoundResourceDescription[boundResourcesCount];
                            for (int i = 0; i < boundResourcesCount; i++)
                            {
                                desc.BoundResources[i] = br.Read<RHI.BoundResourceDescription>();
                            }
                        }
                    }

                    {
                        int immutableSamplerCount = br.ReadByte();
                        if (immutableSamplerCount == 0)
                        {
                            desc.ImmutableSamplers = Array.Empty<KeyValuePair<uint, RHI.ImmutableSamplerDescription>>();
                        }
                        else
                        {
                            desc.ImmutableSamplers = new KeyValuePair<uint, RHI.ImmutableSamplerDescription>[immutableSamplerCount];
                            for (int i = 0; i < immutableSamplerCount; i++)
                            {
                                int index = br.ReadInt32();
                                string name = br.ReadString();

                                desc.ImmutableSamplers[i] = new KeyValuePair<uint, RHI.ImmutableSamplerDescription>((uint)index, br.Read<RHI.ImmutableSamplerDescription>());
                            }
                        }

                        //temp
                        for (int i = 0; i < desc.ImmutableSamplers.Length; i++)
                        {
                            var kvp = desc.ImmutableSamplers[i].Value;
                            kvp.MaxAnistropy = 16;

                            desc.ImmutableSamplers[i] = new KeyValuePair<uint, RHI.ImmutableSamplerDescription>(desc.ImmutableSamplers[i].Key, kvp);
                        }
                    }

                    {
                        int shaderResourcesCount = br.ReadByte();
                        if (shaderResourcesCount == 0)
                        {
                            variables = Array.Empty<ShaderVariable>();
                        }
                        else
                        {
                            variables = new ShaderVariable[shaderResourcesCount];
                            for (int i = 0; i < shaderResourcesCount; i++)
                            {
                                ShaderVariable v = new ShaderVariable
                                {
                                    Type = br.Read<ShaderVariableType>(),
                                    Name = br.ReadString(),
                                    BindGroup = br.ReadString(),
                                    Index = br.ReadByte()
                                };

                                byte attributes = br.ReadByte();
                                v.Attributes = attributes > 0 ? new ShaderVariableAttribute[attributes] : Array.Empty<ShaderVariableAttribute>();

                                for (int j = 0; j < attributes; j++)
                                {
                                    ShaderVariableAttribute attrib = new ShaderVariableAttribute
                                    {
                                        Type = (ShaderAttributeType)br.ReadByte(),
                                        Value = null
                                    };

                                    if (attrib.Type == ShaderAttributeType.Property)
                                    {
                                        attrib.Value = new ShaderVariableAttribProperty
                                        {
                                            Name = br.ReadString(),
                                        };
                                    }

                                    v.Attributes[j] = attrib;
                                }

                                variables[i] = v;
                            }
                        }
                    }
                }

                ShaderAPITargets apiTargets = (ShaderAPITargets)br.ReadByte();
                ShaderAPITargets currTarget = ShaderAPITargets.None;

                switch (RenderingManager.Device.API)
                {
                    case RHI.GraphicsAPI.None:
                        {
                            Log.Error("[g:{path}]: Graphics API does not support graphics shaders: {api}", path, RenderingManager.Device.API);
                            return false;
                        }
                    case RHI.GraphicsAPI.Vulkan:
                        {
                            if (!apiTargets.HasFlag(ShaderAPITargets.Vulkan))
                            {
                                Log.Error("[g:{path}]: Shader does not have target for API: {api}", path, RenderingManager.Device.API);
                                return false;
                            }

                            currTarget = ShaderAPITargets.Vulkan;
                            break;
                        }
                    case RHI.GraphicsAPI.Direct3D12:
                        {
                            if (!apiTargets.HasFlag(ShaderAPITargets.Direct3D12))
                            {
                                Log.Error("[g:{path}]: Shader does not have target for API: {api}", path, RenderingManager.Device.API);
                                return false;
                            }

                            currTarget = ShaderAPITargets.Direct3D12;
                            break;
                        }
                    default:
                        {
                            Log.Error("[g:{path}]: Unknown graphics API: {api}", path, RenderingManager.Device.API);
                            return false;
                        }
                }

                ShaderBytecodeTargets bytecodeTargets = (ShaderBytecodeTargets)br.ReadByte();

                int apiTargetCount = BitOperations.PopCount((uint)apiTargets);
                int bytecodeTargetCount = BitOperations.PopCount((uint)bytecodeTargets);

                bytecode = new RHI.GraphicsPipelineBytecode();
                for (int i = 0; i < apiTargetCount; i++)
                {
                    ShaderAPITargets target = (ShaderAPITargets)br.ReadByte();
                    if (target == currTarget)
                    {
                        if (bytecodeTargets.HasFlag(ShaderBytecodeTargets.Vertex))
                            bytecode.Vertex = br.ReadBytes((int)br.ReadUInt32());
                        if (bytecodeTargets.HasFlag(ShaderBytecodeTargets.Pixel))
                            bytecode.Pixel = br.ReadBytes((int)br.ReadUInt32());

                        return true;
                    }
                }
            }

            Log.Error("[g:{path}]: Failed to find shader bytecode chunk for API: {api}", path, Engine.GlobalSingleton.RenderingManager.GraphicsDevice.API);
            return false;
        }

        public const uint HeaderId = 0x204c4243;
        public const uint HeaderVersion = 0;

        private enum ShaderBytecodeTargets : byte
        {
            None = 0,
            Vertex = 1 << 0,
            Pixel = 1 << 1,
        }

        private enum ShaderAPITargets : byte
        {
            None = 0,
            Vulkan = 1 << 0,
            Direct3D12 = 1 << 1,
        }
    }

    public record struct ShaderVariable
    {
        public ShaderVariableType Type;
        public string Name;
        public string BindGroup;
        public byte Index;

        public ShaderVariableAttribute[] Attributes;
    }

    public enum ShaderVariableType : byte
    {
        ConstantBuffer = 0,
        StructuredBuffer,
        RWStructuredBuffer,
        Texture1D,
        Texture1DArray,
        Texture2D,
        Texture2DArray,
        Texture3D,
        TextureCube,
    }

    public record struct ShaderVariableAttribute
    {
        public ShaderAttributeType Type;
        public object? Value;
    }

    public enum ShaderAttributeType : byte
    {
        Constants = 0,
        Property
    }

    public record struct ShaderVariableAttribProperty
    {
        public string Name;
    }
}
