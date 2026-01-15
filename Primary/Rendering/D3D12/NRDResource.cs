using Primary.Common;
using Primary.RHI2.Direct3D12;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;

namespace Primary.Rendering.D3D12
{
    [SupportedOSPlatform("windows")]
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct NRDResource : IEquatable<NRDResource>
    {
        [FieldOffset(0)]
        public NRDResourceId EncId;

        [FieldOffset(1)]
        public int Index;

        [FieldOffset(1)]
        public void* Native;

        public NRDResource(int index, NRDResourceId id)
        {
            EncId = id;
            Index = index;
        }

        public NRDResource(D3D12RHIBufferNative* buffer)
        {
            EncId = NRDResourceId.Buffer | NRDResourceId.External;
            Native = buffer;
        }

        public NRDResource(D3D12RHITextureNative* texture)
        {
            EncId = NRDResourceId.Texture | NRDResourceId.External;
            Native = texture;
        }

        public NRDResource(D3D12RHISamplerNative* sampler)
        {
            EncId = NRDResourceId.Sampler | NRDResourceId.External;
            Native = sampler;
        }

        public ID3D12Resource2* GetNativeResource(ResourceManager resources) => Id switch
        {
            NRDResourceId.Buffer => IsExternal ? ((D3D12RHIBufferNative*)Native)->Resource : resources.GetResource(this),
            NRDResourceId.Texture => IsExternal ? ((D3D12RHITextureNative*)Native)->Resource : resources.GetResource(this),
            _ => throw new NullReferenceException()
        };

        public bool Equals(NRDResource other) => EncId == other.EncId && (IsExternal ? Native == other.Native : Index == other.Index);
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is NRDResource && Equals((NRDResource)obj);

        public override int GetHashCode() => IsExternal ? ((nint)Native).GetHashCode() : Index;

        public override string ToString() => IsExternal ? ((nint)Native).ToString("x8") : Index.ToString();

        public NRDResourceId Id => (NRDResourceId)((int)EncId & 0b01111111);

        public bool IsExternal => FlagUtility.HasFlag(EncId, NRDResourceId.External);
        public bool IsTransient => !FlagUtility.HasFlag(EncId, NRDResourceId.External);

        public bool IsNull => IsExternal ? Native == null : Index < 0;

        public static readonly NRDResource Null = new NRDResource(-1, NRDResourceId.Buffer);
    }

    public enum NRDResourceId : byte
    {
        Buffer = 0,
        Texture,
        Sampler,

        External = 1 << 7
    }
}
