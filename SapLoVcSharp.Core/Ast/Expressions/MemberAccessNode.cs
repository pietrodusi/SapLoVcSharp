using SapLoVcSharp.Core.Common;
using SapLoVcSharp.Core.Lexing;

namespace SapLoVcSharp.Core.Ast.Expressions
{
    /// <summary>
    /// Represents member access (e.g., $SELF.COLOR, $ROOT.MODEL).
    /// </summary>
    public class MemberAccessNode : ExpressionNode
    {
        public ObjectReference Obj { get; }
        public string MemberName { get; }

        public MemberAccessNode(
            ObjectReference obj,
            string memberName,
            SourcePosition position)
            : base(position)
        {
            Obj = obj;
            MemberName = memberName;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitMemberAccess(this);
        }

        public override string ToString() => $"{Obj}.{MemberName}";
    }

    public enum ObjectReference
    {
        Self,       // $SELF
        Parent,     // $PARENT
        Root,       // $ROOT
    }
}