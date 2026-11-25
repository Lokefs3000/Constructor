using Editor.UI;
using Primary;
using Primary.Rendering;
using System.Numerics;

namespace EdUIDesigner
{
    internal sealed class Designer : Engine
    {
        private UIManager _uiManager;

        public Designer(ReadOnlySpan<string> args) : base(args)
        {
            _uiManager = new UIManager(Log.EdUI);
        }

        public override void Dispose()
        {
            

            base.Dispose();
        }

        public void Run()
        {
            Window primaryWindow = WindowManager.CreateWindow("UI designer", new Vector2(1336.0f, 726.0f), CreateWindowFlags.Resizable);
            UIDockHost primaryHost = _uiManager.CreateHostedDock(primaryWindow);



            RenderingManager.SetNewRenderPath(new DesignerRenderPath(this));

            while (!primaryWindow.IsClosed)
            {
                Time.BeginNewFrame();
                ProfilingManager.StartProfilingForFrame();

                ThreadHelper.ExecutePendingTasks();

                _uiManager.UpdatePendingLayouts();

                InputSystem.UpdatePending();
                EventManager.PollEvents();

                RenderingManager.Render();
            }
        }

        internal UIManager UIManager => _uiManager;
    }
}
