using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Modifiers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace Editor.UI.Serialization.Modifiers
{
    internal sealed class MenuBarModifier : SerializationModifier
    {
        protected override void AddProperties()
        {
            //UseNodeChildren();

            AddProperty<UIMenuBar, UIColor>("Fill", nameof(UIMenuBar.FillColor));
            AddProperty<UIMenuBar, UIColor>("Stroke", nameof(UIMenuBar.StrokeColor));

            AddProperty<UIMenuBar, float>("StrokeWeight", nameof(UIMenuBar.StrokeWeight));
        }

        protected override object? HandleNode(XmlNode node, IUILayoutModifier self, object? parent)
        {
            UIMenuBar menuBar = Unsafe.As<UIMenuBar>(self);
            return SerializationTable.Default.DeserializeNode(node, Unsafe.As<UIElement>(parent) ?? throw new NotImplementedException());
        }

        public override Type Type => typeof(UIMenuBar);
        public override string PrettyName => "MenuBar";
    }
}
