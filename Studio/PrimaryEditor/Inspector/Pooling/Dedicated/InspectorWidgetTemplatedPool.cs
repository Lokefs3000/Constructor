using System;
using System.Collections.Generic;
using System.Text;
using Primary.Pooling;
using PrimaryEditor.Inspector.Widgets;

namespace PrimaryEditor.Inspector.Pooling.Dedicated
{
    internal sealed class InspectorWidgetTemplatedPool<T> : IInspectorWidgetPool where T : InspectorWidget
    {
        private readonly DisposableObjectPool<T> _pool;
        private bool _disposedValue;

        internal InspectorWidgetTemplatedPool()
        {
            _pool = new DisposableObjectPool<T>(new PoolingPolicy());
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _pool.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public T GetWidget() => _pool.Get();
        public void ReturnWidget(T widget) => _pool.Return(widget);

        InspectorWidget IInspectorWidgetPool.GetWidget() => _pool.Get();
        void IInspectorWidgetPool.ReturnWidget(InspectorWidget widget) => _pool.Return((T)widget);

        private sealed record class PoolingPolicy : IObjectPoolPolicy<T>
        {
            public T Create() => (T)Activator.CreateInstance(typeof(T), true)!;
            public bool Return(ref T obj)
            {
                obj.ClearForPooling();
                return true;
            }
        }
    }
}
