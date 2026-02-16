using Editor.UI.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Serialization.Groups
{
    internal class TreeViewGroup : SerializationGroup
    {
        public override UIElement CreateInstance(UIElement parent) => new UITreeView();

        protected override void RegisterAttributes()
        {
            
        }

        public override Type Type => typeof(UITreeView);
        public override string PrettyName => "TreeView";
    }
}
