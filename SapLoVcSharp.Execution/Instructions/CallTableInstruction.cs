using SapLoVcSharp.Execution.Contexts;

namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Calls a variant table to look up matching rows based on provided arguments.
    /// Pops argument values from stack (in reverse order), queries table, pushes result.
    /// Example: TABLE T_PRICING (MODEL = 'Racing', SIZE = 42)
    /// </summary>
    public class CallTableInstruction : Instruction
    {
        public string TableName { get; set; }
        public List<TableArgumentSpec> Arguments { get; set; }

        public CallTableInstruction(string tableName, List<TableArgumentSpec> arguments, int position)
            : base(OpCode.CallTable, position)
        {
            TableName = tableName;
            Arguments = arguments;
        }

        public override async Task ExecuteAsync(VirtualMachine vm)
        {
            // Track table usage
            vm.TablesUsed.Add(TableName);

            // Pop argument values from stack (in reverse order of how they were pushed)
            var arguments = new Dictionary<string, (object? Value, bool IsOptional)>();

            for (int i = Arguments.Count - 1; i >= 0; i--)
            {
                var arg = Arguments[i];
                var value = vm.Stack.Pop();

                // Include all arguments with their optional flag
                arguments[arg.ColumnName] = (value, arg.IsOptional);
            }

            bool matchFound;

            // Determine execution mode
            bool hasOptionalParams = Arguments.Any(a => a.IsOptional);
            bool hasLocalVarNames = Arguments.Any(a => !string.IsNullOrEmpty(a.LocalVariableName));
            bool isConstraintContext = vm.ConstraintContext != null;

            if (isConstraintContext && hasOptionalParams)
            {
                // INFERENCE MODE: Has optional params (?=) - infer values for optional parameters only
                matchFound = await ExecuteInferenceModeAsync(vm, arguments);
            }
            else if (isConstraintContext && hasLocalVarNames && !hasOptionalParams)
            {
                // Try RESTRICTION MODE: All params use =, with local var names
                // Check if we have any value restrictions - if not, fall back to simple mode
                matchFound = await TryExecuteRestrictionModeAsync(vm, arguments);
            }
            else
            {
                // SIMPLE MODE: Just check if a match exists (procedures, boolean checks)
                matchFound = await vm.TableResolver.ResolveAsync(TableName, arguments);
            }

            // Push result onto stack (true if match found, false otherwise)
            vm.Stack.Push(matchFound);
        }

        private async Task<bool> ExecuteInferenceModeAsync(
            VirtualMachine vm,
            Dictionary<string, (object? Value, bool IsOptional)> arguments)
        {
            // For constraints with optional output parameters: get all matching rows
            var matchingRows = await vm.TableResolver.GetMatchingRowsAsync(TableName, arguments);
            bool matchFound = matchingRows.Any();

            // Collect inferred values for optional output parameters
            foreach (var arg in Arguments.Where(a => a.IsOptional && !string.IsNullOrEmpty(a.LocalVariableName)))
            {
                // Collect all distinct values for this column from matching rows
                var inferredValues = matchingRows
                    .Where(row => row.ContainsKey(arg.ColumnName))
                    .Select(row => row[arg.ColumnName])
                    .Distinct()
                    .ToList();

                // Always store inferred values - even if empty (which restricts to no values)
                vm.ConstraintContext.SetInferredValues(arg.LocalVariableName!, inferredValues);
            }

            return matchFound;
        }

        private async Task<bool> TryExecuteRestrictionModeAsync(
            VirtualMachine vm,
            Dictionary<string, (object? Value, bool IsOptional)> arguments)
        {
            // Get current allowed values for each characteristic from configuration context
            var allowedValueSets = new Dictionary<string, List<string>>();
            bool hasRestrictions = false;

            foreach (var arg in Arguments.Where(a => !string.IsNullOrEmpty(a.LocalVariableName)))
            {
                // Map local variable to configuration characteristic
                var configCharName = vm.ConstraintContext.GetConfigCharacteristicName(arg.LocalVariableName!);

                if (configCharName != null)
                {
                    // Get current allowed values for this characteristic
                    var allowedValues = await vm.Context.GetAvailableValuesAsync(configCharName);

                    // If restrictions exist, use them
                    if (allowedValues != null && allowedValues.Any())
                    {
                        allowedValueSets[arg.ColumnName] = allowedValues;
                        hasRestrictions = true;
                    }
                    else
                    {
                        // No restrictions - use current value if available
                        var currentValue = await vm.Context.GetValueAsync(configCharName);
                        if (currentValue != null)
                        {
                            allowedValueSets[arg.ColumnName] = new List<string> { currentValue.ToString()! };
                            hasRestrictions = true;
                        }
                        // If no current value, we still need to get all possible values from the table
                    }
                }
            }

            // Get matching rows - either filtered by allowed values or all rows if no restrictions
            List<Dictionary<string, string>> matchingRows;

            if (hasRestrictions && allowedValueSets.Count > 0)
            {
                // Find matching rows where all values are in the allowed sets
                matchingRows = await vm.TableResolver.GetMatchingRowsWithAllowedValuesAsync(
                    TableName,
                    allowedValueSets);
            }
            else
            {
                // No restrictions at all - get ALL rows from the table
                // This allows inferring all possible values for unrestricted characteristics
                matchingRows = await vm.TableResolver.GetMatchingRowsAsync(
                    TableName,
                    new Dictionary<string, (object? Value, bool IsOptional)>());
            }

            bool matchFound = matchingRows.Any();

            // Collect values from matching rows for ALL characteristics (not just optional)
            foreach (var arg in Arguments.Where(a => !string.IsNullOrEmpty(a.LocalVariableName)))
            {
                var inferredValues = matchingRows
                    .Where(row => row.ContainsKey(arg.ColumnName))
                    .Select(row => row[arg.ColumnName])
                    .Where(v => !string.IsNullOrEmpty(v))
                    .Distinct()
                    .ToList();

                // Always store inferred values - even if empty (which restricts to no values)
                vm.ConstraintContext.SetInferredValues(arg.LocalVariableName!, inferredValues);
            }

            return matchFound;
        }

        public override string ToString()
        {
            var args = string.Join(", ", Arguments.Select(a =>
                a.IsOptional ? $"{a.ColumnName} ?=" : $"{a.ColumnName} ="));
            return $"CALL_TABLE {TableName}({args})";
        }
    }

    /// <summary>
    /// Specification for a table argument (column mapping).
    /// </summary>
    public class TableArgumentSpec
    {
        public string ColumnName { get; set; }
        public bool IsOptional { get; set; }
        public string? LocalVariableName { get; set; }  // For optional params: the variable to store inferred values

        public TableArgumentSpec(string columnName, bool isOptional = false, string? localVariableName = null)
        {
            ColumnName = columnName;
            IsOptional = isOptional;
            LocalVariableName = localVariableName;
        }
    }
}
