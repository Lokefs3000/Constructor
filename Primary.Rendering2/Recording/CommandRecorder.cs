using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Rendering2.Assets;
using Primary.Rendering2.Memory;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Recording
{
    internal sealed class CommandRecorder : IDisposable
    {
        private SequentialLinearAllocator _allocator;
        private List<int> _executionCommandOffsets;

        private RecCommandEffectFlags _currentEffectFlags;

        private bool _disposedValue;

        internal CommandRecorder()
        {
            _allocator = new SequentialLinearAllocator(2048);
            _executionCommandOffsets = new List<int>();
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
        }

        internal unsafe void AddStateChangeCommand<T>(RecCommandType commandType, RecCommandEffectFlags effectFlags, T command) where T : unmanaged, IStateChangeCommand
        {
            int size = Unsafe.SizeOf<T>() + Unsafe.SizeOf<UnmanagedCommandMeta>();
            nint ptr = _allocator.Allocate(size);

            Unsafe.WriteUnaligned(ptr.ToPointer(), new UnmanagedCommandMeta { Type = commandType });
            Unsafe.WriteUnaligned((ptr + Unsafe.SizeOf<UnmanagedCommandMeta>()).ToPointer(), command);
        }

        internal unsafe void AddExecutionCommand<T>(RecCommandType commandType, T command) where T : unmanaged, IExecutionCommand
        {
            int currentOffset = _allocator.CurrentOffset;

            int size = Unsafe.SizeOf<T>() + Unsafe.SizeOf<ExecutionCommandMeta>();
            nint ptr = _allocator.Allocate(size);

            Unsafe.WriteUnaligned(ptr.ToPointer(), new ExecutionCommandMeta { Type = commandType });
            Unsafe.WriteUnaligned((ptr + Unsafe.SizeOf<ExecutionCommandMeta>()).ToPointer(), command);

            _executionCommandOffsets.Add(currentOffset);
            _currentEffectFlags = RecCommandEffectFlags.None;
        }

        internal unsafe void AddModificationCommand<T>(RecCommandType commandType, T command) where T : unmanaged, IModificationCommand
        {
            int size = Unsafe.SizeOf<T>() + Unsafe.SizeOf<ModificationCommandMeta>();
            nint ptr = _allocator.Allocate(size);

            Unsafe.WriteUnaligned(ptr.ToPointer(), new ModificationCommandMeta { Type = commandType });
            Unsafe.WriteUnaligned((ptr + Unsafe.SizeOf<ModificationCommandMeta>()).ToPointer(), command);
        }

        internal unsafe void AddSetParameters(PropertyBlock block)
        {
            ShaderAsset? shader = block.Shader;
            if (shader == null)
                return;

            ShaderGlobalsManager globalsManager = ShaderGlobalsManager.Instance;
            ReadOnlySpan<ShaderProperty> properties = shader.Properties;

            {
                int size = Unsafe.SizeOf<UCSetProperties>() + Unsafe.SizeOf<ModificationCommandMeta>();
                nint ptr = _allocator.Allocate(size);

                Unsafe.WriteUnaligned(ptr.ToPointer(), new ModificationCommandMeta { Type = RecCommandType.SetProperties });
                Unsafe.WriteUnaligned((ptr + Unsafe.SizeOf<ModificationCommandMeta>()).ToPointer(), new UCSetProperties { ResourceCount = properties.Length, DataBlockSize = shader.PropertyBlockSize });
            }

            foreach (ref readonly ShaderProperty property in properties)
            {
                if (property.Type == ShaderPropertyType.Texture || property.Type == ShaderPropertyType.Buffer)
                {
                    if ((property.Visiblity == ShaderPropertyVisiblity.Public && block.TryGetPropertyValue(property.Index, out PropertyData data)) || globalsManager.TryGetPropertyValue(property.Name, out data))
                    {
                        nint dataPtr = nint.Zero;

                        switch (property.Type)
                        {
                            case ShaderPropertyType.Buffer: dataPtr = data.Resource.Resource?.Handle ?? data.Resource.Index; break;
                            case ShaderPropertyType.Texture:
                                {
                                    if (data.Aux != null)
                                    {
                                        TextureAsset asset = Unsafe.As<TextureAsset>(data.Aux);
                                        dataPtr = asset.Handle;
                                    }
                                    else
                                        dataPtr = data.Resource.Resource?.Handle ?? data.Resource.Index;

                                    break;
                                }
                        }

                        if (dataPtr == nint.Zero)
                        {
                            if (property.Type == ShaderPropertyType.Texture)
                            {
                                switch (property.Default)
                                {
                                    case Assets.ShaderPropertyDefault.TexWhite: dataPtr = AssetManager.Static.DefaultWhite.Handle; break;
                                    case Assets.ShaderPropertyDefault.TexBlack: dataPtr = AssetManager.Static.DefaultWhite.Handle; break;
                                    case Assets.ShaderPropertyDefault.TexNormal: dataPtr = AssetManager.Static.DefaultNormal.Handle; break;
                                    case Assets.ShaderPropertyDefault.TexMask: dataPtr = AssetManager.Static.DefaultMask.Handle; break;
                                }
                            }
                        }

                        int size = Unsafe.SizeOf<UnamangedPropertyData>();
                        nint ptr = _allocator.Allocate(size);

                        Unsafe.WriteUnaligned(ptr.ToPointer(), new UnamangedPropertyData
                        {
                            Meta = new PropertyMeta
                            {
                                Type = (SetPropertyType)((int)(property.Type == ShaderPropertyType.Buffer ? SetPropertyType.Buffer : SetPropertyType.Texture) + (data.Resource.IsExternal ? 2 : 0)),
                                Target = property.Stages
                            },
                            Resource = dataPtr
                        });
                    }
                }
                else
                {

                }
            }
        }

        internal nint GetPointerAtOffset(int offset)
        {
            return _allocator.Pointer + offset;
        }

        internal ReadOnlySpan<int> ExecutionCommandOffsets => _executionCommandOffsets.AsSpan();
    }

    internal enum RecCommandType : byte
    {
        Undefined = 0,

        SetRenderTarget,
        SetDepthStencil,

        ClearRenderTarget,
        ClearDepthStencil,
        ClearRenderTargetCustom,
        ClearDepthStencilCustom,

        SetViewports,
        SetScissors,

        SetStencilReference,

        SetBuffer,
        SetProperties,

        UploadBuffer,
        UploadTexture,

        CopyBuffer,
        CopyTexture,
    }

    internal enum RecCommandContextType : byte
    {
        StateChange = 0,    //Changes the state (Viewports, Buffers, Outputs, etc)
        Execution,          //Flushes the state and executes a command (Draw, Dispatch, etc)
        Modification        //Modifies a resource (Clear, Copy, Barrier, etc)
    }

    internal enum RecCommandEffectFlags : byte
    {
        None = 0,

        ColorTarget = 1 << 0,
        DepthStencilTarget = 1 << 1,

        Viewport = 1 << 2,
        Scissor = 1 << 3,

        StencilRef = 1 << 4,

        VertexBuffer = 1 << 5,
        IndexBuffer = 1 << 6,
    }
}
