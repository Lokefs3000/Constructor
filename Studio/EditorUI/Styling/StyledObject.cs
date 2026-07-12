using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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
            if (_arePropertiesInvalid || _triggerMask != _updatedTriggerMask || context.GetAllProperties)
            {
                ClassListEnumerable classList = context.GetClassList(_classList ?? ROList<string>.Empty);
                PropertyEnumerable properties = context.GetProperties(_updatedTriggerMask, _arePropertiesInvalid ? StylesheetContext.ReturnAll : (ushort)(_triggerMask ^ _updatedTriggerMask));

                // ushort triggerMask = _arePropertiesInvalid ? StylesheetContext.ReturnAll : (ushort)(_triggerMask ^ _updatedTriggerMask);

                foreach (StylePropertyData key in properties)
                {
                    if (_cachedData.TryGetPropertyData(key.PropertyName, out PropertyData? propertyData))
                    {
                        if (_overrideSet.Contains(propertyData.Property) || propertyData.Methods.SetDirect == null)
                            continue;

                        foreach (StylesheetClass stylesheetClass in classList)
                        {
                            ClassStyleKey styleKey = new ClassStyleKey(stylesheetClass, key.AsStyleKey(), key.TriggerMask);
                            if (context.Stylesheets.TryGetClassValue(styleKey, propertyData, out object? value))
                            {
                                if (propertyData.Field != null)
                                    propertyData.Field.SetValue(this, value);
                                else
                                    propertyData.Property.SetValue(this, value);

                                if (propertyData.StateFlags != StateFlags.None)
                                {
                                    AddStateFlags(propertyData.StateFlags);

                                    if (propertyData.Flags.HasFlag(PropertyDataFlags.EffectsParent))
                                        ParentObject?.AddStateFlags(propertyData.StateFlags);
                                }

                                if (propertyData.Callbacks != null && propertyData.Callbacks.Length > 0)
                                {
                                    for (int i = 0; i < propertyData.Callbacks.Length; i++)
                                    {
                                        (t_callbackSet ??= new HashSet<string>()).Add(propertyData.Callbacks[i]);
                                    }
                                }

                                break;
                            }
                        }
                    }
                }

                if (t_callbackSet != null && t_callbackSet.Count > 0)
                {
                    foreach (string callbackName in t_callbackSet)
                    {
                        if (_cachedData.TryGetCallbackMethod(callbackName, out MethodInfo? callbackDelegate))
                        {
                            callbackDelegate.Invoke(this, null);
                        }
                    }

                    t_callbackSet.Clear();
                }

                _arePropertiesInvalid = false;
                _triggerMask = _updatedTriggerMask;
            }
        }
        #endregion
        #region Public
        public void ClearOverride(string propertyName)
        {
            if (_cachedData.TryGetPropertyData(propertyName, out PropertyData? propertyData) && !propertyData.Flags.HasFlag(PropertyDataFlags.IsEditable))
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
                if (index > 0)
                {
                    _classList!.RemoveAt(index);
                    _classList.Insert(0, className);

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
        public abstract void RemoveStateFlags(StateFlags flags);

        protected internal abstract void GetUnstyledObjects(ref StyleQueueContext context);

        protected internal abstract StyledObject? ParentObject { get; }

        public abstract StateFlags StateFlags { get; }
        #endregion
        #region Setters
        private void SetValue<T>(T value, string propertyName, bool setAsOverriden)
        {
            if (_cachedData.TryGetPropertyData(propertyName, out PropertyData? propertyData))
            {
                if (propertyData.Methods.SetDirect != null)
                {
                    if (propertyData.Field != null)
                    {
                        propertyData.Methods.GetSetFieldDirectUnsafe<T>()!(this, in value);
                    }
                    else
                    {
                        propertyData.Methods.GetSetPropertyDirectUnsafe<T>()!(this, in value);
                    }
                }

                if (propertyData.TriggerMask.HasValue)
                {
                    Debug.Assert(typeof(T) == typeof(bool));

                    if (BoolUtil<T>.GetAsBoolean(ref value))
                        _updatedTriggerMask |= (ushort)(1 << propertyData.TriggerMask.Value);
                    else
                        _updatedTriggerMask &= (ushort)~(1 << propertyData.TriggerMask.Value);

                    AddStateFlags(StateFlags.SelfInvalidStyle);
                }

                if (setAsOverriden && !propertyData.Flags.HasFlag(PropertyDataFlags.IsEditable))
                {
                    _overrideSet.Add(propertyData.Property);
                }

                if (propertyData.StateFlags != StateFlags.None)
                {
                    AddStateFlags(propertyData.StateFlags);
                }

                if (propertyData.Callbacks != null && propertyData.Callbacks.Length > 0)
                {
                    for (int i = 0; i < propertyData.Callbacks.Length; ++i)
                    {
                        if (_cachedData.TryGetCallbackMethod(propertyData.Callbacks[i], out MethodInfo? callbackDelegate))
                        {
                            callbackDelegate.Invoke(this, null);
                        }
                    }
                }

                _requiredUpdatesSet.Remove(propertyData.Property);
            }
        }

        protected void SetStyledField<T>(T value, [CallerMemberName] string propertyName = "") => SetValue(value, propertyName, true);
        protected void SetEditedField<T>(T value, [CallerMemberName] string propertyName = "") => SetValue(value, propertyName, false);
        #endregion

        public ROList<string> ClassList => _classList ?? ROList<string>.Empty;

        [ThreadStatic]
        private static HashSet<string>? t_callbackSet;
    }
}
