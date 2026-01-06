using Primary.Rendering2.Memory;
using Primary.Rendering2.Pass;
using Primary.Rendering2.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Recording
{
    public sealed class RasterPassContext
    {
        private readonly RenderPassErrorReporter _errorReporter;
        private readonly SequentialLinearAllocator _intermediateAllocator;
        private readonly FrameGraphResources _resources;
        private readonly RenderContextContainer _contextContainer;

        private RenderPassStateData? _stateData;
        private CommandRecorder? _recorder;

        internal RasterPassContext(RenderPassErrorReporter errorReporter, SequentialLinearAllocator intermediateAllocator, FrameGraphResources resources, RenderContextContainer contextContainer)
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

        public RasterCommandBuffer CommandBuffer => new RasterCommandBuffer(_errorReporter, _stateData ?? throw new NullReferenceException(), _recorder ?? throw new NullReferenceException(), _intermediateAllocator, _resources);
        public RenderContextContainer Container => _contextContainer;
    }
}
