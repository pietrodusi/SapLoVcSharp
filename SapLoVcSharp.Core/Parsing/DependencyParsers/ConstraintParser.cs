using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Ast.Statements;
using SapLoVcSharp.Core.Lexing;

namespace SapLoVcSharp.Core.Parsing.DependencyParsers
{
    public class ConstraintParser
    {
        private readonly List<Token> _tokens;
        private int _position;

        private Token Current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

        public ConstraintParser(List<Token> tokens)
        {
            _tokens = tokens;
            _position = 0;
        }
        public ConstraintParser(string code)
        {
            var lexer = new Lexer(code);
            _tokens = lexer.Tokenize();
            _position = 0;
        }

        public ConstraintNode Parse()
        {
            var startPosition = Current.Position;

            // Parse OBJECTS section (required)
            var objects = ParseObjectsSection();

            // Parse CONDITION section (optional)
            ExpressionNode? condition = null;
            if (Current.Type == TokenType.Condition)
            {
                condition = ParseConditionSection();
            }

            // Parse RESTRICTIONS section (required)
            var restrictions = ParseRestrictionsSection();

            // Parse INFERENCES section (optional)
            var inferences = new List<string>();
            if (Current.Type == TokenType.Inferences)
            {
                inferences = ParseInferencesSection();
            }

            return new ConstraintNode(objects, condition, restrictions, inferences, startPosition);
        }

        private List<ObjectDeclaration> ParseObjectsSection()
        {
            Expect(TokenType.Objects, "Expected OBJECTS section");
            Expect(TokenType.Colon, "Expected ':' after OBJECTS");

            var objects = new List<ObjectDeclaration>();

            while (!IsSectionEnd())
            {
                objects.Add(ParseObjectDeclaration());

                if (Current.Type == TokenType.Comma)
                {
                    Advance();
                }
                else if (!IsSectionEnd())
                {
                    if (!IsNextSection())
                    {
                        throw new ParserException(
                            "Expected ',' between object declarations or section terminator",
                            Current.Position);
                    }
                }
            }

            ConsumeSectionTerminator();
            return objects;
        }

        private ObjectDeclaration ParseObjectDeclaration()
        {
            var variableToken = Expect(TokenType.Identifier, "Expected variable name");
            var variable = variableToken.Value;

            Expect(TokenType.IsA, "Expected IS_A");
            Expect(TokenType.OpenParen, "Expected '(' after IS_A");

            var classTypeToken = Expect(TokenType.Number, "Expected class type number");
            var classType = classTypeToken.Value;

            Expect(TokenType.CloseParen, "Expected ')' after class type");

            var classNameToken = Expect(TokenType.Identifier, "Expected class name");
            var className = classNameToken.Value;

            Dictionary<string, string>? variableMappings = null;
            if (Current.Type == TokenType.Where)
            {
                Advance();
                variableMappings = ParseWhereClause();
            }

            return new ObjectDeclaration(variable, className, classType, variableMappings);
        }

        private Dictionary<string, string> ParseWhereClause()
        {
            var mappings = new Dictionary<string, string>();

            do
            {
                var varToken = Expect(TokenType.Identifier, "Expected variable name");
                Expect(TokenType.Equal, "Expected '=' in WHERE clause");
                var charToken = Expect(TokenType.Identifier, "Expected characteristic name");

                mappings[varToken.Value] = charToken.Value;

                if (Current.Type == TokenType.Semicolon)
                {
                    Advance();
                }
                else
                {
                    break;
                }
            }
            while (Current.Type == TokenType.Identifier);

            return mappings;
        }

        private ExpressionNode ParseConditionSection()
        {
            Expect(TokenType.Condition, "Expected CONDITION section");
            Expect(TokenType.Colon, "Expected ':' after CONDITION");

            var conditionTokens = GetTokensUntilSectionEnd();
            var expressionParser = new ExpressionParser(conditionTokens);
            var condition = expressionParser.ParseExpression();

            ConsumeSectionTerminator();
            return condition;
        }

        private List<StatementNode> ParseRestrictionsSection()
        {
            Expect(TokenType.Restrictions, "Expected RESTRICTIONS section");
            Expect(TokenType.Colon, "Expected ':' after RESTRICTIONS");

            var restrictions = new List<StatementNode>();

            var restrictionTokens = GetTokensUntilSectionEnd();

            if (restrictionTokens.Count <= 2 && restrictionTokens[0].Type == TokenType.False)
            {
                restrictions.Add(new ReturnInconsistencyNode(restrictionTokens[0].Position));
            }
            else
            {
                restrictions = new ProcedureParser(restrictionTokens).Parse().Statements;
            }

            //while (!IsSectionEnd())
            //{
            //var restrictionTokens = GetTokensUntilCommaOrSectionEnd();
            //var expressionParser = new ExpressionParser(restrictionTokens);



            //restrictions.Add(expressionParser.ParseExpression());

            //if (Current.Type == TokenType.Comma)
            //{
            //    Advance();
            //}
            //else if (!IsSectionEnd())
            //{
            //    if (!IsNextSection())
            //    {
            //        throw new ParserException(
            //            "Expected ',' between restrictions or section terminator",
            //            Current.Position);
            //    }
            //}
            //}

            ConsumeSectionTerminator();
            return restrictions;
        }

        private List<string> ParseInferencesSection()
        {
            Expect(TokenType.Inferences, "Expected INFERENCES section");
            Expect(TokenType.Colon, "Expected ':' after INFERENCES");

            var inferences = new List<string>();

            while (!IsSectionEnd())
            {
                var token = Expect(TokenType.Identifier, "Expected variable name in INFERENCES");
                inferences.Add(token.Value);

                if (Current.Type == TokenType.Comma)
                {
                    Advance();
                }
                else if (!IsSectionEnd())
                {
                    if (Current.Type != TokenType.EOF)
                    {
                        throw new ParserException(
                            "Expected ',' between inferences or section terminator",
                            Current.Position);
                    }
                }
            }

            ConsumeSectionTerminator();
            return inferences;
        }

        #region Helper Methods

        private bool IsSectionEnd()
        {
            return Current.Type == TokenType.Dot ||
                   IsNextSection() ||
                   Current.Type == TokenType.EOF;
        }

        private bool IsNextSection()
        {
            return Current.Type == TokenType.Objects ||
                   Current.Type == TokenType.Condition ||
                   Current.Type == TokenType.Restrictions ||
                   Current.Type == TokenType.Inferences;
        }

        private void ConsumeSectionTerminator()
        {
            if (Current.Type == TokenType.Dot)
            {
                Advance();
            }
        }

        /// <summary>
        /// Gets tokens until section end, properly handling nested parentheses, brackets, and member access dots.
        /// </summary>
        private List<Token> GetTokensUntilSectionEnd()
        {
            var tokens = new List<Token>();
            int depth = 0; // Track nesting depth for parens/brackets

            while (Current.Type != TokenType.EOF)
            {
                // Track nesting depth
                if (Current.Type == TokenType.OpenParen || Current.Type == TokenType.OpenBracket)
                {
                    depth++;
                    tokens.Add(Current);
                    Advance();
                }
                else if (Current.Type == TokenType.CloseParen || Current.Type == TokenType.CloseBracket)
                {
                    depth--;
                    tokens.Add(Current);
                    Advance();
                }
                else if (depth == 0)
                {
                    // Check if dot is a section terminator or member access
                    if (Current.Type == TokenType.Dot)
                    {
                        // Dot is member access if the LAST token we added was an identifier or object ref
                        bool isMemberAccess = tokens.Count > 0 &&
                                             (tokens[^1].Type == TokenType.Identifier ||
                                              tokens[^1].Type == TokenType.CloseParen ||
                                              tokens[^1].Type == TokenType.Self ||
                                              tokens[^1].Type == TokenType.Parent ||
                                              tokens[^1].Type == TokenType.Root);

                        if (isMemberAccess)
                        {
                            // This is member access, not section terminator
                            tokens.Add(Current);
                            Advance();
                        }
                        else
                        {
                            // This is a section terminator
                            break;
                        }
                    }
                    else if (IsNextSection())
                    {
                        // Start of next section
                        break;
                    }
                    else
                    {
                        tokens.Add(Current);
                        Advance();
                    }
                }
                else
                {
                    // Inside nested structure, keep collecting
                    tokens.Add(Current);
                    Advance();
                }
            }

            tokens.Add(new Token(TokenType.EOF, "", Current.Position));
            return tokens;
        }

        /// <summary>
        /// Gets tokens until comma or section end, properly handling nested parentheses, brackets, and member access dots.
        /// </summary>
        private List<Token> GetTokensUntilCommaOrSectionEnd()
        {
            var tokens = new List<Token>();
            int depth = 0; // Track nesting depth for parens/brackets

            while (Current.Type != TokenType.EOF)
            {
                // Track nesting depth
                if (Current.Type == TokenType.OpenParen || Current.Type == TokenType.OpenBracket)
                {
                    depth++;
                    tokens.Add(Current);
                    Advance();
                }
                else if (Current.Type == TokenType.CloseParen || Current.Type == TokenType.CloseBracket)
                {
                    depth--;
                    tokens.Add(Current);
                    Advance();
                }
                else if (depth == 0)
                {
                    // Check for comma at top level
                    if (Current.Type == TokenType.Comma)
                    {
                        break;
                    }

                    // Check if dot is a section terminator or member access
                    if (Current.Type == TokenType.Dot)
                    {
                        // Dot is member access if the LAST token we added was an identifier or object ref
                        bool isMemberAccess = tokens.Count > 0 &&
                                             (tokens[^1].Type == TokenType.Identifier ||
                                              tokens[^1].Type == TokenType.CloseParen ||
                                              tokens[^1].Type == TokenType.Self ||
                                              tokens[^1].Type == TokenType.Parent ||
                                              tokens[^1].Type == TokenType.Root);

                        if (isMemberAccess)
                        {
                            // This is member access, not section terminator
                            tokens.Add(Current);
                            Advance();
                        }
                        else
                        {
                            // This is a section terminator
                            break;
                        }
                    }
                    else if (IsNextSection())
                    {
                        // Start of next section
                        break;
                    }
                    else
                    {
                        tokens.Add(Current);
                        Advance();
                    }
                }
                else
                {
                    // Inside nested structure, keep collecting
                    tokens.Add(Current);
                    Advance();
                }
            }

            tokens.Add(new Token(TokenType.EOF, "", Current.Position));
            return tokens;
        }

        private void Advance()
        {
            if (_position < _tokens.Count)
                _position++;
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

        private Token Consume()
        {
            var token = Current;
            Advance();
            return token;
        }

        #endregion
    }
}