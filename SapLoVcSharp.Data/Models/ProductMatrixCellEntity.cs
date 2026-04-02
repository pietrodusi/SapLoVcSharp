namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// Represents a cell in a Product Matrix data row.
    ///
    /// Cell values depend on the characteristic's display mode:
    /// - Standard mode (no result): "X" for selected values, empty otherwise
    /// - Standard mode (with result): The selected result characteristic value
    /// - Column mode: The selected characteristic value
    ///
    /// ValueIndex identifies which value column within a characteristic (for Standard mode).
    /// For Column mode, ValueIndex is always 0.
    /// </summary>
    public class ProductMatrixCellEntity
    {
        // Composite Key: MatrixName + SheetIndex + RowIndex + CharacteristicIndex + ValueIndex
        public string MatrixName { get; set; } = null!;
        public int SheetIndex { get; set; }
        public int RowIndex { get; set; }
        public int CharacteristicIndex { get; set; }
        public int ValueIndex { get; set; }

        // Cell value:
        // - "X" for Standard mode without result
        // - Result characteristic value for Standard mode with result
        // - Characteristic value for Column mode
        public string? Value { get; set; }

        // Navigation properties
        public ProductMatrixRowEntity? Row { get; set; }
    }
}
