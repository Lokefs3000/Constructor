using CommunityToolkit.HighPerformance;
using Primary.Assets;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace Primary.Rendering
{
    public sealed class ShaderBindGroup
    {
        private readonly string _groupName;
        private readonly FrozenDictionary<int, ShaderBindGroupVariable> _variables;

        private readonly BindGroupResourceLocation[] _staticLocations;

        public ShaderBindGroup(Span<ShaderBindGroupVariable> variables, string? name)
        {
            name ??= "__Default";
            _groupName = name;

            Dictionary<int, ShaderBindGroupVariable> tempDict = new Dictionary<int, ShaderBindGroupVariable>();
            for (int i = 0; i < variables.Length; i++)
            {
                ref ShaderBindGroupVariable variable = ref variables[i];
                tempDict.Add(variable.Name.GetDjb2HashCode(), new ShaderBindGroupVariable
                {
                    Type = variable.Type,
                    Name = variable.Name,
                    BindIndex = (byte)i,
                    LocalIndex = (byte)i,
                });
            }

            _variables = tempDict.ToFrozenDictionary();

            _staticLocations = new BindGroupResourceLocation[variables.Length];
            Array.Fill(_staticLocations, new BindGroupResourceLocation(null, BindGroupResourceType.None, ushort.MaxValue));
        }

        public ShaderBindGroup(ShaderAsset asset, string? name) : this(asset.GetVariablesForBindGroup(name ?? "__Default"), name)
        {

        }

        public ShaderBindGroup(string? name, params ShaderBindGroupVariable[] variables)
        {
            Array.Sort(variables, new CompareResourceNames());

            name ??= "__Default";
            _groupName = name;

            Dictionary<int, ShaderBindGroupVariable> tempDict = new Dictionary<int, ShaderBindGroupVariable>();
            for (int i = 0; i < variables.Length; i++)
            {
                ref ShaderBindGroupVariable variable = ref variables[i];
                tempDict.Add(variable.Name.GetDjb2HashCode(), new ShaderBindGroupVariable
                {
                    Type = variable.Type,
                    Name = variable.Name,
                    BindIndex = (byte)i,
                    LocalIndex = (byte)i,
                });
            }

            _variables = tempDict.ToFrozenDictionary();

            _staticLocations = new BindGroupResourceLocation[variables.Length];
            Array.Fill(_staticLocations, new BindGroupResourceLocation(null, BindGroupResourceType.None, ushort.MaxValue));
        }

        private bool SetResource(string path, object? resource, BindGroupResourceType type)
        {
            ref readonly ShaderBindGroupVariable variable = ref _variables.GetValueRefOrNullRef(path.GetDjb2HashCode());
            if (Unsafe.IsNullRef(in variable))
            {
                return false;
            }

            //TODO: implement checking for correct type

            if (type == BindGroupResourceType.Pending)
            {
                if (resource is TextureAsset)
                    type = BindGroupResourceType.TextureAsset;
                else if (resource is RHI.Texture)
                    type = BindGroupResourceType.RHITexture;
                else if (resource is RHI.RenderTextureView)
                    type = BindGroupResourceType.RHIRenderTextureView;
                else if (resource is RHI.Buffer)
                    type = BindGroupResourceType.RHIBuffer;
                else
                    type = BindGroupResourceType.None;
            }

            _staticLocations[variable.LocalIndex] = new BindGroupResourceLocation(resource, type, variable.BindIndex);
            return true;
        }

        public object? GetResource(string path)
        {
            ref readonly ShaderBindGroupVariable variable = ref _variables.GetValueRefOrNullRef(path.GetDjb2HashCode());
            if (Unsafe.IsNullRef(in variable))
            {
                return null;
            }

            return _staticLocations[variable.LocalIndex].Resource;
        }

        public bool HasResource(string path) => _variables.ContainsKey(path.GetDjb2HashCode());

        public bool SetResource(string path, object? resource) => SetResource(path, resource, BindGroupResourceType.Pending);
        public bool SetResource(string path, TextureAsset? texture) => SetResource(path, texture, BindGroupResourceType.TextureAsset);
        public bool SetResource(string path, RHI.Resource? resource) => SetResource(path, resource, BindGroupResourceType.Pending);
        public bool SetResource(string path, RHI.Texture? texture) => SetResource(path, texture, BindGroupResourceType.RHITexture);
        public bool SetResource(string path, RHI.Buffer? buffer) => SetResource(path, buffer, BindGroupResourceType.RHIBuffer);
        public bool SetResource(string path, RHI.RenderTextureView? texture) => SetResource(path, texture, BindGroupResourceType.RHIRenderTextureView);

        public string GroupName => _groupName;

        internal Span<BindGroupResourceLocation> StaticResources => _staticLocations;

        private struct CompareResourceNames : IComparer<ShaderBindGroupVariable>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(ShaderBindGroupVariable x, ShaderBindGroupVariable y)
            {
                return string.Compare(x.Name, y.Name, StringComparison.InvariantCulture);
            }
        }
    }

    internal readonly record struct BindGroupResourceLocation(object? Resource, BindGroupResourceType Type, ushort ConstantsOffset);

    internal enum BindGroupResourceType : byte
    {
        None = 0,
        Pending,
        TextureAsset,
        RHITexture,
        RHIBuffer,
        RHIRenderTextureView
    }
}
