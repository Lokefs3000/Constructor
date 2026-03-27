using Editor.UI.Elements;
using Primary.Common;
using Primary.Profiling;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Styling
{
    public sealed class StyleUpdater
    {
        private HashSet<StyleBase> _invalidStyleBases;

        internal StyleUpdater()
        {
            _invalidStyleBases = new HashSet<StyleBase>();
        }

        internal void UpdateAll(string? profilingName = null)
        {
            if (_invalidStyleBases.Count > 0)
            {
                using (new ProfilingScope(profilingName ?? "UpdateStyles"))
                {
                    using RentedArray<(UIElement, UIStateFlags)> flags = RentedArray<(UIElement, UIStateFlags)>.Rent(_invalidStyleBases.Count, true);

                    (UIElement, UIStateFlags)[] internalArray = flags.BackingArray;
                    Parallel.ForEach(_invalidStyleBases, (x, _, i) => internalArray[i] = ((UIElement)x, x.UpdateInvalidProperties()));

                    foreach ((UIElement element, UIStateFlags state) in flags.Span)
                    {
                        if (state > UIStateFlags.None)
                            element.AddStateFlags(state);
                    }

                    _invalidStyleBases.Clear();
                }
            }
        }

        internal void AddInvalidStyleBase(StyleBase styleBase)
        {
            _invalidStyleBases.Add(styleBase);
        }

        internal void RemoveInvalidStyleBase(StyleBase styleBase)
        {
            _invalidStyleBases.Remove(styleBase);
        }

        public bool HasInvalidStyleBases => _invalidStyleBases.Count > 0;
    }
}
