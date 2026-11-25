using System;
using System.Collections.Generic;
using System.Text;

namespace PhysXBindGen.Data
{
    internal abstract class DatatypeState
    {
        public PointerDatatype MakePointerType()
        {
            return new PointerDatatype(this);
        }

        public FixedArrayDataType MakeFixedArrayType(int length)
        {
            return new FixedArrayDataType(this, length);
        }
    }

    internal sealed class PointerDatatype : DatatypeState
    {
        private readonly DatatypeState _state;

        internal PointerDatatype(DatatypeState state)
        {
            _state = state;
        }

        public DatatypeState State => _state;
    }

    internal sealed class FixedArrayDataType : DatatypeState
    {
        private readonly DatatypeState _state;
        private readonly int _length;

        internal FixedArrayDataType(DatatypeState state, int length)
        {
            _state = state;
            _length = length;
        }

        public DatatypeState State => _state;
        public int Length => _length;
    }
}
