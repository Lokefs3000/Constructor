using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EditorUI.Input
{
    public interface IInteractionShape
    {
        public bool Intersects(Vector2 point);
    }
}
