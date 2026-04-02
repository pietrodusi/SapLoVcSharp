using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Expressions
{
    /// <summary>
    /// Represents an IN expression (e.g., COLOR IN ('Red', 'Blue')).
    /// </summary>
    public class InExpressionNode : ExpressionNode
    {
        public ExpressionNode Expression { get; }
        public List<ExpressionNode> Values { get; }

        public InExpressionNode(
            ExpressionNode expression,
            List<ExpressionNode> values,
            SourcePosition position)
            : base(position)
        {
            Expression = expression;
            Values = values;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitInExpression(this);
        }

        public override string ToString() =>
            $"{Expression} IN ({string.Join(", ", Values)})";
    }
}