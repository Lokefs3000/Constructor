using System.Runtime.CompilerServices;

namespace Primary.Rendering
{
    public sealed class RenderPassBlackboard
    {
        private Dictionary<Type, IBlackboardData> _blackboard;

        internal RenderPassBlackboard()
        {
            _blackboard = new Dictionary<Type, IBlackboardData>();
        }

        internal void EraseBlackboards()
        {
            foreach (var kvp in _blackboard)
                kvp.Value.Clear();
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
