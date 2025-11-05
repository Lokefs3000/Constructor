using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2
{
    public sealed class RenderPassBlackboard
    {
        private Dictionary<Type, IBlackboardData> _blackboard;

        internal RenderPassBlackboard()
        {
            _blackboard = new Dictionary<Type, IBlackboardData>();
        }

        public T Add<T>() where T : class, IBlackboardData, new()
        {
            if (!_blackboard.TryGetValue(typeof(T), out IBlackboardData? data))
            {
                data = new T();
                _blackboard.Add(typeof(T), data);
            }

            data.Clear();
            return Unsafe.As<T>(data);
        }

        public T? Get<T>() where T : class, IBlackboardData, new()
        {
            _blackboard.TryGetValue(typeof(T), out IBlackboardData? data);
            return Unsafe.As<T>(data);
        }
    }
}
