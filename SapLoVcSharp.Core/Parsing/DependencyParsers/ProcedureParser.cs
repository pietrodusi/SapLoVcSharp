using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Ast.Statements;
using SapLoVcSharp.Core.Lexing;

namespace SapLoVcSharp.Core.Parsing.DependencyParsers
{
    /// <summary>
    /// Parser for procedure dependencies.
    /// Example: $SELF.COLOR = 'Red' IF MODEL = 'Racing', TABLE T_BIKE (MODEL = MODEL)
    /// </summary>
    public class ProcedureParser
    {
        private readonly List<Token> _tokens;
        private int _position;

        private Token Current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

        public ProcedureParser(List<Token> tokens)
        {
            _tokens = tokens;
            _position = 0;
        }

        public ProcedureParser(string code)
        {
            var lexer = new Lexer(code);
            _tokens = lexer.Tokenize();
            _position = 0;
        }

        public ProcedureNode Parse()
        {
            var statements = new List<StatementNode>();
            var startPosition = Current.Position;

            // Parse comma-separated statements
            while (Current.Type != TokenType.EOF)
            {
                statements.Add(ParseStatement());

                if (Current.Type == TokenType.Comma)
                {
                    Advance();
                }
                else if (Current.Type != TokenType.EOF)
                {
                    throw new ParserException(
                        "Expected ',' between statements or end of procedure",
                        Current.Position);
                }
            }

            return new ProcedureNode(statements, startPosition);
        }

        private StatementNode ParseStatement()
        {
            // Check if it's a table call
            if (Current.Type == TokenType.Table)
            {
                return ParseTableCall();
            }

            // Check if it's a built-in function
            if (IsBuiltInFunction(Current.Type))
            {
                return ParseBuiltInFunctionCall();
            }

            // Otherwise it's an assignment (possibly conditional)
            return ParseAssignment();
        }

        private bool IsBuiltInFunction(TokenType type)
        {
            return type == TokenType.SetDefault ||
                   type == TokenType.DelDefault ||
                   type == TokenType.SumParts ||
                   type == TokenType.CountParts ||
                   type == TokenType.SetPricingFactor;
        }

        private StatementNode ParseBuiltInFunctionCall()
        {
            var token = Consume();
            var position = token.Position;

            var functionType = token.Type switch
            {
                TokenType.SetDefault => BuiltInFunctionType.SetDefault,
                TokenType.DelDefault => BuiltInFunctionType.DelDefault,
                TokenType.SumParts => BuiltInFunctionType.SumParts,
                TokenType.CountParts => BuiltInFunctionType.CountParts,
                TokenType.SetPricingFactor => BuiltInFunctionType.SetPricingFactor,
                _ => throw new ParserException(
                    $"Unknown built-in function: {token.Type}",
                    token.Position)
            };

            Expect(TokenType.OpenParen, $"Expected '(' after built-in function");

            var arguments = new List<ExpressionNode>();

            // Parse arguments using nested ExpressionParser
            if (Current.Type != TokenType.CloseParen)
            {
                do
                {
                    // Get tokens until comma or close paren
                    var argTokens = GetArgumentTokens();
                    var expressionParser = new ExpressionParser(argTokens);
                    arguments.Add(expressionParser.ParseExpression());

                    if (Current.Type == TokenType.Comma)
                    {
                        Advance();
                    }
                    else if (Current.Type != TokenType.CloseParen)
                    {
                        throw new ParserException(
                            "Expected ',' or ')' after function argument",
                            Current.Position);
                    }
                }
                while (Current.Type != TokenType.CloseParen && Current.Type != TokenType.EOF);
            }

            Expect(TokenType.CloseParen, "Expected ')' after function arguments");

            return new BuiltInFunctionCallNode(functionType, arguments, position);
        }

        private List<Token> GetArgumentTokens()
        {
            var tokens = new List<Token>();
            int parenDepth = 0;

            while (Current.Type != TokenType.EOF)
            {
                if (Current.Type == TokenType.OpenParen)
                {
                    parenDepth++;
                }
                else if (Current.Type == TokenType.CloseParen)
                {
                    if (parenDepth == 0)
                    {
                        break; // Hit the closing paren of the function call
                    }
                    parenDepth--;
                }
                else if (Current.Type == TokenType.Comma && parenDepth == 0)
                {
                    break; // Hit argument separator
                }

                tokens.Add(Current);
                Advance();
            }

            tokens.Add(new Token(TokenType.EOF, "", Current.Position));
            return tokens;
        }

        private StatementNode ParseTableCall()
        {
            var startPosition = Current.Position;

            // Collect tokens until IF, comma, or EOF
            var tokens = GetTokensUntilCommaOrIfOrEof();
            var expressionParser = new ExpressionParser(tokens);
            var tableCallExpr = expressionParser.ParseExpression();

            if (tableCallExpr is not TableCallExpressionNode tableCall)
            {
                throw new ParserException(
                    "Expected table call expression",
                    tableCallExpr.Position);
            }

            // Check for IF condition
            ExpressionNode? condition = null;
            if (Current.Type == TokenType.If)
            {
                Advance(); // Consume IF

                var conditionTokens = GetTokensUntilCommaOrEof();
                expressionParser = new ExpressionParser(conditionTokens);
                condition = expressionParser.ParseExpression();
            }

            // Convert TableCallExpressionNode to TableCallNode (statement form)
            return new TableCallNode(
                tableCall.TableName,
                tableCall.Arguments,
                startPosition,
                condition);
        }

        private StatementNode ParseAssignment()
        {
            // Parse target expression (left side of =)
            var targetTokens = GetTokensUntilEqual();
            var expressionParser = new ExpressionParser(targetTokens);
            var target = expressionParser.ParseExpression();

            Expect(TokenType.Equal, "Expected '=' in assignment");

            // Parse value expression (right side of =, until IF or comma or EOF)
            var valueTokens = GetTokensUntilCommaOrIfOrEof();
            expressionParser = new ExpressionParser(valueTokens);
            var value = expressionParser.ParseExpression();

            // Check for IF condition
            ExpressionNode? condition = null;
            if (Current.Type == TokenType.If)
            {
                Advance(); // Consume IF

                var conditionTokens = GetTokensUntilCommaOrEof();
                expressionParser = new ExpressionParser(conditionTokens);
                condition = expressionParser.ParseExpression();
            }

            return new AssignmentNode(target, value, target.Position, condition);
        }

        #region Token Collection Helpers

        private List<Token> GetTokensUntilEqual()
        {
            var tokens = new List<Token>();
            while (Current.Type != TokenType.Equal && Current.Type != TokenType.EOF)
            {
                tokens.Add(Current);
                Advance();
            }
            tokens.Add(new Token(TokenType.EOF, "", Current.Position));
            return tokens;
        }

        private List<Token> GetTokensUntilCommaOrEof()
        {
            var tokens = new List<Token>();
            int parenDepth = 0;

            while (Current.Type != TokenType.EOF)
            {
                if (Current.Type == TokenType.OpenParen)
                {
                    parenDepth++;
                }
                else if (Current.Type == TokenType.CloseParen)
                {
                    parenDepth--;
                }
                else if (Current.Type == TokenType.Comma && parenDepth == 0)
                {
                    break;
                }

                tokens.Add(Current);
                Advance();
            }

            tokens.Add(new Token(TokenType.EOF, "", Current.Position));
            return tokens;
        }

        private List<Token> GetTokensUntilCommaOrIfOrEof()
        {
            var tokens = new List<Token>();
            int parenDepth = 0;

            while (Current.Type != TokenType.EOF)
            {
                if (Current.Type == TokenType.OpenParen)
                {
                    parenDepth++;
                }
                else if (Current.Type == TokenType.CloseParen)
                {
                    parenDepth--;
                }
                else if ((Current.Type == TokenType.Comma || Current.Type == TokenType.If) && parenDepth == 0)
                {
                    break;
                }

                tokens.Add(Current);
                Advance();
            }

            tokens.Add(new Token(TokenType.EOF, "", Current.Position));
            return tokens;
        }

        #endregion

        #region Helper Methods

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