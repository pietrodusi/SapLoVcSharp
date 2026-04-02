using SapLoVcSharp.Core.Ast.Dependencies;

namespace SapLoVcSharp.Data.Serialization
{
    /// <summary>
    /// Serializes and deserializes AST nodes to/from JSON.
    /// </summary>
    public interface IAstSerializer
    {
        /// <summary>
        /// Serialize an AST node to JSON string.
        /// </summary>
        string Serialize(DependencyNode node);

        /// <summary>
        /// Deserialize JSON string to an AST node.
        /// </summary>
        DependencyNode Deserialize(string json);

        /// <summary>
        /// Deserialize JSON string to a specific AST node type.
        /// </summary>
        T Deserialize<T>(string json) where T : DependencyNode;
    }
}