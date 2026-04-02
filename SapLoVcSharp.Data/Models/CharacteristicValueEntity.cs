namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// An allowed value for a characteristic.
    /// Value can have a description (e.g., Value="RED", Description="Red Color")
    /// </summary>
    public class CharacteristicValueEntity
    {
        // Composite key: ClassName + CharacteristicName + Value
        public string ClassName { get; set; } = null!;
        public string CharacteristicName { get; set; } = null!;
        public string Value { get; set; } = null!;           // The value (e.g., "RED", "BLUE")

        public string Description { get; set; } = string.Empty; // Display text (e.g., "Red Color")
        public string LongDescription { get; set; } = string.Empty; // Extended description
        public bool IsDefault { get; set; } = false;

        // Parent characteristic
        public CharacteristicEntity? Characteristic { get; set; }

        // Dependencies assigned to this value
        public List<ValueDependencyEntity> Dependencies { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}