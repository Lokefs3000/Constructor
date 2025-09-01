using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Editor
{
    public sealed class DebugDataContainer
    {
        private List<object> _data;

        public DebugDataContainer()
        {
            _data = new List<object>();
        }

        internal void Clear()
        {
            _data.Clear();
        }

        internal void AddData(object data)
        {
            _data.Add(data);
        }

        public IReadOnlyList<object> Data => _data;
    }
}
