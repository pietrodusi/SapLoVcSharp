using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Dependencies
{
    /// <summary>
    /// Base class for all dependency type nodes.
    /// </summary>
    public abstract class DependencyNode : AstNode
    {
        public string? Name { get; set; }
        public string? Description { get; set; }

        protected DependencyNode(SourcePosition position) : base(position)
        {
        }
    }

    public enum DependencyType
    {
        Procedure,
        Precondition,
        SelectionCondition,
        Constraint,
        Action  // Obsolete but included for completeness
    }
}