using System;
using System.Collections.Generic;
using System.Text;
using Primary.Common;

namespace EditorUI.Scheduling
{
    public sealed class ActionScheduler
    {
        private List<ScheduledAction> _actions;

        internal ActionScheduler()
        {
            _actions = new List<ScheduledAction>();
        }

        internal void FlushScheduledActions()
        {
            if (_actions.Count > 0)
            {
                using RentedArray<ScheduledAction> actions = RentedArray<ScheduledAction>.Rent(_actions.Count);
                _actions.CopyTo(actions.Span);

                actions.Span.Sort(static (x, y) => y.Priority.CompareTo(x.Priority));

                foreach (ScheduledAction action in _actions)
                {
                    if (action.Key != null)
                        ((Action<object, object?>)action.Action)(action.Key, action.UserData);
                    else
                        ((Action<object?>)action.Action)(action.UserData);
                }

                _actions.Clear();
            }
        }

        public void Schedule(Action<object?> action, int priority, object? userData)
        {
            _actions.Add(new ScheduledAction(action, null, userData, priority));
        }

        public void Schedule(Action<object?> action, object? userData) => Schedule(action, KnownPriorities.Default, null);

        public bool TryScheduleUnique(object key, Action<object, object?> action, int priority, object? userData)
        {
            for (int i = 0; i < _actions.Count; i++)
            {
                object? currentKey = _actions[i].Key;
                if (currentKey != null && currentKey.Equals(key))
                {
                    return false;
                }
            }

            _actions.Add(new ScheduledAction(action, key, userData, priority));
            return true;
        }

        public bool TryScheduleUnique(object key, Action<object, object?> action, object? userData) => TryScheduleUnique(key, action, KnownPriorities.Default, userData);

        private readonly record struct ScheduledAction(object Action, object? Key, object? UserData, int Priority);
    }
}
