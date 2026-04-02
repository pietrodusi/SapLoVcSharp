namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// Links procedures to configuration profiles (many-to-many).
    /// </summary>
    public class ConfigurationProfileProcedureEntity
    {
        public string ConfigurationProfileId { get; set; } = null!;
        public ConfigurationProfileEntity? ConfigurationProfile { get; set; }

        public string DependencyId { get; set; } = null!;
        public DependencyEntity? Dependency { get; set; }

        public int SequenceNumber { get; set; } = 0;         // Processing order
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}