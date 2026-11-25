using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using PhysXBindGen.Data;
using PhysXBindGen.Exceptions;
using Primary.Common;

namespace PhysXBindGen.Parsing
{
    internal sealed class TypeParser
    {
        private readonly GeneratorState _state;

        internal TypeParser(GeneratorState state)
        {
            _state = state;
        }

        public bool Scan(ReadOnlySpan<char> source)
        {
            TokenReader reader = new TokenReader(source);
            while (reader.ReadNext(true))
            {
                if (reader.Token == TokenType.NewLine)
                {
                    continue;
                }
                else if (reader.Token == TokenType.Identifier)
                {
                    if (reader.Value.SequenceEqual("OPAQUE_CLASS"))
                    {
                        if (!ParseOpaqueClass(ref reader))
                            return false;
                    }
                    else if (reader.Value.SequenceEqual("ALIASED_TYPE"))
                    {
                        if (!ParseAliasedType(ref reader))
                            return false;
                    }
                }
                else if (reader.Token == TokenType.Typedef)
                {
                    if (!ParseTypedef(ref reader))
                        return false;
                }
                else if (reader.Token == TokenType.Enum)
                {
                    if (!ParseEnum(ref reader))
                        return false;
                }
                else if (reader.Token == TokenType.Struct || reader.Token == TokenType.Union)
                {
                    if (!ParseStruct(ref reader))
                        return false;
                }
                else if (reader.Token == TokenType.Pragma || reader.Token == TokenType.Include || reader.Token == TokenType.Define)
                {
                    while (reader.ReadNext(true))
                    {
                        if (reader.Token == TokenType.NewLine || reader.Token == TokenType.EndOfFile)
                            break;
                    }
                }
                else
                {
                    throw new InvalidTokenException($"Expected a valid token instead of '{reader.Token}'");
                }
            }

            return true;
        }

        private bool ParseOpaqueClass(ref TokenReader reader)
        {
            if (!reader.ReadNextToken(TokenType.ParanOpen))
                return false;

            if (!reader.ReadNextToken(TokenType.Identifier))
                return false;

            string className = reader.Value.ToString();
            ClassState? inheritedClass = null;

            if (reader.TryReadNextToken(TokenType.Comma))
            {
                reader.Forget();
                if (!reader.ReadNextToken(TokenType.Identifier))
                    return false;

                string inheritClassName = reader.Value.ToString();
                if (!_state.TryGetClass(inheritClassName, out inheritedClass))
                    return false;
            }

            ClassState classState = _state.CreateClass($"C{className}", className, inheritedClass);

            if (!reader.ReadNextToken(TokenType.ParanClose) ||
                !reader.ReadNextToken(TokenType.Semicolon))
                return false;

            return true;
        }

        private bool ParseAliasedType(ref TokenReader reader)
        {
            if (!reader.ReadNextToken(TokenType.ParanOpen))
                return false;

            if (!reader.ReadNextToken(TokenType.Identifier))
                return false;

            string sourceTypeName = reader.Value.ToString();
            if (!_state.TryGetTypedef(sourceTypeName, out TypedefState? typedef))
                return false;

            if (!reader.ReadNextToken(TokenType.Comma) ||
                !reader.ReadNextToken(TokenType.Identifier))
                return false;

            string aliasName = $"C{reader.Value}";
            TypedefState typedefState = _state.CreateTypedef(aliasName, typedef.Datatype, false);

            if (!reader.ReadNextToken(TokenType.ParanClose) ||
                !reader.ReadNextToken(TokenType.Semicolon))
                return false;

            return true;
        }

        private bool ParseTypedef(ref TokenReader reader)
        {
            if (!reader.ReadNextToken(TokenType.Identifier) ||
                !_state.TryGetTypedef(reader.Value.ToString(), out TypedefState? typedef) ||
                !reader.ReadNextToken(TokenType.Identifier))
                return false;

            string typedefName = reader.Value.ToString();
            _state.CreateTypedef(typedefName, typedef.Datatype, typedef.IsBuiltIn);

            if (!reader.ReadNextToken(TokenType.Semicolon))
                return false;

            return true;
        }

        private bool ParseEnum(ref TokenReader reader)
        {
            reader.Backup();
            if (reader.ReadNextToken(TokenType.Class))
            {
                reader.Forget();
            }
            else
            {
                reader.Restore();
            }

            if (!reader.ReadNextToken(TokenType.Identifier))
                return false;

            string enumName = reader.Value.ToString();

            TypedefDatatype datatype = TypedefDatatype.Int32;
            bool hasWidthAssigned = false;

            if (reader.TryReadNextToken(TokenType.Colon))
            {
                if (!reader.ReadNextToken(TokenType.Identifier) ||
                    !_state.TryGetTypedef(reader.Value.ToString(), out TypedefState? typedefState))
                    return false;

                datatype = typedefState.Datatype;
                hasWidthAssigned = true;
            }

            EnumState enumState = _state.CreateEnum(enumName, datatype);

            if (!reader.ReadNextToken(TokenType.CurlyOpen))
                return false;

            bool hasHadValue = false;
            long rangeMin = 0;
            long rangeMax = 0;

            long currentValue = 0;

            bool hadCommaLastValue = true;
            while (true)
            {
                if (!reader.ReadNext())
                    return false;

                if (reader.Token == TokenType.Identifier)
                {
                    if (!hadCommaLastValue)
                        return false;
                    hadCommaLastValue = false;

                    string enumValueName = reader.Value.ToString();

                    if (reader.TryReadNextToken(TokenType.Assignment))
                    {
                        if (!reader.ReadNext())
                            return false;

                        long valueS;
                        if (reader.Token == TokenType.Number)
                        {
                            valueS = (long)reader.Number;
                            if (reader.TryReadNextToken(TokenType.BitshiftLeft))
                            {
                                if (!reader.ReadNextToken(TokenType.Number))
                                    return false;

                                valueS <<= (int)reader.Number;
                            }
                        }
                        else if (reader.Token == TokenType.Identifier)
                        {
                            valueS = 0;

                            bool isFirst = true;
                            while (true)
                            {
                                if (!enumState.TryGetValue(reader.Value, out long value))
                                    return false;

                                if (isFirst)
                                {
                                    isFirst = false;
                                    valueS = value;
                                }

                                reader.Backup();
                                if (!reader.ReadNext())
                                    return false;

                                if (reader.Token == TokenType.BitwiseAnd)
                                {
                                    valueS &= value;
                                }
                                else if (reader.Token == TokenType.BitwiseOr)
                                {
                                    valueS |= value;
                                }
                                else
                                {
                                    reader.Restore();
                                    break;
                                }

                                reader.Forget();
                                if (!reader.TryReadNextToken(TokenType.Identifier))
                                    break;
                            }
                          
                        }
                        else
                        {
                            return false;
                        }

                        currentValue = valueS + 1;

                        if (!hasHadValue)
                        {
                            rangeMin = currentValue;
                            rangeMax = valueS;

                            hasHadValue = true;
                        }
                        else
                        {
                            rangeMin = Math.Min(rangeMin, valueS);
                            rangeMax = Math.Max(rangeMax, currentValue);
                        }

                        if (reader.TryReadNextToken(TokenType.Comma))
                            hadCommaLastValue = true;

                        enumState.AddValue(enumValueName, hasWidthAssigned ? GetValueWithOverflow(valueS, datatype) : valueS);
                    }
                    else
                    {
                        enumState.AddValue(enumValueName, hasWidthAssigned ? GetValueWithOverflow(currentValue++, datatype) : currentValue++);
                        rangeMax = Math.Max(currentValue, rangeMax);

                        if (reader.TryReadNextToken(TokenType.Comma))
                            hadCommaLastValue = true;
                    }
                }
                else if (reader.Token == TokenType.CurlyClose)
                {
                    break;
                }
                else
                {
                    return false;
                }
            }

            if (!hasWidthAssigned)
            {
                if (rangeMin < 0)
                {
                    datatype = (rangeMin < int.MinValue || rangeMax > int.MaxValue) ? TypedefDatatype.Int64 : TypedefDatatype.Int32;
                }
                else if (rangeMax > int.MaxValue)
                {
                    datatype = (rangeMax > uint.MaxValue) ? TypedefDatatype.UInt64 : TypedefDatatype.UInt32;
                }

                enumState.OverflowValues(datatype);
            }

            if (!reader.ReadNextToken(TokenType.Semicolon))
                return false;

            return true;

            static long GetValueWithOverflow(long value, TypedefDatatype datatype) => datatype switch
            {
                TypedefDatatype.Int8 => (sbyte)value,
                TypedefDatatype.UInt8 => (byte)value,
                TypedefDatatype.Int16 => (short)value,
                TypedefDatatype.UInt16 => (ushort)value,
                TypedefDatatype.Int32 => (int)value,
                TypedefDatatype.UInt32 => (uint)value,
                TypedefDatatype.Int64 => value,
                TypedefDatatype.UInt64 => value,
                _ => throw new NotSupportedException(),
            };
        }

        private bool ParseStruct(ref TokenReader reader)
        {
            bool treatAsUnion = reader.Token == TokenType.Union;

            if (!reader.ReadNextToken(TokenType.Identifier))
                return false;

            string structName = reader.Value.ToString();
            StructState structState = _state.CreateStruct(structName, treatAsUnion);

            if (!reader.ReadNextToken(TokenType.CurlyOpen))
                return false;

            while (true)
            {
                if (!reader.ReadNext())
                    return false;

                if (reader.Token == TokenType.CurlyClose)
                {
                    break;
                }
                else if (reader.Token == TokenType.Identifier)
                {
                    if (!_state.TryGetDatatype(reader.Value, out DatatypeState? datatype) ||
                        datatype == structState)
                        return false;

                    while (reader.TryReadNextToken(TokenType.Asterix))
                    {
                        datatype = datatype.MakePointerType();
                    }

                    if (!reader.ReadNextToken(TokenType.Identifier))
                        return false;

                    string memberName = reader.Value.ToString();

                    while (reader.TryReadNextToken(TokenType.SquareOpen))
                    {
                        if (!reader.ReadNext())
                            return false;

                        if (reader.Token == TokenType.Identifier)
                        {
                            if (!ResolveStaticSize(ref reader, out int length))
                                return false;

                            datatype = datatype.MakeFixedArrayType(length);
                        }
                        else if (reader.Token == TokenType.Number)
                        {
                            if (reader.NumberType != TokenNumberType.Int32 ||
                                reader.Number < 1)
                                return false;

                            datatype = datatype.MakeFixedArrayType((int)reader.Number);
                        }
                        else
                        {
                            return false;
                        }

                        if (!reader.ReadNextToken(TokenType.SquareClose))
                            return false;
                    }

                    structState.AddMember(memberName, datatype);

                    if (!reader.ReadNextToken(TokenType.Semicolon))
                        return false;
                }
                else
                {
                    return false;
                }
            }

            if (!reader.ReadNextToken(TokenType.Semicolon))
                return false;

            return true;
        }

        private bool ResolveStaticSize(ref TokenReader reader, [NotNullWhen(true)] out int length)
        {
            length = -1;

            if (reader.Token == TokenType.Identifier &&
                reader.Value.Equals("static_cast", StringComparison.Ordinal))
            {
                // we are screwed if this isn't casting to an int32
                if (!reader.ReadNextToken(TokenType.Less) ||
                    !reader.ReadNextToken(TokenType.Identifier) ||
                    !reader.ReadNextToken(TokenType.Greater) ||
                    !reader.ReadNextToken(TokenType.ParanOpen) ||
                    !reader.ReadNextToken(TokenType.Identifier))
                    return false;

                string enumName = reader.Value.ToString();
                if (!_state.TryGetEnum(enumName, out EnumState? enumState))
                    return false;

                if (!reader.ReadNextToken(TokenType.Colon) ||
                    !reader.ReadNextToken(TokenType.Colon) ||
                    !reader.ReadNextToken(TokenType.Identifier))
                    return false;

                if (!enumState.TryGetValue(reader.Value, out long value))
                    return false;

                length = (int)value;

                if (!reader.ReadNextToken(TokenType.ParanClose))
                    return false;

                return true;
            }

            return false;
        }
    }
}
