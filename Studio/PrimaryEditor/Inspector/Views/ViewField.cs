using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using PrimaryEditor.Inspector.Views.IO;

namespace PrimaryEditor.Inspector.Views
{
    public sealed class ViewField : ViewObject
    {
        private readonly ViewSource _fieldSource;
        private readonly string _displayName;

        internal ViewField(ref ViewTypeResolver typeResolver, CustomViewFieldJson sourceData, float indentOffset) : base(ref typeResolver, sourceData, indentOffset)
        {
            _fieldSource = typeResolver.ResolveSource(sourceData.Name);
            _displayName = sourceData.Display ?? sourceData.Name;
        }

        internal ViewField(ref ViewTypeResolver typeResolver, string typeName, string? displayName, float paddingValue, float indentOffset) : base(paddingValue, indentOffset)
        {
            _fieldSource = typeResolver.ResolveSource(typeName);
            _displayName = displayName ?? typeName;
        }

        internal override bool HasReferencesTo(Assembly assembly)
        {
            if (_fieldSource.Type.Assembly == assembly)
                return true;

            if (_fieldSource.Opaque is FieldInfo)
            {
                FieldInfo fieldInfo = _fieldSource.Field!;
                return fieldInfo.FieldType.Assembly == assembly || fieldInfo.DeclaringType?.Assembly == assembly;
            }
            else
            {
                PropertyInfo propertyInfo = _fieldSource.Property!;
                return propertyInfo.PropertyType.Assembly == assembly || propertyInfo.DeclaringType?.Assembly == assembly;
            }
        }

        public ViewSource FieldSource => _fieldSource;
        public string DisplayName => _displayName;
    }
}
