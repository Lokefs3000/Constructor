using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Reflection.Cache;
using EditorUI.Styling.Enumerator;
using Primary.Collections.ReadOnly;

namespace EditorUI.Styling
{
    public readonly record struct StylesheetContext(WidgetCachedData CachedData, StylesheetProvider Stylesheets, ROList<StylesheetClass> ClassList)
    {
        public ClassListEnumerable GetClassList(ROList<string> classList)
        {
            return new ClassListEnumerable(classList, ClassList, Stylesheets);
        }

        public PropertyEnumerable GetProperties(ushort triggerMask, ushort refTriggerMask)
        {
            return new PropertyEnumerable(CachedData, triggerMask, refTriggerMask);
        }

        public const ushort ReturnAll = 1 << 15;
    }
}
