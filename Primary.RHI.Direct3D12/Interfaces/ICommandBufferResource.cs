using Primary.RHI.Direct3D12.Helpers;
using Vortice.Direct3D12;

namespace Primary.RHI.Direct3D12.Interfaces
{
    internal interface ICommandBufferResource
    {
        public bool IsShaderVisible { get; }
        public ResourceType Type { get; }
        public string ResourceName { get; }
        public CpuDescriptorHandle CpuDescriptor { get; }
        public ResourceStates GenericState { get; }
        public ResourceStates CurrentState { get; }

        public void EnsureResourceStates(ResourceBarrierManager manager, ResourceStates requiredStates, bool toggle = false);
        public void SetImplicitResourcePromotion(ResourceStates state);
        public void TransitionImmediate(ID3D12GraphicsCommandList7 commandList, ResourceStates newState);
    }
}
