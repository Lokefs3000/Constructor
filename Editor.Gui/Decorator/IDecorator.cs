using Editor.Gui.Elements;
using Editor.Gui.Graphics;

namespace Editor.Gui.Decorator
{
    public interface IDecorator
    {
        public void DrawVisual(Element element, GuiCommandBuffer commandBuffer);
        public void ModifyLayout(Element element);
    }
}
