using Primary.Scenes;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
                while (true)
                {
                    bool hasFoundEntry = false;

                    for (int i = 0; i < _selected.Count; i++)
                    {
                        if (_selected[i] is SelectedSceneEntity selected && selected.Entity == e)
                        {
                            _selected.RemoveAt(i);
                            Deselected?.Invoke(selected);

                            hasFoundEntry = true;
                            break;
                        }
                    }

                    if (!hasFoundEntry)
                        break;
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

        /// <summary>Not thread-safe</summary>
        public static void Select(SelectedBase selected, SelectionMode mode = SelectionMode.Single)
        {
            SelectionManager @this = Editor.GlobalSingleton.SelectionManager;
            if (mode == SelectionMode.Single)
                @this.SetOnlySelectedContext(selected);
            else
                @this.AddSelectedAndSetContext(selected);
        }

        /// <summary>Not thread-safe</summary>
        public static void Deselect(SelectedBase selected)
        {
            SelectionManager @this = Editor.GlobalSingleton.SelectionManager;
            @this.RemoveSelected(selected);
        }

        /// <summary>Not thread-safe</summary>
        public static void Deselect(Predicate<SelectedBase> predicate)
        {
            SelectionManager @this = Editor.GlobalSingleton.SelectionManager;
            SelectedBase? selected = @this._selected.Find(predicate);
            if (selected != null)
                @this.RemoveSelected(selected);
        }

        /// <summary>Not thread-safe</summary>
        public static void Deselect<T>(Predicate<T> predicate) where T : SelectedBase
        {
            SelectionManager @this = Editor.GlobalSingleton.SelectionManager;
            SelectedBase? selected = @this._selected.Find((x) => x is T t && predicate(t));
            if (selected != null)
                @this.RemoveSelected(selected);
        }

        /// <summary>Not thread-safe</summary>
        public static bool IsSelected(SelectedBase selected)
        {
            SelectionManager @this = Editor.GlobalSingleton.SelectionManager;
            return @this._selected.Contains(selected);
        }

        /// <summary>Not thread-safe</summary>
        public static bool IsSelected(Predicate<SelectedBase> predicate)
        {
            SelectionManager @this = Editor.GlobalSingleton.SelectionManager;
            return @this._selected.Find(predicate) != null;
        }

        /// <summary>Not thread-safe</summary>
        public static bool IsSelected<T>(Predicate<T> predicate) where T : SelectedBase
        {
            SelectionManager @this = Editor.GlobalSingleton.SelectionManager;
            return @this._selected.Find((x) => x is T t && predicate(t)) != null;
        }

        /// <summary>Not thread-safe</summary>
        public static SelectedBase? FindSelected(Predicate<SelectedBase> predicate)
        {
            SelectionManager @this = Editor.GlobalSingleton.SelectionManager;
            return @this._selected.Find(predicate);
        }

        /// <summary>Not thread-safe</summary>
        public static T? FindSelected<T>(Predicate<T> predicate) where T : SelectedBase
        {
            SelectionManager @this = Editor.GlobalSingleton.SelectionManager;
            return Unsafe.As<T>(@this._selected.Find((x) => x is T t && predicate(t)));
        }

        /// <summary>Not thread-safe</summary>
        public static void Clear()
        {
            SelectionManager @this = Editor.GlobalSingleton.SelectionManager;
            for (int i = 0; i < @this._selected.Count; i++)
            {
                @this.Deselected?.Invoke(@this._selected[i]);
            }

            @this._selected.Clear();
        }

        internal SelectedBase? CurrentContext => _currentContext;

        internal IReadOnlyList<SelectedBase> Selection => _selected;

        internal event Action<SelectedBase>? Selected;
        internal event Action<SelectedBase>? Deselected;

        internal static event Action<SelectedBase> NewSelected
        {
            add => Editor.GlobalSingleton.SelectionManager.Selected += value;
            remove => Editor.GlobalSingleton.SelectionManager.Selected -= value;
        }

        internal static event Action<SelectedBase> OldDeselected
        {
            add => Editor.GlobalSingleton.SelectionManager.Deselected += value;
            remove => Editor.GlobalSingleton.SelectionManager.Deselected -= value;
        }
    }

    public enum SelectionMode : byte
    {
        Single = 0,
        Multi
    }

    public abstract class SelectedBase
    {

    }

    internal sealed class SelectedSceneEntity : SelectedBase
    {
        public SceneEntity Entity;

        public override bool Equals(object? obj) => obj is SelectedSceneEntity other && other.Entity == Entity;

        public override int GetHashCode() => Entity.GetHashCode();
    }
}
