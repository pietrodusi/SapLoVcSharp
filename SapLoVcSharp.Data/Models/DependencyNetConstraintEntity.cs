namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// Links constraints to dependency nets (many-to-many).
    /// </summary>
    public class DependencyNetConstraintEntity
    {
        public string DependencyNetId { get; set; } = null!;
        public DependencyNetEntity? DependencyNet { get; set; }

        public string DependencyId { get; set; } = null!;
        public DependencyEntity? Dependency { get; set; }

        public int SequenceNumber { get; set; } = 0;         // Order in the net
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}