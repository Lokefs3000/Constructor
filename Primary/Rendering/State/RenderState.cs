using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering.Resources;
using Primary.Rendering.Assets;
using Primary.Rendering.Commands;
using Primary.Rendering.Recording;
using Primary.RHI;
using Primary.Rendering.Statistics;

namespace Primary.Rendering.State
{
    internal unsafe abstract class RenderState : IDisposable
    {
        private readonly FrameGraph _frameGraph;

        private DirtyClassValue<PropertyBlock> _dataBlock;
        private long _lastDataBlockUpdateIndex;

        private nint _constantsData;
        private int _constantsDataSetSize;
        private int _constantsDataSize;

        private nint _resourcesData;
        private int _resourcesDataSize;

        private PropertyChangeFlags _changeFlags;
        private bool _hasConstantsSeparated;

        private List<ActiveResourceData> _activeResources;

        protected bool _couldBarriersHaveChanged;

        private bool _disposedValue;

        internal RenderState(FrameGraph frameGraph)
        {
            _frameGraph = frameGraph;

            _dataBlock = new DirtyClassValue<PropertyBlock>(null);
            _lastDataBlockUpdateIndex = long.MinValue;

            _constantsData = (nint)NativeMemory.Alloc(128);
            _constantsDataSetSize = 0;
            _constantsDataSize = 0;

            _resourcesData = nint.Zero;
            _resourcesDataSize = 0;

            _changeFlags = PropertyChangeFlags.None;
            _hasConstantsSeparated = false;

            _activeResources = new List<ActiveResourceData>();

            _couldBarriersHaveChanged = false;
        }

        protected abstract void DisposeInternal(bool disposing);

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                DisposeInternal(disposing);

                if (_constantsData != nint.Zero)
                    NativeMemory.Free(_constantsData.ToPointer());
                _constantsData = nint.Zero;

                if (_resourcesData != nint.Zero)
                    NativeMemory.Free(_resourcesData.ToPointer());
                _resourcesData = nint.Zero;

                _disposedValue = true;
            }
        }

        ~RenderState()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal virtual void ClearState()
        {
            _dataBlock.Clear();
            _couldBarriersHaveChanged = false;

            _activeResources.Clear();
        }

        internal virtual void SoftResetForNextPass()
        {
            _dataBlock.Value = null;
            _lastDataBlockUpdateIndex = long.MinValue;

            if (_constantsDataSetSize > 0)
                _changeFlags |= PropertyChangeFlags.Constants;
            _constantsDataSetSize = 0;
        }

        internal void SetPipelineLimits(RHIGraphicsPipeline pipeline)
        {
            ref readonly RHIGraphicsPipelineDescription desc = ref pipeline.Description;

            _constantsDataSize = desc.Expected32BitConstants * 4;
            _hasConstantsSeparated = desc.UseBufferForHeader;

            _changeFlags |= PropertyChangeFlags.All;
        }

        internal void SetPipelineLimits(RHIComputePipeline pipeline)
        {
            ref readonly RHIComputePipelineDescription desc = ref pipeline.Description;

            _constantsDataSize = desc.Expected32BitConstants * 4;
            _hasConstantsSeparated = desc.UseBufferForHeader;

            _changeFlags |= PropertyChangeFlags.All;
        }

        internal virtual bool TryCommitState(ref RenderPassStats stats, BarrierGroup barrierGroup, DescriptorAllocator descriptorAllocator, CommandRecorder commandRecorder, ObjectsCache objectsCache)
        {
            PropertyChangeFlags changeFlags = _changeFlags;
            bool wantsToCommitNewShaderInput = false;

            if (_dataBlock.Value != null && (_dataBlock.IsDirty || _dataBlock.Value.IsOutOfDate || _dataBlock.Value.UpdateIndex != _lastDataBlockUpdateIndex))
            {
                PropertyBlock block = _dataBlock.Value!;

                if (block.IsOutOfDate)
                {
                    if (!block.Reload())
                        return false;
                }
                if (_dataBlock.IsDirty)
                {
                    _lastDataBlockUpdateIndex = long.MinValue;
                }

                int blockSize = block.HeaderFlags.HasFlags(ShHeaderFlags.ExternalProperties) ? 0 : block.BlockSize;
                int resourceDataSize = blockSize + block.ResourceCount * sizeof(uint);

                if (block.UpdateIndex != _lastDataBlockUpdateIndex)
                {
                    if (_resourcesDataSize < resourceDataSize)
                    {
                        if (_resourcesData != nint.Zero)
                            NativeMemory.Free(_resourcesData.ToPointer());

                        _resourcesData = (nint)NativeMemory.Alloc((nuint)resourceDataSize);
                        _resourcesDataSize = resourceDataSize;
                    }
                }

                // if (block.UpdateIndex != _lastDataBlockUpdateIndex)
                // {
                //     recorder.AddCommand(RecCommandType.SetResourcesInfo, new CmdSetResourcesInfo
                //     {
                //         HeaderFlags = block.Shader?.HeaderFlags ?? ShHeaderFlags.None,
                //         ConstantsSize = _constantsDataSize,
                //         DataSizeRequired = block.ResourceCount * sizeof(uint) + blockSize + (_hasConstantsSeparated ? 0 : _constantsDataSize)
                //     });
                // 
                //     hasSetResourcesInfo = true;
                // }

                _lastDataBlockUpdateIndex = block.UpdateIndex;
                changeFlags |= PropertyChangeFlags.Block;

                if (blockSize > 0)
                {
                    Span<byte> span = new Span<byte>(_resourcesData.ToPointer(), blockSize);
                    block.CopyBlockDataTo(span);
                }

                _activeResources.Clear();

                IShaderResourceSource? resourceSource = block.Shader;
                if (resourceSource != null)
                {
                    //TODO: Add validation to ensure a non read write resource gets bound to a read write property

                    int dataBaseOffset = blockSize;

                    Span<uint> resourceDataSpan = new Span<uint>((_resourcesData + dataBaseOffset).ToPointer(), block.ResourceCount);
                    int resourceDataIndex = 0;

                    ShaderGlobalsManager globalsManager = ShaderGlobalsManager.Instance;

                    ReadOnlySpan<ShaderProperty> properties = resourceSource.Properties;
                    for (int i = 0; i < properties.Length; ++i)
                    {
                        ref readonly ShaderProperty property = ref properties[i];

                        if (property.Type == ShPropertyType.Texture || property.Type == ShPropertyType.Buffer || property.Type == ShPropertyType.Sampler)
                        {
                            PropertyData data = block.GetPropertyValue(property.IndexOrByteOffset);
                            if (property.Type != ShPropertyType.Sampler && data.FGResource.IsNull && data.ObjResource == null)
                            {
                                globalsManager.TryGetPropertyValue(property.Name, out data);
                            }
                            else
                            {
                                if (data.ParentIndex != ushort.MaxValue)
                                {
                                    if (data.ObjResource == null && property.Type == ShPropertyType.Sampler)
                                    {
                                        ref readonly PropertyData parentData = ref block.GetPropertyValue(data.ParentIndex);
                                        if (parentData.ObjResource is TextureAsset asset)
                                        {
                                            data = new PropertyData(data.ParentIndex, ResourceHandle.Null, asset.RawRHISampler);
                                        }
                                    }
                                }
                            }

                            ResourceHandle resource = data.FGResource;
                            RHISampler? sampler = Engine.GlobalSingleton.RenderingManager.DefaultSampler;

                            if (resource.IsNull)
                            {
                                switch (property.Type)
                                {
                                    case ShPropertyType.Buffer:
                                        {
                                            if (data.ObjResource != null)
                                            {
                                                resource = _frameGraph.ImportBufferAsPure((RHIBuffer)data.ObjResource);
                                            }

                                            break;
                                        }
                                    case ShPropertyType.Texture:
                                        {
                                            if (data.ObjResource != null)
                                            {
                                                if (data.ObjResource is TextureAsset texture)
                                                {
                                                    if (texture.IsLoaded && texture.RawRHITexture != null)
                                                    {
                                                        resource = _frameGraph.ImportTextureAsPure(texture.RawRHITexture!);
                                                    }
                                                }
                                                else
                                                {
                                                    resource = _frameGraph.ImportTextureAsPure((RHITexture)data.ObjResource);
                                                }
                                            }

                                            break;
                                        }
                                    case ShPropertyType.Sampler:
                                        {
                                            if (data.ObjResource != null)
                                            {
                                                sampler = (RHISampler)data.ObjResource;
                                            }

                                            break;
                                        }
                                }
                            }

                            if (resource.IsNull)
                            {
                                if (property.Type == ShPropertyType.Buffer)
                                {
                                    // No fallback exists for buffers
                                    return false;
                                }
                                else if (property.Type == ShPropertyType.Texture)
                                {
                                    switch (property.Default)
                                    {
                                        case ShPropertyDefault.NumOne:
                                        case ShPropertyDefault.NumIdentity:
                                        case ShPropertyDefault.TexWhite: resource = _frameGraph.ImportTextureAsPure(AssetManager.Static.DefaultWhite.RawRHITexture!); break;
                                        case ShPropertyDefault.NumZero:
                                        case ShPropertyDefault.TexBlack: resource = _frameGraph.ImportTextureAsPure(AssetManager.Static.DefaultBlack.RawRHITexture!); break;
                                        case ShPropertyDefault.TexNormal: resource = _frameGraph.ImportTextureAsPure(AssetManager.Static.DefaultNormal.RawRHITexture!); break;
                                        case ShPropertyDefault.TexMask: resource = _frameGraph.ImportTextureAsPure(AssetManager.Static.DefaultMask.RawRHITexture!); break;
                                    }
                                }
                            }

                            if (property.Type == ShPropertyType.Sampler)
                            {
                                resourceDataSpan[resourceDataIndex++] = (uint)descriptorAllocator.GetDescriptorForSampler(sampler).GPULocalIndex;
                            }
                            else
                            {
                                DescriptorHeapType heapType;
                                if (property.Flags.HasFlags(ShPropertyFlags.ReadWrite))
                                {
                                    heapType = DescriptorHeapType.UnorderedAccessView;
                                }
                                else
                                {
                                    heapType = DescriptorHeapType.ShaderResourceView;

                                    if (resource.ResourceType == FGResourceType.Buffer)
                                    {
                                        if (resource.IsImported)
                                        {
                                            RHIBuffer buffer = _frameGraph.ResourceManager.GetRHIResource(resource.AsBuffer());
                                            ref readonly RHIBufferDescription bufferDesc = ref buffer.Description;

                                            if (bufferDesc.Usage.HasFlags(RHIResourceUsage.ConstantBuffer))
                                                heapType = DescriptorHeapType.ConstantBufferView;
                                        }
                                        else
                                        {
                                            ref readonly FGBufferDesc bufferDesc = ref _frameGraph.ResourceManager.GetDescription(resource.AsBuffer());

                                            if (bufferDesc.Target.HasFlags(FGBufferTarget.Constant))
                                                heapType = DescriptorHeapType.ConstantBufferView;
                                        }
                                    }
                                }

                                resourceDataSpan[resourceDataIndex++] = (uint)descriptorAllocator.GetDescriptorForResourceViewGPU(heapType, resource, out bool updateDescriptorHeaps).GPULocalIndex;

                                if (updateDescriptorHeaps)
                                {
                                    descriptorAllocator.RecordDescriptorSets(commandRecorder);

                                    _activeResources.Clear();
                                    resourceDataIndex = 0;
                                    i = -1;

                                    continue;
                                }

                                ResourceBarrierSync sync = property.Stages switch
                                {
                                    ShPropertyStages.None => ResourceBarrierSync.None,
                                    ShPropertyStages.VertexShading => ResourceBarrierSync.VertexShading,
                                    ShPropertyStages.PixelShading => ResourceBarrierSync.PixelShading,
                                    ShPropertyStages.ComputeShading => ResourceBarrierSync.ComputeShading,
                                    ShPropertyStages.AllShading => ResourceBarrierSync.AllShading,

                                    _ => throw new NotSupportedException()
                                };

                                ResourceBarrierAccess access = property.Flags.HasFlags(ShPropertyFlags.ReadWrite) ? ResourceBarrierAccess.UnorderedAccess : ResourceBarrierAccess.ShaderResource;
                                barrierGroup.RequestBarrier(resource, commandRecorder.CommandOffset, sync, access);

                                _activeResources.Add(new ActiveResourceData(resource, sync, access));
                            }
                        }
                    }
                }

                commandRecorder.RecordCommand(CmdCommandType.SetResourcesData, new CmdSetResourcesData
                {
                    ResourcesByteOffset = (ushort)(_hasConstantsSeparated ? 0 : _constantsDataSize),
                    ResourcesDataSize = (ushort)resourceDataSize,
                    DescriptorsByteOffset = (ushort)blockSize,
                    UseDedicatedBuffer = _hasConstantsSeparated
                });

                commandRecorder.RecordCommandData(new Span<byte>(_resourcesData.ToPointer(), resourceDataSize));

                wantsToCommitNewShaderInput = true;

                _dataBlock.IsDirty = false;
            }
            else if (_couldBarriersHaveChanged)
            {
                foreach (ActiveResourceData resourceData in _activeResources)
                {
                    barrierGroup.RequestBarrier(resourceData.Handle, commandRecorder.CommandOffset, resourceData.Sync, resourceData.Access);
                }
            }

            if (_changeFlags.HasFlags(PropertyChangeFlags.Constants) && _constantsDataSetSize > 0)
            {
                int setSizeClamped = Math.Min(_constantsDataSize, _constantsDataSetSize);
                if (setSizeClamped > 0)
                {
                    commandRecorder.RecordCommand(CmdCommandType.SetConstantsData, new CmdSetConstantsData
                    {
                        ConstantsDataSize = (ushort)_constantsDataSetSize
                    });

                    commandRecorder.RecordCommandData(new Span<byte>(_constantsData.ToPointer(), _constantsDataSetSize));
                    wantsToCommitNewShaderInput = true;
                }
            }

            if (wantsToCommitNewShaderInput)
            {
                commandRecorder.RecordCommand(CmdCommandType.CommitShaderInput);
            }

            if (_couldBarriersHaveChanged)
                ++stats.BarrierRefreshes;

            _changeFlags = PropertyChangeFlags.None;
            _couldBarriersHaveChanged = false;

            return true;
        }

        internal void SetPropertyBlock(PropertyBlock block)
        {
            _dataBlock.Value = block;
            _changeFlags |= PropertyChangeFlags.Block;
        }

        internal void SetConstants(ReadOnlySpan<uint> constants)
        {
            if (constants.Length > 32)
                throw new ArgumentOutOfRangeException(nameof(constants));

            _constantsDataSetSize = constants.Length * sizeof(uint);
            NativeMemory.Copy(Unsafe.AsPointer(ref constants.DangerousGetReference()), _constantsData.ToPointer(), (uint)_constantsDataSetSize);

            _changeFlags |= PropertyChangeFlags.Constants;
        }

        internal void NotifyOfPossibleBarrierChange()
        {
            _couldBarriersHaveChanged = true;
        }

        private readonly record struct PropertyResourceData(ResourceHandle Resource, ShPropertyStages Stages, ShPropertyFlags Flags);
        private readonly record struct ActiveResourceData(ResourceHandle Handle, ResourceBarrierSync Sync, ResourceBarrierAccess Access);

        private enum PropertyChangeFlags : byte
        {
            None = 0,

            Block = 1 << 0,
            Constants = 1 << 2,

            All = Block | Constants,
        }
    }

}
