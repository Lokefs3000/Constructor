using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections.ReadOnly;

namespace PrimaryEditor.Inspector
{
    public abstract class InspectorContext
    {
        public abstract void UpdateValues();
        public abstract bool MatchesContext<T>(ref T value);

        public abstract ROList<InspectorGroup> Groups { get; }
    }
}
