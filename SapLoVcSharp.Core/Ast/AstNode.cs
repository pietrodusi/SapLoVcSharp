using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast
{
    /// <summary>
    /// Base class for all AST nodes.
    /// </summary>
    public abstract class AstNode
    {
        public SourcePosition Position { get; }

        protected AstNode(SourcePosition position)
        {
            Position = position;
        }

        public abstract void Accept(IAstVisitor visitor);
    }
}