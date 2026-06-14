using Editor.Gui.Windows;
using Editor.Interaction;
using Editor.Interaction.Controls;
using Editor.Interaction.Tools;
using Editor.UI;
using Editor.UI.Elements;

namespace Editor.Gui.View
{
    internal sealed class ToolViewSnippet : ViewSnippet
    {
        private UIToggleButton? _translateButton;
        private UIToggleButton? _rotateButton;
        private UIToggleButton? _scaleButton;

        public ToolViewSnippet(string? snippetFile, EditorViewWindow window) : base(snippetFile, window)
        {

        }

        protected override void SetupSelf()
        {
            _translateButton = RootElement?.FindElementWithId<UIToggleButton>("translate-btn");
            _rotateButton = RootElement?.FindElementWithId<UIToggleButton>("rotate-btn");
            _scaleButton = RootElement?.FindElementWithId<UIToggleButton>("scale-btn");

            _translateButton?.OnPressed += static () => ToolManager.SwitchTool<TranslateTool>();

            ToolManager.OnToolChanged += OnToolChangedCallback;

            OnToolChangedCallback(null, ToolManager.Instance.CurrentTool);
        }

        protected override void CleanupSelf()
        {
            ToolManager.OnToolChanged -= OnToolChangedCallback;
        }

        private void OnToolChangedCallback(ITool? previousTool, ITool? newTool)
        {
            _translateButton?.IsToggled = newTool is TranslateTool;
        }
    }
}
