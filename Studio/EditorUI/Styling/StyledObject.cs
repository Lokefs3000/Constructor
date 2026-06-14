using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.Diagnostics;
using EditorUI.Reflection;
using EditorUI.Reflection.Cache;
using EditorUI.Styling.Enumerator;
using EditorUI.Utility;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Utility;

namespace EditorUI.Styling
{
    public abstract class StyledObject
    {
        private WidgetCachedData _cachedData;

        private ushort _triggerMask;
        private ushort _updatedTriggerMask;

        private bool _arePropertiesInvalid;

        private List<string>? _classList;

        private HashSet<StyleProperty> _overrideSet;
        private HashSet<StyleProperty> _requiredUpdatesSet;

        public StyledObject()
        {
            _cachedData = WidgetPropertyCache.Instance.GetCachedData(GetType());

            _triggerMask = 0;
            _updatedTriggerMask = 0;

            _arePropertiesInvalid = true;

            _classList = null;

            _overrideSet = new HashSet<StyleProperty>();
            _requiredUpdatesSet = new HashSet<StyleProperty>();
        }

        #region Update
        internal void ResolveInvalidProperties(ref readonly StylesheetContext context)
        {
            if (_arePropertiesInvalid || _triggerMask != _updatedTriggerMask)
            {
                ClassListEnumerable classList = context.GetClassList(_classList ?? ROList<string>.Empty);
                PropertyEnumerable properties = context.GetProperties(_updatedTriggerMask, _arePropertiesInvalid ? StylesheetContext.ReturnAll : (ushort)(_triggerMask ^ _updatedTriggerMask));

                foreach (StyleKey key in properties)
                {
                    if (_cachedData.TryGetPropertyData(key.Property, out PropertyData? propertyData))
                    {
                        if (_overrideSet.Contains(propertyData.Property))
                            continue;

                        foreach (StylesheetClass stylesheetClass in classList)
                        {
                            ClassStyleKey styleKey = new ClassStyleKey(stylesheetClass, key);
                            if (context.Stylesheets.TryGetClassValue(styleKey, propertyData.PropertyType, out object? value))
                            {
                                if (propertyData.Field != null)
                                    propertyData.Field.SetValue(this, value);
                                else
                                    propertyData.Property.SetValue(this, value);

                                if (propertyData.StateFlags != StateFlags.None)
                                {
                                    AddStateFlags(propertyData.StateFlags);

                                    if (propertyData.Flags.HasFlags(PropertyDataFlags.EffectsParent))
                                        ParentObject?.AddStateFlags(propertyData.StateFlags);
                                }

                                break;
                            }
                        }
                    }
                }

                _arePropertiesInvalid = false;
                _triggerMask = _updatedTriggerMask;
            }
        }
        #endregion

        #region Public
        public void ClearOverride(string propertyName)
        {
            if (_cachedData.TryGetPropertyData(propertyName, out PropertyData? propertyData) && !propertyData.Flags.HasFlags(PropertyDataFlags.IsEditable))
            {
                if (_overrideSet.Remove(propertyData.Property))
                {
                    _requiredUpdatesSet.Add(propertyData.Property);
                }
            }
        }

        public bool TryAddClass(string className)
        {
            if ((_classList ??= new List<string>()).AddUnique(className))
                AddStateFlags(StateFlags.SelfInvalidStyle);
            return true;
        }

        public bool TryRemoveClass(string className)
        {
            bool ret = _classList?.Remove(className) ?? false;
            if (ret)
            {
                _arePropertiesInvalid = true;
                AddStateFlags(StateFlags.SelfInvalidStyle);
            }

            return ret;
        }

        public void ClearClassList()
        {
            if (_classList != null && _classList.Count > 0)
            {
                _classList.Clear();

                _arePropertiesInvalid = true;
                AddStateFlags(StateFlags.SelfInvalidStyle);
            }
        }

        public bool TryFocusClass(string className)
        {
            int index = _classList?.IndexOf(className) ?? -1;
            if (index != -1)
            {
                if (index < _classList!.Count - 1)
                {
                    _classList.RemoveAt(index);
                    _classList.Add(className);

                    _arePropertiesInvalid = true;
                    AddStateFlags(StateFlags.SelfInvalidStyle);
                }

                return true;
            }

            return false;
        }

        public bool TryMoveClass(string className, int destinationIndex)
        {
            Guard.IsInRange(destinationIndex, 0, _classList?.Count ?? 0);

            int index = _classList?.IndexOf(className) ?? -1;
            if (index != -1)
            {
                if (index != destinationIndex)
                {
                    (_classList![index], _classList[destinationIndex]) = (_classList[destinationIndex], _classList[index]);

                    _arePropertiesInvalid = true;
                    AddStateFlags(StateFlags.SelfInvalidStyle);
                }

                return true;
            }

            return false;
        }

        public int TryFindClassIndex(string className)
        {
            if (_classList != null)
            {
                for (int i = 0; i < _classList.Count; i++)
                {
                    if (_classList[i] == className)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }
        #endregion

        #region Templated
        public abstract void AddStateFlags(StateFlags flags);

        protected internal abstract StyledObject? ParentObject { get; }
        #endregion

        #region Setters
        private void SetValue<T>(T value, string propertyName, bool setAsOverriden)
        {
            if (_cachedData.TryGetPropertyData(propertyName, out PropertyData? propertyData))
            {
                if (propertyData.Field != null)
                {
                    propertyData.Methods.GetSetFieldDirectUnsafe<T>()(this, in value);
                }
                else
                {
                    propertyData.Methods.GetSetPropertyDirectUnsafe<T>()(this, in value);
                }

                if (propertyData.TriggerMask > 0)
                {
                    Debug.Assert(typeof(T) == typeof(bool));

                    if (BoolUtil<T>.GetAsBoolean(ref value))
                        _updatedTriggerMask |= propertyData.TriggerMask;
                    else
                        _updatedTriggerMask &= (ushort)~propertyData.TriggerMask;

                    AddStateFlags(StateFlags.SelfInvalidStyle);
                }

                if (setAsOverriden && !propertyData.Flags.HasFlags(PropertyDataFlags.IsEditable))
                {
                    _overrideSet.Add(propertyData.Property);
                }

                _requiredUpdatesSet.Remove(propertyData.Property);
            }
        }

        protected void SetStyledField<T>(T value, [CallerMemberName] string propertyName = "") => SetValue(value, propertyName, true);
        protected void SetEditedField<T>(T value, [CallerMemberName] string propertyName = "") => SetValue(value, propertyName, false);
        #endregion

        public ROList<string> ClassList => _classList ?? ROList<string>.Empty;
    }
}
