using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Diagnostics;

namespace Primary.Rendering
{
    public readonly record struct ResourceHandle : IEquatable<ResourceHandle>
    {
        private readonly int _index;
        private readonly ushort _localIndex;
        private readonly FGResourceType _resourceType;
        private readonly bool _isImported;

        internal ResourceHandle(int index, ushort localIndex, FGResourceType resourceType, bool isImported)
        {
            _index = index;
            _localIndex = localIndex;
            _resourceType = resourceType;
            _isImported = isImported;
        }

        public override int GetHashCode() => _index.GetHashCode();

        public TextureHandle AsTexture()
        {
            Guard.IsBitwiseEqualTo(_resourceType, FGResourceType.Texture, "Only a texture resource can be cast to a texture handle");
            return new TextureHandle(this);
        }

        public BufferHandle AsBuffer()
        {
            Guard.IsBitwiseEqualTo(_resourceType, FGResourceType.Buffer, "Only a buffer resource can be cast to a buffer handle");
            return new BufferHandle(this);
        }

        public readonly int Index => _index;
        public readonly bool IsImported => _isImported;
        public readonly FGResourceType ResourceType => _resourceType;

        internal readonly ushort LocalIndex => _localIndex;

        public readonly bool IsNull => _index == -1;

        public static readonly ResourceHandle Null = new ResourceHandle(-1, ushort.MaxValue, FGResourceType.Buffer, false);
    }

    public enum FGResourceType : byte
    {
        Buffer = 0,
        Texture
    }
}
