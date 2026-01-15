using Primary.Assets;
using Primary.Common;
using Primary.Rendering.Assets;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI2;
using Primary.RHI2.Direct3D12;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.DirectX.D3D12_BARRIER_ACCESS;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_LAYOUT;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_SYNC;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;

namespace Primary.Rendering.D3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe sealed class ComputeState : IDisposable
    {
        private readonly NRDDevice _device;

        private DirtyValue<D3D12RHIComputePipeline?> _pipeline;

        private nint _propertyRawData;
        private int _propertyDataSize;
        private int _activeDataSize;

        private PropertyResourceData[] _resourceData;
        private int _activeResourceCount;

        private nint _constantsData;
        private int _constantsDataSize;

        private bool _useBufferForHeader;
        private PropertyChangeFlags _hasHadPropertyChange;

        //// state \\\\

        private nint _propertyDataBuffer;
        private int _propertyDataBufferSize;

        private bool _disposedValue;

        internal ComputeState(NRDDevice device)
        {
            _device = device;

            _pipeline = new DirtyValue<D3D12RHIComputePipeline>();

            _propertyRawData = nint.Zero;
            _propertyDataSize = 0;
            _activeDataSize = 0;

            _constantsData = (nint)NativeMemory.Alloc(128);
            _constantsDataSize = 0;

            _resourceData = Array.Empty<PropertyResourceData>();
            _activeResourceCount = 0;

            _useBufferForHeader = false;
            _hasHadPropertyChange = PropertyChangeFlags.None;

            _propertyDataBuffer = nint.Zero;
            _propertyDataBufferSize = 0;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (_propertyDataBuffer != nint.Zero)
                    NativeMemory.Free(_propertyDataBuffer.ToPointer());
                _propertyDataBuffer = nint.Zero;

                _disposedValue = true;
            }
        }

        ~ComputeState()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ResetInternal()
        {
            _pipeline.Value = null;

            ClearPropertyData(0, false);

            _constantsDataSize = 0;

            _hasHadPropertyChange = PropertyChangeFlags.None;
        }

        internal void SetPipeline(D3D12RHIComputePipeline? pipeline) => _pipeline.Value = pipeline;

        internal void ClearPropertyData(int minimumResourceDataSize, bool useBufferForHeader)
        {
            _activeDataSize = 0;

            if (_resourceData.Length < minimumResourceDataSize)
                _resourceData = new PropertyResourceData[BitOperations.RoundUpToPowerOf2((uint)minimumResourceDataSize)];
            else
                Array.Clear(_resourceData);

            _activeResourceCount = minimumResourceDataSize;

            _useBufferForHeader = useBufferForHeader;
            _hasHadPropertyChange |= PropertyChangeFlags.Properties;
        }

        internal void SetPropertyRawData(nint ptr, int size)
        {
            if (_propertyDataSize < size)
            {
                if (_propertyRawData != nint.Zero)
                    NativeMemory.Free(_propertyRawData.ToPointer());

                _propertyDataSize = (int)BitOperations.RoundUpToPowerOf2((uint)size);
                _propertyRawData = (nint)NativeMemory.Alloc((uint)_propertyDataSize);
            }

            _activeDataSize = size;

            if (size > 0)
            {
                NativeMemory.Copy(_propertyRawData.ToPointer(), ptr.ToPointer(), (nuint)size);
                _hasHadPropertyChange |= PropertyChangeFlags.RawData;
            }
        }

        internal void SetPropertyResource(int index, NRDResource resource, ShPropertyStages stages, SetPropertyFlags flags)
        {
            _resourceData[index] = new PropertyResourceData(resource, stages, flags);
        }

        internal void SetConstants(nint constantsData, int constantsSize)
        {
            NativeMemory.Copy(constantsData.ToPointer(), _constantsData.ToPointer(), (nuint)constantsSize);
            _constantsDataSize = constantsSize;

            _hasHadPropertyChange |= PropertyChangeFlags.Constants;
        }

        internal bool FlushState(ID3D12GraphicsCommandList10* cmdList)
        {
            if (_pipeline.Value == null)
                return false;

            D3D12RHIComputePipeline pipeline = _pipeline.Value;

            if (_pipeline.Dirty)
            {
                ID3D12PipelineState* pipelineState = pipeline.PipelineState.Get();
                if (pipelineState == null)
                    return false;

                cmdList->SetComputeRootSignature(pipeline.RootSignature.Get());
                cmdList->SetPipelineState(pipelineState);
            }

            if (_hasHadPropertyChange > 0)
            {
                int constantsSize = 0;
                if (_pipeline.Value != null)
                {
                    constantsSize = Math.Min(_constantsDataSize, pipeline.Description.Expected32BitConstants * 4);
                }

                int totalDataSize = _activeResourceCount * sizeof(uint) + _activeDataSize + constantsSize;

                if (_propertyDataBufferSize < totalDataSize)
                {
                    if (_propertyDataBuffer != nint.Zero)
                        NativeMemory.Free(_propertyDataBuffer.ToPointer());

                    uint newSize = BitOperations.RoundUpToPowerOf2((uint)totalDataSize);

                    _propertyDataBuffer = (nint)NativeMemory.Alloc(newSize);
                    _propertyDataBufferSize = (int)newSize;

                    _hasHadPropertyChange |= PropertyChangeFlags.All;
                }

                nint dataPtr = _propertyDataBuffer;

                if (FlagUtility.HasFlag(_hasHadPropertyChange, PropertyChangeFlags.Constants) && constantsSize > 0)
                {
                    NativeMemory.Copy(_constantsData.ToPointer(), dataPtr.ToPointer(), (nuint)constantsSize);
                    dataPtr += constantsSize;
                }

                if (FlagUtility.HasFlag(_hasHadPropertyChange, PropertyChangeFlags.RawData) && _activeDataSize > 0)
                {
                    NativeMemory.Copy(_propertyRawData.ToPointer(), dataPtr.ToPointer(), (nuint)_activeDataSize);
                    dataPtr += _activeDataSize;
                }

                if (FlagUtility.HasFlag(_hasHadPropertyChange, PropertyChangeFlags.Properties) && _activeResourceCount > 0)
                {
                    //pretty bad brute force system here
                    //should prob do some kind of optimization but im yet to deduce a good one hence why it looks like this for now

                RetryDescriptorAlloc:
                    for (int i = 0; i < _activeResourceCount; i++)
                    {
                        ref readonly PropertyResourceData rd = ref _resourceData[i];

                        if (rd.Resource.IsNull)
                        {
                            EngLog.NRD.Error("[{idx}]: Null resource in resource data!", i);
                            return false;
                        }

                        if (rd.Resource.Id != NRDResourceId.Sampler)
                        {
                            NRDResource resource = rd.Resource;

                            uint index = _device.GPUDescriptorHeap.GetDescriptorIndex(resource, FlagUtility.HasFlag(rd.Flags, SetPropertyFlags.UnorderedAccess), out bool createdNewHeap);
                            if (createdNewHeap)
                            {
                                SetHeapBundle bundle = new SetHeapBundle(_device.GPUDescriptorHeap.CurrentActiveHeap, _device.SamplerDescriptorHeap.CurrentActiveHeap);
                                cmdList->SetDescriptorHeaps(1, (ID3D12DescriptorHeap**)&bundle);

                                goto RetryDescriptorAlloc;
                            }

                            Unsafe.WriteUnaligned(dataPtr.ToPointer(), index);
                            dataPtr += sizeof(uint);

                            if (resource.Id == NRDResourceId.Buffer)
                            {
                                BarrierManager.GetShaderBufferBarriers(resource, _device.ResourceManager, rd.Stages, out D3D12_BARRIER_SYNC sync, out D3D12_BARRIER_ACCESS access);
                                _device.BarrierManager.AddBufferBarrier(resource, sync, access);
                            }
                            else
                            {
                                BarrierManager.GetShaderTextureBarriers(resource, _device.ResourceManager, rd.Stages, out D3D12_BARRIER_SYNC sync, out D3D12_BARRIER_ACCESS access, out D3D12_BARRIER_LAYOUT layout);
                                _device.BarrierManager.AddTextureBarrier(resource, sync, access, layout);
                            }
                        }
                        else
                        {
                            D3D12RHISamplerNative* sampler = (D3D12RHISamplerNative*)rd.Resource.Native;

                            uint index = _device.SamplerDescriptorHeap.GetDescriptorIndex(sampler->Base.Description, out bool createdNewHeap);
                            if (createdNewHeap)
                            {
                                SetHeapBundle bundle = new SetHeapBundle(_device.GPUDescriptorHeap.CurrentActiveHeap, _device.SamplerDescriptorHeap.CurrentActiveHeap);
                                cmdList->SetDescriptorHeaps(1, (ID3D12DescriptorHeap**)&bundle);

                                goto RetryDescriptorAlloc;
                            }

                            Unsafe.WriteUnaligned(dataPtr.ToPointer(), index);
                            dataPtr += sizeof(uint);

                        }
                    }
                }

                _device.BarrierManager.FlushBarriers(cmdList, BarrierFlushTypes.Buffer | BarrierFlushTypes.Texture);

                if (_useBufferForHeader)
                    throw new NotImplementedException();
                else
                {
#if DEBUG
                    for (int i = 0; i < _activeResourceCount; i++)
                    {
                        ref readonly PropertyResourceData rd = ref _resourceData[i];
                        if (!rd.Resource.IsNull)
                        {
                            NRDResource resource = rd.Resource;

                            if (resource.Id == NRDResourceId.Buffer)
                            {
                                BarrierManager.GetShaderBufferBarriers(resource, _device.ResourceManager, rd.Stages, out D3D12_BARRIER_SYNC sync, out D3D12_BARRIER_ACCESS access);
                                _device.BarrierManager.DbgEnsureState(resource, cmdList, sync, access);
                            }
                            else
                            {
                                BarrierManager.GetShaderTextureBarriers(resource, _device.ResourceManager, rd.Stages, out D3D12_BARRIER_SYNC sync, out D3D12_BARRIER_ACCESS access, out D3D12_BARRIER_LAYOUT layout);
                                _device.BarrierManager.DbgEnsureState(resource, cmdList, sync, access, layout);
                            }
                        }
                    }
#endif

                    cmdList->SetComputeRoot32BitConstants(0, (uint)totalDataSize / sizeof(uint), _propertyDataBuffer.ToPointer(), 0);
                }
            }

            _device.BarrierManager.FlushBarriers(cmdList, BarrierFlushTypes.Buffer | BarrierFlushTypes.Texture);
            _device.ResourceManager.FlushPendingInits(cmdList);

            return true;
        }

        private enum PropertyChangeFlags : byte
        {
            None = 0,

            Properties = 1 << 0,
            Constants = 1 << 1,
            RawData = 1 << 2,
            
            All = Properties | Constants
        }
    }
}
