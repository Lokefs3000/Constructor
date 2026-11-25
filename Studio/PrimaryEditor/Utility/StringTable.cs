using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Collections.ReadOnly;

namespace PrimaryEditor.Utility
{
    internal sealed class StringTable
    {
        private Dictionary<string, uint> _stringIds;
        private List<string> _stringList;

        internal StringTable()
        {
            _stringIds = new Dictionary<string, uint>();
            _stringList = new List<string>();
        }

        public uint GetStringId(string str)
        {
            ref uint index = ref CollectionsMarshal.GetValueRefOrAddDefault(_stringIds, str, out bool exists);
            if (!exists)
            {
                index = (uint)_stringList.Count;
                _stringList.Add(str);
            }

            return index;
        }

        public ROList<string> StringList => _stringList;
    }
}
