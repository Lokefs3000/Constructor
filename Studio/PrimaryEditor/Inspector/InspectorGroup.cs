using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections.ReadOnly;
using PrimaryEditor.Core;
using PrimaryEditor.Inspector.Setup;
using PrimaryEditor.Inspector.Values;

namespace PrimaryEditor.Inspector
{
    public abstract class InspectorGroup
    {
        private List<IInspectorValue> _values;

        public InspectorGroup()
        {
            _values = new List<IInspectorValue>();
        }

        protected void SetupValuesFor<T>()
        {
            try
            {
                InspectorManager inspector = EditorRuntime.Instance.InspectorManager;

                _values.Clear();
                if (inspector.DescriptionCache.TryGetDescription(typeof(T), out InspectorDescription? description))
                {
                    InstantiateAll(description);

                    void InstantiateAll(InspectorDescription description, IInspectorObject? parentObject = null)
                    {
                        foreach (DescriptionValue value in description.Values)
                        {
                            IInspectorValue inspectorValue = value.Instantiate(description.Type, parentObject);
                            _values.Add(inspectorValue);

                            if (value.Description != null)
                            {
                                InstantiateAll(value.Description, (IInspectorObject)inspectorValue);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                _values.Clear();
                throw;
            }
        }

        protected void UpdateValuesOfAll(ref OpaqueRef valueType)
        {
            foreach (IInspectorValue inspectorValue in _values)
            {
                if (inspectorValue.OwningValue != null)
                {
                    IInspectorValue parentValue = inspectorValue.OwningValue;
                    if (parentValue.TargetType.IsClass)
                        inspectorValue.UpdateValueFromObject(parentValue.GetObjectValue());
                    else
                        inspectorValue.UpdateValueFromValueType(ref parentValue.GetValueType());
                }
                else
                {
                    inspectorValue.UpdateValueFromValueType(ref valueType);
                }
            }
        }

        protected void UpdateValuesOfAll(object obj)
        {
            foreach (IInspectorValue inspectorValue in _values)
            {
                if (inspectorValue.OwningValue != null)
                {
                    IInspectorValue parentValue = inspectorValue.OwningValue;
                    if (parentValue.TargetType.IsClass)
                        inspectorValue.UpdateValueFromObject(parentValue.GetObjectValue());
                    else
                        inspectorValue.UpdateValueFromValueType(ref parentValue.GetValueType());
                }
                else
                {
                    inspectorValue.UpdateValueFromObject(obj);
                }
            }
        }

        public ROList<IInspectorValue> Values => _values;

        public abstract int UniqueHash { get; }
    }
}
