using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Expressions
{
    /// <summary>
    /// Represents a standalone object reference ($SELF, $PARENT, $ROOT) without member access.
    /// Used in contexts like: $SET_DEFAULT($SELF, HEIGHT, 100)
    /// </summary>
    public class ObjectReferenceNode : ExpressionNode
    {
        public ObjectReference Obj { get; }

        public ObjectReferenceNode(ObjectReference obj, SourcePosition position)
            : base(position)
        {
            Obj = obj;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitObjectReference(this);
        }

        public override string ToString() => Obj.ToString();
    }
}