using Editor.UI.Styling;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI
{
    public interface IElementOwner
    {
        public StyleUpdater StyleUpdater { get; }
        public StyleProvider StyleProvider { get; }

        public IWindowHost? ParentHost { get; }
    }
}
