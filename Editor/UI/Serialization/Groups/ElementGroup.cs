using Editor.UI.Datatypes;
using Editor.UI.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Serialization.Groups
{
    internal sealed class ElementGroup : SerializationGroup
    {
        public override UIElement CreateInstance(UIElement parent) => new UIElement();

        protected override void RegisterAttributes()
        {
            AddAttribute<UIElement, UIValue2>("Position", (x, y) => x.Transform.Position = y);
            AddAttribute<UIElement, UIValue2>("Size", (x, y) => x.Transform.Size = y);

            AddAttribute<UIElement, string>("Id", (x, y) => x.Id = y);
        }

        public override Type Type => typeof(UIElement);
        public override string PrettyName => "Element";
    }
}
