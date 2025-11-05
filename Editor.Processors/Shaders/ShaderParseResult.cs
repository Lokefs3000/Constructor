using RHI = Primary.RHI;

namespace Editor.Processors.Shaders
{
    internal class ShaderParseResult
    {
        public readonly List<ShaderVariable> Variables = new List<ShaderVariable>();
        public readonly List<ShaderInputLayout> InputLayout = new List<ShaderInputLayout>();
        public readonly List<ImmutableSampler> ImmutableSamplers = new List<ImmutableSampler>();
        public readonly List<ShaderFunction> Functions = new List<ShaderFunction>();
        public readonly Dictionary<string, ShaderStruct> Structs = new Dictionary<string, ShaderStruct>();
        public uint ConstantsSize = 0;

        public readonly SortedSet<string> BindGroups = new SortedSet<string>(new Comparer()) { "__Default" };

        public string? Path = null;

        public string? EntryPointVertex = null;
        public string? EntryPointPixel = null;

        public string OutputSource = string.Empty;

        private struct Comparer : IComparer<string>
        {
            public int Compare(string? x, string? y)
            {
                return string.Compare(x, y, StringComparison.InvariantCulture);
            }
        }
    }

    internal struct ShaderVariable
    {
        public ShaderVariableType Type;
        public string Signature;
        public string? VariableName;
        public string Name;
        public string BindGroup;
        public int Index;
        public ShaderAttribute[] Attributes;

        public bool IsConstants
        {
            get
            {
                if (Attributes.Length == 0)
                    return false;
                if (Type != ShaderVariableType.ConstantBuffer)
                    return false;

                return Array.Exists(Attributes, (x) => x.Type == ShaderAttributeType.Constants);
            }
        }
    }

    internal enum ShaderVariableType : byte
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

    internal struct ShaderAttribute
    {
        public ShaderAttributeType Type;
        public object? Value;
    }

    internal enum ShaderAttributeType : byte
    {
        Constants = 0,
        Property,
    }

    internal struct ShaderAttribProperty
    {
        public string Name;
        public ShaderPropertyDefault Default;
    }

    internal enum ShaderPropertyDefault : byte
    {
        White = 0,
        Normal,
        Mask
    }

    internal struct ShaderInputLayout
    {
        public string Name;
        public int? Offset;
        public int? Slot;
        public RHI.InputClassification? Class;
        public RHI.InputElementFormat? Format;
    }

    internal struct ImmutableSampler
    {
        public int Index;
        public string Name;

        public RHI.TextureFilter Filter;
        public RHI.TextureAddressMode AddressModeU;
        public RHI.TextureAddressMode AddressModeV;
        public RHI.TextureAddressMode AddressModeW;
        public uint MaxAnistropy;
        public float MipLODBias;
        public float MinLOD;
        public float MaxLOD;
        public RHI.SamplerBorder Border;
    }

    internal struct ShaderFunction
    {
        public string Name;
        public int BodyBegin;
        public int BodyEnd;
    }

    internal struct ShaderStruct
    {
        public string Name;
        public int DefBegin;
        public int DefEnd;
        public ShaderStructVariable[] Variables;
    }

    internal struct ShaderStructVariable
    {
        public ShaderStructVariableType Type;
        public byte Rows;
        public byte Columns;
        public string Name;
    }

    internal enum ShaderStructVariableType
    {
        Struct = 0,

        Bool,
        Int,
        UInt,
        DWord,
        Half,
        Float,
        Double,
        UInt64,
        Int64,
        Float16,
        UInt16,
        Int16,
        Matrix
    }
}
