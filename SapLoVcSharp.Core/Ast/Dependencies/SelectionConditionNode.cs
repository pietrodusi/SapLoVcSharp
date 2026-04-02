using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Dependencies
{
    /// <summary>
    /// Represents a selection condition dependency.
    /// </summary>
    public class SelectionConditionNode : DependencyNode
    {
        public ExpressionNode Condition { get; }

        public SelectionConditionNode(ExpressionNode condition, SourcePosition position)
            : base(position)
        {
            Condition = condition;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitSelectionCondition(this);
        }

        public override string ToString() => $"SelectionCondition({Condition})";
    }
}