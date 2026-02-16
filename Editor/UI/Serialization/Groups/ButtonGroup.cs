using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Visual;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Serialization.Groups
{
    internal sealed class ButtonGroup : SerializationGroup
    {
        public override UIElement CreateInstance(UIElement parent) => new UIButton();

        protected override void RegisterAttributes()
        {
            AddAttribute<UIButton, float>("CornerRadius", (x, y) => x.CornerRadius = y);
            AddAttribute<UIButton, UIRoundedCorner>("CornerRounding", (x, y) => x.CornerRounding = y);

            AddAttribute<UIButton, UIColor>("Fill", (x, y) => x.FillColor = y);

            AddAttribute<UIButton, UIColor>("Stroke", (x, y) => x.StrokeColor = y);
            AddAttribute<UIButton, UIStrokePosition>("StrokePosition", (x, y) => x.StrokePosition = y);
            AddAttribute<UIButton, float>("StrokeWeight", (x, y) => x.StrokeWeight = y);
        }

        public override Type Type => typeof(UIButton);
        public override string PrettyName => "Button";
    }
}
