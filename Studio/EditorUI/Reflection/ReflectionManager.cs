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
        private WidgetDatabase _widgetDatabase;

        private MethodGenerator _methodGenerator;

        internal ReflectionManager()
        {
            _widgetPropertyCache = new WidgetPropertyCache(this);
            _widgetDatabase = new WidgetDatabase();

            _methodGenerator = new MethodGenerator();
        }

        public WidgetPropertyCache WidgetPropertyCache => _widgetPropertyCache;
        public WidgetDatabase WidgetDatabase => _widgetDatabase;

        public MethodGenerator MethodGenerator => _methodGenerator;
    }
}
