using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Expressions
{
    /// <summary>
    /// Represents an identifier (characteristic name, variable).
    /// </summary>
    public class IdentifierNode : ExpressionNode
    {
        public string Name { get; }

        public IdentifierNode(string name, SourcePosition position)
            : base(position)
        {
            Name = name;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitIdentifier(this);
        }

        public override string ToString() => $"Identifier({Name})";
    }
}