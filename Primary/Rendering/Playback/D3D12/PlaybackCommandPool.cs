using System;
using System.Collections.Generic;
using System.Text;
using Primary.Common;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Primary.Rendering.Playback.D3D12
{
    internal unsafe sealed class PlaybackCommandPool : IDisposable
    {
        private readonly PlaybackDevice _device;
        private readonly PlaybackExecutor _executor;

        private readonly List<Ptr<ID3D12GraphicsCommandList10>> _directCommandLists;
        private readonly List<Ptr<ID3D12GraphicsCommandList10>> _computeCommandLists;

        private readonly List<Ptr<ID3D12CommandAllocator>> _directAllocators;
        private readonly List<Ptr<ID3D12CommandAllocator>> _computeAllocators;

        private int _directCommandListIndex;
        private int _computeCommandListIndex;

        private int _directAllocatorIndex;
        private int _computeAllocatorIndex;

        private bool _disposedValue;

        internal PlaybackCommandPool(PlaybackDevice device, PlaybackExecutor executor)
        {
            _device = device;
            _executor = executor;

            _directCommandLists = new List<Ptr<ID3D12GraphicsCommandList10>>();
            _computeCommandLists = new List<Ptr<ID3D12GraphicsCommandList10>>();

            _directAllocators = new List<Ptr<ID3D12CommandAllocator>>();
            _computeAllocators = new List<Ptr<ID3D12CommandAllocator>>();

            _directCommandListIndex = 0;
            _computeCommandListIndex = 0;

            _directAllocatorIndex = 0;
            _computeAllocatorIndex = 0;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                foreach (Ptr<ID3D12GraphicsCommandList10> commandList in _directCommandLists)
                {
                    if (!commandList.IsNull)
                    {
                        commandList.Pointer->Release();
                    }
                }

                foreach (Ptr<ID3D12GraphicsCommandList10> commandList in _computeCommandLists)
                {
                    if (!commandList.IsNull)
                    {
                        commandList.Pointer->Release();
                    }
                }

                foreach (Ptr<ID3D12CommandAllocator> commandAllocator in _directAllocators)
                {
                    if (!commandAllocator.IsNull)
                    {
                        commandAllocator.Pointer->Release();
                    }
                }

                foreach (Ptr<ID3D12CommandAllocator> commandAllocator in _computeAllocators)
                {
                    if (!commandAllocator.IsNull)
                    {
                        commandAllocator.Pointer->Release();
                    }
                }

                _disposedValue = true;
            }
        }

        ~PlaybackCommandPool()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ClearForNewRender()
        {
            _directCommandListIndex = 0;
            _computeCommandListIndex = 0;

            _directAllocatorIndex = 0;
            _computeAllocatorIndex = 0;
        }

        internal ID3D12GraphicsCommandList10* GetDirectCommandList()
        {
            ID3D12GraphicsCommandList10* commandList = null;
            ID3D12CommandAllocator* commandAllocator = null;

            if (_directCommandLists.Count == _directCommandListIndex)
            {
                HResult hr = _device.NativeDevice.Device.CreateCommandList1(
                    0,
                    CommandListType.Direct,
                    CommandListFlags.None,
                    SilkMarshal.GuidPtrOf<ID3D12GraphicsCommandList10>(),
                    (void**)&commandList);

                if (hr.IsFailure)
                {
                    EngLog.Render.Fatal("Failed to create direct graphics command");
                    EngLog.Render.Fatal("    HResult value: {v:x8}", hr.Value);

                    _device.NativeDevice.FlushPendingMessages();
                    EngLog.ThrowFatal();
                }

                _directCommandLists.Add(commandList);
                ++_directCommandListIndex;

                // commandList->Close();
            }
            else
            {
                commandList = _directCommandLists[_directCommandListIndex++];
            }

            if (_directAllocators.Count == _directAllocatorIndex)
            {
                HResult hr = _device.NativeDevice.Device.CreateCommandAllocator(
                    CommandListType.Direct,
                    SilkMarshal.GuidPtrOf<ID3D12CommandAllocator>(),
                    (void**)&commandAllocator);

                if (hr.IsFailure)
                {
                    EngLog.Render.Fatal("Failed to create direct command allocator");
                    EngLog.Render.Fatal("    HResult value: {v:x8}", hr.Value);

                    _device.NativeDevice.FlushPendingMessages();
                    EngLog.ThrowFatal();
                }

                _directAllocators.Add(commandAllocator);
                ++_directAllocatorIndex;
            }
            else
            {
                commandAllocator = _directAllocators[_directAllocatorIndex++];
            }

            commandAllocator->Reset();
            commandList->Reset(commandAllocator, null);

            return commandList;
        }

        internal ID3D12GraphicsCommandList10* GetComputeCommandList()
        {
            ID3D12GraphicsCommandList10* commandList = null;
            ID3D12CommandAllocator* commandAllocator = null;

            if (_computeCommandLists.Count == _computeCommandListIndex)
            {
                HResult hr = _device.NativeDevice.Device.CreateCommandList1(
                    0,
                    CommandListType.Compute,
                    CommandListFlags.None,
                    SilkMarshal.GuidPtrOf<ID3D12GraphicsCommandList10>(),
                    (void**)&commandList);

                if (hr.IsFailure)
                {
                    EngLog.Render.Fatal("Failed to create direct compute command");
                    EngLog.Render.Fatal("    HResult value: {v:x8}", hr.Value);

                    _device.NativeDevice.FlushPendingMessages();
                    EngLog.ThrowFatal();
                }

                _computeCommandLists.Add(commandList);
                ++_computeCommandListIndex;

                // commandList->Close();
            }
            else
            {
                commandList = _computeCommandLists[_computeCommandListIndex++];
            }

            if (_computeAllocators.Count == _computeAllocatorIndex)
            {
                HResult hr = _device.NativeDevice.Device.CreateCommandAllocator(
                    CommandListType.Compute,
                    SilkMarshal.GuidPtrOf<ID3D12CommandAllocator>(),
                    (void**)&commandAllocator);

                if (hr.IsFailure)
                {
                    EngLog.Render.Fatal("Failed to create compute command allocator");
                    EngLog.Render.Fatal("    HResult value: {v:x8}", hr.Value);

                    _device.NativeDevice.FlushPendingMessages();
                    EngLog.ThrowFatal();
                }

                _computeAllocators.Add(commandAllocator);
                ++_computeAllocatorIndex;
            }
            else
            {
                commandAllocator = _computeAllocators[_computeAllocatorIndex++];
            }

            commandAllocator->Reset();
            commandList->Reset(commandAllocator, null);

            return commandList;
        }

        internal void FinishDirectCommandLists() => _directCommandListIndex = 0;
        internal void FinishComputeCommandLists() => _computeCommandListIndex = 0;
    }
}
