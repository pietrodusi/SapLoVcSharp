namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// Links dependencies (procedures/preconditions) to characteristic values.
    /// </summary>
    public class ValueDependencyEntity
    {
        // Composite FK to CharacteristicValue
        public string ClassName { get; set; } = null!;
        public string CharacteristicName { get; set; } = null!;
        public string CharacteristicValue { get; set; } = null!;

        public CharacteristicValueEntity? CharacteristicValueEntity { get; set; }

        public string DependencyId { get; set; } = null!;
        public DependencyEntity? Dependency { get; set; }

        public string DependencyType { get; set; } = null!;  // "Procedure", "Precondition"
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}