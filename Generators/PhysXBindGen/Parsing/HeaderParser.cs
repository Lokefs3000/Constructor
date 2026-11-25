using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using PhysXBindGen.Data;
using PhysXBindGen.Exceptions;

namespace PhysXBindGen.Parsing
{
    internal sealed class HeaderParser
    {
        private readonly GeneratorState _state;

        internal HeaderParser(GeneratorState state)
        {
            _state = state;
        }

        public bool Scan(ReadOnlySpan<char> source)
        {
            ReadOnlySpan<char> activeComment = [];

            TokenReader reader = new TokenReader(source);
            while (reader.ReadNext())
            {
                if (reader.Token == TokenType.Comment)
                {
                    activeComment = reader.Value;
                }
                else if (reader.Token == TokenType.Identifier)
                {
                    if (reader.Value.Equals("INTEROP_API", StringComparison.Ordinal))
                    {
                        bool shouldReturnBeConstant = reader.TryReadNextToken(TokenType.Const);

                        if (!TryReadDatatype(ref reader, true, out DatatypeState? returnType))
                            throw new StateNotFoundException($"Failed to read return type '{reader.Value}'");

                        reader.ReadNextToken(TokenType.Identifier);
                        int underscoreIndex = reader.Value.IndexOf('_');

                        MethodState methodState;
                        if (underscoreIndex != -1)
                        {
                            ReadOnlySpan<char> ownerClassName = reader.Value[..underscoreIndex];
                            if (!_state.TryGetClass(ownerClassName, out ClassState? classState))
                                throw new StateNotFoundException($"Failed to find class owning function '{reader.Value}'");

                            ReadOnlySpan<char> methodName = reader.Value[(underscoreIndex + 1)..];
                            ReadOnlySpan<char> overload = [];

                            int secondUnderscoreIndex = methodName.LastIndexOf('_');
                            if (secondUnderscoreIndex != -1)
                            {
                                overload = methodName[(secondUnderscoreIndex + 1)..];
                                methodName = methodName[..secondUnderscoreIndex];
                            }

                            methodState = classState.CreateMethod(methodName.ToString(), returnType, overload.IsEmpty ? null : overload.ToString(), activeComment.IsEmpty ? null : activeComment.ToString());
                        }
                        else
                        {
                            methodState = _state.CreateMethod(reader.Value.ToString(), returnType, activeComment.IsEmpty ? null : activeComment.ToString());
                        }

                        bool isFirstArgument = true;
                        bool hasHadCommaSeperator = true;

                        reader.ReadNextToken(TokenType.ParanOpen);
                        while (reader.ReadNext())
                        {
                            bool shouldBeConstant = reader.Token == TokenType.Const;
                            if (shouldBeConstant)
                            {
                                if (!reader.ReadNext())
                                    break;
                            }

                            if (reader.Token == TokenType.ParanClose)
                            {
                                break;
                            }
                            else if (reader.Token == TokenType.Comma)
                            {
                                if (hasHadCommaSeperator)
                                    throw new SourceException("Expected argument after previous comma");
                                hasHadCommaSeperator = true;
                            }
                            else if (reader.Token == TokenType.Identifier)
                            {
                                if (!hasHadCommaSeperator)
                                    throw new InvalidTokenException(TokenType.Comma, reader.Token);
                                hasHadCommaSeperator = false;

                                if (!TryReadDatatype(ref reader, false, out DatatypeState? argumentType))
                                    throw new StateNotFoundException($"Failed to read argument type '{reader.Value}'");

                                reader.ReadNextToken(TokenType.Identifier);
                                if (underscoreIndex != -1 && isFirstArgument)
                                {
                                    if (argumentType is not PointerDatatype pointer || pointer.State != methodState.Owner || !reader.Value.Equals("cpp", StringComparison.Ordinal))
                                        throw new SourceException("Function with owning class must have first argument be self reference");
                                }
                                else
                                {
                                    ReadOnlySpan<char> argumentName = reader.Value;
                                    if (methodState.Owner != null && argumentName.Equals("cpp", StringComparison.Ordinal))
                                        throw new SourceException("Function with owning class cannot redefine self reference argument");

                                    double? defaultValue = null;
                                    if (reader.TryReadNextToken(TokenType.Assignment))
                                    {
                                        if (!reader.ReadNext())
                                            return false;

                                        if (reader.Token == TokenType.Number)
                                        {
                                            defaultValue = (long)reader.Number;
                                        }
                                        else if (reader.Token == TokenType.BoolFalse)
                                        {
                                            defaultValue = 0.0;
                                        }
                                        else if (reader.Token == TokenType.BoolTrue)
                                        {
                                            defaultValue = 1.0;
                                        }
                                        else if (reader.Token == TokenType.Nullptr)
                                        {
                                            defaultValue = MethodState.Null;
                                        }
                                        else if (reader.Token == TokenType.Identifier && argumentType is EnumState enumState)
                                        {
                                            reader.ReadNextToken(TokenType.Colon);
                                            reader.ReadNextToken(TokenType.Colon);
                                            reader.ReadNextToken(TokenType.Identifier);

                                            if (!enumState.TryGetValue(reader.Value, out long value))
                                                throw new StateNotFoundException($"Failed to find enum value '{reader.Value}'");

                                            defaultValue = value;
                                        }
                                        else
                                        {
                                            throw new InvalidTokenException("Expected either number or null for default state");
                                        }
                                    }

                                    methodState.AddArgument(argumentName.ToString(), argumentType, defaultValue);
                                }

                                isFirstArgument = false;
                            }
                            else
                            {
                                throw new InvalidTokenException($"Unexpected token in function argument list '{reader.Token}'");
                            }
                        }

                        reader.ReadNextToken(TokenType.Semicolon);
                    }
                }
            }

            return true;
        }

        private bool TryReadDatatype(ref TokenReader reader, bool shouldReadIdentifier, [NotNullWhen(true)] out DatatypeState? type)
        {
            if (shouldReadIdentifier)
                reader.ReadNextToken(TokenType.Identifier);
            if (!_state.TryGetDatatype(reader.Value, out type))
                throw new StateNotFoundException($"Failed to find type '{reader.Value}'");

            while (reader.TryReadNextToken(TokenType.Asterix))
            {
                type = type.MakePointerType();
            }

            return true;
        }
    }
}
