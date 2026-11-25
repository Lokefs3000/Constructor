using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Threading.Scheduling
{
    internal sealed class MethodJob : IJob
    {
        private JobTemplatePool<MethodJob>? _pool;
        private Action? _methodToExecute;

        public MethodJob()
        {
            _pool = null;
            _methodToExecute = null;
        }

        internal void SetMethod(Action method, JobTemplatePool<MethodJob> pool)
        {
            _methodToExecute = method;
            _pool = pool;
        }

        void IJob.Execute()
        {
            try
            {
                _methodToExecute?.Invoke();
            }
            finally
            {
                _methodToExecute = null;
                _pool!.Return(this);
            }
        }
    }
}
