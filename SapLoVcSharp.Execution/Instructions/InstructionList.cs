using SapLoVcSharp.Core.Ast.Dependencies;

namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Container for a compiled list of instructions with metadata.
    /// Represents a fully compiled dependency ready for execution.
    /// </summary>
    public class InstructionList
    {
        public string DependencyId { get; set; } = null!;
        public DependencyType DependencyType { get; set; }
        public List<Instruction> Instructions { get; set; } = new();
        public InstructionMetadata Metadata { get; set; } = new();
        public DateTime CompiledAt { get; set; } = DateTime.UtcNow;
        public int Version { get; set; } = 1;

        public override string ToString() => $"{DependencyType} ({Instructions.Count} instructions)";
    }

    /// <summary>
    /// Metadata about a compiled instruction list.
    /// Provides information useful for optimization and debugging.
    /// </summary>
    public class InstructionMetadata
    {
        /// <summary>
        /// For constraints: object variable bindings from OBJECTS section
        /// Maps variable name to its class binding information
        /// </summary>
        public Dictionary<string, ObjectBinding>? ObjectBindings { get; set; }

        /// <summary>
        /// For constraints: characteristics to infer from INFERENCES section
        /// </summary>
        public List<string>? InferenceCharacteristics { get; set; }

        /// <summary>
        /// Indicates if this dependency contains async table calls
        /// </summary>
        public bool HasTableCalls { get; set; }

        /// <summary>
        /// Indicates if this dependency uses built-in functions
        /// </summary>
        public bool HasBuiltInFunctions { get; set; }

        /// <summary>
        /// Estimated complexity (e.g., instruction count, nesting depth)
        /// </summary>
        public int EstimatedComplexity { get; set; }
    }

    /// <summary>
    /// Represents an object binding from a constraint's OBJECTS section.
    /// Example: PC IS_A (300) BIKE WHERE MOD = MODEL
    /// </summary>
    public class ObjectBinding
    {
        public string Variable { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public string? ClassType { get; set; }
        public Dictionary<string, string>? WhereClause { get; set; }

        public override string ToString() =>
            $"{Variable} IS_A ({ClassType}) {ClassName}" +
            (WhereClause != null ? $" WHERE {string.Join(", ", WhereClause.Select(kv => $"{kv.Key} = {kv.Value}"))}" : "");
    }

    /// <summary>
    /// Dependency type enum for instruction list classification.
    /// </summary>
    public enum DependencyType
    {
        Procedure,
        Constraint,
        Precondition,
        SelectionCondition
    }
}
