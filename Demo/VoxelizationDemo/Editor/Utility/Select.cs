using Primary.Collections.ReadOnly;
using Primary.Scenes;
using PrimaryEditor.Selection;
using VoxelizationDemo.Core;

namespace VoxelizationDemo.Editor.Utility
{
    internal static class Select
    {
        public static void Clear()
            => VoxelRuntime.Instance.EditorManager.SelectionManager.Clear();

        public static void Add(object obj, bool allowDeselect = false)
            => VoxelRuntime.Instance.EditorManager.SelectionManager.Select(obj, allowDeselect, Mode);
        public static void Add(object obj, SelectMode mode, bool allowDeselect = false)
            => VoxelRuntime.Instance.EditorManager.SelectionManager.Select(obj, allowDeselect, mode);

        public static void Remove(object obj)
            => VoxelRuntime.Instance.EditorManager.SelectionManager.Deselect(obj);

        public static bool IsSelected(object obj)
            => VoxelRuntime.Instance.EditorManager.SelectionManager.IsSelected(obj);

        public static ROHashSet<object> Objects => VoxelRuntime.Instance.EditorManager.SelectionManager.Objects;

        public static object? ActiveObject
        {
            get => VoxelRuntime.Instance.EditorManager.SelectionManager.ActiveObject;
            set => VoxelRuntime.Instance.EditorManager.SelectionManager.ActiveObject = value;
        }

        public static SceneEntity ActiveEntity
        {
            get => VoxelRuntime.Instance.EditorManager.SelectionManager.ActiveEntity;
            set => VoxelRuntime.Instance.EditorManager.SelectionManager.ActiveEntity = value;
        }

        public static event Action<object> OnObjectSelected
        {
            add => VoxelRuntime.Instance.EditorManager.SelectionManager.OnObjectSelected += value;
            remove => VoxelRuntime.Instance.EditorManager.SelectionManager.OnObjectSelected -= value;
        }

        public static event Action<object> OnObjectDeselected
        {
            add => VoxelRuntime.Instance.EditorManager.SelectionManager.OnObjectDeselected += value;
            remove => VoxelRuntime.Instance.EditorManager.SelectionManager.OnObjectDeselected -= value;
        }

        public static event Action<object?, object?> OnActiveObjectChanged
        {
            add => VoxelRuntime.Instance.EditorManager.SelectionManager.OnActiveObjectChanged += value;
            remove => VoxelRuntime.Instance.EditorManager.SelectionManager.OnActiveObjectChanged -= value;
        }

        public static event Action<SceneEntity, SceneEntity> OnActiveEntityChanged
        {
            add => VoxelRuntime.Instance.EditorManager.SelectionManager.OnActiveEntityChanged += value;
            remove => VoxelRuntime.Instance.EditorManager.SelectionManager.OnActiveEntityChanged -= value;
        }

        public static SelectMode Mode => VoxelRuntime.Instance.EditorManager.ContextManager.ImGuiCtx.IO.KeyCtrl > 0 ? SelectMode.Append : SelectMode.Clear;
    }
}
