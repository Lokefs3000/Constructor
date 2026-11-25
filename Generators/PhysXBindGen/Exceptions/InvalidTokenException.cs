using System;
using System.Collections.Generic;
using System.Text;
using PhysXBindGen.Parsing;

namespace PhysXBindGen.Exceptions
{
    internal sealed class InvalidTokenException : Exception
    {
        public InvalidTokenException()
        {
        }

        public InvalidTokenException(string? message) : base(message)
        {
        }

        public InvalidTokenException(TokenType expected, TokenType got) : base($"Expected token '{expected}' but got instead '{got}'")
        {

        }
    }
}
