using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Lexing;
using SapLoVcSharp.Core.Parsing;
using SapLoVcSharp.Core.Parsing.DependencyParsers;

namespace SapLoVcSharp.Core.Tests.Helpers
{
    /// <summary>
    /// Helper methods for parser testing.
    /// </summary>
    public static class ParserTestHelper
    {
        public static ExpressionNode ParseExpression(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new ExpressionParser(tokens);
            return parser.ParseExpression();
        }

        public static ProcedureNode ParseProcedure(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new ProcedureParser(tokens);
            return parser.Parse();
        }

        public static PreconditionNode ParsePrecondition(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new PreconditionParser(tokens);
            return parser.Parse();
        }

        public static SelectionConditionNode ParseSelectionCondition(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new SelectionConditionParser(tokens);
            return parser.Parse();
        }

        public static ConstraintNode ParseConstraint(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new ConstraintParser(tokens);
            return parser.Parse();
        }
    }
}