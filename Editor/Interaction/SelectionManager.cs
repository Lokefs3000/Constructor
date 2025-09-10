using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Interaction
{
    public sealed class SelectionManager
    {
        private List<SelectedBase> _selected;
        private SelectedBase? _currentContext;

        internal SelectionManager()
        {
            _selected = new List<SelectedBase>();
            _currentContext = null;

            SceneEntityManager.SceneEntityDeleted += (e) =>
            {
                for (int i = 0; i < _selected.Count; i++)
                {
                    if (_selected[i] is SelectedSceneEntity selected && selected.Entity == e)
                    {
                        _selected.RemoveAt(i--);
                    }
                }
            };
        }

        /// <summary>Not thread-safe</summary>
        internal void SetOnlySelectedContext(SelectedBase selected)
        {
            bool hasFound = false;
            for (int i = 0; i < _selected.Count; i++)
            {
                if (!_selected[i].Equals(selected))
                {
                    SelectedBase @base = _selected[i];
                    _selected.RemoveAt(i--);

                    Deselected?.Invoke(@base);
                }
                else
                {
                    _currentContext = _selected[i];
                    hasFound = true;
                }
            }

            if (!hasFound)
            {
                _selected.Add(selected);
                _currentContext = selected;

                Selected?.Invoke(selected);
            }
        }

        /// <summary>Not thread-safe</summary>
        internal void AddSelectedAndSetContext(SelectedBase selected)
        {
            Debug.Assert(!_selected.Contains(selected));

            _selected.Add(selected);
            _currentContext = selected;

            Selected?.Invoke(selected);
        }

        /// <summary>Not thread-safe</summary>
        internal void AddSelected(SelectedBase selected)
        {
            Debug.Assert(!_selected.Contains(selected));

            _selected.Add(selected);

            Selected?.Invoke(selected);
        }

        /// <summary>Not thread-safe</summary>
        internal void RemoveSelected(SelectedBase selected)
        {
            _selected.Remove(selected);

            Deselected?.Invoke(selected);
        }

        internal SelectedBase? CurrentContext => _currentContext;

        internal IReadOnlyList<SelectedBase> Selection => _selected;

        internal event Action<SelectedBase>? Selected;
        internal event Action<SelectedBase>? Deselected;
    }

    internal abstract class SelectedBase
    {

    }

    internal sealed class SelectedSceneEntity : SelectedBase
    {
        public SceneEntity Entity;

        public override bool Equals(object? obj) => obj is SelectedSceneEntity other && other.Entity == Entity;
    }
}
