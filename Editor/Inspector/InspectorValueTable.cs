using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using Editor.Gui.Inspector;
using Editor.Inspector.Layout;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Threading;

namespace Editor.Inspector
{
    public sealed class InspectorValueTable
    {
        private ICustomInspector? _currentInspector;
        private List<IInspectorBase> _values;

        internal InspectorValueTable()
        {
            _currentInspector = null;
            _values = new List<IInspectorBase>();
        }

        internal void SetupInspectorValues(InspectorManager manager, ICustomInspector inspector, InspectorLayout layout)
        {
            _currentInspector = inspector;

            _values.Clear();
            _values.EnsureCapacity((int)BitOperations.RoundUpToPowerOf2((uint)layout.Objects.Length));

            LoadSubInspectorLayout(manager, layout, null);
        }

        private void LoadSubInspectorLayout(InspectorManager manager, InspectorLayout layout, IInspectorBase? parent)
        {
            foreach (LayoutObject @object in layout.Objects)
            {
                IInspectorBase @base;
                if (@object.IsObject)
                {
                    @base = manager.Constructors.GetInspectorObjectConstructor(@object.Field.GetCanonicalType())(manager, parent, @object.PropertyName!, @object.Field);
                }
                else
                {
                    @base = manager.Constructors.GetInspectorValueConstructor(@object.Field.GetCanonicalType())(manager, parent, @object.PropertyName!, @object.Field);
                }

                _values.Add(@base);

                if (@object.IsObject && manager.GetLayoutForType(@object.Field.GetCanonicalType(), out InspectorLayout? subLayout))
                {
                    LoadSubInspectorLayout(manager, subLayout, @base);
                }
            }
        }

        internal void UpdateValues<T>(ref T target)
        {
            if (typeof(T).IsClass)
            {
                GenericRefObject generic = new GenericRefObject { Value = target! };
                for (int i = 0; i < _values.Count; i++)
                {
                    IInspectorBase @base = _values[i];
                    @base.UpdateValueObject(generic);
                }
            }
            else
            {
                for (int i = 0; i < _values.Count; i++)
                {
                    IInspectorBase @base = _values[i];
                    @base.UpdateValueByRef(ref Unsafe.As<T, GenericRefValue>(ref target));
                }
            }
        }

        internal void UpdateValuesIncremental<T>(ref T target, Range range)
        {
            
        }

        public ICustomInspector? CurrentInspector => _currentInspector;
        public ROList<IInspectorBase> Values => _values;
    }
}
