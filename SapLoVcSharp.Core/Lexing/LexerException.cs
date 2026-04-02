using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Lexing
{
    /// <summary>
    /// Exception thrown when the lexer encounters an invalid character or token.
    /// </summary>
    public class LexerException : SapLoVcException
    {
        public LexerException(string message, SourcePosition position)
            : base(message, position)
        {
        }

        public LexerException(string message, SourcePosition position, Exception innerException)
            : base(message, position, innerException)
        {
        }
    }
}