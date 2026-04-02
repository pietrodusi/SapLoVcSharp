using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Ast.Statements;
using SapLoVcSharp.Core.Common;
using SapLoVcSharp.Core.Lexing;

namespace SapLoVcSharp.Core.Parsing
{
    /// <summary>
    /// Pratt parser for expressions with proper precedence handling.
    /// </summary>
    public class ExpressionParser
    {
        private readonly List<Token> _tokens;
        private int _position;

        private Token Current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];
        private Token Peek(int offset = 1) =>
            _position + offset < _tokens.Count ? _tokens[_position + offset] : _tokens[^1];

        public ExpressionParser(List<Token> tokens)
        {
            _tokens = tokens;
            _position = 0;
        }

        public ExpressionNode ParseExpression(Precedence precedence = Precedence.None)
        {
            var left = ParsePrefix();

            while (precedence < PrecedenceTable.GetPrecedence(Current.Type))
            {
                left = ParseInfix(left);
            }

            return left;
        }

        private ExpressionNode ParsePrefix()
        {
            var token = Current;

            return token.Type switch
            {
                TokenType.Number => ParseNumber(),
                TokenType.String => ParseString(),
                TokenType.True or TokenType.False => ParseBoolean(),
                TokenType.Identifier => ParseIdentifierOrCall(),
                TokenType.Self or TokenType.Parent or TokenType.Root => ParseObjectReference(),
                TokenType.Not => ParseUnaryNot(),
                TokenType.Minus => ParseUnaryMinus(),
                TokenType.OpenParen => ParseGrouped(),
                TokenType.Specified => ParseSpecified(),
                TokenType.Table => ParseTableCallExpression(),

                // Math functions
                TokenType.Sin or TokenType.Cos or TokenType.Tan or
                TokenType.Exp or TokenType.Ln or TokenType.Log10 or
                TokenType.Abs or TokenType.Sqrt or
                TokenType.ArcSin or TokenType.ArcCos or TokenType.ArcTan or
                TokenType.Sign or TokenType.Frac or
                TokenType.Ceil or TokenType.Trunc or TokenType.Floor or
                TokenType.LC or TokenType.UC or
                // Constraint functions
                TokenType.PartOf or TokenType.SubpartOf or TokenType.TypeOf
                    => ParseFunctionCall(),

                _ => throw new ParserException(
                    $"Unexpected token in expression: {token.Type}",
                    token.Position)
            };
        }


        private ExpressionNode ParseObjectReference()
        {
            var token = Consume();
            var position = token.Position;

            var objRef = token.Type switch
            {
                TokenType.Self => ObjectReference.Self,
                TokenType.Parent => ObjectReference.Parent,
                TokenType.Root => ObjectReference.Root,
                _ => throw new ParserException(
                    $"Expected object reference but got {token.Type}",
                    token.Position)
            };

            // Check if it's followed by a dot (member access)
            if (Current.Type == TokenType.Dot)
            {
                Advance(); // Consume dot
                var memberToken = Expect(TokenType.Identifier, "Expected member name after '.'");
                return new MemberAccessNode(objRef, memberToken.Value, position);
            }

            // It's just a standalone object reference (e.g., in $SET_DEFAULT($SELF, ...))
            return new ObjectReferenceNode(objRef, position);
        }

        private ExpressionNode ParseInfix(ExpressionNode left)
        {
            var token = Current;

            return token.Type switch
            {
                TokenType.Plus or TokenType.Minus or
                TokenType.Multiply or TokenType.Divide or
                TokenType.And or TokenType.Or or
                TokenType.Equal or TokenType.NotEqual or
                TokenType.Less or TokenType.LessEqual or
                TokenType.Greater or TokenType.GreaterEqual or
                TokenType.EQ or TokenType.NE or
                TokenType.LT or TokenType.LE or
                TokenType.GT or TokenType.GE or
                TokenType.Concat
                    => ParseBinaryExpression(left),

                TokenType.In => ParseInExpression(left),

                TokenType.Specified => ParsePostfixSpecified(left),  // ADD THIS

                _ => throw new ParserException(
                    $"Unexpected infix operator: {token.Type}",
                    token.Position)
            };
        }

        private ExpressionNode ParsePostfixSpecified(ExpressionNode expression)
        {
            var token = Consume(); // Consume SPECIFIED
            return new SpecifiedNode(expression, token.Position);
        }

        #region Prefix Parsing Methods

        private ExpressionNode ParseTableCallExpression()
        {
            var position = Current.Position;

            Expect(TokenType.Table, "Expected TABLE keyword");

            // Get table name
            var tableNameToken = Expect(TokenType.Identifier, "Expected table name");
            var tableName = tableNameToken.Value;

            Expect(TokenType.OpenParen, "Expected '(' after table name");

            var arguments = new List<TableArgument>();

            // Parse table arguments
            if (Current.Type != TokenType.CloseParen)
            {
                do
                {
                    arguments.Add(ParseTableArgument());

                    // After parsing an argument, we should see either comma or close paren
                    if (Current.Type == TokenType.Comma)
                    {
                        Advance(); // Consume comma and continue to next argument
                    }
                    else if (Current.Type == TokenType.CloseParen)
                    {
                        break; // Done with arguments
                    }
                    else
                    {
                        throw new ParserException(
                            $"Expected ',' or ')' after table argument but found {Current.Type} '{Current.Value}'",
                            Current.Position);
                    }
                }
                while (true);
            }

            Expect(TokenType.CloseParen, "Expected ')' after table arguments");

            return new TableCallExpressionNode(tableName, arguments, position);
        }

        private TableArgument ParseTableArgument()
        {
            // Parse: TABLE_COLUMN = expression
            // or: TABLE_COLUMN ?= expression (for export parameters with default values)

            var columnToken = Expect(TokenType.Identifier, "Expected table column name");
            var tableColumn = columnToken.Value;

            // Check for optional '?=' for default value assignment
            bool isOptional = false;
            if (Current.Type == TokenType.QuestionMark && Peek().Type == TokenType.Equal)
            {
                Advance(); // Consume '?'
                Advance(); // Consume '='
                isOptional = true;
            }
            else
            {
                Expect(TokenType.Equal, "Expected '=' after table column name");
            }

            // Parse the value expression
            // Table arguments are simple expressions (identifiers, member access, literals)
            // We parse a primary expression (which handles member access via ParsePrefix)
            var value = ParsePrefix();

            return new TableArgument(tableColumn, value, isOptional);
        }

        private ExpressionNode ParseNumber()
        {
            var token = Consume();

            if (!double.TryParse(token.Value,
                                 System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture,
                                 out var value))
            {
                throw new ParserException(
                    $"Invalid number format: {token.Value}",
                    token.Position);
            }

            return new LiteralNode(value, LiteralType.Number, token.Position);
        }

        private ExpressionNode ParseString()
        {
            var token = Consume();
            return new LiteralNode(token.Value, LiteralType.String, token.Position);
        }

        private ExpressionNode ParseBoolean()
        {
            var token = Consume();
            var value = token.Type == TokenType.True;
            return new LiteralNode(value, LiteralType.Boolean, token.Position);
        }

        private ExpressionNode ParseIdentifierOrCall()
        {
            var token = Current;
            var name = token.Value;
            var position = token.Position;

            Advance(); // Consume identifier

            // Check for member access (identifier.member) - used in constraints
            if (Current.Type == TokenType.Dot)
            {
                Advance(); // Consume dot
                var memberToken = Expect(TokenType.Identifier, "Expected member name after '.'");
                return new ConstraintMemberAccessNode(name, memberToken.Value, position);
            }

            // Check if it's a function call
            if (Current.Type == TokenType.OpenParen)
            {
                return ParseFunctionCallWithName(name, position);
            }

            // Just an identifier
            return new IdentifierNode(name, position);
        }

        private ExpressionNode ParseUnaryNot()
        {
            var token = Consume();
            var operand = ParseExpression(Precedence.Unary);
            return new UnaryExpressionNode(UnaryOperator.Not, operand, token.Position);
        }

        private ExpressionNode ParseUnaryMinus()
        {
            var token = Consume();
            var operand = ParseExpression(Precedence.Unary);
            return new UnaryExpressionNode(UnaryOperator.Negate, operand, token.Position);
        }

        private ExpressionNode ParseGrouped()
        {
            var openParen = Consume(); // Consume '('
            var expression = ParseExpression();
            Expect(TokenType.CloseParen, "Expected ')' after grouped expression");
            return expression;
        }

        private ExpressionNode ParseSpecified()
        {
            var token = Consume(); // Consume SPECIFIED
            var expression = ParseExpression(Precedence.Unary);
            return new SpecifiedNode(expression, token.Position);
        }

        private ExpressionNode ParseFunctionCall()
        {
            var token = Consume();
            var functionName = token.Value.ToUpper();
            var position = token.Position;

            return ParseFunctionCallWithName(functionName, position);
        }

        private ExpressionNode ParseFunctionCallWithName(string functionName, SourcePosition position)
        {
            // Validate that this is a known function
            var upperFunctionName = functionName.ToUpper();
            if (!IsKnownFunction(upperFunctionName))
            {
                throw new ParserException(
                    $"Unknown function '{functionName}'. Did you mean to use 'TABLE {functionName} (...)' for a table call?",
                    position);
            }

            Expect(TokenType.OpenParen, $"Expected '(' after function name '{functionName}'");

            var arguments = new List<ExpressionNode>();

            // Parse arguments
            if (Current.Type != TokenType.CloseParen)
            {
                do
                {
                    arguments.Add(ParseExpression());

                    if (Current.Type == TokenType.Comma)
                    {
                        Advance();
                    }
                    else
                    {
                        break;
                    }
                }
                while (Current.Type != TokenType.CloseParen);
            }

            Expect(TokenType.CloseParen, $"Expected ')' after function arguments");

            return new CallExpressionNode(functionName, arguments, position);
        }

        private bool IsKnownFunction(string functionName)
        {
            // List of all known SAP LO-VC functions
            return functionName switch
            {
                // Trigonometric
                "SIN" or "COS" or "TAN" or
                "ASIN" or "ACOS" or "ATAN" or
                "ARCSIN" or "ARCCOS" or "ARCTAN" or

                // Exponential and logarithmic
                "EXP" or "LN" or "LOG" or "LOG10" or

                // Mathematical
                "ABS" or "SQRT" or "SIGN" or "FRAC" or
                "CEIL" or "TRUNC" or "FLOOR" or "ROUND" or
                "MAX" or "MIN" or

                // String functions
                "LC" or "UC" or "LEN" or

                // Constraint functions
                "TYPE_OF" or "PART_OF" or "SUBPART_OF" => true,

                _ => false
            };
        }

        #endregion

        #region Infix Parsing Methods

        private ExpressionNode ParseBinaryExpression(ExpressionNode left)
        {
            var token = Consume();
            var precedence = PrecedenceTable.GetPrecedence(token.Type);

            var op = TokenTypeToBinaryOperator(token.Type);
            var right = ParseExpression(precedence);

            return new BinaryExpressionNode(left, op, right, token.Position);
        }

        private ExpressionNode ParseInExpression(ExpressionNode left)
        {
            var token = Consume(); // Consume IN

            Expect(TokenType.OpenParen, "Expected '(' after IN");

            var values = new List<ExpressionNode>();

            // Parse values
            if (Current.Type != TokenType.CloseParen)
            {
                do
                {
                    values.Add(ParseExpression());

                    if (Current.Type == TokenType.Comma)
                    {
                        Advance();
                    }
                    else
                    {
                        break;
                    }
                }
                while (Current.Type != TokenType.CloseParen);
            }

            Expect(TokenType.CloseParen, "Expected ')' after IN values");

            return new InExpressionNode(left, values, token.Position);
        }

        #endregion

        #region Helper Methods

        private BinaryOperator TokenTypeToBinaryOperator(TokenType type)
        {
            return type switch
            {
                TokenType.Plus => BinaryOperator.Add,
                TokenType.Minus => BinaryOperator.Subtract,
                TokenType.Multiply => BinaryOperator.Multiply,
                TokenType.Divide => BinaryOperator.Divide,

                TokenType.Equal or TokenType.EQ => BinaryOperator.Equal,
                TokenType.NotEqual or TokenType.NE => BinaryOperator.NotEqual,
                TokenType.Less or TokenType.LT => BinaryOperator.Less,
                TokenType.LessEqual or TokenType.LE => BinaryOperator.LessEqual,
                TokenType.Greater or TokenType.GT => BinaryOperator.Greater,
                TokenType.GreaterEqual or TokenType.GE => BinaryOperator.GreaterEqual,

                TokenType.And => BinaryOperator.And,
                TokenType.Or => BinaryOperator.Or,

                TokenType.Concat => BinaryOperator.Concat,

                _ => throw new ParserException(
                    $"Unknown binary operator: {type}",
                    Current.Position)
            };
        }

        private void Advance()
        {
            if (_position < _tokens.Count)
                _position++;
        }

        private Token Consume()
        {
            var token = Current;
            Advance();
            return token;
        }

        private Token Expect(TokenType expected, string errorMessage)
        {
            if (Current.Type != expected)
            {
                throw new ParserException(
                    $"{errorMessage}. Expected {expected} but found {Current.Type}",
                    Current.Position);
            }

            return Consume();
        }

        #endregion
    }
}