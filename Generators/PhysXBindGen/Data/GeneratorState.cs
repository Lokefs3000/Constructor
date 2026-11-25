using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using PhysXBindGen.Exceptions;

namespace PhysXBindGen.Data
{
    internal sealed class GeneratorState
    {
        private readonly Dictionary<string, DatatypeState> _datatypes;
        private readonly Dictionary<string, DatatypeState>.AlternateLookup<ReadOnlySpan<char>> _datatypesAlt;

        internal GeneratorState()
        {
            _datatypes = new Dictionary<string, DatatypeState>(s_builtInTypedefs);
            _datatypesAlt = _datatypes.GetAlternateLookup<ReadOnlySpan<char>>();
        }

        internal ClassState CreateClass(string aliasedClassName, string className, ClassState? inherited)
        {
            ref DatatypeState? state = ref CollectionsMarshal.GetValueRefOrAddDefault(_datatypes, className, out bool exists);
            if (exists)
                throw new DuplicateStateException($"Duplicate class name '{className}'");

            state = new ClassState(className, inherited);

            _datatypes.Add(aliasedClassName, state);
            return (ClassState)state;
        }

        internal TypedefState CreateTypedef(string typedefName, TypedefDatatype datatype, bool isGenericAlias)
        {
            ref DatatypeState? state = ref CollectionsMarshal.GetValueRefOrAddDefault(_datatypes, typedefName, out bool exists);
            if (exists)
                throw new DuplicateStateException($"Duplicate typedef name '{typedefName}'");

            state = new TypedefState(typedefName, datatype, isGenericAlias, false);
            return (TypedefState)state;
        }

        internal EnumState CreateEnum(string enumName, TypedefDatatype datatype)
        {
            ref DatatypeState? state = ref CollectionsMarshal.GetValueRefOrAddDefault(_datatypes, enumName, out bool exists);
            if (exists)
                throw new DuplicateStateException($"Duplicate enum name '{enumName}'");

            state = new EnumState(enumName, datatype);
            return (EnumState)state;
        }

        internal StructState CreateStruct(string structName, bool treatAsUnion)
        {
            ref DatatypeState? state = ref CollectionsMarshal.GetValueRefOrAddDefault(_datatypes, structName, out bool exists);
            if (exists)
                throw new DuplicateStateException($"Duplicate struct name '{structName}'");

            state = new StructState(structName, treatAsUnion);
            return (StructState)state;
        }

        internal MethodState CreateMethod(string methodName, DatatypeState returnType, string? comment)
        {
            ref DatatypeState? state = ref CollectionsMarshal.GetValueRefOrAddDefault(_datatypes, methodName, out bool exists);
            if (exists)
                throw new DuplicateStateException($"Duplicate method name '{methodName}'");

            state = new MethodState(methodName, returnType, null, comment, null);
            return (MethodState)state;
        }

        internal bool TryGetDatatype(ReadOnlySpan<char> datatypeName, [NotNullWhen(true)] out DatatypeState? datatypeState) => _datatypesAlt.TryGetValue(datatypeName, out datatypeState);

        internal bool TryGetClass(ReadOnlySpan<char> className, [NotNullWhen(true)] out ClassState? classState)
        {
            classState = null;
            return _datatypesAlt.TryGetValue(className, out DatatypeState? datatype) && (classState = datatype as ClassState) != null;
        }

        internal bool TryGetTypedef(ReadOnlySpan<char> typedefName, [NotNullWhen(true)] out TypedefState? typedefState)
        {
            typedefState = null;
            return _datatypesAlt.TryGetValue(typedefName, out DatatypeState? datatype) && (typedefState = datatype as TypedefState) != null;
        }

        internal bool TryGetEnum(ReadOnlySpan<char> enumName, [NotNullWhen(true)] out EnumState? enumState)
        {
            enumState = null;
            return _datatypesAlt.TryGetValue(enumName, out DatatypeState? datatype) && (enumState = datatype as EnumState) != null;
        }

        internal bool TryGetStruct(ReadOnlySpan<char> structName, [NotNullWhen(true)] out StructState? structState)
        {
            structState = null;
            return _datatypesAlt.TryGetValue(structName, out DatatypeState? datatype) && (structState = datatype as StructState) != null;
        }

        internal bool TryGetMethod(ReadOnlySpan<char> methodName, [NotNullWhen(true)] out MethodState? methodState)
        {
            methodState = null;
            return _datatypesAlt.TryGetValue(methodName, out DatatypeState? datatype) && (methodState = datatype as MethodState) != null;
        }

        internal Dictionary<string, DatatypeState> Datatypes => _datatypes;

        private static readonly ImmutableArray<KeyValuePair<string, DatatypeState>> s_builtInTypedefs = [
            new KeyValuePair<string, DatatypeState>("int8_t", new TypedefState("int8_t", TypedefDatatype.Int8, true, true)),
            new KeyValuePair<string, DatatypeState>("char", new TypedefState("char", TypedefDatatype.Int8, true, true)),
            new KeyValuePair<string, DatatypeState>("uint8_t", new TypedefState("uint8_t", TypedefDatatype.UInt8, true, true)),
            new KeyValuePair<string, DatatypeState>("int16_t", new TypedefState("int16_t", TypedefDatatype.Int16, true, true)),
            new KeyValuePair<string, DatatypeState>("uint16_t", new TypedefState("uint16_t", TypedefDatatype.UInt16, true, true)),
            new KeyValuePair<string, DatatypeState>("int32_t", new TypedefState("int32_t", TypedefDatatype.Int32, true, true)),
            new KeyValuePair<string, DatatypeState>("uint32_t", new TypedefState("uint32_t", TypedefDatatype.UInt32, true, true)),
            new KeyValuePair<string, DatatypeState>("int64_t", new TypedefState("int64_t", TypedefDatatype.Int64, true, true)),
            new KeyValuePair<string, DatatypeState>("uint64_t", new TypedefState("uint64_t", TypedefDatatype.UInt64, true, true)),
            new KeyValuePair<string, DatatypeState>("float", new TypedefState("float", TypedefDatatype.Single, true, true)),
            new KeyValuePair<string, DatatypeState>("double", new TypedefState("double", TypedefDatatype.Double, true, true)),
            new KeyValuePair<string, DatatypeState>("void", new TypedefState("void", TypedefDatatype.Void, true, true)),
            new KeyValuePair<string, DatatypeState>("bool", new TypedefState("bool", TypedefDatatype.Bool, true, true))
            ];
    }
}
