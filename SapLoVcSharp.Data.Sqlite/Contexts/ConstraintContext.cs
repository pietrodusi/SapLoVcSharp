using SapLoVcSharp.Execution.Contexts;

namespace SapLoVcSharp.Data.Sqlite.Contexts
{
    /// <summary>
    /// Constraint context for managing transactional constraint execution.
    /// Supports local variables, object bindings, and transaction semantics.
    /// </summary>
    public class ConstraintContext : IConstraintContext
    {
        private readonly Dictionary<string, object?> _localVariables = new();
        private readonly Dictionary<string, ConstraintObjectBinding> _objectBindings = new();
        private readonly Dictionary<string, List<string>> _inferredValues = new();
        private Dictionary<string, object?>? _snapshot;

        public bool InTransaction { get; private set; }

        public void BindVariable(string variableName, string className, string? classType, Dictionary<string, string>? variableMappings)
        {
            _objectBindings[variableName] = new ConstraintObjectBinding
            {
                ClassName = className,
                ClassType = classType,
                VariableMappings = variableMappings ?? new Dictionary<string, string>()
            };
        }

        public ConstraintObjectBinding? GetBinding(string variableName)
        {
            return _objectBindings.TryGetValue(variableName, out var binding) ? binding : null;
        }

        public Task<object?> GetMemberValueAsync(string variable, string memberName)
        {
            // Get the binding for this variable (e.g., "PC")
            var binding = GetBinding(variable);
            if (binding == null)
            {
                throw new InvalidOperationException($"Variable '{variable}' is not bound");
            }

            // Find the local variable that maps to this member
            // For example, if PC is bound with mapping ["COLOR" -> "lv_COLOR"]
            // then PC.COLOR should return _localVariables["lv_COLOR"]
            if (binding.VariableMappings.TryGetValue(memberName, out var localVarName))
            {
                return Task.FromResult(GetLocalVariable(localVarName));
            }

            throw new InvalidOperationException(
                $"Member '{memberName}' is not mapped for variable '{variable}'");
        }

        public Task SetMemberValueAsync(string variable, string memberName, object? value)
        {
            // Get the binding for this variable
            var binding = GetBinding(variable);
            if (binding == null)
            {
                throw new InvalidOperationException($"Variable '{variable}' is not bound");
            }

            // Find the local variable that maps to this member
            if (binding.VariableMappings.TryGetValue(memberName, out var localVarName))
            {
                SetLocalVariable(localVarName, value);
                return Task.CompletedTask;
            }

            throw new InvalidOperationException(
                $"Member '{memberName}' is not mapped for variable '{variable}'");
        }

        public async Task InitializeLocalVariableAsync(
            string localVarName,
            string configCharacteristicName,
            IConfigurationContext configContext)
        {
            // Read value from configuration context
            var value = await configContext.GetValueAsync(configCharacteristicName);

            // Store in local variable
            _localVariables[localVarName] = value;
        }

        public object? GetLocalVariable(string name)
        {
            return _localVariables.TryGetValue(name, out var value) ? value : null;
        }

        public void SetLocalVariable(string name, object? value)
        {
            _localVariables[name] = value;
        }

        public void SetInferredValues(string localVarName, List<string> values)
        {
            _inferredValues[localVarName] = values;
        }

        public List<string>? GetInferredValues(string localVarName)
        {
            return _inferredValues.TryGetValue(localVarName, out var values) ? values : null;
        }

        public string? GetConfigCharacteristicName(string localVarName)
        {
            // Search through all bindings to find the config characteristic that maps to this local variable
            // VariableMappings: {localVarName -> configCharName}
            foreach (var binding in _objectBindings.Values)
            {
                // The dictionary key is the local var name, value is the config char name
                if (binding.VariableMappings.TryGetValue(localVarName, out var configCharName))
                {
                    return configCharName;
                }
            }

            return null;
        }

        public void BeginTransaction()
        {
            if (InTransaction)
            {
                throw new InvalidOperationException("Transaction already in progress");
            }

            // Take snapshot of current local variables
            _snapshot = new Dictionary<string, object?>(_localVariables);
            InTransaction = true;
        }

        public async Task<bool> CommitAsync(IConfigurationContext configContext)
        {
            if (!InTransaction)
            {
                throw new InvalidOperationException("No transaction in progress");
            }

            try
            {
                // Apply all local variable changes to configuration context
                foreach (var (name, value) in _localVariables)
                {
                    // Only commit variables that were initialized from config (WHERE clause mappings)
                    // Skip pure local variables (those not in any binding's variable mappings)
                    bool isConfigVariable = _objectBindings.Values
                        .Any(binding => binding.VariableMappings.ContainsKey(name));

                    if (isConfigVariable)
                    {
                        // Find the corresponding config characteristic name
                        var configName = _objectBindings.Values
                            .SelectMany(binding => binding.VariableMappings)
                            .FirstOrDefault(mapping => mapping.Key == name).Value;

                        if (!string.IsNullOrEmpty(configName))
                        {
                            await configContext.SetValueAsync(configName, value);
                        }
                    }
                }

                // Clear transaction state
                _snapshot = null;
                InTransaction = false;
                _localVariables.Clear();

                return true;
            }
            catch
            {
                // On error, rollback
                Rollback();
                throw;
            }
        }

        public void Rollback()
        {
            if (!InTransaction)
            {
                throw new InvalidOperationException("No transaction in progress");
            }

            // Restore snapshot
            if (_snapshot != null)
            {
                _localVariables.Clear();
                foreach (var (key, value) in _snapshot)
                {
                    _localVariables[key] = value;
                }
            }

            _snapshot = null;
            InTransaction = false;
        }

        public void Clear()
        {
            _localVariables.Clear();
            _objectBindings.Clear();
            _inferredValues.Clear();
            _snapshot = null;
            InTransaction = false;
        }

    }

    /// <summary>
    /// Object binding information for constraint execution
    /// </summary>
    public class ConstraintObjectBinding
    {
        public string ClassName { get; set; } = null!;
        public string? ClassType { get; set; }
        public Dictionary<string, string> VariableMappings { get; set; } = new();
    }
}
