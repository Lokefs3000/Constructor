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

        public ViewSource FieldSource => _fieldSource;
        public string DisplayName => _displayName;
    }
}
