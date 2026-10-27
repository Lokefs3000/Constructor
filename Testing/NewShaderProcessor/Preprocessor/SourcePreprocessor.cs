using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Primary.Collections;
using Primary.Common;
using Primary.Data;
using Serilog;

namespace NewShaderProcessor.Preprocessor
{
    internal ref struct SourcePreprocessor : IDisposable
    {
        private readonly ILogger? _logger;

        private RentedList<string> _variants;

        private readonly ReadOnlySpan<char> _source;
        private ReadOnlySpan<char> _token;

        private int _start;
        private int _index;

        private string _sourceName;
        private int _line;

        internal SourcePreprocessor(ILogger? logger, ReadOnlySpan<char> source, string sourceName)
        {
            _logger = logger;

            _variants = new RentedList<string>();

            _source = source;
            _token = ReadOnlySpan<char>.Empty;

            _start = 0;
            _index = -1;

            _sourceName = sourceName;
            _line = 0;
        }

        public void Dispose()
        {
            _variants.Dispose();
        }

        internal void Execute()
        {
            foreach (var span in _source.Tokenize('\n'))
            {
                _index = 0;
                _start = 0;

                _token = span.Trim('\r');
                if (!_token.IsEmpty)
                {
                    char first = Advance();
                    if (first == '#')
                    {
                        ReadOnlySpan<char> directive = ReadIdentifier();
                        if (directive.SequenceEqual("line"))
                        {
                            TrySkipWhitespace();
                            _line = int.Parse(ReadIdentifier());

                            TrySkipWhitespace();
                            if (Peek() == '"')
                            {
                                _sourceName = ReadString().ToString();
                            }
                        }
                        else if (directive.SequenceEqual("pragma"))
                        {
                            TrySkipWhitespace();
                            ReadOnlySpan<char> pragmaName = ReadIdentifier();

                            if (pragmaName.SequenceEqual("variant_toggle"))
                            {
                                HandleVariantTogglePragma();
                            }
                        }
                    }
                }

                ++_line;
            }
        }

        private void HandleVariantTogglePragma()
        {
            StartPragmaArgumentList(out bool isContainedInList);

            if (IsPragmaArgumentListDone(isContainedInList))
                ThrowException("Missing variant toggle name");

            string variantName = ReadString().ToString();
            if (string.IsNullOrEmpty(variantName))
                ThrowException("Variant name must be non-empty");

            if (_variants.Contains(variantName))
                ThrowException("Duplicate variant name {n}", variantName);

            _variants.Add(variantName);

            if (!IsPragmaArgumentListDone(isContainedInList))
                ThrowException("Expected end for toggle variant");
        }

        private void StartPragmaArgumentList(out bool isContainedInList)
        {
            TrySkipWhitespace();

            if (Peek() == '(')
            {
                Advance();
                isContainedInList = true;
            }
            else
            {
                isContainedInList = false;
            }
        }

        private bool IsPragmaArgumentListDone(bool isContainedInList)
        {
            char next = Peek();
            if (isContainedInList)
            {
                if (next == ')')
                {
                    return true;
                }
            }
            else if (next == '\n')
            {
                return true;
            }

            return false;
        }

        private ReadOnlySpan<char> ReadIdentifier()
        {
            _start = _index;

            while (true)
            {
                char next = Advance();
                if (!char.IsLetterOrDigit(next) && next != '_')
                {
                    break;
                }
            }

            return _token[_start.._index];
        }

        private ReadOnlySpan<char> ReadString()
        {
            if (Advance() != '"')
            {
                ThrowException("Expected string");
            }

            _start = _index;

            while (true)
            {
                if (IsEOF)
                    ThrowException("Expected quotation mark for string end");

                char next = Advance();
                if (char.IsControl(next))
                    ThrowException("Expected valid char in string");

                if (next == '"')
                    break;
            }

            return _token[_start..(_index - 1)];
        }

        private void TrySkipWhitespace()
        {
            if (!char.IsWhiteSpace(Peek()))
                return;

            while (char.IsWhiteSpace(Advance())) ;
        }

        private void SkipUntil(char target)
        {
            while (!IsEOF)
            {
                char letter = Advance();
                if (letter == target)
                {
                    break;
                }
            }
        }

        private void SkipUntil(ReadOnlySpan<char> sequence)
        {
            if (sequence.IsEmpty)
                return;

            int index = 0;
            while (!IsEOF)
            {
                char letter = Advance();
                if (sequence.DangerousGetReferenceAt(index) == letter)
                {
                    if (++index >= sequence.Length)
                    {
                        return;
                    }
                }
            }
        }

        [DoesNotReturn]
        private readonly void ThrowException(string message, params object?[]? args)
        {
            _logger?.Error($"[{{loc}}]: {message}", args == null ? [LocationFromIndex] : [LocationFromIndex, .. args]);
            throw new PreprocessorException(LocationFromIndex, args == null ? message : string.Format(message, args));
        }

        private readonly char Peek() => _index >= _token.Length ? '\0' : _token.DangerousGetReferenceAt(_index);
        private readonly char PeekNext() => _index + 1 >= _token.Length ? '\0' : _token.DangerousGetReferenceAt(_index);

        private char Advance() => _index >= _token.Length ? '\0' : _token.DangerousGetReferenceAt(_index++);

        private readonly bool IsEOF => _index >= _token.Length;

        private readonly SourceLocation LocationFromStart => new SourceLocation(_sourceName, _line, _start);
        private readonly SourceLocation LocationFromIndex => new SourceLocation(_sourceName, _line, _index);

        private readonly record struct StateBackup(string Source, int Start, int Index, int Line, int LineStartIndex);
    }
}
