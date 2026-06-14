using Editor.Interaction.Controls;
using Editor.Interaction.Tools;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Profiling;
using Primary.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Editor.Interaction
{
    public sealed class ToolManager
    {
        private Dictionary<Type, ITool?> _tools;

        private Dictionary<Type, IToolControl> _toolControls;

        private List<ToolTransformData> _transforms;
        private List<ToolTransformData> _activeTransforms;

        private HashSet<Type> _disabledTypes;

        private EditorOriginMode _originMode;
        private EditorToolSpace _toolSpace;

        private bool _isSnappingDefault;
        private bool _isSnappingActive;
        private float _snapScale;

        private ITool? _currentTool;

        internal ToolManager(EditorRuntime editor)
        {
            _tools = new Dictionary<Type, ITool?>();

            _toolControls = new Dictionary<Type, IToolControl>();

            _transforms = new List<ToolTransformData>();
            _activeTransforms = new List<ToolTransformData>();

            _disabledTypes = new HashSet<Type>();

            _originMode = EditorOriginMode.Individual;
            _toolSpace = EditorToolSpace.Local;

            _isSnappingDefault = false;
            _isSnappingActive = false;
            _snapScale = 1.0f;

            _currentTool = null;

            editor.ReflectionManager.TypeLoader.AddCallback<ToolControlTypesAttribute>(OnToolControlTypeLoaded);

            SelectionManager.ObjectSelected += ObjectSelectedCallback;
            SelectionManager.ObjectDeselected += ObjectDeselectedCallback;
        }

        private void ObjectSelectedCallback(object obj)
        {
            Type type = obj.GetType();
            if (_toolControls.TryGetValue(type, out IToolControl? control))
            {
                IToolTransform? transform = control.Selected(obj);
                if (transform != null)
                {
                    _transforms.Add(new ToolTransformData(obj, transform));

                    if (!_disabledTypes.Contains(type))
                        _activeTransforms.Add(new ToolTransformData(obj, transform));
                }
            }
        }

        private void ObjectDeselectedCallback(object obj)
        {
            Type type = obj.GetType();
            if (_toolControls.TryGetValue(type, out IToolControl? control))
            {
                Predicate<ToolTransformData> predicate = (x) => x.Selected.Equals(obj);
                if (_transforms.RemoveWhere(predicate, out ToolTransformData data))
                {
                    if (!_disabledTypes.Contains(type))
                        _activeTransforms.RemoveWhere(predicate);

                    control.Deselected(obj, data.Transform);
                }
            }
        }

        private void OnToolControlTypeLoaded(Type type, object obj)
        {
            if (!type.IsAssignableTo(typeof(IToolControl)))
                return;

            IToolControl? control = (IToolControl?)Activator.CreateInstance(type);
            if (control == null)
            {
                EdLog.Interaction.Error("Failed to create tool control instance {t}", type);
                return;
            }

            ToolControlTypesAttribute attrib = (ToolControlTypesAttribute)obj;
            foreach (Type selectionType in attrib.Types)
            {
                if (!_toolControls.TryAdd(selectionType, control))
                {
                    EdLog.Interaction.Error("A tool control is already assigned to the type {t}", selectionType);
                    return;
                }
            }
        }

        /// <summary>Not thread-safe</summary>
        private void SwitchToolFromType(Type newToolType)
        {
            if (_currentTool?.GetType() == newToolType)
                return;

            ITool? tool = GetToolInfo(newToolType);
            if (tool != null)
            {
                _currentTool?.Deselected(this);
                tool.Selected(this);

                OnToolChanged?.Invoke(_currentTool, tool);

                _currentTool = tool;
            }
        }

        /// <summary>Not thread-safe</summary>
        private void SetSelectionTypeState(Type type, bool state)
        {
            if (state)
            {
                if (_disabledTypes.Remove(type))
                {
                    foreach (ToolTransformData transformData in _transforms)
                    {
                        if (transformData.Selected.GetType() == type)
                            _activeTransforms.Add(transformData);
                    }

                    OnTypeStateChanged?.Invoke(type, true);
                }
            }
            else
            {
                if (_disabledTypes.Add(type))
                {
                    for (int i = 0; i < _activeTransforms.Count; i++)
                    {
                        if (_activeTransforms[i].Selected.GetType() == type)
                            _activeTransforms.RemoveAt(i--);
                    }

                    OnTypeStateChanged?.Invoke(type, false);
                }
            }
        }

        private ITool? GetToolInfo(Type type)
        {
            if (_tools.TryGetValue(type, out var tuple))
                return tuple;

            ITool? tool = Activator.CreateInstance(type) as ITool;

            _tools.Add(type, tool);
            return tool;
        }

        /// <summary>Not thread-safe</summary>
        internal T? GetToolControl<T>() where T : class, IToolControl
        {
            foreach (var (_, controlTool) in _toolControls)
            {
                if (controlTool is T t)
                    return t;
            }

            return null;
        }

        /// <summary>Not thread-safe</summary>
        internal void Update()
        {
            using (new ProfilingScope("UpdateTools"))
            {
                bool wasActivePreviously = _isSnappingActive;
                _isSnappingActive = Flags.HasFlag(InputSystem.Keyboard.KeyModifiers, KeyModifier.Shift) || _isSnappingDefault;

                if (wasActivePreviously != _isSnappingActive)
                    OnSnappingChanged?.Invoke(_isSnappingActive);

                _currentTool?.Update(this);
            }
        }

        public static void SwitchTool<T>() where T : ITool
        {
            ToolManager self = EditorRuntime.GlobalSingleton.ToolManager;
            self.SwitchToolFromType(typeof(T));
        }

        public static void SetTypeState<T>(bool state)
        {
            ToolManager self = EditorRuntime.GlobalSingleton.ToolManager;
            self.SetSelectionTypeState(typeof(T), state);
        }

        public static bool GetTypeState<T>()
        {
            ToolManager self = EditorRuntime.GlobalSingleton.ToolManager;
            return !self._disabledTypes.Contains(typeof(T));
        }

        public ITool? CurrentTool => _currentTool;

        public ROList<ToolTransformData> Transforms => _activeTransforms;

        public EditorOriginMode OriginMode { get => _originMode; set => _originMode = value; }
        public EditorToolSpace ToolSpace { get => _toolSpace; set => _toolSpace = value; }

        public static bool IsSnappingDefault { get => Instance._isSnappingDefault; set => Instance._isSnappingDefault = value; }
        public static float SnapScale
        {
            get => Instance._snapScale;
            set
            {
                if (Instance._snapScale != value)
                {
                    Instance._snapScale = value;
                    OnSnapScaleChanged?.Invoke(value);
                }
            }
        }

        public static bool IsSnappingActive => Instance._isSnappingActive;

        public static bool IsCurrentToolActive => Instance._currentTool?.IsInteracting ?? false;

        public static event Action<ITool?, ITool?>? OnToolChanged;
        public static event Action<Type, bool>? OnTypeStateChanged;

        public static event Action<bool>? OnSnappingChanged;
        public static event Action<float>? OnSnapScaleChanged;

        public static ToolManager Instance => EditorRuntime.GlobalSingleton.ToolManager;
    }

    public readonly record struct ToolTransformData(object Selected, IToolTransform Transform);

    public enum EditorTool : byte
    {
        Translate = 0,
        Rotate,
        Scale,
    }

    public enum EditorOriginMode : byte
    {
        Individual = 0,
        Center
    }

    public enum EditorToolSpace : byte
    {
        Local = 0,
        Global
    }

    internal enum EditorControlTool : byte
    {
        Generic = 0,
        GeoEdit
    }
}
