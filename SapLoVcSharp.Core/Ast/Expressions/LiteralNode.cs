using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Expressions
{
    /// <summary>
    /// Represents a literal value (number, string, boolean).
    /// </summary>
    public class LiteralNode : ExpressionNode
    {
        public object Value { get; }
        public LiteralType Type { get; }

        public LiteralNode(object value, LiteralType type, SourcePosition position)
            : base(position)
        {
            Value = value;
            Type = type;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitLiteral(this);
        }

        public override string ToString() => $"Literal({Value})";
    }

    public enum LiteralType
    {
        Number,
        String,
        Boolean
    }
}