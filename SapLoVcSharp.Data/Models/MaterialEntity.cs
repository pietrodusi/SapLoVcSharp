namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// Represents a configurable material/product.
    /// </summary>
    public class MaterialEntity
    {
        public string MaterialNumber { get; set; } = null!;  // Primary Key
        public string Description { get; set; } = null!;
        public string MaterialType { get; set; } = "KMAT";   // KMAT = Configurable Material
        public string BaseUnit { get; set; } = "EA";         // Each, KG, etc.
        public string Status { get; set; } = "Active";

        // Configuration Profile
        public string? ConfigurationProfileId { get; set; }
        public ConfigurationProfileEntity? ConfigurationProfile { get; set; }

        // Classification (Many-to-Many)
        public List<MaterialClassAssignmentEntity> ClassAssignments { get; set; } = new();

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}