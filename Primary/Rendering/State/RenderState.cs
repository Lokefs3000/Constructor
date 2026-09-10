using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Common.Memory;
using Primary.Rendering.Assets;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.RHI;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering.State
{
    internal unsafe abstract class RenderState : IDisposable
    {
        private DirtyClassValue<PropertyBlock> _dataBlock;
        private long _lastDataBlockUpdateIndex;

        private nint _constantsData;
        private int _constantsDataSetSize;
        private int _constantsDataSize;

        private PropertyChangeFlags _changeFlags;
        private bool _hasConstantsSeparated;

        private bool _disposedValue;

        internal RenderState()
        {
            _dataBlock = new DirtyClassValue<PropertyBlock>(null);
            _lastDataBlockUpdateIndex = long.MinValue;

            _constantsData = (nint)NativeMemory.Alloc(128);
            _constantsDataSetSize = 0;
            _constantsDataSize = 0;

            _changeFlags = PropertyChangeFlags.None;
            _hasConstantsSeparated = false;
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

        internal virtual bool CommitState(LinearBlockAllocator allocator, CommandRecorder recorder)
        {
            PropertyChangeFlags changeFlags = PropertyChangeFlags.None;
            bool hasSetResourcesInfo = false;

            if (_dataBlock.Value != null && (_dataBlock.IsDirty || (_dataBlock.Value.IsOutOfDate || _dataBlock.Value.UpdateIndex != _lastDataBlockUpdateIndex)))
            {
                PropertyBlock block = _dataBlock.Value!;

                if (block.IsOutOfDate)
                {
                    if (!block.Reload())
                        return false;
                }
                if (_dataBlock.IsDirty)
                    _lastDataBlockUpdateIndex = long.MinValue;

                if (block.UpdateIndex != _lastDataBlockUpdateIndex)
                {
                    recorder.AddCommand(RecCommandType.SetResourcesInfo, new CmdSetResourcesInfo
                    {
                        HeaderFlags = block.Shader?.HeaderFlags ?? ShHeaderFlags.None,
                        ConstantsSize = _constantsDataSize,
                        DataSizeRequired = block.ResourceCount * sizeof(uint) + block.BlockSize + (_hasConstantsSeparated ? 0 : _constantsDataSize)
                    });

                    hasSetResourcesInfo = true;
                }

                _lastDataBlockUpdateIndex = block.UpdateIndex;
                changeFlags |= PropertyChangeFlags.Block;

                if (block.BlockSize > 0)
                {
                    nint blockPointer = allocator.Allocate(block.BlockSize);
                    block.CopyBlockDataTo(blockPointer);

                    recorder.AddCommand(RecCommandType.SetRawData, new CmdSetRawData
                    {
                        DataOffset = _hasConstantsSeparated ? 0 : _constantsDataSize,
                        DataSize = block.BlockSize,
                        DataPointer = blockPointer
                    });
                }

                IShaderResourceSource? resourceSource = block.Shader;
                if (resourceSource != null)
                {
                    //TODO: Add validation to ensure a non read write resource gets bound to a read write property

                    int dataBaseOffset = block.BlockSize + (_hasConstantsSeparated ? 0 : _constantsDataSize);

                    ShaderGlobalsManager globalsManager = ShaderGlobalsManager.Instance;
                    foreach (ref readonly ShaderProperty property in resourceSource.Properties)
                    {
                        if (property.Type == ShPropertyType.Texture || property.Type == ShPropertyType.Buffer || property.Type == ShPropertyType.Sampler)
                        {
                            PropertyData data = default;
                            if (!Flags.HasFlag(property.Flags, ShPropertyFlags.Global))
                            {
                                data = block.GetPropertyValue(property.IndexOrByteOffset);

                                if (data.ParentIndex != ushort.MaxValue)
                                {
                                    if (data.Aux == null && property.Type == ShPropertyType.Sampler)
                                    {
                                        ref readonly PropertyData parentData = ref block.GetPropertyValue(data.ParentIndex);
                                        if (parentData.Aux is TextureAsset asset)
                                        {
                                            data = new PropertyData(data.ParentIndex, FrameGraphResource.Invalid, asset.RawRHISampler);
                                        }
                                    }
                                }
                            }
                            else if (!globalsManager.TryGetPropertyValue(property.Name, out data))
                            {
                                recorder.AddCommand(RecCommandType.SetResource, new CmdSetResource
                                {
                                    Stages = property.Stages,
                                    Flags = property.Flags,

                                    DataOffset = dataBaseOffset,
                                    Resource = property.Type switch
                                    {
                                        ShPropertyType.Buffer => CmdDataResource.NullBuffer,
                                        ShPropertyType.Texture => CmdDataResource.NullTexture,
                                        ShPropertyType.Sampler => CmdDataResource.NullSampler,
                                        _ => throw new NotSupportedException()
                                    },

                                    Intent = PropertyBindIntent.Default
                                });

                                dataBaseOffset += 4;
                                continue;
                            }

                            CmdDataResource resource = default;

                            switch (property.Type)
                            {
                                case ShPropertyType.Buffer:
                                    {
                                        if (data.Resource.IsNull)
                                            resource = CmdDataResource.NullBuffer;
                                        else
                                            resource = data.Resource.AsBuffer();

                                        break;
                                    }
                                case ShPropertyType.Texture:
                                    {
                                        if (data.Aux != null)
                                        {
                                            TextureAsset asset = Unsafe.As<TextureAsset>(data.Aux);
                                            resource = new CmdDataResource(CmdResourceType.Texture, true, asset.RawRHITexture == null ? nint.Zero : (nint)asset.RawRHITexture.GetAsNative());
                                        }
                                        else
                                        {
                                            if (data.Resource.IsNull)
                                                resource = CmdDataResource.NullTexture;
                                            else
                                                resource = data.Resource.AsTexture();
                                        }

                                        break;
                                    }
                                case ShPropertyType.Sampler:
                                    {
                                        if (data.Aux != null)
                                            resource = Unsafe.As<RHISampler>(data.Aux);
                                        else
                                            resource = CmdDataResource.NullSampler;

                                        break;
                                    }
                            }

                            if (resource.IsNull)
                            {
                                if (property.Type == ShPropertyType.Texture)
                                {
                                    switch (property.Default)
                                    {
                                        case ShPropertyDefault.NumOne:
                                        case ShPropertyDefault.NumIdentity:
                                        case ShPropertyDefault.TexWhite: resource = new CmdDataResource(CmdResourceType.Texture, true, (nint)AssetManager.Static.DefaultWhite.RawRHITexture!.GetAsNative()); break;
                                        case ShPropertyDefault.NumZero:
                                        case ShPropertyDefault.TexBlack: resource = new CmdDataResource(CmdResourceType.Texture, true, (nint)AssetManager.Static.DefaultBlack.RawRHITexture!.GetAsNative()); break;
                                        case ShPropertyDefault.TexNormal: resource = new CmdDataResource(CmdResourceType.Texture, true, (nint)AssetManager.Static.DefaultNormal.RawRHITexture!.GetAsNative()); break;
                                        case ShPropertyDefault.TexMask: resource = new CmdDataResource(CmdResourceType.Texture, true, (nint)AssetManager.Static.DefaultMask.RawRHITexture!.GetAsNative()); break;
                                    }
                                }
                                else if (property.Type == ShPropertyType.Sampler)
                                {
                                    resource = new CmdDataResource(CmdResourceType.Sampler, true, nint.Zero);
                                }
                            }

                            recorder.AddResourceToSet(data.Resource);
                            recorder.AddCommand(RecCommandType.SetResource, new CmdSetResource
                            {
                                Stages = property.Stages,
                                Flags = property.Flags,

                                DataOffset = dataBaseOffset,
                                Resource = resource,

                                Intent = data.Intent
                            });

                            dataBaseOffset += 4;
                        }
                    }
                }

                _dataBlock.IsDirty = false;
            }
            else
            {
                recorder.AddCommand(RecCommandType.SetResourcesInfo, new CmdSetResourcesInfo
                {
                    HeaderFlags = _hasConstantsSeparated ? ShHeaderFlags.HeaderIsBuffer : ShHeaderFlags.None,
                    ConstantsSize = _constantsDataSize,
                    DataSizeRequired = _hasConstantsSeparated ? 0 : _constantsDataSize
                });
            }

            if (Flags.HasFlag(_changeFlags, PropertyChangeFlags.Constants) && _constantsDataSetSize > 0)
            {
                nint dataPtr = allocator.Allocate(_constantsDataSize);
                NativeMemory.Copy(_constantsData.ToPointer(), dataPtr.ToPointer(), (nuint)_constantsDataSize);

#if DEBUG
                Span<uint> debugView = new Span<uint>(dataPtr.ToPointer(), _constantsDataSize / Unsafe.SizeOf<uint>());
#endif

                recorder.AddCommand(RecCommandType.SetConstants, new CmdSetConstants
                {
                    DataOffset = 0,
                    DataSize = _constantsDataSize,
                    DataPointer = dataPtr
                });

                changeFlags |= PropertyChangeFlags.Constants;
            }

            if (changeFlags > 0)
            {
                recorder.AddBlankCommand(RecCommandType.CommitResources);
            }

            _changeFlags = PropertyChangeFlags.None;
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

        private readonly record struct PropertyResourceData(FrameGraphResource Resource, ShPropertyStages Stages, ShPropertyFlags Flags);

        private enum PropertyChangeFlags : byte
        {
            None = 0,

            Block = 1 << 0,
            Constants = 1 << 2,

            All = Block | Constants,
        }
    }

}
