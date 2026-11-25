using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Primary.Threading.Scheduling
{
    internal sealed class JobTemplatePool<T> : IJobTemplatePool where T : class, IJob, new()
    {
        private readonly ConcurrentBag<T> _values;

        internal JobTemplatePool()
        {
            _values = new ConcurrentBag<T>();
        }

        internal T Take()
        {
            return _values.TryTake(out T? value) ? value : new T();
        }

        internal void Return(T value)
        {
            _values.Add(value);
        }

        IJob IJobTemplatePool.Take() => Take();
        void IJobTemplatePool.Return(IJob value) => Return((T)value);
    }
}
