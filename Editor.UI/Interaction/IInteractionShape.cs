using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Interaction
{
    public interface IInteractionShape
    {
        public bool Intersects(Vector2 point);
    }
}
