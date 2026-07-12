using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using EditorUI.Reflection;
using EditorUI.Reflection.Cache;
using Primary.Common;

namespace EditorUI.Styling.Enumerator
{
    public readonly record struct PropertyEnumerable(WidgetCachedData CachedData, ushort TriggerMask, ushort RefTriggerMask) : IEnumerable<StylePropertyData>
    {
        public Enumerator GetEnumerator() => new Enumerator(CachedData, TriggerMask, RefTriggerMask);

        IEnumerator<StylePropertyData> IEnumerable<StylePropertyData>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public record struct Enumerator : IEnumerator<StylePropertyData>
        {
            private readonly WidgetCachedData _cachedData;

            private readonly int _currentMask;
            private readonly int _referenceMask;

            private readonly bool _returnAll;

            private int _index;
            private ushort _triggerMask;

            private StylePropertyData _value;

            public Enumerator(WidgetCachedData cachedData, ushort triggerMask, ushort refTriggerMask)
            {
                _cachedData = cachedData;

                _currentMask = triggerMask;
                _referenceMask = refTriggerMask;

                _returnAll = Flags.HasFlag(refTriggerMask, StylesheetContext.ReturnAll);

                _index = cachedData.Properties.IsEmpty ? -1 : 0;
                _triggerMask = triggerMask;

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

                    if (propertyData.Methods.SetDirect != null && !propertyData.Flags.HasFlag(PropertyDataFlags.IsEditable)/* &&
                        (_returnAll || (_mask == 0 ? propertyData.TriggerMask == -1 : Flags.HasEither(propertyData.TriggerMask, _mask)))*/)
                    {
                        _value = new StylePropertyData(propertyData.Name, propertyData.StyleFriendlyName, _triggerMask);

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

            public readonly StylePropertyData Current => _value;
            readonly object IEnumerator.Current => Current;
        }
    }

    public readonly record struct StylePropertyData(string PropertyName, string StyleName, ushort TriggerMask)
    {
        public StyleKey AsStyleKey() => new StyleKey(StyleName);
    }
}
