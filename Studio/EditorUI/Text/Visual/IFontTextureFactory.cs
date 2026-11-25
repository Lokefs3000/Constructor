using System;
using System.Collections.Generic;
using System.Text;
using Primary.Mathematics;

namespace EditorUI.Text.Visual
{
    public interface IFontTextureFactory
    {
        public IFontTexture CreateTexture(Int2 size);
    }
}
