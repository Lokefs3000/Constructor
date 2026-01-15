using Editor.GeoEdit;

namespace Editor.Interaction.Logic
{
    internal sealed class GeoSelectionLogic : SelectionLogic<SelectedGeoObjectBase>
    {
        public GeoSelectionLogic() { }

        public override void Run(SelectedGeoObjectBase current)
        {
            if (current is SelectedGeoBrush brush)
            {
                SelectionManager.DeselectMultiple((x) =>
                {
                    if (x is SelectedGeoShapeBase shapeBase)
                    {
                        return brush.Brush == shapeBase.Brush;
                    }

                    return false;
                });
            }
            else if (current is SelectedGeoShapeBase shapeBase)
            {
                SelectionManager.DeselectMultiple((x) =>
                {
                    if (x is SelectedGeoBrush geoBrush)
                    {
                        return shapeBase.Brush == geoBrush.Brush;
                    }

                    return false;
                });
            }
        }
    }
}
