using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Dependencies
{
    /// <summary>
    /// Represents a precondition dependency.
    /// </summary>
    public class PreconditionNode : DependencyNode
    {
        public ExpressionNode Condition { get; }

        public PreconditionNode(ExpressionNode condition, SourcePosition position)
            : base(position)
        {
            Condition = condition;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitPrecondition(this);
        }

        public override string ToString() => $"Precondition({Condition})";
    }
}