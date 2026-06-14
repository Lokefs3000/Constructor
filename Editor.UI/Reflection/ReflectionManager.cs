using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Editor.UI.Reflection
{
    public sealed class ReflectionManager
    {
        private PropertyCache _propertyCache;
        private MethodGenerator _methodGenerator;
        private ElementCache _elementCache;
        private ModifierCache _modifierCache;

        internal ReflectionManager()
        {
            _propertyCache = new PropertyCache();
            _methodGenerator = new MethodGenerator();
            _elementCache = new ElementCache();
            _modifierCache = new ModifierCache();

            _elementCache.DiscoverAssembly(typeof(ReflectionManager).Assembly);
            _modifierCache.DiscoverAssembly(typeof(ReflectionManager).Assembly);

            _elementCache.DiscoverAssembly(Assembly.GetEntryAssembly()!);
            _modifierCache.DiscoverAssembly(Assembly.GetEntryAssembly()!);
        }

        public PropertyCache PropertyCache => _propertyCache;
        public MethodGenerator MethodGenerator => _methodGenerator;
        public ElementCache ElementCache => _elementCache;
        public ModifierCache ModifierCache => _modifierCache;
    }
}
