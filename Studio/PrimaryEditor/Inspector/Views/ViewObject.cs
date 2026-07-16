using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Reflection;
using System.Text;
using Primary.Common;
using Primary.Mathematics;
using PrimaryEditor.Inspector.Views.IO;

namespace PrimaryEditor.Inspector.Views
{
    public abstract class ViewObject
    {
        private readonly ImmutableArray<ViewCondition> _conditions;
        private readonly ViewConditionMode _conditionMode;

        private readonly Int2 _relativeOffset;

        internal ViewObject(ref ViewTypeResolver typeResolver, CustomViewJson sourceData, float indentOffset)
        {
            if (sourceData.Conditions != null && sourceData.Conditions.Length > 0)
            {
                using RentedArray<ViewCondition> conditions = RentedArray<ViewCondition>.Rent(sourceData.Conditions.Length);
                for (int i = 0; i < sourceData.Conditions.Length; ++i)
                {
                    ConditionJson json = sourceData.Conditions[i];
                    ViewSource conditionSource = typeResolver.ResolveSource(json.Name);

                    conditions[i] = conditionSource.Type.IsValueType ?
                        (ViewCondition)Activator.CreateInstance(typeof(ViewCondition.ValueType<>).MakeGenericType(conditionSource.Type), BindingFlags.Instance | BindingFlags.NonPublic, null, [conditionSource, json], null)! :
                        (ViewCondition)Activator.CreateInstance(typeof(ViewCondition.Object<>).MakeGenericType(conditionSource.Type), BindingFlags.Instance | BindingFlags.NonPublic, null, [conditionSource, json], null)!;
                }

                _conditions = [.. conditions];
                _conditionMode = sourceData.Comparison;
            }
            else
            {
                _conditions = ImmutableArray<ViewCondition>.Empty;
            }

            _relativeOffset = (new Vector2(sourceData.Indent + indentOffset, sourceData.Padding) * 24.0f).AsInt2();
        }

        public ImmutableArray<ViewCondition> Conditions => _conditions;
        public ViewConditionMode ConditionMode => _conditionMode;

        public Int2 RelativeOffset => _relativeOffset;

        internal static ViewObject InstantiateFromJson(ref ViewTypeResolver typeResolver, CustomViewJson sourceData, float indentOffset)
        {
            if (sourceData is CustomViewFieldJson field)
                return new ViewField(ref typeResolver, field, indentOffset);
            else if (sourceData is CustomViewGroupJson group)
                return new ViewGroup(ref typeResolver, group, indentOffset);
            else if (sourceData is CustomViewPresetJson preset)
                return new ViewPreset(ref typeResolver, preset, indentOffset);
            else
                throw new NotSupportedException(sourceData.GetType().FullName);
        }
    }

    public enum ViewConditionMode : byte
    {
        All = 0,
        Any,
        Exclusive,
        None,
    }
}
