using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Primary.Common;
using PrimaryEditor.Inspector.Views.IO;

namespace PrimaryEditor.Inspector.Views
{
    public sealed class ViewPreset : ViewObject
    {
        private readonly string _displayName;
        private readonly FrozenDictionary<string, ImmutableArray<ViewConstant>> _presets;
        private readonly ImmutableArray<ViewObject> _values;

        internal ViewPreset(ref ViewTypeResolver typeResolver, CustomViewPresetJson sourceData, float indentOffset) : base(ref typeResolver, sourceData, indentOffset)
        {
            _displayName = sourceData.Display;

            if (sourceData.Presets.Count > 0)
            {
                Dictionary<string, ImmutableArray<ViewConstant>> presets = new Dictionary<string, ImmutableArray<ViewConstant>>();
                foreach (var (presetName, presetValues) in sourceData.Presets)
                {
                    if (presetValues.Length > 0)
                    {
                        using RentedArray<ViewConstant> constants = RentedArray<ViewConstant>.Rent(presetValues.Length);
                        for (int i = 0; i < presetValues.Length; ++i)
                        {
                            PresetValueJson json = presetValues[i];
                            ViewSource constantSource = typeResolver.ResolveSource(json.Name);

                            constants[i] = constantSource.Type.IsValueType ?
                                (ViewConstant)Activator.CreateInstance(typeof(ViewConstant.ValueType<>).MakeGenericType(constantSource.Type), BindingFlags.Instance | BindingFlags.NonPublic, null, [constantSource, json], null)! :
                                (ViewConstant)Activator.CreateInstance(typeof(ViewConstant.Object<>).MakeGenericType(constantSource.Type), BindingFlags.Instance | BindingFlags.NonPublic, null, [constantSource, json], null)!;
                        }

                        presets.Add(presetName, [.. constants]);
                    }
                }

                _presets = presets.ToFrozenDictionary();
            }
            else
            {
                _presets = FrozenDictionary<string, ImmutableArray<ViewConstant>>.Empty;
            }

            if (sourceData.Values != null && sourceData.Values.Length > 0)
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

        public string DisplayName => _displayName;
        public FrozenDictionary<string, ImmutableArray<ViewConstant>> Presets => _presets;
        public ImmutableArray<ViewObject> Values => _values;
    }
}
