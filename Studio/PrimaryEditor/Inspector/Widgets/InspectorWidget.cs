using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Collections.ReadOnly;
using Primary.Utility;
using PrimaryEditor.Inspector.Values;
using PrimaryEditor.Inspector.Views;

namespace PrimaryEditor.Inspector.Widgets
{
    public abstract class InspectorWidget : IDisposable
    {
        protected int _uniqueHash;
        protected List<IInspectorValue> _inspectorValues;

        protected List<WidgetCondition> _conditions;
        protected ViewConditionMode _conditionMode;

        protected ViewObject? _viewData;

        private bool _disposedValue;

        internal InspectorWidget()
        {
            _uniqueHash = -1;
            _inspectorValues = new List<IInspectorValue>();

            _conditions = new List<WidgetCondition>();
            _conditionMode = 0;

            _viewData = null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    DestroySelf();
                }

                _disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal virtual void DestroySelf()
        {
            ClearForPooling();
        }

        internal virtual void ClearForPooling()
        {
            for (int i = 0; i < _conditions.Count; ++i)
            {
                _conditions[i].Values.Dispose();
            }

            _uniqueHash = -1;
            _inspectorValues.Clear();

            _conditions.Clear();
            _conditionMode = 0;

            _viewData = null;
        }

        internal virtual void SetupForDisplay(ViewObject? viewData)
        {
            _viewData = viewData;
        }

        internal virtual void AddDisplaySource(IInspectorValue? valueForThis, ROList<IInspectorValue> allValues)
        {
            if (_uniqueHash != -1 && valueForThis != null)
            {
                throw new InvalidDataException("Invalid inspector value");
            }

            _uniqueHash = valueForThis?.UniqueHash ?? -1;
            if (valueForThis != null)
            {
                if (!_inspectorValues.AddUnique(valueForThis))
                {
                    throw new InvalidDataException("Value already added");
                }
            }

            if (_viewData != null)
            {
                foreach (ViewCondition condition in _viewData.Conditions)
                {
                    IInspectorValue? valueMatchingConditionSource = null;

                    ViewSource source = condition.Source;
                    foreach (IInspectorValue inspectorValue in allValues)
                    {
                        if (source.Type == inspectorValue.TargetType && inspectorValue.Name == source.Name)
                        {
                            valueMatchingConditionSource = inspectorValue;
                            break;
                        }
                    }

                    if (valueMatchingConditionSource != null)
                    {
                        int indexOf = _conditions.FindIndex((x) => x.View == condition);
                        if (indexOf != -1)
                        {
                            ref WidgetCondition conditionData = ref _conditions.AsSpan()[indexOf];
                            conditionData.Values.Add(valueMatchingConditionSource);
                        }
                        else
                        {
                            _conditions.Add(new WidgetCondition(condition, [valueMatchingConditionSource]));
                        }
                    }
                    else
                    {
                        EdLog.Inspector.Warning("Failed to find condition with name '{n}' in inspector value list", source.Name);
                    }
                }

                _conditionMode = _viewData.ConditionMode;
            }
        }

        public int UniqueHash => _uniqueHash;
        public ROList<IInspectorValue> InspectorValues => _inspectorValues;

        public ROList<WidgetCondition> Conditions => _conditions;
    }
}
