using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Primary.Assets;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.UI.Serialization.Groups
{
    internal sealed class SplitPanelGroup : SerializationGroup
    {
        public override UIElement CreateInstance(UIElement parent) => Unsafe.As<UISplitContainer>(parent).AddSplit(null, UISplitDirection.Vertical);

        protected override void RegisterAttributes()
        {
            AddAttribute<UISplitPanel, UISplitDirection>("Direction", (x, y) => x.Direction = y);
            AddAttribute<UISplitPanel, float>("Position", (x, y) => x.Position = y);
        }

        public override Type Type => typeof(UISplitPanel);
        public override string PrettyName => "SplitPanel";
    }
}
