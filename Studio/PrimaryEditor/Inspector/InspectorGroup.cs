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
        private Type? _type;
        private List<IInspectorValue> _values;

        public InspectorGroup()
        {
            _type = null;
            _values = new List<IInspectorValue>();
        }

        protected void SetupValuesFor<T>()
        {
            try
            {
                InspectorManager inspector = EditorRuntime.Instance.InspectorManager;

                _type = typeof(T);

                _values.Clear();
                if (inspector.DescriptionCache.TryGetDescription(typeof(T), out InspectorDescription? description))
                {
                    InstantiateAll(description);

                    void InstantiateAll(InspectorDescription description, string? parentName = null, IInspectorObject? parentObject = null)
                    {
                        foreach (DescriptionValue value in description.Values)
                        {
                            string valueName = parentName == null ? value.Name : $"{parentName}.{value.Name}";

                            IInspectorValue inspectorValue = value.Instantiate(description.Type, parentObject, valueName);
                            _values.Add(inspectorValue);

                            if (value.Description != null)
                            {
                                InstantiateAll(value.Description, valueName, (IInspectorObject)inspectorValue);
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

        public Type Type => _type;
        public ROList<IInspectorValue> Values => _values;

        public abstract int UniqueHash { get; }
    }
}
