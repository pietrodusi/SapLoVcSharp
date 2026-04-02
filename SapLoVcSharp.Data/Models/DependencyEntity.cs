namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// Database entity for storing dependencies.
    /// Plain POCO - no EF attributes.
    /// </summary>
    public class DependencyEntity
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string SourceCode { get; set; } = null!;
        public string AstJson { get; set; } = null!;
        public string Status { get; set; } = "Active";
        public int Version { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}