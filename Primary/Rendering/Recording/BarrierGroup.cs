using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Collections.ReadOnly;
using Primary.Memory;

namespace Primary.Rendering.Recording
{
    internal sealed class BarrierGroup
    {
        private readonly List<BarrierRequest> _barriers;
        private ResourceBarrierState?[] _currentStates;

        internal BarrierGroup()
        {
            _barriers = new List<BarrierRequest>();
            _currentStates = [];
        }

        internal void ClearBarrierData()
        {
            _barriers.Clear();
        }

        internal void SetupForResources(int resourceCount)
        {
            if (_currentStates.Length < resourceCount)
            {
                _currentStates = new ResourceBarrierState?[MemoryHelper.AlignUp(resourceCount, 16)];
            }

            Array.Fill(_currentStates, null, 0, resourceCount);
        }

        internal void RequestBarrier(ResourceHandle handle, int cmdOffset, ResourceBarrierSync sync, ResourceBarrierAccess access, bool alwaysAtEnd = false)
        {
            if (_currentStates.Length <= handle.Index)
            {
                ResourceBarrierState?[] newArray = new ResourceBarrierState?[MemoryHelper.AlignUp(handle.Index + 1, 16)];
                Array.Copy(_currentStates, newArray, _currentStates.Length);

                _currentStates = newArray;
            }

            ResourceBarrierState newBarrierState = new ResourceBarrierState(sync, access);

            ref ResourceBarrierState? barrierState = ref _currentStates[handle.Index];
            if (barrierState.HasValue && barrierState.DangerousGetValueOrNullReference().Equals(newBarrierState))
            {
                // Resource is already in the correct barrier state
                return;
            }

            barrierState = newBarrierState;

            if (alwaysAtEnd)
            {
                goto ImmediateGoToEnd;
            }

            int previousSafeBarrier = _barriers.Count - 1;
            int previousSafeOffset = cmdOffset;

            Span<BarrierRequest> requestsSpan = _barriers.AsSpan();
            for (int i = requestsSpan.Length - 1; i >= 0; i--)
            {
                ref BarrierRequest request = ref requestsSpan[i];
                if (request.Resource == handle)
                {
                    if (request.Sync == sync && request.Access == access)
                    {
                        // The resource should already be transitioned
                        return;
                    }    

                    _barriers.Insert(previousSafeBarrier + 1, new BarrierRequest(handle, previousSafeOffset, sync, access));
                    return;
                }
                else if (request.CommandOffset == 0)
                {
                    _barriers.Insert(i + 1, new BarrierRequest(handle, request.CommandOffset, sync, access));
                    return;
                }

                if (previousSafeOffset != request.CommandOffset)
                {
                    previousSafeBarrier = i;
                    previousSafeOffset = request.CommandOffset;
                }
            }

        ImmediateGoToEnd:

            // This is not batchable with any other request
            _barriers.Add(new BarrierRequest(handle, cmdOffset, sync, access));
        }

        internal ROList<BarrierRequest> BarrierRequests => _barriers;
    }

    internal readonly record struct BarrierRequest(
        ResourceHandle Resource,
        int CommandOffset,
        ResourceBarrierSync Sync,
        ResourceBarrierAccess Access);

    internal readonly record struct ResourceBarrierState(
        ResourceBarrierSync Sync,
        ResourceBarrierAccess Access) : IEquatable<ResourceBarrierState>;
}
