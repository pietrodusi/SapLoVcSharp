using SapLoVcSharp.Core.Ast.Statements;
using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Dependencies
{
    /// <summary>
    /// Represents a procedure dependency.
    /// </summary>
    public class ProcedureNode : DependencyNode
    {
        public List<StatementNode> Statements { get; }

        public ProcedureNode(List<StatementNode> statements, SourcePosition position)
            : base(position)
        {
            Statements = statements;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitProcedure(this);
        }

        public override string ToString() =>
            $"Procedure({string.Join(", ", Statements)})";
    }
}