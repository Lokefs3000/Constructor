using Editor.Gui.Designer;
using System.Diagnostics.CodeAnalysis;

namespace Editor.UI.Designer
{
    internal sealed class UIDesigner : UIWindow
    {
        private HierchyManager _hierchyManager;
        private CanvasManager _canvasManager;
        private ToolboxManager _toolboxManager;

        public UIDesigner(int uniqueWindowId) : base(uniqueWindowId)
        {
            WindowTitle = "UI designer";
        }

        protected override void InitializePostLoad()
        {
            _hierchyManager = new HierchyManager(this);
            _canvasManager = new CanvasManager(this);
            _toolboxManager = new ToolboxManager(this);

            CreateNewLayout();
        }

        internal void CreateNewLayout()
        {
            _hierchyManager.ClearView();
            _canvasManager.ClearView();
        }

        internal HierchyManager HierchyManager => _hierchyManager;
        internal CanvasManager CanvasManager => _canvasManager;
        internal ToolboxManager ToolboxManager => _toolboxManager;
    }
}
