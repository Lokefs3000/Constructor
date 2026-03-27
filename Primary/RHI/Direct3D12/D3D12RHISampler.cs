using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Primary.RHI2.Direct3D12
{
    [SupportedOSPlatform("windows")]
    public unsafe sealed class D3D12RHISampler : RHISampler
    {
        private readonly D3D12RHIDevice _device;

        private D3D12RHISamplerNative* _nativeRep;

        internal D3D12RHISampler(D3D12RHIDevice device, RHISamplerDescription description)
        {
            _device = device;
            _description = description;

            {
                _nativeRep = (D3D12RHISamplerNative*)NativeMemory.Alloc((nuint)Unsafe.SizeOf<D3D12RHISamplerNative>());
                _nativeRep->Base = new RHISamplerNative
                {
                    Description = description,
                };
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _device.AddResourceFreeNextFrame(() =>
                {
                    if (_nativeRep != null)
                        NativeMemory.Free(_nativeRep);
                    _nativeRep = null;
                });

                _disposedValue = true;
            }
        }

        protected override void SetDebugName(string? debugName)
        {

        }

        public override unsafe RHISamplerNative* GetAsNative() => (RHISamplerNative*)_nativeRep;
        public override unsafe RHIResourceNative* GetBaseAsNative() => (RHIResourceNative*)_nativeRep;

        public override RHIResourceType Type => RHIResourceType.Sampler;
    }

    public unsafe struct D3D12RHISamplerNative
    {
        public RHISamplerNative Base;

        public static implicit operator RHISamplerNative(D3D12RHISamplerNative native) => native.Base;
    }
}
