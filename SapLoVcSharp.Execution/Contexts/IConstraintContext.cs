namespace SapLoVcSharp.Execution.Contexts
{
    /// <summary>
    /// Context for constraint execution with variable bindings and transactional semantics.
    /// Constraints execute in an isolated context, validate consistency, then apply if successful.
    /// </summary>
    public interface IConstraintContext
    {
        /// <summary>
        /// Binds a constraint variable to a class instance.
        /// Called when executing BIND_OBJECT instruction.
        /// </summary>
        /// <param name="variable">Variable name (e.g., "PC")</param>
        /// <param name="className">Class name (e.g., "BIKE")</param>
        /// <param name="classType">Optional class type (e.g., "300")</param>
        /// <param name="whereClause">Optional WHERE clause mappings (e.g., {{"MOD", "MODEL"}})</param>
        void BindVariable(
            string variable,
            string className,
            string? classType,
            Dictionary<string, string>? whereClause);

        /// <summary>
        /// Gets a member value from a constraint variable.
        /// Example: PC.COLOR returns the value of COLOR characteristic
        /// </summary>
        Task<object?> GetMemberValueAsync(string variable, string memberName);

        /// <summary>
        /// Sets a member value in a constraint variable.
        /// Example: PC.COLOR = 'Red' sets the COLOR characteristic to 'Red'
        /// </summary>
        Task SetMemberValueAsync(string variable, string memberName, object? value);

        // Local variable management (for WHERE clause variables like lv_CASE)

        /// <summary>
        /// Initializes a local constraint variable from the configuration context.
        /// Called for WHERE clause: lv_CASE = CASE reads CASE from config and stores in lv_CASE.
        /// </summary>
        Task InitializeLocalVariableAsync(string localVarName, string configCharacteristicName, IConfigurationContext configContext);

        /// <summary>
        /// Gets a local constraint variable value.
        /// Example: lv_CASE returns the local variable value
        /// </summary>
        object? GetLocalVariable(string name);

        /// <summary>
        /// Sets a local constraint variable value.
        /// Example: lv_CASE = 'A' sets the local variable
        /// </summary>
        void SetLocalVariable(string name, object? value);

        /// <summary>
        /// Sets inferred values for a local variable from a TABLE call.
        /// Stores a collection of possible values rather than a single value.
        /// Used for optional output parameters in TABLE calls (e.g., CATEGORY ?= lv_CATEGORY)
        /// </summary>
        void SetInferredValues(string localVarName, List<string> values);

        /// <summary>
        /// Gets inferred values for a local variable from TABLE calls.
        /// Returns null if no inferences have been collected for this variable.
        /// </summary>
        List<string>? GetInferredValues(string localVarName);

        /// <summary>
        /// Finds the configuration characteristic name that corresponds to a local variable name.
        /// Returns null if no mapping is found.
        /// Example: "lv_CATEGORY" → "CATEGORY"
        /// </summary>
        string? GetConfigCharacteristicName(string localVarName);

        // Transaction management

        /// <summary>
        /// Begins a constraint execution transaction.
        /// Creates an isolated context for constraint execution.
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// Applies constraint context changes to the configuration context.
        /// Called at the end of successful constraint execution (no inconsistencies).
        /// Returns true if successfully applied, false if conflicts detected.
        /// </summary>
        Task<bool> CommitAsync(IConfigurationContext configContext);

        /// <summary>
        /// Rolls back constraint context changes.
        /// Called when constraint returns inconsistency or encounters error.
        /// </summary>
        void Rollback();

        /// <summary>
        /// Gets whether the constraint context is currently in a transaction.
        /// </summary>
        bool InTransaction { get; }

        /// <summary>
        /// Clears all context state including local variables, bindings, inferences, and transaction state.
        /// Should be called after each constraint execution to ensure clean state for next execution.
        /// </summary>
        void Clear();
    }
}
