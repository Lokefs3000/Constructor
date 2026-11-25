using System;
using System.Collections.Generic;
using System.Text;
using Primary.Mathematics;

namespace EditorUI.Text.Visual
{
    public interface IFontTexture : IDisposable
    {
        public void Resize(Int2 newSize);
    }
}
