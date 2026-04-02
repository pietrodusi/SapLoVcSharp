using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Expressions
{
    /// <summary>
    /// Base class for all expression nodes.
    /// </summary>
    public abstract class ExpressionNode : AstNode
    {
        protected ExpressionNode(SourcePosition position) : base(position)
        {
        }
    }
}