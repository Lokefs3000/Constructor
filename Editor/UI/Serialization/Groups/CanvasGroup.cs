using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Visual;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Serialization.Groups
{
    internal sealed class CanvasGroup : SerializationGroup
    {
        public override UIElement CreateInstance(UIElement parent) => new UICanvas();

        protected override void RegisterAttributes()
        {
            AddAttribute<UICanvas, Vector2>("ClientSize", (x, y) => x.ClientSize = y);
            AddAttribute<UICanvas, Vector2>("ClientOffset", (x, y) => x.ClientOffset = y);
        }

        public override Type Type => typeof(UICanvas);
        public override string PrettyName => "Canvas";
    }
}
