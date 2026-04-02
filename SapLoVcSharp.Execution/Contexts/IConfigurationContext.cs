using SapLoVcSharp.Core.Ast.Expressions;

namespace SapLoVcSharp.Execution.Contexts
{
    /// <summary>
    /// Configuration context for accessing and modifying characteristics.
    /// Represents the state of a configuration session.
    /// </summary>
    public interface IConfigurationContext
    {
        /// <summary>
        /// The current object ($SELF)
        /// </summary>
        IConfigurationObject Self { get; }

        /// <summary>
        /// The parent object ($PARENT), null if no parent
        /// </summary>
        IConfigurationObject? Parent { get; }

        /// <summary>
        /// The root object ($ROOT), null if no root
        /// </summary>
        IConfigurationObject? Root { get; }

        /// <summary>
        /// Resolves an object reference to the actual object
        /// </summary>
        IConfigurationObject ResolveObject(ObjectReference objRef);

        /// <summary>
        /// Gets a characteristic value by name
        /// </summary>
        Task<object?> GetValueAsync(string characteristicName);

        /// <summary>
        /// Sets a characteristic value by name
        /// </summary>
        Task SetValueAsync(string characteristicName, object? value);

        // ===== Value Restriction API =====

        /// <summary>
        /// Gets the list of available values for a characteristic.
        /// Returns null if no restrictions have been applied (all values available).
        /// </summary>
        Task<List<string>?> GetAvailableValuesAsync(string characteristicName);

        /// <summary>
        /// Restricts a characteristic to only the specified values.
        /// If the characteristic already has restrictions, this will intersect with existing restrictions.
        /// </summary>
        Task RestrictValuesAsync(string characteristicName, List<string> allowedValues);

        /// <summary>
        /// Checks if a specific value is available for a characteristic.
        /// Returns true if no restrictions exist, or if the value is in the allowed list.
        /// </summary>
        Task<bool> IsValueAvailableAsync(string characteristicName, string value);

        /// <summary>
        /// Clears all value restrictions for a characteristic (makes all values available again).
        /// </summary>
        Task ClearRestrictionsAsync(string characteristicName);

        /// <summary>
        /// Gets all characteristics that have value restrictions applied.
        /// </summary>
        Task<List<string>> GetRestrictedCharacteristicsAsync();

        // ===== Change Detection API =====

        /// <summary>
        /// Creates a snapshot of all current characteristic values.
        /// Used for change detection between cycles.
        /// </summary>
        Dictionary<string, object?> CreateSnapshot();

        /// <summary>
        /// Compares current state with a previous snapshot and returns characteristics that changed.
        /// </summary>
        HashSet<string> DetectChanges(Dictionary<string, object?> previousSnapshot);
    }

    /// <summary>
    /// Represents a configurable object (material instance).
    /// Provides access to characteristic values.
    /// </summary>
    public interface IConfigurationObject
    {
        /// <summary>
        /// Gets the value of a characteristic
        /// </summary>
        Task<object?> GetCharacteristicValueAsync(string name);

        /// <summary>
        /// Sets the value of a characteristic
        /// </summary>
        Task SetCharacteristicValueAsync(string name, object? value);

        /// <summary>
        /// Checks if a characteristic has a value (for SPECIFIED operator)
        /// </summary>
        Task<bool> IsCharacteristicSpecifiedAsync(string name);
    }
}
