using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace Primary.Reflection
{
    public static class TypeKeyReflector
    {
        public static string TypeToKey(Type type)
        {
            return $"{type.Assembly.GetName().Name}?{type.FullName}";
        }

        [RequiresUnreferencedCode("The type might be removed")]
        public static Type? KeyToType(string key)
        {
            int index = key.IndexOf('?');
            if (index == -1)
                return null;

            return Type.GetType($"{key[(index + 1)..]}, {key[..index]}");
        }
    }
}
