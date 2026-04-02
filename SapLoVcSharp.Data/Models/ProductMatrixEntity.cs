namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// Represents a Product Matrix that provides a spreadsheet-like interface for defining
    /// characteristic value combinations that auto-generate Variant Tables.
    /// </summary>
    public class ProductMatrixEntity
    {
        public string Name { get; set; } = null!;  // Primary Key (uppercase, max 100)
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";  // Active, Locked, Released

        // Material relationship
        public string MaterialNumber { get; set; } = null!;
        public MaterialEntity? Material { get; set; }

        // Generated variant table (nullable - set after generation)
        public string? GeneratedVariantTableName { get; set; }
        public VariantTableEntity? GeneratedVariantTable { get; set; }

        // Sheets (1:N)
        public List<ProductMatrixSheetEntity> Sheets { get; set; } = new();

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
