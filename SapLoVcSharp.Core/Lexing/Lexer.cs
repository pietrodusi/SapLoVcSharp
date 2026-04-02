using System.Text;
using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Lexing
{
    /// <summary>
    /// Lexer for SAP LO-VC object dependency syntax.
    /// Converts source code text into a sequence of tokens.
    /// </summary>
    public class Lexer : ILexer
    {
        private readonly string _source;
        private int _position;
        private int _line;
        private int _column;
        private TokenType _lastTokenType = TokenType.EOF;
        private bool _isStartOfLine = true;

        private char Current => _position < _source.Length ? _source[_position] : '\0';
        private char Peek(int offset = 1) =>
            _position + offset < _source.Length ? _source[_position + offset] : '\0';

        public Lexer(string source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _position = 0;
            _line = 1;
            _column = 1;
            _isStartOfLine = true;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (Current != '\0')
            {
                var token = NextToken();
                if (token.Type != TokenType.EOF)
                {
                    tokens.Add(token);
                }
                else
                {
                    break;
                }
            }

            // Add EOF token
            tokens.Add(new Token(TokenType.EOF, string.Empty, CurrentPosition()));

            return tokens;
        }

        private Token NextToken()
        {
            // Skip whitespace and comments
            SkipWhitespaceAndComments();

            if (Current == '\0')
                return new Token(TokenType.EOF, string.Empty, CurrentPosition());

            var position = CurrentPosition();

            // String literals
            if (Current == '\'' || Current == '"')
            {
                var token = ReadString(position);
                _lastTokenType = token.Type;
                return token;
            }

            // Numbers
            if (char.IsDigit(Current))
            {
                var token = ReadNumber(position);
                _lastTokenType = token.Type;
                return token;
            }

            // Negative numbers: only if preceded by operator, comma, paren, or start
            if (Current == '-' && char.IsDigit(Peek()) && IsNegativeNumberContext())
            {
                var token = ReadNegativeNumber(position);
                _lastTokenType = token.Type;
                return token;
            }

            // Special variables starting with $
            if (Current == '$')
            {
                var token = ReadSpecialVariable(position);
                _lastTokenType = token.Type;
                return token;
            }

            // Identifiers and keywords
            if (char.IsLetter(Current) || Current == '_')
            {
                var token = ReadIdentifierOrKeyword(position);
                _lastTokenType = token.Type;
                return token;
            }

            // Operators and punctuation
            var opToken = ReadOperatorOrPunctuation(position);
            _lastTokenType = opToken.Type;
            return opToken;
        }

        private bool IsNegativeNumberContext()
        {
            // Negative number is valid after these tokens
            return _lastTokenType == TokenType.EOF ||
                   _lastTokenType == TokenType.Equal ||
                   _lastTokenType == TokenType.Less ||
                   _lastTokenType == TokenType.Greater ||
                   _lastTokenType == TokenType.LessEqual ||
                   _lastTokenType == TokenType.GreaterEqual ||
                   _lastTokenType == TokenType.NotEqual ||
                   _lastTokenType == TokenType.EQ ||
                   _lastTokenType == TokenType.NE ||
                   _lastTokenType == TokenType.LT ||
                   _lastTokenType == TokenType.LE ||
                   _lastTokenType == TokenType.GT ||
                   _lastTokenType == TokenType.GE ||
                   _lastTokenType == TokenType.Plus ||
                   _lastTokenType == TokenType.Minus ||
                   _lastTokenType == TokenType.Multiply ||
                   _lastTokenType == TokenType.Divide ||
                   _lastTokenType == TokenType.OpenParen ||
                   _lastTokenType == TokenType.OpenBracket ||
                   _lastTokenType == TokenType.Comma ||
                   _lastTokenType == TokenType.Semicolon ||
                   _lastTokenType == TokenType.Colon;
        }

        private void SkipWhitespaceAndComments()
        {
            while (true)
            {
                if (char.IsWhiteSpace(Current))
                {
                    if (Current == '\n')
                    {
                        _isStartOfLine = true;
                    }
                    Advance();
                }
                else if (Current == '*' && _isStartOfLine)
                {
                    // This is a comment line - skip until end of line
                    while (Current != '\n' && Current != '\0')
                        Advance();
                }
                else
                {
                    // We've found non-whitespace, non-comment content
                    _isStartOfLine = false;
                    break;
                }
            }
        }

        private Token ReadString(SourcePosition position)
        {
            char quote = Current;
            Advance(); // Skip opening quote

            var sb = new StringBuilder();

            while (Current != quote && Current != '\0')
            {
                if (Current == '\\' && Peek() == quote) // Escaped quote
                {
                    Advance();
                    sb.Append(quote);
                    Advance();
                }
                else
                {
                    sb.Append(Current);
                    Advance();
                }
            }

            if (Current == '\0')
                throw new LexerException("Unterminated string literal", position);

            Advance(); // Skip closing quote

            return new Token(TokenType.String, sb.ToString(), position);
        }

        private Token ReadNumber(SourcePosition position)
        {
            var sb = new StringBuilder();

            // Read integer part
            while (char.IsDigit(Current))
            {
                sb.Append(Current);
                Advance();
            }

            // Read decimal part
            if (Current == '.' && char.IsDigit(Peek()))
            {
                sb.Append(Current);  // Append the '.'
                Advance();           // Move past the '.'

                // Now read the fractional part
                while (char.IsDigit(Current))
                {
                    sb.Append(Current);
                    Advance();
                }
            }

            // Read exponent part (e.g., 314E-2)
            if (char.ToUpper(Current) == 'E')
            {
                sb.Append(Current);
                Advance();

                if (Current == '+' || Current == '-')
                {
                    sb.Append(Current);
                    Advance();
                }

                while (char.IsDigit(Current))
                {
                    sb.Append(Current);
                    Advance();
                }
            }

            return new Token(TokenType.Number, sb.ToString(), position);
        }

        private Token ReadNegativeNumber(SourcePosition position)
        {
            var sb = new StringBuilder();

            // Add the negative sign
            sb.Append(Current);
            Advance();

            // Read integer part
            while (char.IsDigit(Current))
            {
                sb.Append(Current);
                Advance();
            }

            // Read decimal part
            if (Current == '.' && char.IsDigit(Peek()))
            {
                sb.Append(Current);
                Advance();

                while (char.IsDigit(Current))
                {
                    sb.Append(Current);
                    Advance();
                }
            }

            // Read exponent part
            if (char.ToUpper(Current) == 'E')
            {
                sb.Append(Current);
                Advance();

                if (Current == '+' || Current == '-')
                {
                    sb.Append(Current);
                    Advance();
                }

                while (char.IsDigit(Current))
                {
                    sb.Append(Current);
                    Advance();
                }
            }

            return new Token(TokenType.Number, sb.ToString(), position);
        }

        private Token ReadSpecialVariable(SourcePosition position)
        {
            var sb = new StringBuilder();
            sb.Append(Current); // $
            Advance();

            // Read the rest of the variable name
            while (char.IsLetterOrDigit(Current) || Current == '_')
            {
                sb.Append(Current);
                Advance();
            }

            var value = sb.ToString().ToUpper();

            var type = value switch
            {
                "$SELF" => TokenType.Self,
                "$PARENT" => TokenType.Parent,
                "$ROOT" => TokenType.Root,
                "$SET_DEFAULT" => TokenType.SetDefault,
                "$DEL_DEFAULT" => TokenType.DelDefault,
                "$SUM_PARTS" => TokenType.SumParts,
                "$COUNT_PARTS" => TokenType.CountParts,
                "$SET_PRICING_FACTOR" => TokenType.SetPricingFactor,
                _ => throw new LexerException($"Unknown special variable: {value}", position)
            };

            return new Token(type, value, position);
        }

        private Token ReadIdentifierOrKeyword(SourcePosition position)
        {
            var sb = new StringBuilder();

            while (char.IsLetterOrDigit(Current) || Current == '_')
            {
                sb.Append(Current);
                Advance();
            }

            var value = sb.ToString();
            var upperValue = value.ToUpper();

            // Check if it's a keyword (case-insensitive)
            var type = upperValue switch
            {
                // Constraint sections
                "OBJECTS" => TokenType.Objects,
                "CONDITION" => TokenType.Condition,
                "RESTRICTIONS" => TokenType.Restrictions,
                "INFERENCES" => TokenType.Inferences,

                // Logical operators
                "AND" => TokenType.And,
                "OR" => TokenType.Or,
                "NOT" => TokenType.Not,

                // Comparison operators
                "EQ" => TokenType.EQ,
                "NE" => TokenType.NE,
                "LT" => TokenType.LT,
                "LE" => TokenType.LE,
                "GT" => TokenType.GT,
                "GE" => TokenType.GE,

                // Special keywords
                "IN" => TokenType.In,
                "IF" => TokenType.If,
                "SPECIFIED" => TokenType.Specified,
                "TYPE_OF" => TokenType.TypeOf,
                "PART_OF" => TokenType.PartOf,
                "SUBPART_OF" => TokenType.SubpartOf,
                "MDATA" => TokenType.MData,
                "TRUE" => TokenType.True,
                "FALSE" => TokenType.False,

                // Object declaration
                "IS_A" => TokenType.IsA,
                "IS_OBJECT" => TokenType.IsObject,
                "WHERE" => TokenType.Where,

                // Table
                "TABLE" => TokenType.Table,

                // Mathematical functions
                "SIN" => TokenType.Sin,
                "COS" => TokenType.Cos,
                "TAN" => TokenType.Tan,
                "EXP" => TokenType.Exp,
                "LN" => TokenType.Ln,
                "LOG10" => TokenType.Log10,
                "ABS" => TokenType.Abs,
                "SQRT" => TokenType.Sqrt,
                "ARCSIN" => TokenType.ArcSin,
                "ARCCOS" => TokenType.ArcCos,
                "ARCTAN" => TokenType.ArcTan,
                "SIGN" => TokenType.Sign,
                "FRAC" => TokenType.Frac,
                "CEIL" => TokenType.Ceil,
                "TRUNC" => TokenType.Trunc,
                "FLOOR" => TokenType.Floor,

                // String functions
                "LC" => TokenType.LC,
                "UC" => TokenType.UC,

                _ => TokenType.Identifier
            };

            return new Token(type, value, position);
        }

        private Token ReadOperatorOrPunctuation(SourcePosition position)
        {
            // Two-character operators (check these first!)
            if (Current == '<' && Peek() == '=')
            {
                Advance(); Advance();
                return new Token(TokenType.LessEqual, "<=", position);
            }
            if (Current == '=' && Peek() == '<')
            {
                Advance(); Advance();
                return new Token(TokenType.LessEqual, "=<", position);
            }
            if (Current == '>' && Peek() == '=')
            {
                Advance(); Advance();
                return new Token(TokenType.GreaterEqual, ">=", position);
            }
            if (Current == '=' && Peek() == '>')
            {
                Advance(); Advance();
                return new Token(TokenType.GreaterEqual, "=>", position);
            }
            if (Current == '<' && Peek() == '>')
            {
                Advance(); Advance();
                return new Token(TokenType.NotEqual, "<>", position);
            }
            if (Current == '>' && Peek() == '<')
            {
                Advance(); Advance();
                return new Token(TokenType.NotEqual, "><", position);
            }
            if (Current == '|' && Peek() == '|')
            {
                Advance(); Advance();
                return new Token(TokenType.Concat, "||", position);
            }

            // Single-character operators
            var (type, ch) = Current switch
            {
                ':' => (TokenType.Colon, ':'),
                ',' => (TokenType.Comma, ','),
                ';' => (TokenType.Semicolon, ';'),
                '.' => (TokenType.Dot, '.'),
                '(' => (TokenType.OpenParen, '('),
                ')' => (TokenType.CloseParen, ')'),
                '[' => (TokenType.OpenBracket, '['),
                ']' => (TokenType.CloseBracket, ']'),
                '+' => (TokenType.Plus, '+'),
                '-' => (TokenType.Minus, '-'),
                '*' => (TokenType.Multiply, '*'),
                '/' => (TokenType.Divide, '/'),
                '<' => (TokenType.Less, '<'),
                '>' => (TokenType.Greater, '>'),
                '=' => (TokenType.Equal, '='),
                '?' => (TokenType.QuestionMark, '?'),
                _ => throw new LexerException($"Unexpected character: '{Current}'", position)
            };

            Advance();
            return new Token(type, ch.ToString(), position);
        }

        private void Advance()
        {
            if (Current == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }

            _position++;
        }

        private SourcePosition CurrentPosition() =>
            new(_line, _column, _position);
    }
}