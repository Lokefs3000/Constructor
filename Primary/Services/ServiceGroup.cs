using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Primary.Services
{
    public sealed class ServiceGroup : IDisposable
    {
        private readonly Dictionary<Type, IService> _services;

        private bool _disposedValue;

        public ServiceGroup()
        {
            _services = new Dictionary<Type, IService>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    ClearServices();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void ClearServices()
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            foreach (var (_, service) in _services)
            {
                if (service is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _services.Clear();
        }

        public bool AddService<T>(T service) where T : class, IService
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            return _services.TryAdd(typeof(T), service);
        }

        public bool RemoveService<T>() where T : class, IService
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            if (_services.Remove(typeof(T), out IService? service))
            {
                if (service is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                return true;
            }

            return false;
        }

        public bool RemoveService<T>([NotNullWhen(true)] out T? service) where T : class, IService
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            if (_services.Remove(typeof(T), out IService? generic))
            {
                if (generic is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                service = Unsafe.As<T>(generic);
                return true;
            }

            service = null;
            return false;
        }

        public T GetService<T>() where T : class, IService
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            return Unsafe.As<T>(_services[typeof(T)]);
        }

        public T? FindService<T>() where T : class, IService
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            return _services.TryGetValue(typeof(T), out IService? service) ? Unsafe.As<T>(service) : null;
        }

        public bool TryGetService<T>([NotNullWhen(true)] out T? service) where T : class, IService
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            if (_services.TryGetValue(typeof(T), out IService? generic))
            {
                service = Unsafe.As<T>(generic);
                return true;
            }

            service = null;
            return false;
        }
    }
}
