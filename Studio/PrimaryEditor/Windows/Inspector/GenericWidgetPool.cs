using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Widgets;

namespace PrimaryEditor.Windows.Inspector
{
    internal sealed class GenericWidgetPool<T> : IGenericWidgetPool where T : Widget, new()
    {
        private readonly Stack<T> _pool;

        private bool _disposedValue;

        public GenericWidgetPool()
        {
            _pool = new Stack<T>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    while (_pool.TryPop(out T? value))
                    {
                        value.Destroy();
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

        public Widget GetWidget()
        {
            if (!_pool.TryPop(out T? value))
                value = new T();

            return value;
        }
    }

    internal interface IGenericWidgetPool : IDisposable
    {
        public Widget GetWidget();
    }
}
