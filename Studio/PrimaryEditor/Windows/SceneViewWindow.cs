using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Serialization;
using EditorUI.Windowing;

namespace PrimaryEditor.Windows
{
    public sealed class SceneViewWindow : EditorWindow
    {
        public SceneViewWindow(WindowManager windowManager, ValueSerializer valueSerializer) : base(windowManager, valueSerializer)
        {
            LoadLayout("Editor/UI/SceneView.layout");
        }

        protected internal override void InitializeSelf()
        {

        }

        protected internal override void CleanupReloadSelf()
        {
            
        }
    }
}
