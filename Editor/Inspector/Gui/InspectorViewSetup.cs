using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Editor.Gui.Inspector;

namespace Editor.Inspector.Gui
{
    internal readonly record struct InspectorViewSetup(IInspectorView View, ImmutableArray<InspectorElement> Elements, InspectorViewDisplay Display);
}
