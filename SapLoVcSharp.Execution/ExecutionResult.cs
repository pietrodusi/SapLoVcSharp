namespace SapLoVcSharp.Execution
{
    /// <summary>
    /// Result of executing a dependency via the virtual machine.
    /// </summary>
    public class ExecutionResult
    {
        /// <summary>
        /// Whether the execution completed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Whether the configuration is inconsistent (constraint violation)
        /// </summary>
        public bool Inconsistent { get; set; }

        /// <summary>
        /// Optional message describing the result or error
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Exception if execution failed
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Return value (for preconditions/selection conditions)
        /// </summary>
        public object? ReturnValue { get; set; }

        /// <summary>
        /// List of variant tables accessed during execution
        /// </summary>
        public List<string> TablesUsed { get; set; } = new();

        public override string ToString() =>
            Success ? "Success" :
            Inconsistent ? $"Inconsistent: {Message}" :
            $"Failed: {Message}";
    }
}
