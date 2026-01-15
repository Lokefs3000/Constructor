using Primary.Assets;
using Primary.Common;
using Primary.Rendering.Assets;
using Primary.Rendering.Memory;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.RHI2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Windows;

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

        private bool _disposedValue;

        internal RenderState()
        {
            _dataBlock = new DirtyClassValue<PropertyBlock>(null);
            _lastDataBlockUpdateIndex = long.MinValue;

            _constantsData = (nint)NativeMemory.Alloc(128);
            _constantsDataSetSize = 0;
            _constantsDataSize = 0;

            _changeFlags = PropertyChangeFlags.None;
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

        internal virtual bool CommitState(SequentialLinearAllocator allocator, CommandRecorder recorder)
        {
            if (_dataBlock.IsDirty || (_dataBlock.Value != null && (_dataBlock.Value.IsOutOfDate || _dataBlock.Value.UpdateIndex != _lastDataBlockUpdateIndex)))
            {
                PropertyBlock block = _dataBlock.Value!;

                if (block.IsOutOfDate)
                {
                    block.Reload();

                    recorder.AddCommand(RecCommandType.SetResourcesInfo, new CmdSetResourcesInfo
                    {
                        HeaderFlags = block.Shader?.HeaderFlags ?? ShHeaderFlags.None,
                        DataSizeRequired = block.ResourceCount * sizeof(uint) + block.BlockSize + _constantsDataSize
                    });
                }

                _lastDataBlockUpdateIndex = block.UpdateIndex;
                _changeFlags |= PropertyChangeFlags.Block;

                if (block.BlockSize > 0)
                {
                    nint blockPointer = allocator.Allocate(block.BlockSize);
                    block.CopyBlockDataTo(blockPointer);

                    recorder.AddCommand(RecCommandType.SetRawData, new CmdSetRawData
                    {
                        DataOffset = _constantsDataSize,
                        DataSize = block.BlockSize,
                        DataPointer = blockPointer
                    });
                }

                IShaderResourceSource? resourceSource = block.Shader;
                if (resourceSource != null)
                {
                    //TODO: Add validation to ensure a non read write resource gets bound to a read write property

                    int dataBaseOffset = block.BlockSize + _constantsDataSize;

                    ShaderGlobalsManager globalsManager = ShaderGlobalsManager.Instance;
                    foreach (ref readonly ShaderProperty property in resourceSource.Properties)
                    {
                        if (property.Type == ShPropertyType.Texture || property.Type == ShPropertyType.Buffer || property.Type == ShPropertyType.Sampler)
                        {
                            PropertyData data = default;
                            if (!FlagUtility.HasFlag(property.Flags, ShPropertyFlags.Global))
                            {
                                data = block.GetPropertyValue(property.IndexOrByteOffset);
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
                                    }
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
                            }

                            recorder.AddCommand(RecCommandType.SetResource, new CmdSetResource
                            {
                                Stages = property.Stages,
                                Flags = property.Flags,

                                DataOffset = dataBaseOffset,
                                Resource = resource
                            });

                            dataBaseOffset += 4;
                        }
                    }
                }

                _dataBlock.IsDirty = false;
            }

            if (FlagUtility.HasFlag(_changeFlags, PropertyChangeFlags.Constants) && _constantsDataSize > 0)
            {
                nint dataPtr = allocator.Allocate(_constantsDataSize);
                NativeMemory.Copy(_constantsData.ToPointer(), dataPtr.ToPointer(), (nuint)_constantsDataSize);

                recorder.AddCommand(RecCommandType.SetConstants, new CmdSetConstants
                {
                    DataSize = _constantsDataSize,
                    DataPointer = dataPtr
                });
            }

            _changeFlags = PropertyChangeFlags.None;
            return true;
        }

        internal void SetPropertyBlock(PropertyBlock block) => _dataBlock.Value = block;

        private readonly record struct PropertyResourceData(FrameGraphResource Resource, ShPropertyStages Stages, ShPropertyFlags Flags);

        private enum PropertyChangeFlags : byte
        {
            None = 0,

            Block = 1 << 0,
            Constants = 1 << 2
        }
    }

}
