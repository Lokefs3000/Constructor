using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Primary.Rendering.Reflection
{
    internal sealed class RenderPassSetupCache
    {
        private Dictionary<Type, RenderPassSetupAttribute?> _setups;

        internal RenderPassSetupCache()
        {
            _setups = new Dictionary<Type, RenderPassSetupAttribute?>();
        }

        ///<summary>Not thread-safe</summary>
        internal RenderPassSetupAttribute? TryGetSetupData(Type type)
        {
            if (_setups.TryGetValue(type, out RenderPassSetupAttribute? setup))
                return setup;

            setup = type.GetCustomAttribute<RenderPassSetupAttribute>();
            _setups.Add(type, setup);

            return setup;
        }

        ///<summary>Not thread-safe</summary>
        internal RenderPassSetupAttribute? TryGetSetupData<T>() where T : class, IRenderPass, new() => TryGetSetupData(typeof(T));
    }
}
