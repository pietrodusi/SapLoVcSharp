using SapLoVcSharp.Core.Common;
using SapLoVcSharp.Core.Lexing;

namespace SapLoVcSharp.Core.Parsing
{
    /// <summary>
    /// Exception thrown when the parser encounters a syntax error.
    /// </summary>
    public class ParserException : SapLoVcException
    {
        public TokenType? ExpectedType { get; }
        public TokenType? ActualType { get; }

        public ParserException(string message, SourcePosition position)
            : base(message, position)
        {
        }

        public ParserException(
            string message,
            SourcePosition position,
            TokenType? expected,
            TokenType? actual)
            : base(message, position)
        {
            ExpectedType = expected;
            ActualType = actual;
        }
    }
}