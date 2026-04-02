using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Expressions
{
    /// <summary>
    /// Represents SPECIFIED expression (e.g., SPECIFIED COLOR).
    /// </summary>
    public class SpecifiedNode : ExpressionNode
    {
        public ExpressionNode Expression { get; }

        public SpecifiedNode(ExpressionNode expression, SourcePosition position)
            : base(position)
        {
            Expression = expression;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitSpecified(this);
        }

        public override string ToString() => $"SPECIFIED {Expression}";
    }
}