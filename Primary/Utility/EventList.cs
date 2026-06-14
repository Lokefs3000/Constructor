using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Utility
{
    public sealed class EventList<TArgument> where TArgument : allows ref struct
    {
        private List<EventDelegate> _invocationList;

        public EventList()
        {
            _invocationList = new List<EventDelegate>();
        }

        public void Invoke(TArgument arg)
        {
            for (int i = 0; i < _invocationList.Count; ++i)
            {
                _invocationList[i](arg);
            }
        }

        public bool Subscribe(EventDelegate listener) => _invocationList.AddUnique(listener);
        public bool Unsubscribe(EventDelegate listener) => _invocationList.Remove(listener);

        public void operator +=(EventDelegate listener) => Subscribe(listener);
        public void operator -=(EventDelegate listener) => Unsubscribe(listener);

        public delegate void EventDelegate(TArgument arg);
    }

    public readonly record struct ROEventList<TArgument> where TArgument : allows ref struct
    {
        private readonly EventList<TArgument> _list;

        public ROEventList(EventList<TArgument> list)
        {
            _list = list;
        }

        public bool Subscribe(EventList<TArgument>.EventDelegate listener) => _list.Subscribe(listener);
        public bool Unsubscribe(EventList<TArgument>.EventDelegate listener) => _list.Unsubscribe(listener);

        public void operator +=(EventList<TArgument>.EventDelegate listener) => _list.Subscribe(listener);
        public void operator -=(EventList<TArgument>.EventDelegate listener) => _list.Unsubscribe(listener);

        public static implicit operator ROEventList<TArgument>(EventList<TArgument> list) => new ROEventList<TArgument>(list);
    }
}
