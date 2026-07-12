using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Widgets;
using PrimaryEditor.Inspector.Values;

namespace PrimaryEditor.Windows.Inspector
{
    internal sealed class WidgetPool : IDisposable
    {
        private readonly InspectorWindow _window;

        private readonly Stack<InspectorWidgetValue> _widgetValues;
        private readonly Stack<InspectorWidgetObject> _widgetObjects;

        private readonly Dictionary<Type, IGenericWidgetPool> _genericPools;

        private bool _disposedValue;

        internal WidgetPool(InspectorWindow window)
        {
            _window = window;

            _widgetValues = new Stack<InspectorWidgetValue>();
            _widgetObjects = new Stack<InspectorWidgetObject>();

            _genericPools = new Dictionary<Type, IGenericWidgetPool>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    while (_widgetValues.TryPop(out InspectorWidgetValue? widgetValue))
                    {
                        widgetValue.Dispose();
                    }

                    while (_widgetObjects.TryPop(out InspectorWidgetObject? widgetObject))
                    {
                        widgetObject.Dispose();
                    }

                    foreach (var (_, pool) in _genericPools)
                    {
                        pool.Dispose();
                    }
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal InspectorWidget GetWidgetForValue(IInspectorValue inspectorValue)
        {
            if (inspectorValue is IInspectorObject inspectorObject)
            {
                if (!_widgetObjects.TryPop(out InspectorWidgetObject? widgetObject))
                    widgetObject = new InspectorWidgetObject();

                widgetObject.SetupValueFor(inspectorObject);
                widgetObject.AddValueSource(inspectorValue);

                return widgetObject;
            }
            else
            {
                if (!_widgetValues.TryPop(out InspectorWidgetValue? widgetValue))
                    widgetValue = new InspectorWidgetValue(_window);

                Widget? valueWidget = null;

                Type? type = GetWidgetTypeFor(inspectorValue.TargetType);
                if (type != null)
                {
                    if (!_genericPools.TryGetValue(type, out IGenericWidgetPool? widgetPool))
                    {
                        widgetPool = (IGenericWidgetPool)Activator.CreateInstance(typeof(GenericWidgetPool<>).MakeGenericType(type))!;
                        _genericPools.Add(type, widgetPool);
                    }

                    valueWidget = widgetPool.GetWidget();
                }

                widgetValue.SetupValueFor(inspectorValue, valueWidget);
                widgetValue.AddValueSource(inspectorValue);

                return widgetValue;
            }
        }

        private static Type? GetWidgetTypeFor(Type targetType)
        {
            if (targetType.IsEnum)
                return typeof(DropdownField);
            if (targetType == typeof(bool))
                return typeof(Checkbox);

            return null;
        }
    }
}
