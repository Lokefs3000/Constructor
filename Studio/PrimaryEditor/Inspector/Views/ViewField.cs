using System;
using System.Collections.Generic;
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

        public ViewSource FieldSource => _fieldSource;
        public string DisplayName => _displayName;
    }
}
