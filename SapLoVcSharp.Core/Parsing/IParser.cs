using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Ast.Expressions;

namespace SapLoVcSharp.Core.Parsing
{
    /// <summary>
    /// Interface for parsing SAP LO-VC source code.
    /// </summary>
    public interface IParser
    {
        /// <summary>
        /// Parses a complete dependency (procedure, precondition, etc.).
        /// </summary>
        DependencyNode ParseDependency(string source, DependencyType type);

        /// <summary>
        /// Parses just an expression (useful for testing).
        /// </summary>
        ExpressionNode ParseExpression(string source);
    }
}