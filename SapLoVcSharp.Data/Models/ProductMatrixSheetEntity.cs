namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// Represents a sheet within a Product Matrix. Each sheet can generate a different variant table.
    /// </summary>
    public class ProductMatrixSheetEntity
    {
        // Composite Key: MatrixName + SheetIndex
        public string MatrixName { get; set; } = null!;
        public int SheetIndex { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        // Navigation properties
        public ProductMatrixEntity? Matrix { get; set; }
        public List<ProductMatrixCharacteristicEntity> Characteristics { get; set; } = new();
        public List<ProductMatrixRowEntity> Rows { get; set; } = new();
    }
}
