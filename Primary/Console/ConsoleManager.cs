using Primary.Common;
using Primary.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Console
{
    internal class ConsoleManager
    {
        private readonly Dictionary<string, FieldInfo> _variables;

        internal ConsoleManager()
        {
            _variables = new Dictionary<string, FieldInfo>();

            ScanClassForCVars(typeof(RenderingCVars));
        }

        internal void ScanClassForCVars([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type)
        {
            CommandClassNamespaceAttribute? namespaceAttribute = type.GetCustomAttribute<CommandClassNamespaceAttribute>();
            Checking.Assert(namespaceAttribute != null, "CVar class must have a namespace attribute attached");

            FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                string name;

                CommandAliasAttribute? aliasAttribute = field.GetCustomAttribute<CommandAliasAttribute>();
                if (aliasAttribute != null)
                    name = $"{namespaceAttribute.Namespace}_{aliasAttribute.Alias}";
                else
                    name = $"{namespaceAttribute.Namespace}_{field.Name.ToLowerInvariant()}";

                if (!_variables.TryAdd(name, field))
                {
                    EngLog.Console.Error("Failed to add cvar as a different one already exists with the name: {n} (field: {f}, class: {c})", name, field.Name, type.Name);
                }
            }
        }
    }
}
