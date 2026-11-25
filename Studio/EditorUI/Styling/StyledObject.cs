using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using EditorUI.Reflection;
using EditorUI.Reflection.Cache;
using EditorUI.Reflection.Dynamic;
using EditorUI.Styling.Enumerator;
using EditorUI.Utility;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Rendering.Recording;
using Primary.Utility;

namespace EditorUI.Styling
{
    public abstract class StyledObject
    {
        private WidgetCachedData _cachedData;

        private ushort _triggerMask;
        private ushort _updatedTriggerMask;

        private ulong[] _effectTargets;

        private bool _arePropertiesInvalid;

        private List<string>? _classList;

        private HashSet<object> _overrideSet;

        public StyledObject()
        {
            _cachedData = WidgetPropertyCache.Instance.GetCachedData(GetType());

            _triggerMask = 0;
            _updatedTriggerMask = 0;

            _effectTargets = _cachedData.TriggerValueCount == 0 ? [] : new ulong[_cachedData.TriggerValueCount];

            _arePropertiesInvalid = true;

            _classList = null;

            _overrideSet = new HashSet<object>();
        }

        #region Update
        internal void ResolveInvalidProperties(ref readonly StylesheetContext context)
        {
            if (_arePropertiesInvalid || _triggerMask != _updatedTriggerMask || context.GetAllProperties)
            {
                ClassListEnumerable classList = context.GetClassList(_classList ?? ROList<string>.Empty);
                PropertyEnumerable properties = context.GetProperties(_updatedTriggerMask, _arePropertiesInvalid ? StylesheetContext.ReturnAll : (ushort)(_triggerMask ^ _updatedTriggerMask));

                int previousMaxTriggerIndex = sizeof(ushort) * 8 - ushort.LeadingZeroCount(_triggerMask);
                if (context.GetAllProperties)
                {
                    for (int i = 0; i < _effectTargets.Length; ++i)
                    {
                        _effectTargets[i] = 0;
                    }
                }

                foreach (StylePropertyData key in properties)
                {
                    if (_cachedData.TryGetPropertyData(key.PropertyName, out PropertyData? propertyData))
                    {
                        if (_overrideSet.Contains(propertyData.PropertyOrField) || propertyData.Methods.SetIndirect == null)
                            continue;

                        foreach (StylesheetClass stylesheetClass in classList)
                        {
                            ClassStyleKey styleKey = new ClassStyleKey(stylesheetClass, key.AsStyleKey(), key.TriggerMask);
                            if (context.Stylesheets.TryGetClassValue(styleKey, propertyData, out object? value, out int triggerIndex))
                            {
                                bool shouldSetCurrentProperty = true;
                                if (!context.GetAllProperties)
                                {
                                    for (int i = previousMaxTriggerIndex; i >= 0; --i)
                                    {
                                        ref ulong propertyMask = ref _effectTargets.DangerousGetReferenceAt(i);
                                        bool wasSetHere = Flags.HasFlag(propertyMask, propertyData.PropertyMask);

                                        if (wasSetHere)
                                        {
                                            if (i != triggerIndex)
                                            {
                                                propertyMask &= ~propertyData.PropertyMask;
                                            }
                                            else
                                            {
                                                shouldSetCurrentProperty = false;
                                            }

                                            break;
                                        }
                                    }
                                }

                                if (shouldSetCurrentProperty)
                                {
                                    propertyData.Methods.SetIndirect?.Invoke(this, value);

                                    if (propertyData.StateFlags != StateFlags.None)
                                    {
                                        AddStateFlags(propertyData.StateFlags);
                                    }

                                    if (propertyData.Links != null)
                                    {
                                        foreach (PropertyLink link in propertyData.Links)
                                        {
                                            link.Methods.SetIndirect?.Invoke(this, link.Value);
                                        }
                                    }

                                    if (propertyData.Callbacks != null && propertyData.Callbacks.Length > 0)
                                    {
                                        for (int i = 0; i < propertyData.Callbacks.Length; i++)
                                        {
                                            (t_callbackSet ??= new HashSet<string>()).Add(propertyData.Callbacks[i]);
                                        }
                                    }

                                    if (triggerIndex != -1)
                                    {
                                        _effectTargets.DangerousGetReferenceAt(triggerIndex) |= propertyData.PropertyMask;
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
                _overrideSet.Remove(propertyData.PropertyOrField);
                _arePropertiesInvalid = true;

                AddStateFlags(StateFlags.SelfInvalidStyle);
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
            if (_cachedData.TryGetPropertyData(propertyName, out PropertyData? propertyData) && propertyData.Methods.SetDirect != null)
            {
                for (int i = 0; i < _effectTargets.Length; ++i)
                {
                    _effectTargets.DangerousGetReferenceAt(i) &= ~propertyData.PropertyMask;
                }

                if (propertyData.Methods.SetDirect != null)
                {
                    propertyData.Methods.GetSetDirect<T>()?.Invoke(this, value);
                }
                else
                {
                    return;
                }

                if (propertyData.Links != null)
                {
                    foreach (PropertyLink link in propertyData.Links)
                    {
                        link.Methods.SetIndirect?.Invoke(this, link.Value);
                    }
                }

                if (setAsOverriden && !propertyData.Flags.HasFlag(PropertyDataFlags.IsEditable))
                {
                    _overrideSet.Add(propertyData.PropertyOrField);
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
            }
        }

        protected void SetStyledField<T>(T value, [CallerMemberName] string propertyName = "") => SetValue(value, propertyName, true);
        protected void SetEditedField<T>(T value, [CallerMemberName] string propertyName = "") => SetValue(value, propertyName, false);

        protected void SetTriggerValue(string propertyName, bool setValue)
        {
            if (_cachedData.TryGetTriggerData(propertyName, out TriggerData? triggerData))
            {
                if (setValue)
                    _updatedTriggerMask |= triggerData.TriggerMask;
                else
                    _updatedTriggerMask &= (ushort)~triggerData.TriggerMask;

                AddStateFlags(StateFlags.SelfInvalidStyle);
            }
        }
        #endregion

        public ROList<string> ClassList => _classList ?? ROList<string>.Empty;

        [ThreadStatic]
        private static HashSet<string>? t_callbackSet;
    }
}
