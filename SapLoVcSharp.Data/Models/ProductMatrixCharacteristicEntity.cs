namespace SapLoVcSharp.Data.Models
{
    /// <summary>
    /// Represents a characteristic in a Product Matrix sheet.
    ///
    /// Display Modes:
    /// - "Standard": All characteristic values are shown as separate columns in header row 2.
    ///   Cells either toggle X (if no result) or contain dropdown for result value (if has result).
    /// - "Column": Single column, no values in header. Cells contain dropdown to select characteristic value.
    ///
    /// Result Characteristic (only for Standard mode):
    /// When a result characteristic is specified, cells contain dropdown to select result values
    /// instead of X toggles.
    /// </summary>
    public class ProductMatrixCharacteristicEntity
    {
        // Composite Key: MatrixName + SheetIndex + CharacteristicIndex
        public string MatrixName { get; set; } = null!;
        public int SheetIndex { get; set; }
        public int CharacteristicIndex { get; set; }

        // Main characteristic for this entry
        public string CharacteristicName { get; set; } = null!;
        public string ClassName { get; set; } = null!;

        // Display mode: "Standard" (all values as columns) or "Column" (single column with dropdown)
        public string DisplayMode { get; set; } = "Standard";

        // Optional result characteristic (only for Standard mode)
        // When set, cells show dropdown to select result value instead of X toggle
        public string? ResultCharacteristicName { get; set; }
        public string? ResultClassName { get; set; }

        // Navigation properties
        public ProductMatrixSheetEntity? Sheet { get; set; }
    }
}
