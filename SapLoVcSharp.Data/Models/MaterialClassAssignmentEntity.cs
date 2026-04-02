namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// Links materials to classes (many-to-many).
    /// </summary>
    public class MaterialClassAssignmentEntity
    {
        public string MaterialNumber { get; set; } = null!;
        public MaterialEntity? Material { get; set; }

        public string ClassName { get; set; } = null!;       // Changed from ClassId
        public ClassEntity? Class { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}