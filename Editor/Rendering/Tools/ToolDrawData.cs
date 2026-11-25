using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.Rendering.Tools
{
    public sealed class ToolDrawData
    {
        private ToolHandleType _handleType;

        private Vector3 _handleOrigin;
        private Quaternion _handleRotation;
        private float _handleScale;

        private Color[] _handleAxisColors;

        internal ToolDrawData()
        {
            _handleType = ToolHandleType.Translate;

            _handleOrigin = Vector3.Zero;
            _handleRotation = Quaternion.Identity;
            _handleScale = 1.0f;

            _handleAxisColors = [
                Color.Red,
                Color.Green,
                Color.Blue
                ];
        }

        internal void ResetToDefault()
        {
            _handleType = ToolHandleType.Translate;

            _handleOrigin = Vector3.Zero;
            _handleRotation = Quaternion.Identity;
            _handleScale = 1.0f;

            _handleAxisColors[0] = Color.Red;
            _handleAxisColors[1] = Color.Green;
            _handleAxisColors[2] = Color.Blue;
        }

        public ToolHandleType HandleType { get => _handleType; set => _handleType = value; }

        public Vector3 HandleOrigin { get => _handleOrigin; set => _handleOrigin = value; }
        public Quaternion HandleRotation { get => _handleRotation; set => _handleRotation = value; }
        public float HandleScale { get => _handleScale; set => _handleScale = value; }

        public Span<Color> HandleAxisColors => _handleAxisColors;
    }

    public enum ToolHandleType : byte
    {
        Translate = 0,
        Rotate,
        Scale
    }

    public enum ToolHandleAxis : byte
    {
        X = 0,
        Y,
        Z
    }
}
