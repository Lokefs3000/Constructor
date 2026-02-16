using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Primary.Assets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Serialization.Groups
{
    internal sealed class SplitContainerGroup : SerializationGroup
    {
        public override UIElement CreateInstance(UIElement parent) => new UISplitContainer();

        protected override void RegisterAttributes()
        {
            AddAttribute<UISplitContainer, UIColor>("Split", (x, y) => x.SplitColor = y);
        }

        public override Type Type => typeof(UISplitContainer);
        public override string PrettyName => "SplitContainer";
    }
}
