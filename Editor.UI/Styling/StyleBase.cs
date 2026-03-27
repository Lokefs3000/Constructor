using CommunityToolkit.HighPerformance;
using Editor.UI.Elements;
using Editor.UI.Reflection;
using Primary.Collections;
using Primary.Utility;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Editor.UI.Styling
{
    public class StyleBase
    {
        private CachedElementData _data;
        private StyleCache _cache;

        private HashSet<StateOverrideKey> _overriden;
        private HashSet<string> _invalid;

        private List<string>? _classes;

        private int _enabledStates;

        private int _currentStateShift;
        private string _currentStateName;

        protected StyleBase()
        {
            _data = UIManager.Instance.ReflectionManager.ElementCache.GetElementData(GetType());
            _cache = UIManager.Instance.ReflectionManager.PropertyCache.GetCache(GetType());

            _overriden = new HashSet<StateOverrideKey>();
            _invalid = new HashSet<string>();

            _classes = null;

            _enabledStates = 0;

            _currentStateShift = -1;
            _currentStateName = "Normal";
        }

        internal void UpdateAllProperties()
        {
            if (_cache.Properties.Length == 0)
                return;

            UIElement? thisAsElement = this as UIElement;
            if (thisAsElement != null)
                thisAsElement.WindowOwner?.StyleUpdater.RemoveInvalidStyleBase(this);

            UIStateFlags flags = UIStateFlags.None;

            _invalid.Clear();

            HashSet<(string, int)> properties = _cache.Properties
                .Where((x) => x.Type == StylePropertyType.Styleable && !_overriden.Contains(new StateOverrideKey(x.Name)))
                .Select((x, i) => (x.Name, Array.FindIndex(_cache.Properties, (x2) => x2.Name == x.Name)))
                .ToHashSet();

            if (properties.Count == 0)
                return;

            StyleProvider provider = Unsafe.As<UIElement>(this).WindowOwner!.StyleProvider;

            List<(string, int)> updates = new List<(string, int)>();

            string[] classList = UIManager.Instance.StyleManager.ClassListCache.GetClassList(GetType());
            foreach (string className in (_classes != null ? classList.Concat(_classes) : classList).Reverse())
            {
                foreach (StylesheetClass @class in provider.GetClassesWithName(className))
                {
                    foreach (var property in properties)
                    {
                        if (@class.TryGetProperty(property.Item1, out object? value))
                        {
                            _cache.Properties[property.Item2].Field.SetValue(this, value);

                            updates.Add(property);
                            flags |= _cache.Properties[property.Item2].Effect;
                        }
                    }

                    if (updates.Count > 0)
                    {
                        foreach (var update in updates)
                        {
                            properties.Remove(update);
                        }

                        if (properties.Count == 0)
                            return;

                        updates.Clear();
                    }
                }
            }

            if (thisAsElement != null)
                thisAsElement.AddStateFlags(flags);
        }

        internal void UpdateProperty(string propertyName, bool ignoreOverride = false)
        {
            if (!ignoreOverride && _overriden.Contains(new StateOverrideKey(propertyName)))
                return;

            StyleProvider provider = Unsafe.As<UIElement>(this).WindowOwner!.StyleProvider;

            StyleProperty property = _cache.FindProperty(propertyName);
            if (property.Field != null && property.Type == StylePropertyType.Styleable)
            {
                string[] classList = UIManager.Instance.StyleManager.ClassListCache.GetClassList(GetType());
                if (provider.TryGetClassValue(propertyName, (_classes != null ? classList.Concat(_classes) : classList).Reverse(), out object? value))
                {
                    property.Field.SetValue(this, value);
                }
                else
                    return;

                bool wasRemoved = _invalid.Remove(propertyName);
                if (this is UIElement element && property.Effect != UIStateFlags.None)
                {
                    element.AddStateFlags(property.Effect);

                    if (wasRemoved)
                        element.WindowOwner?.StyleUpdater.RemoveInvalidStyleBase(this);
                }
            }


        }

        internal UIStateFlags UpdateInvalidProperties()
        {
            UIStateFlags flags = UIStateFlags.None;

            PropertyCache propertyCache = UIManager.Instance.ReflectionManager.PropertyCache;
            StyleProvider provider = Unsafe.As<UIElement>(this).WindowOwner!.StyleProvider;

            using RentedList<string> updated = new RentedList<string>(16);

            int stateMaxIndex = _currentStateShift + 1;

            string[] classList = UIManager.Instance.StyleManager.ClassListCache.GetClassList(GetType());
            foreach (string className in (_classes != null ? classList.Concat(_classes) : classList).Reverse())
            {
                foreach (StylesheetClass @class in provider.GetClassesWithName(className))
                {
                    foreach (string property in _invalid)
                    {
                        Debug.Assert(!_overriden.Contains(new StateOverrideKey(property)));

                        for (int i = stateMaxIndex; i >= 0; --i)
                        {
                            if (@class.TryGetProperty(property, out object? value, _data.StateData.StateNames[i]))
                            {
                                if (propertyCache.TryFindProperty(GetType(), property, out StyleProperty styleProperty))
                                {
                                    styleProperty.Field.SetValue(this, value);
                                    flags |= styleProperty.Effect;

                                    updated.Add(property);
                                }
                                else
                                    UIManager.Logger?.Warning("Failed to find data for invalid property: {p}", property);

                                break;
                            }
                        }
                       
                    }

                    if (updated.Count > 0)
                    {
                        foreach (var update in updated)
                        {
                            _invalid.Remove(update);
                        }

                        if (_invalid.Count == 0)
                            return flags;

                        updated.Clear();
                    }
                }
            }

            if (_invalid.Count > 0)
            {
                updated.Clear();
                foreach (string property in _invalid)
                {
                    if (propertyCache.TryFindProperty(GetType(), property, out StyleProperty styleProperty))
                    {
                        if (styleProperty.HasDefaultValue)
                        {
                            styleProperty.Field.SetValue(this, styleProperty.DefaultValue);
                            flags |= styleProperty.Effect;

                            updated.Add(property);
                        }
                    }
                    else
                        UIManager.Logger?.Warning("Failed to find data for invalid property: {p}", property);
                }

                if (updated.Count > 0)
                {
                    foreach (var update in updated)
                    {
                        _invalid.Remove(update);
                    }

                    if (_invalid.Count == 0)
                        return flags;
                }

                StringBuilder sb = new StringBuilder();
                foreach (string invalid in _invalid)
                {
                    sb.Append(' ', 4);
                    sb.AppendLine(invalid);
                }

                --sb.Length;

                UIManager.Logger?.Warning("Failed to resolve all invalid properties:\n{ls}", sb.ToString());
                _invalid.Clear();
            }

            return flags;
        }

        internal void SetAsOverriden(string propertyName, string stateName = "Normal")
        {
            _overriden.Add(new StateOverrideKey(propertyName, stateName));
        }

        public void AddClass(string className)
        {
            if ((_classes ??= new List<string>()).AddUnique(className))
            {
                InvalidateAll();
            }
        }

        public void RemoveClass(string className)
        {
            if (_classes?.Remove(className) ?? false)
            {
                InvalidateAll();
            }
        }

        public void InvalidateAll(string stateName = "Normal")
        {
            foreach (ref readonly StyleProperty styleProperty in _cache.Properties.AsSpan())
            {
                if (styleProperty.Type == StylePropertyType.Styleable && !_overriden.Contains(new StateOverrideKey(styleProperty.Name)))
                {
                    _invalid.Add(styleProperty.Name);
                }
            }

            if (this is UIElement element)
                element.WindowOwner?.StyleUpdater.AddInvalidStyleBase(this);
        }

        public void ResetProperty(string propertyName, string stateName = "Normal")
        {
            _overriden.Remove(new StateOverrideKey(propertyName, stateName));
            _invalid.Add(propertyName);
        }

        public void SetStyleProperty<T>(in T value, string propertyName, string stateName = "Normal")
        {
            PropertyCache propertyCache = UIManager.Instance.ReflectionManager.PropertyCache;

            if (propertyCache.TryFindProperty(GetType(), propertyName, out StyleProperty property) && property.Type == StylePropertyType.Styleable)
            {
                _overriden.Add(new StateOverrideKey(propertyName, stateName));
                _invalid.Remove(propertyName);

                UIManager.Instance.ReflectionManager.MethodGenerator.GetSetterDelegate<T>(property.Field)(this, value);

                if (this is UIElement element && property.Effect != UIStateFlags.None)
                    element.AddStateFlags(property.Effect);
            }
        }

        public void SetEditableProperty<T>(in T value, string propertyName, string stateName = "Normal")
        {
            PropertyCache propertyCache = UIManager.Instance.ReflectionManager.PropertyCache;

            if (propertyCache.TryFindProperty(GetType(), propertyName, out StyleProperty property) && property.Type == StylePropertyType.Editable)
            {
                _overriden.Add(new StateOverrideKey(propertyName, stateName));
                _invalid.Remove(propertyName);

                UIManager.Instance.ReflectionManager.MethodGenerator.GetSetterDelegate<T>(property.Field)(this, value);

                if (this is UIElement element && property.Effect != UIStateFlags.None)
                    element.AddStateFlags(property.Effect);
            }
        }

        protected void SetStyleProperty<T>(in T value, [CallerMemberName] string callerName = "") => SetStyleProperty(value, callerName, "Normal");
        protected void SetEditableProperty<T>(in T value, [CallerMemberName] string callerName = "") => SetEditableProperty(value, callerName, "Normal");

        /// <summary>NOTE: Enabling/disabling the "Normal" state does not do anything as it is implicit</summary>
        protected void SetState(string stateName, bool enabled)
        {
            if (_data.StateData.StateMap.TryGetValue(stateName, out int shiftValue))
            {
                if (shiftValue != -1)
                {
                    if (enabled)
                    {
                        _enabledStates |= 1 << shiftValue;

                        if (shiftValue > _currentStateShift)
                        {
                            _currentStateShift = shiftValue;
                            _currentStateName = stateName;

                            InvalidateAll(stateName);
                        }
                    }
                    else
                    {
                        _enabledStates &= ~(1 << shiftValue);

                        if (shiftValue == _currentStateShift)
                        {
                            _currentStateShift = 31 - int.LeadingZeroCount(_enabledStates);
                            _currentStateName = _data.StateData.StateNameMap[_currentStateShift];

                            InvalidateAll(_currentStateName);
                        }
                    }
                }
            }
        }

        public bool HasInvalidProperties => _invalid.Count > 0;

        private readonly record struct StateOverrideKey(string Property, string State = "Normal")
        {
            public override int GetHashCode() => Property.GetDjb2HashCode() ^ State.GetDjb2HashCode();
        }
    }
}
