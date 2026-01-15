using Primary.Rendering.Memory;
using Primary.Rendering.Pass;
using Primary.Rendering.Structures;

namespace Primary.Rendering.Recording
{
    public sealed class ComputePassContext : IPassContext
    {
        private readonly RenderPassErrorReporter _errorReporter;
        private readonly SequentialLinearAllocator _intermediateAllocator;
        private readonly FrameGraphResources _resources;
        private readonly RenderContextContainer _contextContainer;

        private RenderPassStateData? _stateData;
        private CommandRecorder? _recorder;

        internal ComputePassContext(RenderPassErrorReporter errorReporter, SequentialLinearAllocator intermediateAllocator, FrameGraphResources resources, RenderContextContainer contextContainer)
        {
            _errorReporter = errorReporter;
            _intermediateAllocator = intermediateAllocator;
            _resources = resources;
            _contextContainer = contextContainer;

            _stateData = null;
            _recorder = null;
        }

        internal void SetupContext(RenderPassStateData stateData, CommandRecorder recorder)
        {
            _stateData = stateData;
            _recorder = recorder;
        }

        public ComputeCommandBuffer CommandBuffer => new ComputeCommandBuffer(_errorReporter, _stateData ?? throw new NullReferenceException(), _recorder ?? throw new NullReferenceException(), _intermediateAllocator, _resources);
        public RenderContextContainer Container => _contextContainer;
    }
}
