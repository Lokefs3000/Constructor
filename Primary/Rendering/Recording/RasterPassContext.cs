using Primary.Common.Memory;
using Primary.Rendering.Commands;
using Primary.Rendering.Pass;
using Primary.Rendering.State;
using Primary.Rendering.Structures;

namespace Primary.Rendering.Recording
{
    public sealed class RasterPassContext : IPassContext
    {
        private readonly RenderPassErrorReporter _errorReporter;
        private readonly LinearBlockAllocator _intermediateAllocator;
        private readonly FrameGraphResources _resources;
        private readonly RenderContextContainer _contextContainer;
        private readonly RasterState _state;

        private RenderPassStateData? _stateData;
        private CommandRecorder? _recorder;

        internal RasterPassContext(RenderPassErrorReporter errorReporter, LinearBlockAllocator intermediateAllocator, FrameGraphResources resources, RenderContextContainer contextContainer, RasterState state)
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

        public RasterCommandBuffer CommandBuffer => new RasterCommandBuffer(new RSCommandBuffer(_errorReporter, _stateData, _recorder, _intermediateAllocator, _resources, _state));
        public RenderContextContainer Container => _contextContainer;
    }
}
