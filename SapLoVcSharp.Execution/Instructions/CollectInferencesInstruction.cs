namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Applies inferred value restrictions to characteristics based on constraint local variables.
    /// Executes after RESTRICTIONS but before COMMIT in constraint execution.
    ///
    /// Maps inferred local variables (e.g., lv_HEIGHT) back to configuration characteristics (e.g., FRAME_HEIGHT)
    /// and restricts the characteristic to only the values that appear in the constraint context.
    /// </summary>
    public class CollectInferencesInstruction : Instruction
    {
        /// <summary>
        /// List of characteristic names (local variables in constraint) to infer values for.
        /// Example: ["lv_HEIGHT", "lv_COLOR"]
        /// </summary>
        public List<string> InferredLocalVars { get; }

        public CollectInferencesInstruction(
            List<string> inferredLocalVars,
            int position)
            : base(OpCode.ApplyInferences, position)
        {
            InferredLocalVars = inferredLocalVars;
        }

        public override async Task ExecuteAsync(VirtualMachine vm)
        {
            if (!InferredLocalVars.Any())
            {
                // No inferences - nothing to do
                return;
            }

            // For each inferred local variable, apply the collected inferences to the configuration context
            foreach (var localVarName in InferredLocalVars)
            {
                // Get the inferred values from TABLE calls (if any)
                var inferredValues = vm.ConstraintContext.GetInferredValues(localVarName);

                if (inferredValues != null)
                {
                    // Find the corresponding configuration characteristic name
                    var configCharName = FindConfigCharacteristic(localVarName, vm);

                    if (configCharName != null)
                    {
                        // Restrict the configuration characteristic to the inferred values
                        // This applies the complete set of values collected from all matching table rows
                        // NOTE: An empty list means "no values allowed" (e.g., empty variant table)
                        await vm.Context.RestrictValuesAsync(configCharName, inferredValues);

                        // If there's only one possible value, set it automatically
                        if (inferredValues.Count == 1)
                        {
                            await vm.Context.SetValueAsync(configCharName, inferredValues[0]);

                            // IMPORTANT: Also update the local variable so CommitAsync doesn't overwrite this value
                            vm.ConstraintContext.SetLocalVariable(localVarName, inferredValues[0]);
                        }
                    }
                }
                else
                {
                    // Fallback: check if there's a single value set in the local variable
                    // (for non-TABLE inferences or simple assignments)
                    var value = vm.ConstraintContext.GetLocalVariable(localVarName);
                    if (value != null)
                    {
                        var configCharName = FindConfigCharacteristic(localVarName, vm);
                        if (configCharName != null)
                        {
                            var valueStr = value.ToString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(valueStr))
                            {
                                await vm.Context.RestrictValuesAsync(configCharName, new List<string> { valueStr });
                            }
                        }
                    }
                }
            }
        }

        private string? FindConfigCharacteristic(string localVarName, VirtualMachine vm)
        {
            // Use the constraint context to map from local variable to config characteristic
            // Example: "lv_CATEGORY" → "CATEGORY"
            var configCharName = vm.ConstraintContext.GetConfigCharacteristicName(localVarName);

            if (configCharName != null)
            {
                return configCharName;
            }

            // Fallback: assume the local variable name directly maps to the characteristic
            // This works for cases like: INFERENCES: FRAME_HEIGHT (where FRAME_HEIGHT is both the local var and config name)
            return localVarName;
        }

        public override string ToString()
        {
            var inferences = string.Join(", ", InferredLocalVars);
            return $"APPLY_INFERENCES [{inferences}]";
        }
    }
}
