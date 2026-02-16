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
        private readonly RenderContextContainer _contextContainer;
        private readonly ComputeState _state;

        private RenderPassStateData? _stateData;
        private CommandRecorder? _recorder;

        internal ComputePassContext(RenderPassErrorReporter errorReporter, LinearBlockAllocator intermediateAllocator, FrameGraphResources resources, RenderContextContainer contextContainer, ComputeState state)
        {
            _errorReporter = errorReporter;
            _intermediateAllocator = intermediateAllocator;
            _resources = resources;
            _contextContainer = contextContainer;
            _state = state;

            _stateData = null;
            _recorder = null;
        }

        internal void SetupContext(RenderPassStateData stateData, CommandRecorder recorder)
        {
            _stateData = stateData;
            _recorder = recorder;
        }

        public ComputeCommandBuffer CommandBuffer => new ComputeCommandBuffer(new CSCommandBuffer(_errorReporter, _stateData, _recorder, _intermediateAllocator, _resources, _state));
        public RenderContextContainer Container => _contextContainer;
    }
}
