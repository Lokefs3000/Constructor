using Editor.Rendering;

namespace Editor.GeoEdit
{
    internal interface IGeoTool
    {
        public void ConnectEvents();
        public void DisconnectEvents();

        public void Update();
        //public void Render(ref readonly GeoToolRenderInterface @interface);
    }
}
