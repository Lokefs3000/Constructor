using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Mathematics;
using EditorUI.Serialization;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary.Common;

namespace PrimaryEditor.Windows
{
    public sealed class EditorViewWindow : WidgetWindow
    {
        public EditorViewWindow(WindowManager windowManager, ValueSerializer valueSerializer) : base(windowManager, valueSerializer)
        {
            Widget testWidget1 = new Widget
            {
                Position = new UIValue2(100, 100),
                Size = new UIValue2(100, 100),

                BackgroundColor = Color.Red,

                Parent = RootWidget
            };
        }
    }
}
