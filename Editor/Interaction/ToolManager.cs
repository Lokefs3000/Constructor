using Editor.Interaction.Controls;
using Editor.Interaction.Tools;
using Primary.Input;
using Primary.Input.Devices;

namespace Editor.Interaction
{
    public sealed class ToolManager
    {
        private IControlTool[] _controls;
        private ITool[] _tools;

        private EditorTool _tool;
        private EditorOriginMode _originMode;
        private EditorToolSpace _toolSpace;

        private EditorControlTool _currentControlTool;

        private bool _isSnappingDefault;
        private bool _isSnappingActive;
        private float _snapScale;

        internal ToolManager()
        {
            _controls = [
                new CoreControlTool(this),
                //new GeoEditControlTool(this)
                ];

            _tools = [
                null!,//new TranslateTool(this),
                null!,
                null!,
                ];

            _tool = EditorTool.Translate;
            _originMode = EditorOriginMode.Individual;
            _toolSpace = EditorToolSpace.Local;

            _currentControlTool = EditorControlTool.Generic;

            _isSnappingDefault = false;
            _isSnappingActive = false;
            _snapScale = 1.0f;

            _controls[0].Activated();
            //_tools[0].Selected();
        }

        /// <summary>Not thread-safe</summary>
        internal void SwitchTool(EditorTool tool)
        {
            if (tool == _tool)
                return;

            _tools[(int)_tool].Deselected();
            _tools[(int)tool].Selected();

            _tool = tool;
        }

        /// <summary>Not thread-safe</summary>
        internal void SwitchControl(EditorControlTool control)
        {
            if (_currentControlTool == control)
                return;

            _controls[(int)_currentControlTool].Deactivated();
            _tools[(int)_tool].Deselected();

            _currentControlTool = control;

            _controls[(int)control].Activated();
            _tools[(int)_tool].Selected();
        }

        /// <summary>Not thread-safe</summary>
        internal void Update()
        {
            _isSnappingActive = InputSystem.Keyboard.IsKeyDown(KeyCode.LeftControl)
                || InputSystem.Keyboard.IsKeyDown(KeyCode.RightControl);

            if (_isSnappingDefault)
                _isSnappingActive = !_isSnappingActive;

            //_tools[(int)Tool].Update();
        }

        public EditorTool Tool => _tool;
        public EditorOriginMode OriginMode { get => _originMode; set => _originMode = value; }
        public EditorToolSpace ToolSpace { get => _toolSpace; set => _toolSpace = value; }

        internal ITool ToolObject => _tools[(int)_tool];

        internal IControlTool ActiveControlTool => _controls[(int)_currentControlTool];
        internal EditorControlTool ActiveControlToolType => _currentControlTool;

        public static bool IsSnappingDefault { get => Editor.GlobalSingleton.ToolManager._isSnappingDefault; set => Editor.GlobalSingleton.ToolManager._isSnappingDefault = value; }
        public static bool IsSnappingActive => Editor.GlobalSingleton.ToolManager._isSnappingActive;
        public static float SnapScale { get => Editor.GlobalSingleton.ToolManager._snapScale; set => Editor.GlobalSingleton.ToolManager._snapScale = value; }
    }

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
