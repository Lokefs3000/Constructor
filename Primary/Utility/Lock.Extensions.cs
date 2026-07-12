using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Primary.Utility
{
    public static partial class Extensions
    {
        extension(Lock @lock)
        {
            /// <inheritdoc cref="Lock.TryEnter()" />
            public TryEnterLockScope TryEnterScope(out bool success)
            {
                success = @lock.TryEnter();
                if (success)
                    return new TryEnterLockScope(@lock);
                return default;
            }

            /// <inheritdoc cref="Lock.TryEnter(int)" />
            public TryEnterLockScope TryEnterScope(int millisecondsTimeout, out bool success)
            {
                success = @lock.TryEnter(millisecondsTimeout);
                if (success)
                    return new TryEnterLockScope(@lock);
                return default;
            }

            /// <inheritdoc cref="Lock.TryEnter(TimeSpan)" />
            public TryEnterLockScope TryEnterScope(TimeSpan timeout, out bool success)
            {
                success = @lock.TryEnter(timeout);
                if (success)
                    return new TryEnterLockScope(@lock);
                return default;
            }
        }
    }

    public ref struct TryEnterLockScope : IDisposable
    {
        private Lock? _lock;

        internal TryEnterLockScope(Lock @lock)
        {
            _lock = @lock;
        }

        public void Dispose()
        {
            _lock?.Exit();
            _lock = null;
        }
    }
}
