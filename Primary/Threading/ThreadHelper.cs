using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.ObjectPool;
using Primary.Profiling;
using Serilog;
using TerraFX.Interop.Windows;

namespace Primary.Threading
{
    public sealed class ThreadHelper
    {
        private static WeakReference s_instance = new WeakReference(null);

        private ObjectPool<TaskCompletionSource> _tcsPool;
        private ConcurrentQueue<PendingTask> _pendingTasks;

        private int _mainThreadId;

        internal ThreadHelper()
        {
            s_instance.Target = this;

            _tcsPool = ObjectPool.Create<TaskCompletionSource>();
            _pendingTasks = new ConcurrentQueue<PendingTask>();

            _mainThreadId = Environment.CurrentManagedThreadId;
        }

        /// <remarks>Not thread-safe</remarks>
        public void ExecutePendingTasks()
        {
            using (new ProfilingScope("ExecutePendingTasks"))
            {
                TaskCompletionSource? currentTcs = null;
                try
                {
                    while (_pendingTasks.TryDequeue(out PendingTask task))
                    {
                        currentTcs = task.TCS;

                        task.Action();
                        task.TCS.TrySetResult();

                        _tcsPool.Return(currentTcs);
                        currentTcs = null;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "_");

                    if (currentTcs != null)
                    {
                        currentTcs.TrySetException(ex);
                        _tcsPool.Return(currentTcs);
                    }
                }
            }
        }

        /// <remarks>Thread-safe</remarks>
        public static Task ExecuteOnMainThread(Action action)
        {
            ThreadHelper @this = Unsafe.As<ThreadHelper>(s_instance.Target)
                ?? throw new NullReferenceException(nameof(s_instance.Target));

            PendingTask task = new PendingTask(@this._tcsPool.Get(), action);
            @this._pendingTasks.Enqueue(task);

            return task.TCS.Task;
        }

        public static int MainThreadId => Unsafe.As<ThreadHelper>(s_instance.Target)!._mainThreadId;

        private record struct PendingTask(TaskCompletionSource TCS, Action Action);
    }
}
