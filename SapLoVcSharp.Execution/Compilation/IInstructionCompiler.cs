using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Execution.Instructions;

namespace SapLoVcSharp.Execution.Compilation
{
    /// <summary>
    /// Compiler for converting AST nodes to executable instruction lists.
    /// </summary>
    public interface IInstructionCompiler
    {
        /// <summary>
        /// Compiles an AST dependency node to an instruction list.
        /// </summary>
        /// <param name="dependency">The dependency AST to compile</param>
        /// <returns>Compiled instruction list ready for execution</returns>
        InstructionList Compile(DependencyNode dependency);
    }
}
