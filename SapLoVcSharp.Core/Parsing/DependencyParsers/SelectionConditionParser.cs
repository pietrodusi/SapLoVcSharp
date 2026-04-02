using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Lexing;

namespace SapLoVcSharp.Core.Parsing.DependencyParsers
{
    /// <summary>
    /// Parser for selection condition dependencies.
    /// Example: HANDLEBAR = 'Racing'
    /// </summary>
    public class SelectionConditionParser
    {
        private readonly List<Token> _tokens;

        public SelectionConditionParser(List<Token> tokens)
        {
            _tokens = tokens;
        }

        public SelectionConditionNode Parse()
        {
            var expressionParser = new ExpressionParser(_tokens);
            var condition = expressionParser.ParseExpression();

            return new SelectionConditionNode(condition, _tokens[0].Position);
        }
    }
}