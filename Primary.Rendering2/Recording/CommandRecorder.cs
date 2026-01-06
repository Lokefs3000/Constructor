using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering2.Assets;
using Primary.Rendering2.Memory;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Primary.Rendering2.Recording
{
    public sealed class CommandRecorder : IDisposable
    {
        private readonly RenderPassManager _manager;

        private SequentialLinearAllocator _allocator;
        private List<int> _executionCommandOffsets;

        private RecCommandEffectFlags _currentEffectFlags;

        private int _commandCount;

        private bool _disposedValue;

        internal CommandRecorder(RenderPassManager manager)
        {
            _manager = manager;

            _allocator = new SequentialLinearAllocator(2048);
            _executionCommandOffsets = new List<int>();

            _currentEffectFlags = RecCommandEffectFlags.None;

            _commandCount = 0;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _allocator.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ResetForNewRecording()
        {
            _allocator.Reset();
            _executionCommandOffsets.Clear();

            _currentEffectFlags = RecCommandEffectFlags.None;

            _commandCount = 0;
        }

        internal void FinishRecording()
        {
            if ((_executionCommandOffsets.Count > 0 ? _executionCommandOffsets[_executionCommandOffsets.Count - 1] : 0) < _allocator.CurrentOffset)
            {
                AddExecutionCommand(RecCommandType.Dummy, new UCDummy { });
            }
        }

        internal unsafe void AddStateChangeCommand<T>(RecCommandType commandType, RecCommandEffectFlags effectFlags, T command) where T : unmanaged, IStateChangeCommand
        {
            int size = Unsafe.SizeOf<T>() + Unsafe.SizeOf<UnmanagedCommandMeta>();
            nint ptr = _allocator.Allocate(size);

            Unsafe.WriteUnaligned(ptr.ToPointer(), new UnmanagedCommandMeta { Type = commandType });
            Unsafe.WriteUnaligned((ptr + Unsafe.SizeOf<UnmanagedCommandMeta>()).ToPointer(), command);

            if (commandType != RecCommandType.Dummy)
                _commandCount++;
        }

        internal unsafe void AddExecutionCommand<T>(RecCommandType commandType, T command) where T : unmanaged, IExecutionCommand
        {
            int currentOffset = _allocator.CurrentOffset;

            int size = Unsafe.SizeOf<T>() + Unsafe.SizeOf<ExecutionCommandMeta>();
            nint ptr = _allocator.Allocate(size);

            Unsafe.WriteUnaligned(ptr.ToPointer(), new ExecutionCommandMeta { Type = commandType, Effect = _currentEffectFlags });
            Unsafe.WriteUnaligned((ptr + Unsafe.SizeOf<ExecutionCommandMeta>()).ToPointer(), command);

            _executionCommandOffsets.Add(currentOffset);
            _currentEffectFlags = RecCommandEffectFlags.None;

            if (commandType != RecCommandType.Dummy)
                _commandCount++;
        }

        internal unsafe void AddModificationCommand<T>(RecCommandType commandType, T command) where T : unmanaged, IModificationCommand
        {
            int size = Unsafe.SizeOf<T>() + Unsafe.SizeOf<ModificationCommandMeta>();
            nint ptr = _allocator.Allocate(size);

            Unsafe.WriteUnaligned(ptr.ToPointer(), new ModificationCommandMeta { Type = commandType });
            Unsafe.WriteUnaligned((ptr + Unsafe.SizeOf<ModificationCommandMeta>()).ToPointer(), command);

            if (commandType != RecCommandType.Dummy)
                _commandCount++;
        }

        internal unsafe void AddSetParameters(PropertyBlock block)
        {
            ShaderAsset2? shader = block.Shader;
            if (shader == null || shader.ResourceCount == 0)
                return;

            ShaderGlobalsManager globalsManager = ShaderGlobalsManager.Instance;
            ReadOnlySpan<ShaderProperty> properties = shader.Properties;

            {
                int size = Unsafe.SizeOf<UCSetProperties>() + Unsafe.SizeOf<ModificationCommandMeta>();
                nint ptr = _allocator.Allocate(size);

                Unsafe.WriteUnaligned(ptr.ToPointer(), new ModificationCommandMeta { Type = RecCommandType.SetProperties });
                Unsafe.WriteUnaligned((ptr + Unsafe.SizeOf<ModificationCommandMeta>()).ToPointer(), new UCSetProperties { ResourceCount = shader.ResourceCount, DataBlockSize = block.BlockSize, UseBufferForHeader = FlagUtility.HasFlag(shader.HeaderFlags, ShHeaderFlags.HeaderIsBuffer) });
            }

            if (block.BlockSize > 0 && block.BlockPointer != nint.Zero)
            {
                nint ptr = _allocator.Allocate(block.BlockSize);
                Unsafe.CopyBlockUnaligned(ptr.ToPointer(), block.BlockPointer.ToPointer(), (uint)block.BlockSize);
            }

            foreach (ref readonly ShaderProperty property in properties)
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
                        int size2 = Unsafe.SizeOf<UnmanagedPropertyData>();
                        nint ptr2 = _allocator.Allocate(size2);

                        Unsafe.WriteUnaligned(ptr2.ToPointer(), new UnmanagedPropertyData
                        {
                            Meta = new PropertyMeta
                            {
                                Type = SetPropertyType.None,
                                Target = ShPropertyStages.None,
                            },
                            IsExternal = false,
                            Resource = nint.Zero
                        });

                        continue;
                    }

                    bool isExternal = false;
                    nint dataPtr = -1;

                    switch (property.Type)
                    {
                        case ShPropertyType.Buffer:
                            {
                                if (data.Resource.Resource == null)
                                    dataPtr = data.Resource.Index;
                                else
                                {
                                    dataPtr = data.Resource.Resource.Handle;
                                    isExternal = true;
                                }

                                break;
                            }
                        case ShPropertyType.Texture:
                            {
                                if (data.Aux != null)
                                {
                                    TextureAsset asset = Unsafe.As<TextureAsset>(data.Aux);
                                    dataPtr = asset.Handle;
                                    isExternal = true;
                                }
                                else
                                {
                                    if (data.Resource.Resource == null)
                                        dataPtr = data.Resource.Index;
                                    else
                                    {
                                        dataPtr = data.Resource.Resource.Handle;
                                        isExternal = true;
                                    }
                                }

                                break;
                            }
                        case ShPropertyType.Sampler:
                            {
                                isExternal = true;
                                if (data.Aux != null)
                                {
                                    RHI.Sampler sampler = Unsafe.As<RHI.Sampler>(data.Aux);
                                    dataPtr = sampler.Handle;
                                }

                                break;
                            }
                    }

                    if (dataPtr == -1)
                    {
                        if (property.Type == ShPropertyType.Texture)
                        {
                            switch (property.Default)
                            {
                                case ShPropertyDefault.NumOne:
                                case ShPropertyDefault.NumIdentity:
                                case ShPropertyDefault.TexWhite: dataPtr = AssetManager.Static.DefaultWhite.Handle; break;
                                case ShPropertyDefault.NumZero:
                                case ShPropertyDefault.TexBlack: dataPtr = AssetManager.Static.DefaultBlack.Handle; break;
                                case ShPropertyDefault.TexNormal: dataPtr = AssetManager.Static.DefaultNormal.Handle; break;
                                case ShPropertyDefault.TexMask: dataPtr = AssetManager.Static.DefaultMask.Handle; break;
                            }

                            isExternal = true;
                        }
                        else if (property.Type == ShPropertyType.Sampler)
                        {
                            dataPtr = _manager.Resources.DefaultSampler.Handle;
                        }
                    }

                    int size = Unsafe.SizeOf<UnmanagedPropertyData>();
                    nint ptr = _allocator.Allocate(size);

                    Unsafe.WriteUnaligned(ptr.ToPointer(), new UnmanagedPropertyData
                    {
                        Meta = new PropertyMeta
                        {
                            Type = property.Type switch
                            {
                                ShPropertyType.Buffer => isExternal ? SetPropertyType.RHIBuffer : SetPropertyType.Buffer,
                                ShPropertyType.Texture => isExternal ? SetPropertyType.RHITexture : SetPropertyType.Texture,
                                ShPropertyType.Sampler => isExternal ? SetPropertyType.RHISampler : throw new NotSupportedException(),
                                _ => throw new NotImplementedException()
                            },
                            Target = property.Stages
                        },
                        IsExternal = isExternal,
                        Resource = dataPtr
                    });
                }
            }

            _commandCount++;
        }

        public nint GetPointerAtOffset(int offset)
        {
            Debug.Assert(offset <= _allocator.CurrentOffset);
            return _allocator.Pointer + offset;
        }

        public unsafe RecCommandType GetCommandTypeAtOffset(int offset)
        {
            Debug.Assert(offset + Unsafe.SizeOf<RecCommandType>() <= _allocator.CurrentOffset);
            return Unsafe.ReadUnaligned<RecCommandType>((_allocator.Pointer + offset).ToPointer());
        }

        public unsafe T GetCommandAtOffset<T>(int offset) where T : unmanaged
        {
            Debug.Assert(offset + Unsafe.SizeOf<RecCommandType>() <= _allocator.CurrentOffset);
            return Unsafe.ReadUnaligned<T>((_allocator.Pointer + offset).ToPointer());
        }

        public int BufferSize => _allocator.CurrentOffset;
        public int TotalCommandCount => _commandCount;

        public ReadOnlySpan<int> ExecutionCommandOffsets => _executionCommandOffsets.AsSpan();
    }

    public enum RecCommandType : byte
    {
        Undefined = 0,

        Dummy,

        SetRenderTarget,
        SetDepthStencil,

        ClearRenderTarget,
        ClearDepthStencil,
        ClearRenderTargetCustom,
        ClearDepthStencilCustom,

        SetViewport,
        SetScissor,

        SetStencilReference,

        SetBuffer,
        SetProperties,

        UploadBuffer,
        UploadTexture,

        CopyBuffer,
        CopyTexture,

        DrawInstanced,
        DrawIndexedInstanced,

        SetPipeline,

        PresentOnWindow
    }

    public enum RecCommandContextType : byte
    {
        StateChange = 0,    //Changes the state (Viewports, Buffers, Outputs, etc)
        Execution,          //Flushes the state and executes a command (Draw, Dispatch, etc)
        Modification        //Modifies a resource (Clear, Copy, Barrier, etc)
    }

    public enum RecCommandEffectFlags : ushort
    {
        None = 0,

        ColorTarget = 1 << 0,
        DepthStencilTarget = 1 << 1,

        Viewport = 1 << 2,
        Scissor = 1 << 3,

        StencilRef = 1 << 4,

        VertexBuffer = 1 << 5,
        IndexBuffer = 1 << 6,

        Properties = 1 << 7,

        Pipeline = 1 << 8
    }
}
