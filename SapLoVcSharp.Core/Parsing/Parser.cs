using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Ast.Statements;
using SapLoVcSharp.Core.Lexing;
using SapLoVcSharp.Core.Parsing.DependencyParsers;

namespace SapLoVcSharp.Core.Parsing
{
    /// <summary>
    /// Main parser for SAP LO-VC dependencies.
    /// </summary>
    public class Parser : IParser
    {
        private readonly ILexer _lexer;

        public Parser(ILexer lexer)
        {
            _lexer = lexer;
        }

        public DependencyNode ParseDependency(string source, DependencyType type)
        {
            var tokens = _lexer.Tokenize();

            return type switch
            {
                DependencyType.Procedure => ParseProcedure(tokens),
                DependencyType.Precondition => ParsePrecondition(tokens),
                DependencyType.SelectionCondition => ParseSelectionCondition(tokens),
                DependencyType.Constraint => ParseConstraint(tokens),
                _ => throw new ParserException(
                    $"Unsupported dependency type: {type}",
                    tokens[0].Position)
            };
        }

        public ExpressionNode ParseExpression(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var expressionParser = new ExpressionParser(tokens);
            return expressionParser.ParseExpression();
        }

        private ProcedureNode ParseProcedure(List<Token> tokens)
        {
            var parser = new ProcedureParser(tokens);
            return parser.Parse();
        }

        private PreconditionNode ParsePrecondition(List<Token> tokens)
        {
            var parser = new PreconditionParser(tokens);
            return parser.Parse();
        }

        private SelectionConditionNode ParseSelectionCondition(List<Token> tokens)
        {
            var parser = new SelectionConditionParser(tokens);
            return parser.Parse();
        }

        private ConstraintNode ParseConstraint(List<Token> tokens)
        {
            var parser = new ConstraintParser(tokens);
            return parser.Parse();
        }
    }
}