using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.RHI2
{
    public unsafe abstract class RHIComputePipeline : IDisposable, IAsNativeObject<RHIComputePipelineNative>
    {
        protected RHIComputePipelineDescription _description;
        protected RHIComputePipelineBytecode _bytecode;

        protected string? _debugName;

        protected bool _disposedValue;

        protected abstract void Dispose(bool disposing);
        protected abstract void SetDebugName(string? debugName);

        ~RHIComputePipeline()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public string? DebugName
        {
            get => _debugName;
            set
            {
                if (_debugName != value)
                    SetDebugName(_debugName);
                _debugName = value;
            }
        }

        public ref readonly RHIComputePipelineDescription Description => ref _description;
        public ref readonly RHIComputePipelineBytecode Bytecode => ref _bytecode;

        public abstract RHIComputePipelineNative* GetAsNative();
    }

    public struct RHIComputePipelineNative
    {

    }

    public struct RHIComputePipelineDescription
    {
        public RHIGPImmutableSampler[] ImmutableSamplers;

        public int Expected32BitConstants;

        public int Header32BitConstants;
        public bool UseBufferForHeader;

        public RHIComputePipelineDescription()
        {
            ImmutableSamplers = Array.Empty<RHIGPImmutableSampler>();

            Expected32BitConstants = 0;

            Header32BitConstants = 0;
            UseBufferForHeader = false;
        }

        public RHIComputePipelineDescription(RHIComputePipelineDescription other)
        {
            ImmutableSamplers = (RHIGPImmutableSampler[])other.ImmutableSamplers.Clone();

            Expected32BitConstants = other.Expected32BitConstants;

            Header32BitConstants = other.Header32BitConstants;
            UseBufferForHeader = other.UseBufferForHeader;
        }
    }

    public struct RHIComputePipelineBytecode
    {
        public Memory<byte> Compute;

        public RHIComputePipelineBytecode()
        {
            Compute = Memory<byte>.Empty;
        }

        public RHIComputePipelineBytecode(RHIComputePipelineBytecode other)
        {
            Compute = other.Compute.ToArray();
        }
    }
}
