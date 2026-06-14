using Primary.Collections.ReadOnly;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Interaction
{
    public sealed class SelectionGroup
    {
        private readonly object? _owner;

        private int _stackIndex;

        private HashSet<object> _selected;
        private object? _context;

        private ISelectionPolicy? _policy;

        public SelectionGroup(object? owner)
        {
            _owner = owner;

            _stackIndex = 0;

            _selected = new HashSet<object>();
            _context = null;

            _policy = null;
        }

        private void SetSelectionContext(object? context)
        {
            if (context != null && !_selected.Contains(context))
            {
                if (_policy?.IsSelectedObjectValid(context) ?? true)
                    _selected.Add(context);
                else
                    return;
            }

            if (_context == context)
                return;

            _context = context;
            ContextChanged?.Invoke(context);
        }

        internal void ClearSelection()
        {
            _context = null;

            while (_selected.Count > 0)
            {
                object selected = _selected.First();

                ObjectDeselected?.Invoke(selected);
                _selected.Remove(selected);
            }

            ContextChanged?.Invoke(null);
        }

        internal void AddObject(object obj, bool setAsNewContext)
        {
            if (_policy?.IsSelectedObjectValid(obj) ?? true)
            {
                _selected.Add(obj);
                ObjectSelected?.Invoke(obj);

                if (setAsNewContext && _context != obj)
                {
                    _context = obj;
                    ContextChanged?.Invoke(obj);
                }
            }
        }

        internal void RemoveObject(object obj)
        {
            if (_selected.Remove(obj))
            {
                ObjectDeselected?.Invoke(obj);

                if (_context == obj)
                {
                    _context = _selected.FirstOrDefault();
                    ContextChanged?.Invoke(_context);
                }
            }
        }

        internal bool IsSelected(object selected)
        {
            return _selected.Contains(selected);
        }

        public bool IsEmpty => _selected.Count == 0;

        public object? Owner => _owner;

        internal int StackIndex { get => _stackIndex; set => _stackIndex = value; }

        public IEnumerable<object> Selection => _selected;
        public object? Context { get => _context; set => SetSelectionContext(value); }

        public ISelectionPolicy? Policy { get => _policy; set => _policy = value; }

        #region Events
        public event Action<object>? ObjectSelected;
        public event Action<object>? ObjectDeselected;

        public event Action<object?>? ContextChanged;
        #endregion
    }
}
