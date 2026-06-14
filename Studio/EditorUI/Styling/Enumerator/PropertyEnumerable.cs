using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using EditorUI.Reflection;
using EditorUI.Reflection.Cache;
using Primary.Common;

namespace EditorUI.Styling.Enumerator
{
    public readonly record struct PropertyEnumerable(WidgetCachedData CachedData, ushort TriggerMask, ushort RefTriggerMask) : IEnumerable<StyleKey>
    {
        public Enumerator GetEnumerator() => new Enumerator(CachedData, TriggerMask, RefTriggerMask);

        IEnumerator<StyleKey> IEnumerable<StyleKey>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public record struct Enumerator : IEnumerator<StyleKey>
        {
            private readonly WidgetCachedData _cachedData;
            private readonly int _mask;

            private readonly bool _returnAll;

            private int _index;

            private StyleKey _value;

            public Enumerator(WidgetCachedData cachedData, ushort triggerMask, ushort refTriggerMask)
            {
                _cachedData = cachedData;
                _mask = triggerMask | refTriggerMask;

                _returnAll = Flags.HasFlag(triggerMask, StylesheetContext.ReturnAll);

                _index = cachedData.Properties.IsEmpty ? -1 : 0;

                _value = default;
            }

            public void Dispose()
            {
                _index = -1;

                _value = default;
            }

            public bool MoveNext()
            {
                if (_index == -1)
                {
                    _value = default;
                    return false;
                }

                while (_index < _cachedData.Properties.Length)
                {
                    PropertyData propertyData = _cachedData.Properties[_index++];
                    if (!propertyData.Flags.HasFlags(PropertyDataFlags.IsEditable) && (_returnAll || (_mask == 0 ? propertyData.TriggerMask == 0 : Flags.HasEither(propertyData.TriggerMask, _mask))))
                    {
                        _value = new StyleKey(propertyData.Name, propertyData.TriggerMask);

                        if (_index == _cachedData.Properties.Length)
                            _index = -1;
                        return true;
                    }
                }

                _index = -1;
                _value = default;

                return false;
            }

            public void Reset()
            {
                _index = _cachedData.Properties.IsEmpty ? -1 : 0;

                _value = default;
            }

            public readonly StyleKey Current => _value;
            readonly object IEnumerator.Current => Current;
        }
    }
}
