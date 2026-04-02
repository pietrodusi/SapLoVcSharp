using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Lexing;

namespace SapLoVcSharp.Core.Parsing.DependencyParsers
{
    /// <summary>
    /// Parser for precondition dependencies.
    /// Example: MODEL = 'Racing' AND COLOR = 'Red'
    /// Or with table: TABLE T_BIKE (MODEL = MODEL, COLOR = COLOR)
    /// </summary>
    public class PreconditionParser
    {
        private readonly List<Token> _tokens;

        public PreconditionParser(List<Token> tokens)
        {
            _tokens = tokens;
        }

        public PreconditionNode Parse()
        {
            var expressionParser = new ExpressionParser(_tokens);
            var condition = expressionParser.ParseExpression();

            return new PreconditionNode(condition, _tokens[0].Position);
        }
    }
}