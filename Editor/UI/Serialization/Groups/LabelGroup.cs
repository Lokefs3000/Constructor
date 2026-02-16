using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Primary.Assets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Serialization.Groups
{
    internal sealed class LabelGroup : SerializationGroup
    {
        public override UIElement CreateInstance(UIElement parent) => new UILabel();

        protected override void RegisterAttributes()
        {
            AddAttribute<UILabel, UIFontStyle>("FontStyle", (x, y) => x.FontStyle = y);

            AddAttribute<UILabel, string>("Text", (x, y) => x.Text = y);
            AddAttribute<UILabel, float>("Size", (x, y) => x.Size = y);

            AddAttribute<UILabel, UITextAlignment>("Alignment", (x, y) => x.Alignment = y);
            AddAttribute<UILabel, UITextOverflow>("Overflow", (x, y) => x.Overflow = y);

            AddAttribute<UILabel, UITextAutoSize>("AutoSize", (x, y) => x.AutoSize = y);

            AddAttribute<UILabel, UIColor>("Fill", (x, y) => x.FillColor = y);
        }

        public override Type Type => typeof(UILabel);
        public override string PrettyName => "Label";
    }
}
