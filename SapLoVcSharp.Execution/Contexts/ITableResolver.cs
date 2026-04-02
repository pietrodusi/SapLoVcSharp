namespace SapLoVcSharp.Execution.Contexts
{
    /// <summary>
    /// Resolves variant table lookups.
    /// Provides async database access for TABLE calls.
    /// </summary>
    public interface ITableResolver
    {
        /// <summary>
        /// Resolves a table call with the provided arguments.
        /// Returns true if a matching row is found, false otherwise.
        /// </summary>
        /// <param name="tableName">Name of the variant table</param>
        /// <param name="arguments">Column arguments with values and optional flags</param>
        /// <returns>True if match found, false otherwise</returns>
        Task<bool> ResolveAsync(
            string tableName,
            Dictionary<string, (object? Value, bool IsOptional)> arguments);

        /// <summary>
        /// Gets all matching rows from a table that satisfy the provided arguments.
        /// Returns the row data as a list of dictionaries (column name -> value).
        /// Used for inference to collect possible values.
        /// </summary>
        /// <param name="tableName">Name of the variant table</param>
        /// <param name="arguments">Column arguments with values and optional flags</param>
        /// <returns>List of matching rows, each as a dictionary of column values</returns>
        Task<List<Dictionary<string, string>>> GetMatchingRowsAsync(
            string tableName,
            Dictionary<string, (object? Value, bool IsOptional)> arguments);

        /// <summary>
        /// Gets all matching rows from a table where column values are in the provided allowed value sets.
        /// Used for restricting mode where all characteristics are bidirectional (both input and output).
        /// </summary>
        /// <param name="tableName">Name of the variant table</param>
        /// <param name="allowedValues">For each column, the set of allowed values to match against</param>
        /// <returns>List of matching rows, each as a dictionary of column values</returns>
        Task<List<Dictionary<string, string>>> GetMatchingRowsWithAllowedValuesAsync(
            string tableName,
            Dictionary<string, List<string>> allowedValues);
    }
}
