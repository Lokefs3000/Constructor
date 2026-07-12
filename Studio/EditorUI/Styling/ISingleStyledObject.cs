using System;
using System.Collections.Generic;
using System.Text;

namespace EditorUI.Styling
{
    public interface ISingleStyledObject
    {
        protected internal StyledObject? StyledObject { get; }

        protected internal StylesheetProvider StylesheetProvider { get; }
        protected internal bool GetAllProperties { get; }
    }
}
