using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommunityToolkit.HighPerformance;
using PhysXBindGen.Exceptions;

namespace PhysXBindGen.Data
{
    internal sealed class StructState : DatatypeState
    {
        private readonly string _name;
        private readonly bool _treatAsUnion;

        private readonly List<StructMember> _members;

        internal StructState(string name, bool treatAsUnion)
        {
            _name = name;
            _treatAsUnion = treatAsUnion;

            _members = new List<StructMember>();
        }

        public void AddMember(string name, DatatypeState datatype)
        {
            foreach (StructMember structMember in _members)
            {
                if (structMember.Name == name)
                {
                    throw new DuplicateStateException($"Struct member with name '{name}' already exists");
                }
            }

            _members.Add(new StructMember(name, datatype));
        }

        public bool TryGetMember(ReadOnlySpan<char> structName, [NotNullWhen(true)] out DatatypeState? datatype)
        {
            foreach (ref StructMember structValue in _members.AsSpan())
            {
                if (structName.Equals(structValue.Name, StringComparison.Ordinal))
                {
                    datatype = structValue.Datatype;
                    return true;
                }
            }

            datatype = null;
            return false;
        }

        public string Name => _name;
        public bool TreatAsUnion => _treatAsUnion;

        public List<StructMember> Members => _members;
    }

    internal readonly record struct StructMember(string Name, DatatypeState Datatype);
}
