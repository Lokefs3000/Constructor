using CommunityToolkit.HighPerformance;
using Primary.Common;
using Serilog;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Processors.Shaders
{
    public static class ShaderPreprocessor
    {
        public static ShaderPreprocessorResult Inspect(ILogger? logger, string sourceFileName, string sourceFile, string[] includeDirs)
        {
            ShaderPreprocessorResult result = new ShaderPreprocessorResult();

            ParseFileSource(result, logger, in sourceFileName, in sourceFile, includeDirs);
            return result;
        }

        private static void ParseFileSource(ShaderPreprocessorResult result, ILogger? logger, ref readonly string inSourceFileName, ref readonly string inStringSource, string[] searchDirs)
        {
            InternalState state = new InternalState
            {
                Result = result,
                File = new PreprocessorFileResult(),

                Source = inStringSource.ToArray(),
                SearchDirectories = searchDirs,

                Logger = logger,
                SourceFileName = inSourceFileName,
            };

            while (!IsEOF(ref state))
            {
                state.Next();

                char c = Peek(ref state);
                switch (c)
                {
                    case '#': HandlePDirective(ref state); break;
                    case '\n': state.LineIndex++; state.LineStartCharIndex = state.Current + 1; break;

                    default:
                        {
                            //if (char.IsLetter(c))
                            //    HandleIdentifier(ref state);
                            break;
                        }
                }

                Advance(ref state);
            }

            result.Files.Add(inSourceFileName, state.File);
        }

        #region Handlers
        private static void HandlePDirective(ref InternalState state)
        {
            Advance(ref state);

            Span<char> directiveName = ReadIdentifier(ref state);

            if (directiveName.Equals("pragma", StringComparison.CurrentCulture))
            {
                SkipWhitespace(ref state);
                HandlePragmaDirective(ref state);
            }
            else if (directiveName.Equals("include", StringComparison.Ordinal))
            {
                SkipWhitespace(ref state);
                HandleIncludeDirective(ref state);
            }
        }

        private static void HandleIdentifier(ref InternalState state)
        {
            Span<char> identifierText = ReadIdentifier(ref state);
        }

        private static void HandlePragmaDirective(ref InternalState state)
        {
            Span<char> pragmaName = ReadIdentifier(ref state);

            if (pragmaName.Equals("bindgroup", StringComparison.Ordinal))
            {
                SkipWhitespace(ref state);
                Span<char> bindGroupName = ReadString(ref state);

                if (!bindGroupName.ContainsAnyExceptInRange((char)32, (char)127))
                    state.File.BindGroups.Add(bindGroupName.ToString());
                else
                    ReportError(ref state, "Bind group '{gn}' contains letter outside of the valid ascii range (32 - 127)", bindGroupName.ToString());
            }
            else if (pragmaName.Equals("variant", StringComparison.Ordinal))
            {
                SkipWhitespace(ref state);
                Span<char> variantType = ReadIdentifier(ref state);

                if (!Enum.TryParse(variantType, true, out ShaderVariantType type))
                {
                    ReportError(ref state, "Unknown variant type '{vt}' specified in pragma '{ln}'", variantType.ToString(), GetLineAsString(ref state));
                    return;
                }

                SkipWhitespace(ref state);
                Span<char> identiferText = ReadString(ref state);

                if (identiferText.IsEmpty)
                {
                    ReportError(ref state, "Identifier text for shader variant must be longer then 0 characters");
                    return;
                }

                if (identiferText.Length > byte.MaxValue)
                {
                    ReportError(ref state, "Identifier text '{it}' for shader variant must not be longer then 256 characters");
                    return;
                }

                if (identiferText.ContainsAnyExceptInRange((char)32, (char)127))
                {
                    ReportError(ref state, "Identifier text '{it}' for shader variant must only contain letters within the printable ascii range (32 - 127)", identiferText.ToString());
                    return;
                }

                string identifierString = identiferText.ToString().ToLowerInvariant();
                if (state.File.Variants.Exists((x) => identifierString.Equals(x.Identifier, StringComparison.Ordinal)))
                {
                    ReportError(ref state, "Shader variant with identifier text '{it}' already exists", identifierString);
                    return;
                }

                string? displayName = null;

                SkipWhitespace(ref state);
                if (Peek(ref state) != '\n')
                {
                    Span<char> displayNameText = ReadString(ref state);

                    if (displayNameText.IsEmpty)
                    {
                        ReportError(ref state, "Display name for shader variant must be longer then 0 characters");
                        return;
                    }

                    if (displayNameText.Length > byte.MaxValue)
                    {
                        ReportError(ref state, "Display name '{it}' for shader variant must not be longer then 256 characters");
                        return;
                    }

                    if (displayNameText.ContainsAnyExceptInRange((char)32, (char)127))
                    {
                        ReportError(ref state, "Display name '{it}' for shader variant must only contain letters within the printable ascii range (32 - 127)", identiferText.ToString());
                        return;
                    }

                    displayName = displayNameText.ToString();
                }

                state.File.Variants.Add(new ShaderVariant
                {
                    Type = type,
                    Identifier = identifierString,
                    DisplayName = displayName
                });
            }
        }

        private static void HandleIncludeDirective(ref InternalState state)
        {
            Span<char> includePath = ReadString(ref state);

            string includePathStr = includePath.ToString();
            if (state.Result.Files.ContainsKey(includePathStr))
                return; //already processed. and yes ik there are non complicancy issues but ill ignore them for now

            for (int i = 0; i < state.SearchDirectories.Length; i++)
            {
                string fullPath = Path.Combine(state.SearchDirectories[i], includePathStr);
                if (File.Exists(fullPath))
                {
                    using FileStream? stream = FileUtility.TryWaitOpen(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    if (stream != null)
                    {
                        using PoolArray<byte> pool = ArrayPool<byte>.Shared.Rent((int)stream.Length);
                        stream.ReadExactly(pool.AsSpan(0, (int)stream.Length));

                        string includeSource = Encoding.UTF8.GetString(pool.AsSpan(0, (int)stream.Length));
                        string sourceFileName = Path.GetFileName(includePathStr);

                        ParseFileSource(state.Result, state.Logger, in sourceFileName, in includeSource, state.SearchDirectories);
                        return;
                    }
                }
            }

            ReportError(ref state, "Could not find include '{inc}' file within provided search directories", includePathStr);
        }
        #endregion

        #region Utility
        private static bool IsEOF(ref InternalState state) => state.Current >= state.Source.Length;
        private static char Peek(ref InternalState state) => IsEOF(ref state) ? '\0' : state.Source.DangerousGetReferenceAt(state.Current);
        private static char PeekNext(ref InternalState state) => (state.Current + 1 < state.Source.Length) ? '\0' : state.Source.DangerousGetReferenceAt(state.Current + 1);
        private static char Advance(ref InternalState state) => IsEOF(ref state) ? '\0' : state.Source.DangerousGetReferenceAt(state.Current++);

        private static Span<char> GetSnippet(ref InternalState state)
        {
            if (state.Start == state.Current)
            {
                if (IsEOF(ref state))
                    return Span<char>.Empty;
                return new Span<char>(ref state.Source[state.Current]);
            }
            else
            {
                return state.Source.AsSpan(state.Start, state.Current - state.Start);
            }
        }

        private static string GetLineAsString(ref InternalState state)
        {
            int current = state.Current;

            do
            {
                current++;
            } while (current < state.Source.Length && state.Source[current] != '\n');

            return state.Source.AsSpan(state.Start, current - state.Start).ToString();
        }

        private static void ReportError(ref InternalState state, string errorMessage, params object?[]? parameters)
        {
            if (Math.Abs(state.Current - state.Start) <= 1)
                state.Logger?.Error("[P/'{sourceName}':({line}:{char})]: " + errorMessage, [state.SourceFileName, state.LineIndex, state.Current - state.LineStartCharIndex, .. parameters ?? Array.Empty<object?>()]);
            else
                state.Logger?.Error("[P/'{sourceName}':({sline}:{schar},{eline}:{echar})]: " + errorMessage, [state.SourceFileName, state.StartLineIndex, state.Start - state.StartLineStartCharIndex, state.LineIndex, state.Current - state.LineStartCharIndex, .. parameters ?? Array.Empty<object?>()]);
        }

        private static void ReportErrorAndThrow(ref InternalState state, string errorMessage, params object?[]? parameters)
        {
            ReportError(ref state, errorMessage, parameters);
            throw new ShaderParseException(state.StartLineIndex, state.Start - state.LineStartCharIndex, state.SourceFileName);
        }

        private static Span<char> ReadIdentifier(ref InternalState state)
        {
            int start = state.Current;

            if (!char.IsLetter(state.Source[start]))
                return Span<char>.Empty;
            if (IsEOF(ref state) || !char.IsLetterOrDigit(Peek(ref state)))
                return new Span<char>(ref state.Source[start]);

            do
            {
                Advance(ref state);

                if (IsEOF(ref state))
                    break;
            } while (char.IsLetterOrDigit(Peek(ref state)));

            return state.Source.AsSpan(start, state.Current - start);
        }

        private static Span<char> ReadString(ref InternalState state)
        {
            int start = state.Current;
            if (IsEOF(ref state) || !(state.Source[start] == '\"' || state.Source[start] == '\''))
                return Span<char>.Empty;

            do
            {
                if (Advance(ref state) == '\n')
                {
                    state.LineIndex++;
                    state.LineStartCharIndex = state.Current;
                }

                if (IsEOF(ref state))
                    ReportError(ref state, "Reading string ended with EOF");
            } while (state.Source[state.Current] != '\"' && state.Source[state.Current] != '\'');

            Advance(ref state);
            return state.Source.AsSpan(start + 1, state.Current - start - 2);
        }

        private static void SkipWhitespace(ref InternalState state)
        {
            if (IsEOF(ref state) || !char.IsWhiteSpace(state.Source[state.Current]))
                return;

            do
            {
                Advance(ref state);
            } while (char.IsWhiteSpace(Peek(ref state)));
        }
        #endregion

        private struct InternalState
        {
            public ShaderPreprocessorResult Result;
            public PreprocessorFileResult File;

            public char[] Source;
            public string[] SearchDirectories;

            public int Start;
            public int Current;

            public int StartLineIndex;
            public int StartLineStartCharIndex;

            public int LineIndex;
            public int LineStartCharIndex;

            public ILogger? Logger;
            public string SourceFileName;

            public InternalState()
            {
                Result = default!;
                File = default!;

                Source = Array.Empty<char>();
                SearchDirectories = Array.Empty<string>();

                Start = 0;
                Current = 0;

                LineIndex = 0;

                Logger = null;
                SourceFileName = string.Empty;
            }

            public void Next()
            {
                Start = Current;

                StartLineIndex = LineIndex;
                StartLineStartCharIndex = LineStartCharIndex;
            }
        }
    }

    public class ShaderPreprocessorResult
    {
        public Dictionary<string, PreprocessorFileResult> Files;

        internal ShaderPreprocessorResult()
        {
            Files = new Dictionary<string, PreprocessorFileResult>();
        }
    }

    public class PreprocessorFileResult
    {
        public HashSet<string> BindGroups;
        public List<ShaderVariant> Variants;

        internal PreprocessorFileResult()
        {
            BindGroups = new HashSet<string>();
            Variants = new List<ShaderVariant>();
        }
    }

    public record struct ShaderVariant
    {
        public ShaderVariantType Type;
        public string Identifier;
        public string? DisplayName;
    }

    public enum ShaderVariantType : byte
    {
        Toggle = 0,
    }

    [Serializable]
    public class ShaderParseException : Exception
    {
        private int _lineIndex;
        private int _charIndex;

        private string? _sourceName;
        private string? _snippet;

        public ShaderParseException(int lineIndex, int charIndex, string? sourceName, string? snippet = null)
        {
            _lineIndex = lineIndex;
            _charIndex = charIndex;
            _sourceName = sourceName;
            _snippet = snippet;
        }

        public ShaderParseException() : base() { }
        public ShaderParseException(string message) : base(message) { }
        public ShaderParseException(string message, Exception inner) : base(message, inner) { }

        public override string Message => _snippet != null ?
            $"{_sourceName}({_lineIndex}:{_charIndex}): \"{_snippet}\" {base.Message}" :
            $"{_sourceName}({_lineIndex}:{_charIndex}): {base.Message}";

        public int LineIndex => _lineIndex;
        public int CharIndex => _charIndex;

        public string? SourceName => _sourceName;
        public string? Snippet => _snippet;
    }
}
