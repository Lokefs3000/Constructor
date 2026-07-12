using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Assets;
using EditorUI.Mathematics;
using EditorUI.Serialization;
using EditorUI.Text;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary.Assets;
using Primary.Common;
using Primary.Input;
using Primary.Mathematics;
using PrimaryEditor.Assets;
using TerraFX.Interop.Windows;

namespace PrimaryEditor.Windows
{
    public sealed class EditorViewWindow : WidgetWindow
    {
        public EditorViewWindow(WindowManager windowManager, ValueSerializer valueSerializer) : base(windowManager, valueSerializer)
        {
            
        }
    }
}
