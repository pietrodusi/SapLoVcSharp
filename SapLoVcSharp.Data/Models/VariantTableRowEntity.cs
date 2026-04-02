namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// A row in a variant table containing cell values.
    /// </summary>
    public class VariantTableRowEntity
    {
        // Composite key: TableName + RowIndex
        public string TableName { get; set; } = null!;
        public int RowIndex { get; set; }                    // 0-based position

        // Cells in this row
        public List<VariantTableCellEntity> Cells { get; set; } = new();

        // Parent table
        public VariantTableEntity? Table { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}