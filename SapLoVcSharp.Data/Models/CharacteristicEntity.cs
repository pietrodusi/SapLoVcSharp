namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// A characteristic (variable) that can have values.
    /// Name is user-defined unique identifier within a class (e.g., "COLOR", "MODEL")
    /// </summary>
    public class CharacteristicEntity
    {
        // Composite key: ClassName + Name
        public string ClassName { get; set; } = null!;       // FK to Class
        public string Name { get; set; } = null!;            // User-defined (e.g., "COLOR")
        public string Description { get; set; } = string.Empty; // Display text (e.g., "Bike Color")
        public string LongDescription { get; set; } = string.Empty; // Extended description

        public string DataType { get; set; } = "CHAR";       // CHAR, NUM, DATE
        public int Length { get; set; } = 30;
        public int DecimalPlaces { get; set; } = 0;

        // Value assignment behavior
        public bool IsMultiValue { get; set; } = false;
        public bool IsRestrictable { get; set; } = false;
        public bool IsRequired { get; set; } = false;
        public bool IsDisplayOnly { get; set; } = false;
        public bool IsHidden { get; set; } = false;

        // Display order within the class (lower values appear first)
        public int SortOrder { get; set; } = 0;

        // Parent class
        public ClassEntity? Class { get; set; }

        // Allowed values
        public List<CharacteristicValueEntity> Values { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}