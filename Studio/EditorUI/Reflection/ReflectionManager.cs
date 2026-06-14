using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Reflection.Cache;
using EditorUI.Reflection.Dynamic;

namespace EditorUI.Reflection
{
    public sealed class ReflectionManager
    {
        private WidgetPropertyCache _widgetPropertyCache;

        private MethodGenerator _methodGenerator;

        internal ReflectionManager()
        {
            _widgetPropertyCache = new WidgetPropertyCache(this);

            _methodGenerator = new MethodGenerator();
        }

        public WidgetPropertyCache WidgetPropertyCache => _widgetPropertyCache;

        public MethodGenerator MethodGenerator => _methodGenerator;
    }
}
