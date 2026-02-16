using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Visual;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Serialization.Groups
{
    internal sealed class FrameGroup : SerializationGroup
    {
        public override UIElement CreateInstance(UIElement parent) => new UIFrame();

        protected override void RegisterAttributes()
        {
            AddAttribute<UIFrame, float>("CornerRadius", (x, y) => x.CornerRadius = y);
            AddAttribute<UIFrame, UIRoundedCorner>("CornerRounding", (x, y) => x.CornerRounding = y);

            AddAttribute<UIFrame, UIColor>("Fill", (x, y) => x.FillColor = y);

            AddAttribute<UIFrame, UIColor>("Stroke", (x, y) => x.StrokeColor = y);
            AddAttribute<UIFrame, UIStrokePosition>("StrokePosition", (x, y) => x.StrokePosition = y);
            AddAttribute<UIFrame, float>("StrokeWeight", (x, y) => x.StrokeWeight = y);
        }

        public override Type Type => typeof(UIFrame);
        public override string PrettyName => "Frame";
    }
}
