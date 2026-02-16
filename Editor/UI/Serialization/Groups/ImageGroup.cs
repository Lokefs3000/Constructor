using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Primary.Assets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Serialization.Groups
{
    internal sealed class ImageGroup : SerializationGroup
    {
        public override UIElement CreateInstance(UIElement parent) => new UIImage();

        protected override void RegisterAttributes()
        {
            AddAttribute<UIImage, TextureAsset>("Image", (x, y) => x.Image = y);

            AddAttribute<UIImage, UIColor>("Tint", (x, y) => x.TintColor = y);
        }

        public override Type Type => typeof(UIImage);
        public override string PrettyName => "Image";
    }
}
