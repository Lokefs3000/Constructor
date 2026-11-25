using Primary.Common.Memory;
using Primary.Rendering.Ancient;
using Primary.Rendering.Ancient.Pass;
using Primary.Rendering.Ancient.State;
using Primary.Rendering.Ancient.Structures;
using Primary.Rendering.Commands;

namespace Primary.Rendering.Ancient.Recording
{
    public sealed class RasterPassContext : IPassContext
    {
        private readonly RenderPassErrorReporter _errorReporter;
        private readonly LinearBlockAllocator _intermediateAllocator;
        private readonly FrameGraphResources _resources;
        private readonly RasterState _state;

        private RenderPassStateData? _stateData;
        private CommandRecorder? _recorder;
        private RenderContextContainer? _contextContainer;

        internal RasterPassContext(RenderPassErrorReporter errorReporter, LinearBlockAllocator intermediateAllocator, FrameGraphResources resources, RasterState state)
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

        public RasterCommandBuffer CommandBuffer => new RasterCommandBuffer(new RSCommandBuffer(_errorReporter, _stateData!, _recorder!, _intermediateAllocator, _resources, _state));
        public RenderContextContainer Container => _contextContainer!;
    }
}
