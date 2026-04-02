using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Statements
{
    /// <summary>
    /// Represents an assignment statement (e.g., $SELF.COLOR = 'Red' or $SELF.COLOR = 'Red' IF MODEL = 'Racing').
    /// </summary>
    public class AssignmentNode : StatementNode
    {
        public ExpressionNode Target { get; }
        public ExpressionNode Value { get; }
        public ExpressionNode? Condition { get; }

        public AssignmentNode(
            ExpressionNode target,
            ExpressionNode value,
            SourcePosition position,
            ExpressionNode? condition = null)
            : base(position)
        {
            Target = target;
            Value = value;
            Condition = condition;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitAssignment(this);
        }

        public override string ToString() =>
            Condition != null ? $"{Target} = {Value} IF {Condition}" : $"{Target} = {Value}";
    }
}