using System;
using System.Collections.Generic;
using System.Text;

namespace EditorUI.Styling
{
    public interface ISingleStyledObject
    {
        public StyledObject? StyledObject { get; }

        public StylesheetProvider StylesheetProvider { get; }
        public bool GetAllProperties { get; }
    }
}
