namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// A class in the classification system.
    /// Name is user-defined unique identifier (e.g., "BIKE_CLASS")
    /// </summary>
    public class ClassEntity
    {
        public string Name { get; set; } = null!;            // User-defined PK (e.g., "BIKE_CLASS")
        public string Description { get; set; } = string.Empty; // Display text (e.g., "Bicycle Configuration")
        public string ClassType { get; set; } = "300";       // SAP class type (e.g., "300")
        public string Status { get; set; } = "Active";

        // Characteristics in this class
        public List<CharacteristicEntity> Characteristics { get; set; } = new();

        // Material assignments
        public List<MaterialClassAssignmentEntity> MaterialAssignments { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}