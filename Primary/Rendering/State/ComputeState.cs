using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering.Commands;
using Primary.Rendering.Datatypes;
using Primary.Rendering.Recording;
using Primary.Rendering.Statistics;
using Primary.RHI;

namespace Primary.Rendering.State
{
    internal sealed class ComputeState : RenderState
    {
        private readonly FrameGraph _frameGraph;

        private DirtyValue<int> _shader;

        internal ComputeState(FrameGraph frameGraph) : base(frameGraph)
        {
            _frameGraph = frameGraph;

            _shader = new DirtyValue<int>(-1);
        }

        protected override void DisposeInternal(bool disposing) { }

        internal override void ClearState()
        {
            base.ClearState();

            _shader.Reset(-1);
        }

        internal override void SoftResetForNextPass()
        {
            base.SoftResetForNextPass();

            _shader.Value = -1;
        }

        internal override bool TryCommitState(ref RenderPassStats stats, BarrierGroup barrierGroup, DescriptorAllocator descriptorAllocator, CommandRecorder commandRecorder, ObjectsCache objectsCache)
        {
            if (_shader.Value == -1)
                return false;

            RHIComputePipeline shader = objectsCache.RetrieveObject<RHIComputePipeline>(_shader.Value);

            if (_shader.IsDirty)
            {
                if (_shader.IsDirty)
                    SetPipelineLimits(shader);

                commandRecorder.RecordCommand(CmdCommandType.SetPipeline, new CmdSetPipeline { PipelineIndex = _shader.Value });
                _shader.IsDirty = false;
            }

            return base.TryCommitState(ref stats, barrierGroup, descriptorAllocator, commandRecorder, objectsCache);
        }

        internal void SetShader(int shaderIndex) => _shader.Value = shaderIndex;
    }
}
