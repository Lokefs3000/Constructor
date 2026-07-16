using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Primary.Common;
using PrimaryEditor.Inspector.Views.IO;

namespace PrimaryEditor.Inspector.Views
{
    public sealed class ViewGroup : ViewObject
    {
        private readonly ImmutableArray<ViewObject> _values;

        internal ViewGroup(ref ViewTypeResolver typeResolver, CustomViewGroupJson sourceData, float indentOffset) : base(ref typeResolver, sourceData, indentOffset)
        {
            if (sourceData.Values.Length > 0)
            {
                using RentedArray<ViewObject> values = RentedArray<ViewObject>.Rent(sourceData.Values.Length);
                for (int i = 0; i < sourceData.Values.Length; ++i)
                {
                    CustomViewJson json = sourceData.Values[i];
                    values[i] = ViewObject.InstantiateFromJson(ref typeResolver, json, RelativeOffset.X);
                }

                _values = [.. values];
            }
            else
            {
                _values = ImmutableArray<ViewObject>.Empty;
            }
        }

        public ImmutableArray<ViewObject> Values => _values;
    }
}
