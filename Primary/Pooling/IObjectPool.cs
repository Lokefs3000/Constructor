using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Pooling
{
    public interface IObjectPool<T> where T : notnull
    {
        public T Get();
        public void Return(T value);

        public void Clear();
        public void TrimExcess();

        public int Capacity { get; }
        public int Count { get; }
    }
}
