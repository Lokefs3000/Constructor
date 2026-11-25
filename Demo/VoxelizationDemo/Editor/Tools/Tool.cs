using System;
using System.Collections.Generic;
using System.Text;

namespace VoxelizationDemo.Editor.Tools
{
    internal abstract class Tool
    {
        protected bool _isActive;
        protected bool _isBusy;

        public Tool()
        {
            _isActive = false;
            _isBusy = false;
        }

        public virtual void ActiveSelf()
        {
            _isActive = true;
        }

        public virtual void DeactivateSelf()
        {
            _isActive = false;
        }

        public abstract void Update();
        public abstract void Finish();
        public abstract void Cancel();

        public abstract void TryUseTool();

        public bool IsActive => _isActive;
        public bool IsBusy => _isBusy;
    }
}
