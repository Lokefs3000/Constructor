using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using PrimaryEditor.Windows.Inspector.Populators;

namespace PrimaryEditor.Windows.Inspector
{
    internal sealed class PopulatorDatabase
    {
        private readonly FrozenDictionary<Type, WidgetPopulator> _populators;

        internal PopulatorDatabase()
        {
            _populators = new Dictionary<Type, WidgetPopulator>
            {
                { typeof(Enum), new EnumPopulator() },
                { typeof(bool), new BooleanPopulator() },
            }.ToFrozenDictionary();
        }

        internal bool TryGetPopulatorFor(Type type, [NotNullWhen(true)] out WidgetPopulator? value)
        {
            if (type.IsEnum)
                type = typeof(Enum);

            return _populators.TryGetValue(type, out value);
        }
    }
}
