using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Expressions
{
    /// <summary>
    /// Represents member access in constraints using variables (e.g., PC.MODEL, Server.OS).
    /// Different from MemberAccessNode which uses $SELF, $PARENT, $ROOT.
    /// </summary>
    public class ConstraintMemberAccessNode : ExpressionNode
    {
        public string Variable { get; }
        public string MemberName { get; }

        public ConstraintMemberAccessNode(string variable, string memberName, SourcePosition position)
            : base(position)
        {
            Variable = variable;
            MemberName = memberName;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitConstraintMemberAccess(this);
        }

        public override string ToString() => $"{Variable}.{MemberName}";
    }
}