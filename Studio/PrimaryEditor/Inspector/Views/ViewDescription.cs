using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Primary.Common;
using PrimaryEditor.Inspector.Views.IO;

namespace PrimaryEditor.Inspector.Views
{
    public sealed class ViewDescription
    {
        private readonly ImmutableArray<ViewObject> _values;

        internal ViewDescription(ref ViewTypeResolver typeResolver, CustomViewJson[] sourceData)
        {
            if (sourceData.Length > 0)
            {
                using RentedArray<ViewObject> values = RentedArray<ViewObject>.Rent(sourceData.Length);
                for (int i = 0; i < sourceData.Length; ++i)
                {
                    CustomViewJson json = sourceData[i];
                    values[i] = ViewObject.InstantiateFromJson(ref typeResolver, json, 0.0f);
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
