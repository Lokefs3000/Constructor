using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Primary.Collections.ReadOnly;

namespace Editor.Inspector
{
    public sealed class InspectorHolder
    {
        private List<InspectorValueTable> _valueTables;
        private bool[] _equalityArray;

        internal InspectorHolder()
        {
            _valueTables = new List<InspectorValueTable>();
            _equalityArray = [];
        }

        internal void AddValueTable(InspectorValueTable valueTable)
        {
            _valueTables.Add(valueTable);
        }

        public void UpdateTables<T>(Span<T> values)
        {
            Guard.HasSizeEqualTo(values, _valueTables.Count);

            for (int i = 0; i < _valueTables.Count; i++)
            {
                InspectorValueTable valueTable = _valueTables[i];
                valueTable.UpdateValues(ref values[i]);
            }
        }

        public void UpdateTablesIncremental<T>(Span<T> values, Range range)
        {
            Span<InspectorValueTable> tables = _valueTables.AsSpan()[range];
            Guard.HasSizeEqualTo(values, tables.Length);

            for (int i = 0; i < tables.Length; i++)
            {
                InspectorValueTable valueTable = tables[i];
                valueTable.UpdateValues(ref values[i]);
            }
        }

        public InspectorObject<T> GetValueAt<T>(int sourceIndex)
        {
            return (InspectorObject<T>)_valueTables[0].Values[sourceIndex];
        }

        public void SetValues<T>(ReadOnlySpan<object> targets, T? value)
        {
            for (int i = 0; i < _valueTables.Count; i++)
            {
                InspectorValueTable valueTable = _valueTables[i];
                ((InspectorObject<T>)valueTable.Values[i]).SetValue(targets[i], value);
            }
        }

        public ROList<InspectorValueTable> ValueTables => _valueTables;
        public ReadOnlySpan<bool> EqualityArray => _equalityArray;
    }
}
