using Editor.Interaction.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Interaction.Controls
{
    internal interface IControlTool
    {
        public ReadOnlySpan<IToolTransform> Transforms { get; }

        public void Activated();
        public void Deactivated();

        public event Action<IToolTransform>? NewTransformSelected;
        public event Action<IToolTransform>? OldTransformDeselected;
    }
}
