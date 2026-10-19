using Primary.Assets;
using Primary.Common.Memory;
using Primary.Rendering.Ancient;
using Primary.Rendering.Ancient.Pass;
using Primary.Rendering.Ancient.Recording;
using Primary.Rendering.Ancient.State;
using Primary.Rendering.State;
using Primary.RHI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Primary.Rendering.Ancient.Commands
{
    internal class CSCommandBuffer : CommandBuffer
    {
        public CSCommandBuffer(RenderPassErrorReporter errorReporter, RenderPassStateData stateData, CommandRecorder recorder, LinearBlockAllocator intermediateAllocator, FrameGraphResources resources, RenderState state) : base(errorReporter, stateData, recorder, intermediateAllocator, resources, state)
        {
        }

        public void SetPipeline(ComputeShaderKernel? kernel)
        {
            RHIComputePipeline? pipeline = kernel?.Pipeline;
            if (pipeline == null)
            {
                Compute.SetPipeline(-1);
            }
            else
            {
                int index = _resources.AddPotentialPipeline(pipeline);

                Compute.SetPipeline(index);
                Compute.SetPipelineLimits(pipeline);
            }
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
