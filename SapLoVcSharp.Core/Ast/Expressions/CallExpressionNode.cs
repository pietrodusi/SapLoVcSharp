using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Expressions
{
    /// <summary>
    /// Represents a function call (e.g., SIN(x), SQRT(25)).
    /// </summary>
    public class CallExpressionNode : ExpressionNode
    {
        public string FunctionName { get; }
        public List<ExpressionNode> Arguments { get; }

        public CallExpressionNode(
            string functionName,
            List<ExpressionNode> arguments,
            SourcePosition position)
            : base(position)
        {
            FunctionName = functionName;
            Arguments = arguments;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitCallExpression(this);
        }

        public override string ToString() =>
            $"Call({FunctionName}({string.Join(", ", Arguments)}))";
    }
}