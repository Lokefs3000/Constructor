using System;
using System.Collections.Generic;
using System.Text;

namespace VoxelizationDemo.Editor.Popups
{
    public abstract class EditorPopup
    {
        private readonly TaskCompletionSource<object?> _returnTask;

        public EditorPopup()
        {
            _returnTask = new TaskCompletionSource<object?>();
        }

        protected void FinishAndSetValue(object? value = null)
        {
            _returnTask.SetResult(value);
        }

        protected internal void Cancel()
        {
            _returnTask.SetResult(null);
        }

        public abstract bool UpdateAndRender();

        internal ValueTask<object?> Task => new ValueTask<object?>(_returnTask.Task);
    }
}
