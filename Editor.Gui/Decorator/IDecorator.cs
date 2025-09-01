using Editor.Gui.Elements;
using Editor.Gui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Gui.Decorator
{
    public interface IDecorator
    {
        public void DrawVisual(Element element, GuiCommandBuffer commandBuffer);
        public void ModifyLayout(Element element);
    }
}
