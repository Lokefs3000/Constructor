using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Threading.Scheduling
{
    internal sealed class StructJob<T> : IJob where T : struct, IJob
    {
        private JobTemplatePool<StructJob<T>>? _pool;
        private T _workerToExecute;

        public StructJob()
        {
            _pool = null;
            _workerToExecute = default;
        }

        internal void SetStruct(JobTemplatePool<StructJob<T>> pool, T worker)
        {
            _pool = pool;
            _workerToExecute = worker;
        }

        void IJob.Execute()
        {
            try
            {
                _workerToExecute.Execute();
            }
            finally
            {
                _workerToExecute = default;
                _pool!.Return(this);
            }
        }
    }
}
