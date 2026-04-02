using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Lexing
{
    /// <summary>
    /// Represents a lexical token with its type, value, and source position.
    /// </summary>
    public record Token(
        TokenType Type,
        string Value,
        SourcePosition Position)
    {
        public override string ToString() =>
            $"{Type,-20} '{Value,-15}' at {Position}";
    }
}