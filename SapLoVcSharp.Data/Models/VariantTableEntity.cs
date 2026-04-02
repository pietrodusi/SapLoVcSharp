namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// Metadata for a variant table. The actual data is stored in a dedicated database table.
    /// Each variant table gets its own SQL table (e.g., "VT_BIKE_CONFIG") for performance.
    /// </summary>
    public class VariantTableEntity
    {
        public string Name { get; set; } = null!;            // User-defined PK (e.g., "T_BIKE_CONFIG")
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";       // Active, Locked, Released

        /// <summary>
        /// The actual database table name (e.g., "VT_T_BIKE_CONFIG").
        /// Prefixed with VT_ to distinguish from metadata tables.
        /// </summary>
        public string DatabaseTableName { get; set; } = null!;

        /// <summary>
        /// Whether the physical database table has been created.
        /// </summary>
        public bool IsTableCreated { get; set; } = false;

        /// <summary>
        /// Number of rows in the table (cached for performance).
        /// </summary>
        public int RowCount { get; set; } = 0;

        // Table structure metadata (columns only, data is in dedicated table)
        public List<VariantTableColumnEntity> Columns { get; set; } = new();

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}