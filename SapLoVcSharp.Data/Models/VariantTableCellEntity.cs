namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// A single cell value in a variant table.
    /// </summary>
    public class VariantTableCellEntity
    {
        // Composite key: TableName + RowIndex + ColumnIndex
        public string TableName { get; set; } = null!;
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }

        public string Value { get; set; } = string.Empty;    // The actual cell value

        // Parent row
        public VariantTableRowEntity? Row { get; set; }
    }
}