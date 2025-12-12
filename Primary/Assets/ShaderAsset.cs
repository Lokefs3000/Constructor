using Primary.Assets.Types;
using Primary.Rendering;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Primary.Assets
{
    public sealed class ShaderAsset : IAssetDefinition
    {
        private readonly ShaderAssetData _assetData;

        internal ShaderAsset(ShaderAssetData assetData)
        {
            _assetData = assetData;
        }

        public override int GetHashCode()
        {
            return _assetData.HashCode;
        }

        internal ShaderBindGroupVariable[] GetVariablesForBindGroup(string bindGroup) => _assetData.GetVariablesForBindGroup(bindGroup);
        internal int GetIndexForBindGroup(string bindGroup) => _assetData.GetIndexForBindGroup(bindGroup);

        public ShaderBindGroup CreateDefaultBindGroup() => new ShaderBindGroup(this, "__Default");

        internal ShaderAssetData AssetData => _assetData;

        internal RHI.GraphicsPipeline? GraphicsPipeline => _assetData.GraphicsPipeline;
        internal int HashCode => _assetData.HashCode;

        internal FrozenDictionary<string, ShaderVariable> Variables => _assetData.Variables;

        public ResourceStatus Status => _assetData.Status;

        public string Name => _assetData.Name;
        public AssetId Id => _assetData.Id;
    }

    internal sealed class ShaderAssetData : IInternalAssetData
    {
        private readonly WeakReference _asset;

        private ResourceStatus _status;

        private readonly AssetId _id;
        private string _name;

        private RHI.GraphicsPipeline? _graphicsPipeline;
        private int _hashCode;

        private FrozenDictionary<string, ShaderVariable> _variables;
        private FrozenDictionary<string, int> _bindGroupIndices;

        internal ShaderAssetData(AssetId id)
        {
            _asset = new WeakReference(null);

            _status = ResourceStatus.Pending;

            _id = id;
            _name = string.Empty;

            _graphicsPipeline = null;
            _hashCode = 0;

            _variables = FrozenDictionary<string, ShaderVariable>.Empty;
            _bindGroupIndices = FrozenDictionary<string, int>.Empty;
        }

        public void Dispose()
        {
            _status = ResourceStatus.Disposed;

            _graphicsPipeline = null;
            _hashCode = 0;

            _variables = FrozenDictionary<string, ShaderVariable>.Empty;
            _bindGroupIndices = FrozenDictionary<string, int>.Empty;

            _asset.Target = null;
        }

        public void SetAssetInternalStatus(ResourceStatus status)
        {
            _status = status;
        }

        public void SetAssetInternalName(string name)
        {
            _name = name;
        }

        internal void UpdateAssetData(ShaderAsset asset, RHI.GraphicsPipeline graphicsPipeline, int hashCode, Dictionary<string, ShaderVariable> variables)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Success;

            _graphicsPipeline = graphicsPipeline;
            _hashCode = hashCode;

            _variables = variables.ToFrozenDictionary();

            Dictionary<string, int> indices = new Dictionary<string, int>();
            SortedSet<string> sortedGroups = new SortedSet<string>(new CompareStringsInvariant());

            foreach (var value in _variables.Values)
                sortedGroups.Add(value.BindGroup);

            int index = 0;
            foreach (string group in sortedGroups)
            {
                int count = 0;
                foreach (var value in _variables.Values)
                {
                    if (value.BindGroup == group)
                        count++;
                }

                indices.Add(group, index);
                index += count;
            }

            _bindGroupIndices = indices.ToFrozenDictionary();
        }

        internal void UpdateAssetFailed(ShaderAsset asset)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Error;
        }

        internal ShaderBindGroupVariable[] GetVariablesForBindGroup(string bindGroup)
        {
            if (!_bindGroupIndices.ContainsKey(bindGroup))
                return Array.Empty<ShaderBindGroupVariable>();

            int countInGroup = 0;
            foreach (string key in _variables.Keys)
                if (_variables.GetValueRefOrNullRef(key).BindGroup == bindGroup)
                    countInGroup++;

            ShaderBindGroupVariable[] variables = new ShaderBindGroupVariable[countInGroup];

            int index = 0;
            foreach (string key in _variables.Keys)
            {
                ref readonly ShaderVariable shaderVar = ref _variables.GetValueRefOrNullRef(key);
                if (shaderVar.BindGroup == bindGroup)
                {
                    variables[index++] = new ShaderBindGroupVariable
                    {
                        Type = shaderVar.Type,
                        Name = shaderVar.Name,
                        BindIndex = shaderVar.Index
                    };
                }
            }

            Debug.Assert(index == variables.Length);
            return variables;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetIndexForBindGroup(string bindGroup)
        {
            if (!_bindGroupIndices.TryGetValue(bindGroup, out int index))
                index = -1;
            return index;
        }

        internal RHI.GraphicsPipeline? GraphicsPipeline => _graphicsPipeline;
        internal int HashCode => _hashCode;

        internal ResourceStatus Status => _status;

        internal AssetId Id => _id;
        internal string Name => _name;

        internal FrozenDictionary<string, ShaderVariable> Variables => _variables;

        public Type AssetType => typeof(ShaderAsset);
        public IAssetDefinition? Definition => Unsafe.As<IAssetDefinition>(_asset.Target);

        AssetId IInternalAssetData.Id => Id;
        ResourceStatus IInternalAssetData.Status => Status;
        string IInternalAssetData.Name => Name;

        private struct CompareStringsInvariant : IComparer<string>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(string? x, string? y)
            {
                return string.Compare(x, y, StringComparison.InvariantCulture);
            }
        }
    }

    public record struct ShaderBindGroupVariable
    {
        public ShaderVariableType Type;
        public string Name;
        public byte BindIndex;
        public byte LocalIndex;

        public ShaderBindGroupVariable(ShaderVariableType type, string name)
        {
            Type = type;
            Name = name;
            BindIndex = 0;
            LocalIndex = 0;
        }
    }
}
