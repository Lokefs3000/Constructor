using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using EditorUI.Serialization.Value;
using EditorUI.Widgets;
using EditorUI.Widgets.Stylists;
using Primary.Collections.ReadOnly;

namespace EditorUI.Reflection
{
    public sealed class WidgetDatabase
    {
        private Dictionary<string, Type> _database;

        internal WidgetDatabase()
        {
            _database = new Dictionary<string, Type>();
        }

        public void LoadWidgetFromType(Type type, object attributeData)
        {
            if (!type.IsClass && type.IsGenericType)
                return;

            if (!type.IsAssignableTo(typeof(Widget)) && !type.IsAssignableTo(typeof(Stylist)))
                return;

            string widgetName = type.Name;
            if (_database.ContainsKey(widgetName))
                return;

            _database.Add(widgetName, type);
        }

        public bool TryGetWidgetTypeFromName(string typeName, [NotNullWhen(true)] out Type? value)
        {
            return _database.TryGetValue(typeName, out value);
        }

        public RODictionary<string, Type> Entries => _database;
    }
}
