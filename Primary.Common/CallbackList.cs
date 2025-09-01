using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Common
{
    public sealed class CallbackList<T> where T : Delegate
    {
        private readonly List<T> _subscribers;

        public CallbackList()
        {
            _subscribers = new List<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Subscribe(T @delegate)
        {
            if (!_subscribers.Contains(@delegate))
            {
                _subscribers.Add(@delegate);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unsubscribe(T @delegate)
        {
            _subscribers.Remove(@delegate);
        }
    }
}
