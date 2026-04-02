using SapLoVcSharp.Core.Lexing;

namespace SapLoVcSharp.Core.Parsing
{
    /// <summary>
    /// Maps token types to their precedence levels.
    /// </summary>
    public static class PrecedenceTable
    {
        public static Precedence GetPrecedence(TokenType type)
        {
            return type switch
            {
                // Logical
                TokenType.Or => Precedence.Or,
                TokenType.And => Precedence.And,

                // Equality
                TokenType.Equal => Precedence.Equality,
                TokenType.NotEqual => Precedence.Equality,
                TokenType.EQ => Precedence.Equality,
                TokenType.NE => Precedence.Equality,

                // Comparison
                TokenType.Less => Precedence.Comparison,
                TokenType.LessEqual => Precedence.Comparison,
                TokenType.Greater => Precedence.Comparison,
                TokenType.GreaterEqual => Precedence.Comparison,
                TokenType.LT => Precedence.Comparison,
                TokenType.LE => Precedence.Comparison,
                TokenType.GT => Precedence.Comparison,
                TokenType.GE => Precedence.Comparison,

                // Term (addition/subtraction)
                TokenType.Plus => Precedence.Term,
                TokenType.Minus => Precedence.Term,

                // Factor (multiplication/division)
                TokenType.Multiply => Precedence.Factor,
                TokenType.Divide => Precedence.Factor,

                // String concatenation
                TokenType.Concat => Precedence.Term,

                // Special
                TokenType.In => Precedence.Comparison,
                TokenType.Specified => Precedence.Unary,

                _ => Precedence.None
            };
        }
    }
}