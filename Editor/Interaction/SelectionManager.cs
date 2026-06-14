using Editor.Interaction.Controls;
using Editor.Interaction.Logic;
using MathNet.Numerics;
using Primary.Scenes;
using Primary.Scenes.Components;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Editor.Interaction
{
    public sealed class SelectionManager
    {
        private SelectionGroup[] _groupStack;
        private int _groupStackHead;

        private SelectionGroup _defaultGroup;

        private Dictionary<Type, ISelectionLocator> _locators;

        private HashSet<SelectionGroup> _usedGroups;
        private HashSet<object> _usedObjects;

        internal SelectionManager()
        {
            _groupStack = Array.Empty<SelectionGroup>();
            _groupStackHead = 0;

            _defaultGroup = new SelectionGroup(this);

            _locators = new Dictionary<Type, ISelectionLocator>();

            _usedGroups = new HashSet<SelectionGroup>();
            _usedObjects = new HashSet<object>();

            EditorRuntime runtime = EditorRuntime.GlobalSingleton;
            runtime.ReflectionManager.TypeLoader.AddCallback<SelectionLocatorTypesAttribute>(SelectionLocatorTypeLoaded);

            AddCallbacks(_defaultGroup);
        }

        private void SelectionLocatorTypeLoaded(Type type, object obj)
        {
            if (!type.IsAssignableTo(typeof(ISelectionLocator)))
                return;

            ISelectionLocator? locator = Activator.CreateInstance(type) as ISelectionLocator;
            if (locator == null)
            {
                EdLog.Interaction.Error("Failed to create selection locator {t}", type);
                return;
            }

            SelectionLocatorTypesAttribute attribute = (SelectionLocatorTypesAttribute)obj;
            foreach (Type selectionType in attribute.Types)
            {
                if (!_locators.TryAdd(selectionType, locator))
                {
                    EdLog.Interaction.Error("A selection locator is already assigned to the type {t}", selectionType);
                    return;
                }
            }
        }

        private SelectionGroup? LocateGroupForObject(object obj)
        {
            Type type = obj.GetType();
            if (_locators.TryGetValue(type, out ISelectionLocator? locator))
                return locator.Locate(this, obj);
            else
                return _defaultGroup;
        }

        internal void Clear()
        {
            SelectionGroup group = _groupStackHead > 0 ? _groupStack[_groupStackHead - 1] : _defaultGroup;
            group.ClearSelection();
        }

        internal void SelectRange(ReadOnlySpan<object> range, SelectMode mode)
        {
            if (!range.IsEmpty)
            {
                for (int i = 0; i < range.Length; i++)
                {
                    object obj = range[i];
                    SelectionGroup? group = LocateGroupForObject(obj);

                    if (group != null)
                    {
                        bool isFirstUse = _usedGroups.Add(group);
                        if (mode == SelectMode.Clear && isFirstUse)
                            group.ClearSelection();

                        group.AddObject(obj, isFirstUse);
                    }
                }

                _usedGroups.Clear();
            }
        }

        internal void DeselectRange(ReadOnlySpan<object> range)
        {
            if (!range.IsEmpty)
            {
                int top = _groupStackHead - 1;

                for (int i = 0; i < range.Length; i++)
                {
                    object obj = range[i];
                    for (int j = top; j >= -1; --j)
                    {
                        SelectionGroup group = j >= 0 ? _groupStack[j] : _defaultGroup;
                        if (group.IsSelected(obj))
                        {
                            group.RemoveObject(obj);
                            break;
                        }
                    }
                }
            }
        }

        internal IEnumerable<object> GetSelection()
        {
            SelectionGroup group = _groupStackHead > 0 ? _groupStack[_groupStackHead - 1] : _defaultGroup;
            return group.Selection;
        }

        internal object? GetContext()
        {
            SelectionGroup group = _groupStackHead > 0 ? _groupStack[_groupStackHead - 1] : _defaultGroup;
            return group.Context;
        }

        internal SelectionGroup GetCurrentGroup()
        {
            return _groupStackHead > 0 ? _groupStack[_groupStackHead - 1] : _defaultGroup;
        }

        internal void PushSelectionGroup(SelectionGroup group)
        {
            if (group == _defaultGroup)
                return;

            int index = _groupStack.IndexOf(group);
            if (index != -1)
            {
                if (index < _groupStackHead - 1)
                {
                    Array.Copy(_groupStack, index + 1, _groupStack, index, _groupStackHead - index);
                    _groupStack[_groupStackHead - 1] = group;
                }
            }
            else
            {
                if (_groupStack.Length == _groupStackHead)
                    Array.Resize(ref _groupStack, Math.Max(_groupStack.Length * 2, 4));

                AddCallbacks(group);
                _groupStack[_groupStackHead++] = group;
            }
        }

        internal void PopSelectionGroup(SelectionGroup group)
        {
            if (group == _defaultGroup)
                return;

            int index = _groupStack.IndexOf(group);
            if (index != -1 && _groupStackHead > 0)
            {
                if (index < _groupStackHead - 1)
                {
                    Array.Copy(_groupStack, index + 1, _groupStack, index, _groupStackHead - index);
                }

                _groupStack[--_groupStackHead] = default!;
                RemoveCallbacks(group);
            }
        }

        private void AddCallbacks(SelectionGroup group)
        {
            group.ObjectSelected += ObjectSelectedCallback;
            group.ObjectDeselected += ObjectDeselectedCallback;

            foreach (object obj in group.Selection)
            {
                ObjectSelectedCallback(obj);
            }
        }

        private void RemoveCallbacks(SelectionGroup group)
        {
            group.ObjectSelected -= ObjectSelectedCallback;
            group.ObjectDeselected -= ObjectDeselectedCallback;

            foreach (object obj in group.Selection)
            {
                ObjectDeselectedCallback(obj);
            }
        }

        private void ObjectSelectedCallback(object obj)
        {
            if (_usedObjects.Add(obj))
                ObjectSelected?.Invoke(obj);
        }
        private void ObjectDeselectedCallback(object obj)
        {
            if (_usedObjects.Remove(obj))
                ObjectDeselected?.Invoke(obj);
        }

        public SelectionGroup DefaultGroup => _defaultGroup;

        /// <summary>Not thread-safe</summary>
        public static void ClearSelection()
        {
            EditorRuntime runtime = EditorRuntime.GlobalSingleton;
            runtime.SelectionManager.Clear();
        }

        /// <summary>Not thread-safe</summary>
        public static void Select(object selection, SelectMode selectMode = SelectMode.Clear)
        {
            EditorRuntime runtime = EditorRuntime.GlobalSingleton;
            runtime.SelectionManager.SelectRange(new ReadOnlySpan<object>(in selection), selectMode);
        }

        /// <summary>Not thread-safe</summary>
        public static void Select(ReadOnlySpan<object> selection, SelectMode selectMode = SelectMode.Clear)
        {
            EditorRuntime runtime = EditorRuntime.GlobalSingleton;
            runtime.SelectionManager.SelectRange(selection, selectMode);
        }

        /// <summary>Not thread-safe</summary>
        public static void Deselect(object selection)
        {
            EditorRuntime runtime = EditorRuntime.GlobalSingleton;
            runtime.SelectionManager.DeselectRange(new ReadOnlySpan<object>(in selection));
        }

        /// <summary>Not thread-safe</summary>
        public static void Deselect(ReadOnlySpan<object> selection)
        {
            EditorRuntime runtime = EditorRuntime.GlobalSingleton;
            runtime.SelectionManager.DeselectRange(selection);
        }

        /// <summary>Not thread-safe</summary>
        public static void SetNewSelectionGroup(SelectionGroup group)
        {
            EditorRuntime runtime = EditorRuntime.GlobalSingleton;
            runtime.SelectionManager.PushSelectionGroup(group);
        }

        /// <summary>Not thread-safe</summary>
        public static void RemoveSelectionGroup(SelectionGroup group)
        {
            EditorRuntime runtime = EditorRuntime.GlobalSingleton;
            runtime.SelectionManager.PopSelectionGroup(group);
        }

        /// <summary>Not thread-safe</summary>
        public static IEnumerable<object> Selection => EditorRuntime.GlobalSingleton.SelectionManager.GetSelection();

        /// <summary>Not thread-safe</summary>
        public static object? Context => EditorRuntime.GlobalSingleton.SelectionManager.GetContext();

        /// <summary>Not thread-safe</summary>
        public static SelectionGroup? CurrentGroup => EditorRuntime.GlobalSingleton.SelectionManager.GetCurrentGroup();

        public static event Action<object>? ObjectSelected;
        public static event Action<object>? ObjectDeselected;
    }

    public enum SelectMode : byte
    {
        /// <summary>Add the new selection without clearing the old one</summary>
        Append = 0,

        /// <summary>Clear the current selection and add the new one</summary>
        Clear,
    }
}
