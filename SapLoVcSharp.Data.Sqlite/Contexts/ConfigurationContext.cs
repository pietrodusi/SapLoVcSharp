using Microsoft.EntityFrameworkCore;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Execution.Contexts;

namespace SapLoVcSharp.Data.Sqlite.Contexts
{
    /// <summary>
    /// EF Core-backed configuration context for tracking characteristic values
    /// during a configuration session.
    ///
    /// This is an in-memory context for MVP - values are not persisted to database.
    /// </summary>
    public class ConfigurationContext : IConfigurationContext
    {
        private readonly SqliteDbContext _dbContext;
        private readonly string _materialNumber;
        private readonly Dictionary<string, object?> _characteristicValues = new();
        private readonly HashSet<string> _specifiedCharacteristics = new();
        private readonly ConfigurationObject _selfObject;

        // Value restriction tracking: characteristic -> list of allowed values
        // null means no restrictions (all values allowed)
        private readonly Dictionary<string, List<string>?> _valueRestrictions = new();

        // Action log for tracking all changes during execution
        private readonly List<ConfigurationActionLog> _actionLogs = new();

        // Current execution context (set by ExecutionService)
        public int CurrentCycle { get; set; }
        public string? CurrentDependencyName { get; set; }
        public string? CurrentDependencyType { get; set; }

        public IReadOnlyList<ConfigurationActionLog> ActionLogs => _actionLogs;

        public IConfigurationObject Self => _selfObject;
        public IConfigurationObject? Parent => null; // MVP: no parent support yet
        public IConfigurationObject? Root => null;   // MVP: no root support yet

        public ConfigurationContext(SqliteDbContext dbContext, string materialNumber)
        {
            _dbContext = dbContext;
            _materialNumber = materialNumber;
            _selfObject = new ConfigurationObject(this);
        }

        public IConfigurationObject ResolveObject(ObjectReference objRef)
        {
            return objRef switch
            {
                ObjectReference.Self => Self,
                ObjectReference.Parent => Parent ?? throw new InvalidOperationException("Parent object not available in this context"),
                ObjectReference.Root => Root ?? throw new InvalidOperationException("Root object not available in this context"),
                _ => throw new ArgumentException($"Unknown object reference: {objRef}")
            };
        }

        public async Task<object?> GetValueAsync(string characteristicName)
        {
            // Check if value exists in current session
            if (_characteristicValues.TryGetValue(characteristicName, out var value))
            {
                return value;
            }

            // No value assigned yet
            return null;
        }

        public Task SetValueAsync(string characteristicName, object? value)
        {
            return SetValueAsync(characteristicName, value, skipLogging: false);
        }

        public Task SetValueAsync(string characteristicName, object? value, bool skipLogging)
        {
            // Unwrap JsonElement to native type if needed
            value = UnwrapJsonElement(value);

            // Get old value for logging
            _characteristicValues.TryGetValue(characteristicName, out var oldValue);

            // Only log if value actually changed and logging is not skipped
            if (!skipLogging && !AreValuesEqual(oldValue, value) && CurrentDependencyType != null)
            {
                _actionLogs.Add(new ConfigurationActionLog
                {
                    Cycle = CurrentCycle,
                    ActionType = CurrentDependencyType,
                    Source = CurrentDependencyName ?? "Unknown",
                    CharacteristicName = characteristicName,
                    OldValue = oldValue?.ToString(),
                    NewValue = value?.ToString(),
                    Description = $"Value assigned: {characteristicName} = '{value}'"
                });

                // When a dependency (procedure/constraint) assigns a value, restrict domain to that value
                if (value != null)
                {
                    var valueStr = value.ToString()!;

                    // Get previous restriction state for logging
                    _valueRestrictions.TryGetValue(characteristicName, out var previousRestriction);
                    var previousStr = previousRestriction != null ? string.Join(", ", previousRestriction) : "(unrestricted)";

                    // Restrict to single value
                    _valueRestrictions[characteristicName] = new List<string> { valueStr };

                    // Log the restriction
                    _actionLogs.Add(new ConfigurationActionLog
                    {
                        Cycle = CurrentCycle,
                        ActionType = "Restriction",
                        Source = CurrentDependencyName ?? "Unknown",
                        CharacteristicName = characteristicName,
                        OldValue = previousStr,
                        NewValue = valueStr,
                        Description = $"Domain restricted to assigned value: {characteristicName} → [{valueStr}]"
                    });
                }
            }

            _characteristicValues[characteristicName] = value;

            // Mark as specified when a value is set
            if (value != null)
            {
                _specifiedCharacteristics.Add(characteristicName);
            }
            else
            {
                // If setting to null, remove from specified
                _specifiedCharacteristics.Remove(characteristicName);
            }

            return Task.CompletedTask;
        }

        public Task<bool> IsSpecifiedAsync(string characteristicName)
        {
            return Task.FromResult(_specifiedCharacteristics.Contains(characteristicName));
        }

        public Task<List<string>> GetAllCharacteristicNamesAsync()
        {
            // Get all characteristics from classes assigned to this material
            var characteristicNames = _dbContext.Materials
                .Where(m => m.MaterialNumber == _materialNumber)
                .SelectMany(m => m.ClassAssignments)
                .SelectMany(ca => ca.Class!.Characteristics)
                .Select(c => c.Name)
                .Distinct()
                .ToList();

            return Task.FromResult(characteristicNames);
        }

        public async Task<Dictionary<string, object?>> GetAllValuesAsync()
        {
            // Get all characteristic names for this material
            var allCharNames = await GetAllCharacteristicNamesAsync();

            // Return complete context: all characteristics with their current values (or null if not set)
            var result = new Dictionary<string, object?>();
            foreach (var charName in allCharNames)
            {
                result[charName] = _characteristicValues.TryGetValue(charName, out var value) ? value : null;
            }

            return result;
        }

        public Task ClearAsync()
        {
            _characteristicValues.Clear();
            _specifiedCharacteristics.Clear();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Initialize configuration context with default values from characteristic definitions
        /// </summary>
        public async Task InitializeWithDefaultsAsync()
        {
            var material = await _dbContext.Materials
                .Include(m => m.ClassAssignments)
                    .ThenInclude(ca => ca.Class)
                        .ThenInclude(c => c!.Characteristics)
                            .ThenInclude(ch => ch.Values)
                .FirstOrDefaultAsync(m => m.MaterialNumber == _materialNumber);

            if (material == null)
            {
                throw new InvalidOperationException($"Material {_materialNumber} not found");
            }

            // Set default values for characteristics that have defaults
            foreach (var classAssignment in material.ClassAssignments)
            {
                foreach (var characteristic in classAssignment.Class!.Characteristics)
                {
                    // If there's a default value (first value marked as default), set it
                    var defaultValue = characteristic.Values.FirstOrDefault(v => v.IsDefault);
                    if (defaultValue != null)
                    {
                        await SetValueAsync(characteristic.Name, defaultValue.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Validate that all required characteristics have been specified
        /// </summary>
        public async Task<List<string>> GetMissingRequiredCharacteristicsAsync()
        {
            var material = await _dbContext.Materials
                .Include(m => m.ClassAssignments)
                    .ThenInclude(ca => ca.Class)
                        .ThenInclude(c => c!.Characteristics)
                .FirstOrDefaultAsync(m => m.MaterialNumber == _materialNumber);

            if (material == null)
            {
                return new List<string>();
            }

            var missingRequired = new List<string>();

            foreach (var classAssignment in material.ClassAssignments)
            {
                foreach (var characteristic in classAssignment.Class!.Characteristics)
                {
                    if (characteristic.IsRequired && !_specifiedCharacteristics.Contains(characteristic.Name))
                    {
                        missingRequired.Add(characteristic.Name);
                    }
                }
            }

            return missingRequired;
        }

        // ===== Value Restriction Implementation =====

        public Task<List<string>?> GetAvailableValuesAsync(string characteristicName)
        {
            // Return null if no restrictions (all values available)
            // Otherwise return the restricted list
            _valueRestrictions.TryGetValue(characteristicName, out var availableValues);
            return Task.FromResult(availableValues);
        }

        public Task RestrictValuesAsync(string characteristicName, List<string> allowedValues)
        {
            List<string>? previousValues = null;
            List<string> newValues;

            if (_valueRestrictions.TryGetValue(characteristicName, out var existingRestrictions))
            {
                previousValues = existingRestrictions;

                if (existingRestrictions != null)
                {
                    // Intersect with existing restrictions (further narrowing)
                    newValues = existingRestrictions.Intersect(allowedValues).ToList();
                    _valueRestrictions[characteristicName] = newValues;
                }
                else
                {
                    // First restriction for this characteristic
                    newValues = new List<string>(allowedValues);
                    _valueRestrictions[characteristicName] = newValues;
                }
            }
            else
            {
                // First restriction for this characteristic
                newValues = new List<string>(allowedValues);
                _valueRestrictions[characteristicName] = newValues;
            }

            // Log domain restriction
            var previousStr = previousValues != null ? string.Join(", ", previousValues) : "(unrestricted)";
            var newStr = string.Join(", ", newValues);

            _actionLogs.Add(new ConfigurationActionLog
            {
                Cycle = CurrentCycle,
                ActionType = "Restriction",
                Source = CurrentDependencyName ?? "Unknown",
                CharacteristicName = characteristicName,
                OldValue = previousStr,
                NewValue = newStr,
                Description = $"Domain restricted: {characteristicName} → [{newStr}]"
            });

            return Task.CompletedTask;
        }

        public Task<bool> IsValueAvailableAsync(string characteristicName, string value)
        {
            if (!_valueRestrictions.TryGetValue(characteristicName, out var restrictions))
            {
                // No restrictions = all values available
                return Task.FromResult(true);
            }

            if (restrictions == null)
            {
                // Explicitly set to null = all values available
                return Task.FromResult(true);
            }

            // Check if value is in the allowed list (case-insensitive)
            var isAvailable = restrictions.Any(v =>
                string.Equals(v, value, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(isAvailable);
        }

        public Task ClearRestrictionsAsync(string characteristicName)
        {
            _valueRestrictions.Remove(characteristicName);
            return Task.CompletedTask;
        }

        public Task<List<string>> GetRestrictedCharacteristicsAsync()
        {
            var restricted = _valueRestrictions
                .Where(kvp => kvp.Value != null) // Only include those with actual restrictions
                .Select(kvp => kvp.Key)
                .ToList();

            return Task.FromResult(restricted);
        }

        // ===== Change Detection Implementation =====

        public Dictionary<string, object?> CreateSnapshot()
        {
            // Create a deep copy of current characteristic values
            return new Dictionary<string, object?>(_characteristicValues);
        }

        public HashSet<string> DetectChanges(Dictionary<string, object?> previousSnapshot)
        {
            var changedCharacteristics = new HashSet<string>();

            // Check for new or modified characteristics
            foreach (var (name, currentValue) in _characteristicValues)
            {
                if (!previousSnapshot.TryGetValue(name, out var previousValue))
                {
                    // New characteristic added
                    changedCharacteristics.Add(name);
                }
                else if (!AreValuesEqual(previousValue, currentValue))
                {
                    // Value changed
                    changedCharacteristics.Add(name);
                }
            }

            // Check for removed characteristics (should be rare, but handle it)
            foreach (var name in previousSnapshot.Keys)
            {
                if (!_characteristicValues.ContainsKey(name))
                {
                    changedCharacteristics.Add(name);
                }
            }

            return changedCharacteristics;
        }

        private bool AreValuesEqual(object? value1, object? value2)
        {
            // Handle null cases
            if (value1 == null && value2 == null) return true;
            if (value1 == null || value2 == null) return false;

            // Unwrap JsonElement if needed
            value1 = UnwrapJsonElement(value1);
            value2 = UnwrapJsonElement(value2);

            // Handle null after unwrapping
            if (value1 == null && value2 == null) return true;
            if (value1 == null || value2 == null) return false;

            // Compare values
            return value1.Equals(value2);
        }

        private object? UnwrapJsonElement(object? value)
        {
            if (value is System.Text.Json.JsonElement jsonElement)
            {
                return jsonElement.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => jsonElement.GetString(),
                    System.Text.Json.JsonValueKind.Number => jsonElement.GetDouble(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    System.Text.Json.JsonValueKind.Null => null,
                    _ => value
                };
            }
            return value;
        }

        /// <summary>
        /// Nested class that implements IConfigurationObject for Self/Parent/Root references
        /// </summary>
        private class ConfigurationObject : IConfigurationObject
        {
            private readonly ConfigurationContext _context;

            public ConfigurationObject(ConfigurationContext context)
            {
                _context = context;
            }

            public Task<object?> GetCharacteristicValueAsync(string name)
            {
                return _context.GetValueAsync(name);
            }

            public Task SetCharacteristicValueAsync(string name, object? value)
            {
                return _context.SetValueAsync(name, value);
            }

            public Task<bool> IsCharacteristicSpecifiedAsync(string name)
            {
                return _context.IsSpecifiedAsync(name);
            }
        }

        /// <summary>
        /// Log a user input action (before SetValueAsync to capture the old value)
        /// </summary>
        public void LogUserInput(string characteristicName, object? oldValue, object? newValue)
        {
            _actionLogs.Add(new ConfigurationActionLog
            {
                Cycle = 0,
                ActionType = "UserInput",
                Source = "User",
                CharacteristicName = characteristicName,
                OldValue = oldValue?.ToString(),
                NewValue = newValue?.ToString(),
                Description = $"User assigned: {characteristicName} = '{newValue}'"
            });
        }

        /// <summary>
        /// Clear all action logs (for reset)
        /// </summary>
        public void ClearActionLogs()
        {
            _actionLogs.Clear();
        }
    }

    /// <summary>
    /// Represents a single action logged during configuration execution
    /// </summary>
    public class ConfigurationActionLog
    {
        public int Cycle { get; set; }
        public string ActionType { get; set; } = null!;  // "UserInput", "Procedure", "Constraint", "Restriction", "Inference"
        public string Source { get; set; } = null!;      // Name of procedure/constraint or "User"
        public string CharacteristicName { get; set; } = null!;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string Description { get; set; } = null!;
    }
}
