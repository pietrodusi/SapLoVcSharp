namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// Represents a data row in a Product Matrix sheet.
    /// Rows contain cells for each column. The row label is optional.
    /// </summary>
    public class ProductMatrixRowEntity
    {
        // Composite Key: MatrixName + SheetIndex + RowIndex
        public string MatrixName { get; set; } = null!;
        public int SheetIndex { get; set; }
        public int RowIndex { get; set; }

        // Optional row label (displayed in first column)
        public string? RowLabel { get; set; }

        // Navigation properties
        public ProductMatrixSheetEntity? Sheet { get; set; }
        public List<ProductMatrixCellEntity> Cells { get; set; } = new();
    }
}
