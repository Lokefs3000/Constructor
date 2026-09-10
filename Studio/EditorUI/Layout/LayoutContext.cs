using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EditorUI.Layout
{
    public readonly record struct LayoutContext
    {
        private readonly LayoutManager _layoutManager;
        private readonly int? _parentWidth;
        private readonly int? _parentHeight;

        internal LayoutContext(LayoutManager layoutManager, int? parentWidth, int? parentHeight)
        {
            _layoutManager = layoutManager;

            _parentWidth = parentWidth;
            _parentHeight = parentHeight;
        }

        public readonly void Fail()
        {
            
        }

        public readonly int? ParentWidth => _parentWidth;
        public readonly int? ParentHeight => _parentHeight;
    }

    public enum LayoutLockAxis : byte
    {
        None = 0,

        AxisX = 1 << 0,
        AxisY = 1 << 1,

        AxisXY = AxisX | AxisY
    }
}
