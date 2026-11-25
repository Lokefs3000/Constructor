using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Editor.Geometry;
using Editor.Inspector.IL;
using Editor.Inspector.Layout;

namespace Editor.Inspector
{
    public class InspectorObject<T> : IInspectorBase
    {
        protected readonly IInspectorBase? _parent;
        protected readonly string _propertyName;

        protected readonly InspectorField _field;
        protected readonly bool _isValueAClass;

        protected readonly GetInspectedByRefValue<T>? _byRefGetter;
        protected readonly SetInspectedByRefValue<T>? _byRefSetter;

        protected readonly GetInspectedObjectValue<T>? _objectGetter;
        protected readonly SetInspectedObjectValue<T>? _objectSetter;

        protected T? _value;

        public InspectorObject(InspectorManager manager, IInspectorBase? parent, string propertyName, InspectorField field)
        {
            _parent = parent;
            _propertyName = propertyName;

            _field = field;
            _isValueAClass = field.FieldType.IsClass;

            MethodTuple methodTuple = manager.MethodGenerator.GetMethods<T>(field, propertyName);
            _byRefGetter = methodTuple.GetByRefGetter<T>();
            _byRefSetter = methodTuple.GetByRefSetter<T>();

            _objectGetter = methodTuple.GetObjectGetter<T>();
            _objectSetter = methodTuple.GetObjectSetter<T>();

            _value = default;
        }

        public virtual void UpdateValueByRef(ref GenericRefValue baseTarget)
        {
            if (_parent != null)
            {
                if (_parent.IsValueAClass)
                {
                    UpdateValueObject(_parent.GetRefObject());
                    return;
                }

                baseTarget = ref _parent.GetRefValue();
            }

            if (Unsafe.IsNullRef(in baseTarget))
                return;

            _value = _byRefGetter!(_field.FieldOrProperty, ref baseTarget);
        }

        public virtual void UpdateValueObject(GenericRefObject baseTarget)
        {
            if (_parent != null)
            {
                if (!_parent.IsValueAClass)
                {
                    UpdateValueByRef(ref _parent.GetRefValue());
                    return;
                }

                baseTarget = _parent.GetRefObject();
            }

            if (baseTarget.Value == null)
                return;

            _value = _objectGetter!(_field.FieldOrProperty, baseTarget.Value);
        }

        public virtual void SetValue(object target, T? value)
        {
            if (_parent != null)
            {
                if (_parent.IsValueAClass)
                {
                    _objectSetter!(_field.FieldOrProperty, _parent.GetRefObject().Value!, value);
                }
                else
                {
                    _byRefSetter!(_field.FieldOrProperty, ref _parent.GetRefValue(), value);
                }
            }
            else
            {
                _objectSetter!.Invoke(_field.FieldOrProperty, target, value);
            }
        }

        public virtual ref GenericRefValue GetRefValue()
        {
            Debug.Assert(!_isValueAClass);
            if (_value is null)
                return ref Unsafe.NullRef<GenericRefValue>();
            else
                return ref Unsafe.As<T, GenericRefValue>(ref _value);
        }

        public virtual GenericRefObject GetRefObject()
        {
            Debug.Assert(_isValueAClass);
            if (_value is null)
                return new GenericRefObject { Value = null };
            else
                return new GenericRefObject { Value = _value };
        }

        public IInspectorBase? Parent => _parent;
        public LayoutObject Object => throw new NotImplementedException();

        public string PropertyName => _propertyName;
        public Type PropertyType => _field.FieldType;

        public bool IsValueAClass => _isValueAClass;

        public T? Value => _value;
    }

    public struct GenericRefValue { }
    public struct GenericRefObject
    {
        public object? Value;
    }
}
