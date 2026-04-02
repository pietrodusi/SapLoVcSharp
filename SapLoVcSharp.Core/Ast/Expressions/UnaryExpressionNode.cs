using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Expressions
{
    /// <summary>
    /// Represents a unary expression (e.g., NOT a, -a).
    /// </summary>
    public class UnaryExpressionNode : ExpressionNode
    {
        public UnaryOperator Op { get; }
        public ExpressionNode Operand { get; }

        public UnaryExpressionNode(
            UnaryOperator op,
            ExpressionNode operand,
            SourcePosition position)
            : base(position)
        {
            Op = op;
            Operand = operand;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitUnaryExpression(this);
        }

        public override string ToString() => $"Unary({Op} {Operand})";
    }

    public enum UnaryOperator
    {
        Not,            // NOT
        Negate,         // - (unary minus)
    }
}