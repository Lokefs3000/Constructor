using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections;

namespace VoxelizationDemo.Editor.History
{
    public sealed class HistoryGroup
    {
        private RentedList<IHistoryStep> _steps;

        internal HistoryGroup()
        {
            _steps = new RentedList<IHistoryStep>();
        }

        internal void ReturnToPool()
        {
            for (int i = _steps.Count - 1; i >= 0; i--)
            {
                if (_steps[i] is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        AppLog.Editor.Error(ex, "Failed to dispose history step");
                    }
                }
            }

            _steps.Dispose();
        }

        internal void RentFromPool()
        {
            _steps = new RentedList<IHistoryStep>();
        }

        internal void AddStep(IHistoryStep step)
        {
            _steps.Add(step);
        }

        internal void Undo()
        {
            for (int i = _steps.Count - 1; i >= 0; i--)
            {
                try
                {
                    _steps[i].Undo();
                }
                catch (Exception ex)
                {
                    AppLog.Editor.Error(ex, "Failed to undo step");
                }
            }
        }

        internal void Redo()
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                try
                {
                    _steps[i].Redo();
                }
                catch (Exception ex)
                {
                    AppLog.Editor.Error(ex, "Failed to redo step");
                }
            }
        }

        public bool IsEmpty => _steps.IsEmpty;
    }
}
