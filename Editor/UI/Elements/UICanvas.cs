using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Elements
{
    public class UICanvas : UIFrame
    {
        private Vector2 _clientOffset;
        private Vector2 _clientSize;

        protected override Vector2 GetSizeAsParent() => _clientSize;

        public Vector2 ClientOffset { get => _clientOffset; set { _clientOffset = value; InvalidateSelf(UIInvalidationFlags.Layout); } }
        public Vector2 ClientSize { get => _clientSize; set { _clientSize = value; InvalidateSelf(UIInvalidationFlags.Layout); } }
    }
}
