namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// A dependency net is a container for related constraints.
    /// </summary>
    public class DependencyNetEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";

        // Parent profile
        public string ConfigurationProfileId { get; set; } = null!;
        public ConfigurationProfileEntity? ConfigurationProfile { get; set; }

        // Constraints in this net
        public List<DependencyNetConstraintEntity> Constraints { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}