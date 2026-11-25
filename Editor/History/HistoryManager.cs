using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.History
{
    public sealed class HistoryManager : IDisposable
    {
        private static readonly WeakReference s_instance = new WeakReference(null);

        private List<IHistoryStep> _steps;
        private int _stepHead;

        private bool _disposedValue;

        internal HistoryManager()
        {
            s_instance.Target = this;

            _steps = new List<IHistoryStep>();
            _stepHead = 0;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    for (int i = 0; i < _steps.Count; i++)
                    {
                        IHistoryStep oldStep = _steps[i];
                        if (oldStep is IDisposable disposable)
                            disposable.Dispose();
                    }

                    _steps.Clear();
                    _stepHead = 0;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void PerformUndo()
        {
            if (_stepHead > 0)
            {
                IHistoryStep step = _steps[_stepHead - 1];
                try
                {
                    step.PerformUndo();
                    --_stepHead;

                    EdLog.History.Debug("Performed undo on step: {stepDesc} ({step})", step.Description, step);
                }
                catch (Exception ex)
                {
                    EdLog.History.Error(ex, "Failed to perform undo on step: {stepDesc} ({step})", step.Description, step);
                }
            }
        }

        private void PerformRedo()
        {
            if (_stepHead < _steps.Count)
            {
                IHistoryStep step = _steps[_stepHead];
                try
                {
                    step.PerformRedo();
                    ++_stepHead;

                    EdLog.History.Debug("Performed redo on step: {stepDesc} ({step})", step.Description, step);
                }
                catch (Exception ex)
                {
                    EdLog.History.Error(ex, "Failed to perform redo on step: {stepDesc} ({step})", step.Description, step);
                }
            }
        }

        private void AddHistoryStep(IHistoryStep step)
        {
            if (_stepHead < _steps.Count)
            {
                int count = _steps.Count - _stepHead;
                EdLog.History.Debug("Removing {i}# steps because a new one was added", count);

                for (int i = _stepHead; i < _steps.Count; ++i)
                {
                    IHistoryStep oldStep = _steps[i];
                    if (oldStep is IDisposable disposable)
                        disposable.Dispose();
                }

                _steps.RemoveRange(_stepHead, count);
            }

            _steps.Add(step);
            ++_stepHead;
        }

        /// <summary>Not thread-safe</summary>
        public static void AddStep(IHistoryStep step)
        {
            HistoryManager? history = Unsafe.As<HistoryManager>(s_instance.Target);
            history?.AddHistoryStep(step);
        }

        /// <summary>Not thread-safe</summary>
        public static void Undo()
        {
            HistoryManager? history = Unsafe.As<HistoryManager>(s_instance.Target);
            history?.PerformUndo();
        }

        /// <summary>Not thread-safe</summary>
        public static void Redo()
        {
            HistoryManager? history = Unsafe.As<HistoryManager>(s_instance.Target);
            history?.PerformRedo();
        }
    }
}
