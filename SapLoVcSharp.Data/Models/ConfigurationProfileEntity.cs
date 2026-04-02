namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// Configuration profile that links materials to their dependencies.
    /// </summary>
    public class ConfigurationProfileEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = null!;

        // Associated material
        public string MaterialNumber { get; set; } = null!;
        public MaterialEntity? Material { get; set; }

        // Dependency Nets (Constraint containers)
        public List<DependencyNetEntity> DependencyNets { get; set; } = new();

        // Direct Procedures (not in a net)
        public List<ConfigurationProfileProcedureEntity> Procedures { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}