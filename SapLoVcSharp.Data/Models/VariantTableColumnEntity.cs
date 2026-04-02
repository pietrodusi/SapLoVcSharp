namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// Defines a column in a variant table.
    /// Each column corresponds to a characteristic.
    /// </summary>
    public class VariantTableColumnEntity
    {
        // Composite key: TableName + ColumnIndex
        public string TableName { get; set; } = null!;
        public int ColumnIndex { get; set; }                 // 0-based position

        public string CharacteristicName { get; set; } = null!;  // Reference to characteristic
        public string? ClassName { get; set; }               // Optional - if characteristic is in a class
        public bool IsKeyColumn { get; set; } = false;       // Key columns are used for lookups (inference mode)

        /// <summary>
        /// SQL data type for this column (VARCHAR, INTEGER, REAL, etc.).
        /// Defaults to VARCHAR(255) if not specified.
        /// </summary>
        public string SqlDataType { get; set; } = "VARCHAR(255)";

        // Parent table
        public VariantTableEntity? Table { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}