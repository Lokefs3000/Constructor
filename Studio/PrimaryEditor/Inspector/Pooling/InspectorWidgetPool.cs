using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using PrimaryEditor.Inspector.Pooling.Dedicated;
using PrimaryEditor.Inspector.Widgets;

namespace PrimaryEditor.Inspector.Pooling
{
    public sealed class InspectorWidgetPool : IDisposable
    {
        private readonly FrozenDictionary<Type, IInspectorWidgetPool> _pools;
        private bool _disposedValue;

        internal InspectorWidgetPool()
        {
            _pools = new Dictionary<Type, IInspectorWidgetPool>
            {
                { typeof(InspectorWidgetField), new InspectorWidgetTemplatedPool<InspectorWidgetField>() },
                { typeof(InspectorWidgetGroup), new InspectorWidgetTemplatedPool<InspectorWidgetGroup>() },
                { typeof(InspectorWidgetPreset), new InspectorWidgetTemplatedPool<InspectorWidgetPreset>() },
            }.ToFrozenDictionary();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (var (_, pool) in _pools)
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

        public T GetPooledWidget<T>() where T : InspectorWidget
        {
            return ((InspectorWidgetTemplatedPool<T>)_pools[typeof(T)]).GetWidget();
        }

        public void ReturnPooledWidget<T>(T widget) where T : InspectorWidget
        {
            ((InspectorWidgetTemplatedPool<T>)_pools[typeof(T)]).ReturnWidget(widget);
        }
    }
}
