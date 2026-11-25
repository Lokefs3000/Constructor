using System;
using System.Collections.Generic;
using System.Text;

namespace PhysXBindGen.Data
{
    internal sealed class TypedefState : DatatypeState
    {
        private readonly string _name;
        private readonly TypedefDatatype _datatype;
        private readonly bool _isGenericAlias;
        private readonly bool _isBuiltIn;

        internal TypedefState(string name, TypedefDatatype datatype, bool isGenericAlias, bool isBuiltIn)
        {
            _name = name;
            _datatype = datatype;
            _isGenericAlias = isGenericAlias;
            _isBuiltIn = isBuiltIn;
        }

        public string Name => _name;
        public TypedefDatatype Datatype => _datatype;
        public bool IsGenericAlias => _isGenericAlias;
        public bool IsBuiltIn => _isBuiltIn;
    }

    internal enum TypedefDatatype : byte
    {
        Int8 = 0,
        UInt8,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Single,
        Double,
        Void,
        Bool
    }
}
