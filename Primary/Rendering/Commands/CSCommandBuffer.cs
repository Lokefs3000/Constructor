using Primary.Common.Memory;
using Primary.Rendering.Pass;
using Primary.Rendering.Recording;
using Primary.Rendering.State;
using Primary.RHI2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Primary.Rendering.Commands
{
    internal class CSCommandBuffer : CommandBuffer
    {
        public CSCommandBuffer(RenderPassErrorReporter errorReporter, RenderPassStateData stateData, CommandRecorder recorder, LinearBlockAllocator intermediateAllocator, FrameGraphResources resources, RenderState state) : base(errorReporter, stateData, recorder, intermediateAllocator, resources, state)
        {
        }

        public void SetPipeline(RHIComputePipeline pipeline)
        {
            int index = _resources.AddPotentialPipeline(pipeline);

            Compute.SetPipeline(index);
            Compute.SetPipelineLimits(pipeline);
        }

        public void Dispatch(uint threadGroupX, uint threadGroupY, uint threadGroupZ)
        {
            if (Compute.CommitState(_intermediateAllocator, _recorder))
            {
                _recorder.AddCommand(RecCommandType.Dispatch, new CmdDispatch
                {
                    ThreadGroupSizeX = threadGroupX,
                    ThreadGroupSizeY = threadGroupY,
                    ThreadGroupSizeZ = threadGroupZ
                });
            }
        }

        private ComputeState Compute => Unsafe.As<ComputeState>(_state);
    }
}
