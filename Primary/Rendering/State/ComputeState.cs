using Primary.Common.Memory;
using Primary.Rendering.Recording;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.State
{
    internal class ComputeState : RenderState
    {
        private DirtyValue<int> _pipeline;

        protected override void DisposeInternal(bool disposing) { }

        internal override void ClearState()
        {
            base.ClearState();

            _pipeline.Reset(-1);
        }

        internal override void SoftResetForNextPass()
        {
            base.SoftResetForNextPass();

            _pipeline.Reset(-1);
        }

        internal override bool CommitState(LinearBlockAllocator allocator, CommandRecorder recorder)
        {
            if (_pipeline.Value == -1)
                return false;

            if (_pipeline.IsDirty)
            {
                recorder.AddCommand(RecCommandType.SetPipeline, new CmdSetPipeline
                {
                    Index = _pipeline.Value
                });

                _pipeline.IsDirty = false;
            }

            return base.CommitState(allocator, recorder);
        }

        internal void SetPipeline(int pipelineIndex) => _pipeline.Value = pipelineIndex;
    }
}
