using Primary.Common;
using Primary.Interop;
using Primary.RHI.Direct3D12.Helpers;
using Primary.RHI.Direct3D12.Interfaces;
using Primary.RHI.Direct3D12.Memory;
using Primary.RHI.Direct3D12.Utility;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Vortice.Direct3D12;

namespace Primary.RHI.Direct3D12
{
    internal unsafe sealed class CopyCommandBufferImpl : CopyCommandBuffer, ICommandBufferImpl
    {
        private readonly GraphicsDeviceImpl _device;

        private ID3D12CommandAllocator? _allocator;
        private ID3D12GraphicsCommandList7 _commandList;

        private ID3D12Fence _fence;
        private ulong _fenceValue;
        private ManualResetEventSlim _fenceEvent;

        private DynamicUploadHeap _uploadHeap;
        private ResourceBarrierManager _barrierManager;

        private bool _isOpen;
        private bool _isReady;

        private Dictionary<ICommandBufferMappable, SimpleMapInfo> _mappedResources;
        private Dictionary<TextureImpl, TextureMapInfo> _mappedTextures;

        private HashSet<ICommandBufferResource> _referencedResources;

        private bool _disposedValue;

        internal CopyCommandBufferImpl(GraphicsDeviceImpl device)
        {
            const ulong UploadHeapInitialSize = 1048576; //1mb

            _device = device;

            //ResultChecker.ThrowIfUnhandled(_device.D3D12Device.CreateCommandAllocator(CommandListType.Copy, out _allocator!), device);
            ResultChecker.ThrowIfUnhandled(_device.D3D12Device.CreateCommandList1(CommandListType.Copy, CommandListFlags.None, out _commandList!), device);

            //_allocator.Name = "CopyAlloc";
            _commandList.Name = "CopyCmd";

            ResultChecker.ThrowIfUnhandled(_device.D3D12Device.CreateFence(0, FenceFlags.None, out _fence!));
            _fenceValue = 0;
            _fenceEvent = new ManualResetEventSlim(false);

            _uploadHeap = new DynamicUploadHeap(device, true, UploadHeapInitialSize);
            _barrierManager = new ResourceBarrierManager();

            _mappedResources = new Dictionary<ICommandBufferMappable, SimpleMapInfo>();
            _mappedTextures = new Dictionary<TextureImpl, TextureMapInfo>();

            _referencedResources = new HashSet<ICommandBufferResource>();

            _isOpen = false;
            _isReady = true;

            _device.AddCommandBuffer(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (_isOpen)
                    End();

                if (disposing)
                {
                    _uploadHeap.Dispose();
                }

                _device.EnqueueDataFree(() =>
                {
                    _device.RemoveCommandBuffer(this);

                    _fenceEvent.Dispose();
                    _fence.Dispose();
                    _commandList.Dispose();
                });

                _disposedValue = true;
            }
        }

        ~CopyCommandBufferImpl()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void SubmitToQueueInternal(ID3D12Fence? fenceToWaitFor, ulong valueToWaitFor)
        {
            if (_isOpen)
            {
                GraphicsDeviceImpl.Logger.Warning("Cannot submit open command buffer!");
                return;
            }

            if (_isReady)
            {
                return;
            }

            if (fenceToWaitFor != null)
                _device.CopyCommandQueue.Wait(fenceToWaitFor, valueToWaitFor);

            /*if (_fence.CompletedValue < _fenceValue)
            {
                GraphicsDeviceImpl.Logger.Information("C Waiting on fence: {a} -> {b}", _fence.CompletedValue, _fenceValue);

                ExceptionUtility.Assert(_fenceEvent.Wait(2000));
                _fenceEvent.Reset();
            }*/

            //GraphicsDeviceImpl.Logger.Information("C Submitting self..");

            //ResultChecker.PrintIfUnhandled(_fence.SetEventOnCompletion(_fenceValue), _device);

            _device.CopyCommandQueue.ExecuteCommandList(_commandList);
            ResultChecker.PrintIfUnhandled(_device.CopyCommandQueue.Signal(_fence, ++_fenceValue));

            _isReady = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetFrameData()
        {
            _uploadHeap.FinishFrame(_fenceValue + 1, _fence.CompletedValue);
        }

        public override bool Begin()
        {
            if (_isOpen)
            {
                GraphicsDeviceImpl.Logger.Warning("Command buffer is already open!");
                return false;
            }

            if (!_isReady)
            {
                GraphicsDeviceImpl.Logger.Error("Command buffer is not ready to begin!");
                return false;
            }

            _allocator = _device.GetNewCommandAllocator(CommandListType.Copy);

            try
            {
                _allocator.Reset();
                _commandList.Reset(_allocator);
            }
            catch (SharpGenException ex)
            {
                ResultChecker.PrintIfUnhandled(ex.ResultCode, _device);
                return false;
            }

            _referencedResources.Clear();
            _mappedResources.Clear();

            _isOpen = true;
            return true;
        }

        public override void End()
        {
            if (!_isOpen)
            {
                GraphicsDeviceImpl.Logger.Warning("Cannot end closed command list!");
                return;
            }

            //_barrierManager.ClearPendingTransitions();
            //
            //foreach (ICommandBufferResource resource in _referencedResources)
            //{
            //    resource.EnsureResourceStates(_barrierManager, ResourceStates.Common);
            //}
            //
            //_barrierManager.FlushPendingTransitions(_commandList);

            try
            {
                _commandList.Close();
            }
            catch (SharpGenException ex)
            {
                ResultChecker.PrintIfUnhandled(ex.ResultCode, _device);
            }

            _referencedResources.Clear();
            _mappedResources.Clear();

            _device.ReturnCommandAllocator(CommandListType.Copy, _allocator!);

            _isOpen = false;
            _isReady = false;
            return;
        }

        public override nint Map(Buffer buffer, MapIntent intent, ulong dataSize, ulong writeOffset)
        {
            PerformSanityCheck();

            //TODO: add checks here

            ICommandBufferMappable mappable = (ICommandBufferMappable)buffer;
            if (_mappedResources.TryGetValue(mappable, out SimpleMapInfo mapInfo))
                return mapInfo.Allocation.CpuAddress;

            if (dataSize > mappable.TotalSizeInBytes)
            {
                if (_device.CmdBufferValidation)
                    GraphicsDeviceImpl.Logger.Error("Map: {arg} ({val}) is larger then the buffer begin mapped (buffer size: {sz}).", nameof(dataSize), dataSize, mappable.TotalSizeInBytes);

                return nint.Zero;
            }

            if (writeOffset + dataSize > mappable.TotalSizeInBytes)
            {
                if (_device.CmdBufferValidation)
                    GraphicsDeviceImpl.Logger.Error("Map: {arg} ({val}) is too large for the specified data size (data size: {sz2}, buffer size: {sz}).", nameof(writeOffset), writeOffset, dataSize, mappable.TotalSizeInBytes);

                return nint.Zero;
            }

            mapInfo = new SimpleMapInfo(_uploadHeap.Allocate(dataSize == 0 ? mappable.TotalSizeInBytes : dataSize), writeOffset);

            if (mapInfo.Allocation.Buffer != null)
            {
                _mappedResources.TryAdd(mappable, mapInfo);
                return mapInfo.Allocation.CpuAddress;
            }
            else
                return nint.Zero;
        }

        public override nint Map(Texture texture, MapIntent intent, TextureLocation location, uint subresource, uint rowPitch)
        {
            PerformSanityCheck();

            TextureImpl impl = (TextureImpl)texture;
            if (_mappedTextures.TryGetValue(impl, out TextureMapInfo mapInfo))
            {
                if (!mapInfo.Location.Equals(location))
                {
                    if (_device.CmdBufferValidation)
                    {
                        GraphicsDeviceImpl.Logger.Error("Map: Texture is already mapped and new location does not match old.");
                    }

                    return nint.Zero;
                }

                if (mapInfo.Subresource != subresource)
                {
                    if (_device.CmdBufferValidation)
                    {
                        //TODO: change in accordance to the logged message
                        GraphicsDeviceImpl.Logger.Error("Map: Texture is already mapped and new subresource does not match old (this is a stupid requirment i need to change).");
                    }

                    return nint.Zero;
                }

                if (mapInfo.RowPitch != rowPitch)
                {
                    if (_device.CmdBufferValidation)
                    {
                        GraphicsDeviceImpl.Logger.Error("Map: Texture is already mapped and new row pitch does not match old.");
                    }

                    return nint.Zero;
                }

                return mapInfo.Allocation.CpuAddress;
            }

            FormatInfo info = FormatStatistics.Query(impl.Description.Format);
            ulong dataSize = (ulong)info.BytesPerPixel * (location.Width * location.Height * location.Depth);

            if (rowPitch == 0)
                rowPitch = (uint)info.BytesPerPixel * location.Width;

            if (mapInfo.Allocation.Buffer != null)
            {
                _mappedTextures.TryAdd(impl, mapInfo);
                return mapInfo.Allocation.CpuAddress;
            }
            else
                return nint.Zero;
        }

        public override void Unmap(Resource resource)
        {
            PerformSanityCheck();

            ICommandBufferMappable mappable = (ICommandBufferMappable)resource;
            if (_mappedResources.TryGetValue(mappable, out SimpleMapInfo allocation))
            {
                //mappable.EnsureResourceStates(_barrierManager, ResourceStates.CopyDest);
                //_barrierManager.FlushPendingTransitions(_commandList);
                mappable.MappableCopyDataTo(_commandList, ref allocation);
                _mappedResources.Remove(mappable);
            }
            else if (mappable is TextureImpl texture && _mappedTextures.TryGetValue(texture, out TextureMapInfo mapInfo))
            {
                //mappable.EnsureResourceStates(_barrierManager, ResourceStates.CopyDest);
                //_barrierManager.FlushPendingTransitions(_commandList);
                texture.MappableCopyTextureDataTo(_commandList, ref mapInfo);
                _mappedTextures.Remove(texture);
            }
        }

        public override bool CopyBufferRegion(Buffer src, uint srcOffset, Buffer dst, uint dstOffset, uint size)
        {
            PerformSanityCheck();

            if (size > dst.Description.ByteWidth || size > src.Description.ByteWidth)
                return false;
            if (srcOffset + size > src.Description.ByteWidth || dstOffset + size > dst.Description.ByteWidth)
                return false;

            BufferImpl srcImpl = (BufferImpl)src;
            BufferImpl dstImpl = (BufferImpl)dst;

            //srcImpl.EnsureResourceStates(_barrierManager, ResourceStates.CopySource);
            //dstImpl.EnsureResourceStates(_barrierManager, ResourceStates.CopyDest);

            //_barrierManager.FlushPendingTransitions(_commandList);

            //ok some funny stuff i going on here so im leaving a comment for future me:
            //  the call to "CopyBufferRegion" is implicilty converting the two from "Common" to "CopySource"/"CopyDest" automaticly.
            //  and when the command list ends with automaticly converts them back to "Common" when used in a "Copy" queue.
            //  no sync is needed for the beginning cause the rhi guarantees they start and end in "Common".
            //  
            //  yeah its somewhat confusing

            srcImpl.CopyToBuffer(_commandList, dstImpl, srcOffset, dstOffset, size);
            return true;
        }

        public override bool CopyTextureRegion(Resource src, TextureLocation srcLoc, uint srcSubRes, Resource dst, TextureLocation dstLoc, uint dstSubRes)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void BeginEvent(Color32 color, ReadOnlySpan<char> name)
        {
            if (_device.IsPixEnabled)
            {
                PIX.PIXBeginEventOnCommandList(_commandList.NativePointer, color.ARGB, name.ToString());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void EndEvent()
        {
            if (_device.IsPixEnabled)
            {
                PIX.PIXEndEventOnCommandList(_commandList.NativePointer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void SetMarker(Color32 color, ReadOnlySpan<char> name)
        {
            if (_device.IsPixEnabled)
            {
                PIX.PIXSetMarkerOnCommandList(_commandList.NativePointer, color.ARGB, name.ToString());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PerformSanityCheck()
        {
            ExceptionUtility.Assert(_isOpen && _isReady);
        }

        public override string Name { set => _commandList.Name = value; }

        public override bool IsOpen => _isOpen;
        public override bool IsReady => _isReady;

        public override CommandBufferType Type => CommandBufferType.Copy;

        public ID3D12Fence ExecutionCompleteFence => _fence;
        public ulong FenceValue => _fenceValue;
    }
}
