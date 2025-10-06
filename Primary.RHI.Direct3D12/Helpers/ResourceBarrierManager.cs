using CommunityToolkit.HighPerformance;
using Primary.Common;
using System.Buffers;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace Primary.RHI.Direct3D12.Helpers
{
    internal unsafe sealed class ResourceBarrierManager
    {
        private List<PendingBarrierTransition> _pendingTransitions;

        internal ResourceBarrierManager()
        {
            _pendingTransitions = new List<PendingBarrierTransition>();
        }

        internal void FlushPendingTransitions(ID3D12GraphicsCommandList7 commandList)
        {
            if (_pendingTransitions.Count == 0)
                return;

            using PoolArray<ResourceBarrier> barriers = ArrayPool<ResourceBarrier>.Shared.Rent(_pendingTransitions.Count);
            Span<PendingBarrierTransition> pending = _pendingTransitions.AsSpan();

            for (int i = 0; i < pending.Length; i++)
            {
                ref PendingBarrierTransition transition = ref pending[i];
                barriers[i] = new ResourceBarrier(new ResourceTransitionBarrier(transition.Resource, transition.StateBefore, transition.StateAfter, transition.Subresource), transition.Flags);

                *(ResourceStates*)transition.StateToModify.ToPointer() = transition.StateAfter;
            }

            commandList.ResourceBarrier(barriers.AsSpan(0, pending.Length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ClearPendingTransitions()
        {
            _pendingTransitions.Clear();
        }

        //TODO: improve speed of this method and replace the Linear-search algorithm
        internal void AddTransition(ID3D12Resource resource, ResourceStates before, ResourceStates after, ref ResourceStates modifyWhenComplete, uint subresource = D3D12.ResourceBarrierAllSubResources, ResourceBarrierFlags flags = ResourceBarrierFlags.None)
        {
            ExceptionUtility.Assert(before != after);

            nint stateToModify = (nint)Unsafe.AsPointer(ref modifyWhenComplete);

            Span<PendingBarrierTransition> transitions = _pendingTransitions.AsSpan();
            for (int i = 0; i < _pendingTransitions.Count; i++)
            {
                ref PendingBarrierTransition pending = ref transitions[i];
                if (pending.Resource == resource)
                {
                    transitions[i] = new PendingBarrierTransition(resource, subresource, before, after, flags, stateToModify);
                    return;
                }
            }

            _pendingTransitions.Add(new PendingBarrierTransition(resource, subresource, before, after, flags, stateToModify));
        }

        private readonly record struct PendingBarrierTransition(ID3D12Resource Resource, uint Subresource, ResourceStates StateBefore, ResourceStates StateAfter, ResourceBarrierFlags Flags, nint StateToModify);
    }
}
