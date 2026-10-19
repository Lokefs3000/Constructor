using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Utility
{
    internal sealed class TransientEventList<T>
    {
        private readonly List<Action<T>> _actions;

        internal TransientEventList()
        {
            _actions = new List<Action<T>>();
        }

        internal void Subscribe(Action<T> action)
        {
            _actions.Add(action);
        }

        internal void InvokeAndClear(T obj)
        {
            foreach (Action<T> action in _actions)
            {
                try
                {
                    action(obj);
                }
                catch (Exception ex)
                {
                    EngLog.Render.Warning(ex, "Subscriber in event list threw an exception");
                }
            }

            _actions.Clear();
        }
    }
}
