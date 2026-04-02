using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Statements
{
    /// <summary>
    /// Base class for all statement nodes.
    /// </summary>
    public abstract class StatementNode : AstNode
    {
        protected StatementNode(SourcePosition position) : base(position)
        {
        }
    }
}