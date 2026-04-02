using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Statements
{
    /// <summary>
    /// Represents an assignment statement (e.g., $SELF.COLOR = 'Red' or $SELF.COLOR = 'Red' IF MODEL = 'Racing').
    /// </summary>
    public class ReturnInconsistencyNode : StatementNode
    {
        public ReturnInconsistencyNode(SourcePosition position) : base(position) { }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitReturnInconsistency(this);
        }

        public override string ToString() =>
            $"";
    }
}