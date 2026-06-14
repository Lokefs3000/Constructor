using Primary.Common.Memory;
using Primary.Rendering.Commands;
using Primary.Rendering.Pass;
using Primary.Rendering.State;
using Primary.Rendering.Structures;

namespace Primary.Rendering.Recording
{
    public sealed class ComputePassContext : IPassContext
    {
        private readonly RenderPassErrorReporter _errorReporter;
        private readonly LinearBlockAllocator _intermediateAllocator;
        private readonly FrameGraphResources _resources;
        private readonly ComputeState _state;

        private RenderPassStateData? _stateData;
        private CommandRecorder? _recorder;
        private RenderContextContainer? _contextContainer;

        internal ComputePassContext(RenderPassErrorReporter errorReporter, LinearBlockAllocator intermediateAllocator, FrameGraphResources resources, ComputeState state)
        {
            _errorReporter = errorReporter;
            _intermediateAllocator = intermediateAllocator;
            _resources = resources;
            _state = state;

            _stateData = null;
            _recorder = null;
            _contextContainer = null;
        }

        internal void SetupContext(RenderPassStateData stateData, CommandRecorder recorder, RenderContextContainer contextContainer)
        {
            _stateData = stateData;
            _recorder = recorder;
            _contextContainer = contextContainer;
        }

        public ComputeCommandBuffer CommandBuffer => new ComputeCommandBuffer(new CSCommandBuffer(_errorReporter, _stateData!, _recorder!, _intermediateAllocator, _resources, _state));
        public RenderContextContainer Container => _contextContainer!;
    }
}
