namespace SapLoVcSharp.Api.Models;

// ============================================================================
// Configuration Execution Models
// ============================================================================

public class ConfigurationResultDto
{
    public string MaterialNumber { get; set; } = null!;
    public bool Success { get; set; }
    public bool IsComplete { get; set; }
    public bool IsStable { get; set; }
    public int CyclesExecuted { get; set; }
    public long DurationMs { get; set; }
    public Dictionary<string, string?> FinalValues { get; set; } = new();
    /// <summary>
    /// Allowed values per characteristic after applying restrictions.
    /// Maps characteristic name to list of allowed values.
    /// </summary>
    public Dictionary<string, List<string>> AllowedValues { get; set; } = new();
    public List<string?> Errors { get; set; } = new();
    public List<ExecutionLogEntry> ExecutionLog { get; set; } = new();
    /// <summary>
    /// Summary of each dependency execution with tables used
    /// </summary>
    public List<DependencyExecutionSummary> DependencyExecutions { get; set; } = new();
}

public class ExecutionLogEntry
{
    public int Cycle { get; set; }
    public string ActionType { get; set; } = null!;  // "UserInput", "Procedure", "Constraint", "Inference"
    public string Source { get; set; } = null!;      // Name of procedure/constraint or "User"
    public string CharacteristicName { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Description { get; set; } = null!;
}

public class DependencyExecutionSummary
{
    public string DependencyName { get; set; } = null!;
    public string DependencyType { get; set; } = null!;
    public bool Success { get; set; }
    public bool IsConsistent { get; set; }
    public List<string> TablesUsed { get; set; } = new();
}

// ============================================================================
// Material Models
// ============================================================================

public class MaterialDto
{
    public string MaterialNumber { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<string> Classes { get; set; } = new();
}

public class MaterialDetailDto
{
    public string MaterialNumber { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool HasConfigurationProfile { get; set; }
    public List<string> Classes { get; set; } = new();
    public List<CharacteristicDto> Characteristics { get; set; } = new();
}

// ============================================================================
// Characteristic Models
// ============================================================================

public class CharacteristicDto
{
    public string Name { get; set; } = null!;
    public string ClassName { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int Length { get; set; }
    public bool IsRequired { get; set; }
    public bool IsRestrictable { get; set; }
    public bool IsHidden { get; set; }
    public int SortOrder { get; set; }
    public List<CharacteristicValueDto> AllowedValues { get; set; } = new();
}

public class CharacteristicValueDto
{
    public string Value { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

// ============================================================================
// Variant Table Models
// ============================================================================

public class VariantTableDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string DatabaseTableName { get; set; } = null!;
    public int RowCount { get; set; }
    public bool IsTableCreated { get; set; }
    public List<VariantTableColumnDto> Columns { get; set; } = new();
}

public class VariantTableColumnDto
{
    public string CharacteristicName { get; set; } = null!;
    public string SqlDataType { get; set; } = "VARCHAR(255)";
    public bool IsKeyColumn { get; set; }
}

public class CreateVariantTableRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Status { get; set; }
    public List<VariantTableColumnDto> Columns { get; set; } = new();
    public List<string[]>? Rows { get; set; }
}

public class VariantTableDataDto
{
    public string TableName { get; set; } = null!;
    public List<string> Columns { get; set; } = new();
    public List<string[]> Rows { get; set; } = new();
}

public class UploadVariantTableDataRequest
{
    public string Content { get; set; } = null!;
    public string? Separator { get; set; }  // Default: tab
    public bool HasHeader { get; set; } = false;
}

// ============================================================================
// Material Request Models
// ============================================================================

public class CreateMaterialRequest
{
    public string MaterialNumber { get; set; } = null!;
    public string? Description { get; set; }
    public string? Status { get; set; }
}

public class UpdateMaterialRequest
{
    public string? Description { get; set; }
    public string? Status { get; set; }
}

public class AssignClassRequest
{
    public string ClassName { get; set; } = null!;
}

// ============================================================================
// Class Models
// ============================================================================

public class ClassDto
{
    public string Name { get; set; } = null!;
    public string ClassType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int CharacteristicCount { get; set; }
}

public class ClassDetailDto
{
    public string Name { get; set; } = null!;
    public string ClassType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<CharacteristicDto> Characteristics { get; set; } = new();
}

public class CreateClassRequest
{
    public string Name { get; set; } = null!;
    public string ClassType { get; set; } = null!;
    public string? Description { get; set; }
    public string? Status { get; set; }
}

public class UpdateClassRequest
{
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? ClassType { get; set; }
}

// ============================================================================
// Characteristic Request Models
// ============================================================================

public class CreateCharacteristicRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? LongDescription { get; set; }
    public string? DataType { get; set; }
    public int Length { get; set; }
    public bool IsRequired { get; set; }
    public bool IsRestrictable { get; set; }
    public bool IsHidden { get; set; }
    public int SortOrder { get; set; }
    public List<CharacteristicValueDto>? AllowedValues { get; set; }
}

public class UpdateCharacteristicRequest
{
    public string? Description { get; set; }
    public string? LongDescription { get; set; }
    public string? DataType { get; set; }
    public int? Length { get; set; }
    public bool? IsRequired { get; set; }
    public bool? IsRestrictable { get; set; }
    public bool? IsHidden { get; set; }
    public int? SortOrder { get; set; }
    public List<CharacteristicValueDto>? AllowedValues { get; set; }
}

public class ReorderCharacteristicsRequest
{
    public List<CharacteristicOrderItem> Order { get; set; } = new();
}

public class CharacteristicOrderItem
{
    public string Name { get; set; } = null!;
    public int SortOrder { get; set; }
}

// ============================================================================
// Configuration Profile Models
// ============================================================================

public class ConfigurationProfileDto
{
    public string Id { get; set; } = null!;
    public string MaterialNumber { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public List<ProcedureDto> Procedures { get; set; } = new();
}

public class CreateConfigurationProfileRequest
{
    public string MaterialNumber { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

// ============================================================================
// Procedure Models
// ============================================================================

public class ProcedureDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string SourceCode { get; set; } = null!;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateProcedureRequest
{
    public string Name { get; set; } = null!;
    public string SourceCode { get; set; } = null!;
    public string? Description { get; set; }
    public string? Status { get; set; }
}

// ============================================================================
// Constraint Net Models
// ============================================================================

public class ConstraintNetDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public List<DependencyDto> Dependencies { get; set; } = new();
}

public class CreateConstraintNetRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

// ============================================================================
// Dependency Models
// ============================================================================

public class DependencyDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string SourceCode { get; set; } = null!;
    public string Status { get; set; } = string.Empty;
}

public class CreateDependencyRequest
{
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string SourceCode { get; set; } = null!;
    public string? Description { get; set; }
    public string? Status { get; set; }
}

// ============================================================================
// Product Matrix Models
// ============================================================================

public class ProductMatrixListDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string MaterialNumber { get; set; } = null!;
    public string? GeneratedVariantTableName { get; set; }
    public int SheetCount { get; set; }
}

public class ProductMatrixDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string MaterialNumber { get; set; } = null!;
    public string? GeneratedVariantTableName { get; set; }
    public List<ProductMatrixSheetDto> Sheets { get; set; } = new();
}

public class ProductMatrixSheetDto
{
    public int SheetIndex { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public List<ProductMatrixCharacteristicDto> Characteristics { get; set; } = new();
    public List<ProductMatrixRowDto> Rows { get; set; } = new();
}

/// <summary>
/// Represents a characteristic in a Product Matrix sheet.
/// Display modes:
/// - "Standard": All characteristic values shown as separate columns. Cells toggle X (no result) or select result value.
/// - "Column": Single column with dropdown to select characteristic value.
/// </summary>
public class ProductMatrixCharacteristicDto
{
    public int CharacteristicIndex { get; set; }
    public string CharacteristicName { get; set; } = null!;
    public string ClassName { get; set; } = null!;
    public string DisplayMode { get; set; } = "Standard";  // "Standard" or "Column"
    public string? ResultCharacteristicName { get; set; }
    public string? ResultClassName { get; set; }
    /// <summary>Allowed values for this characteristic (fetched from characteristic definition)</summary>
    public List<string> AllowedValues { get; set; } = new();
    /// <summary>Allowed values for the result characteristic (if any)</summary>
    public List<string>? ResultAllowedValues { get; set; }
}

public class ProductMatrixRowDto
{
    public int RowIndex { get; set; }
    public string? RowLabel { get; set; }
    public List<ProductMatrixCellDto> Cells { get; set; } = new();
}

/// <summary>
/// Cell in a Product Matrix.
/// CharacteristicIndex + ValueIndex identify the column:
/// - For Standard mode: ValueIndex is the index into AllowedValues
/// - For Column mode: ValueIndex is always 0
/// </summary>
public class ProductMatrixCellDto
{
    public int CharacteristicIndex { get; set; }
    public int ValueIndex { get; set; }
    public string? Value { get; set; }
}

public class CreateProductMatrixRequest
{
    public string Name { get; set; } = null!;
    public string MaterialNumber { get; set; } = null!;
    public string? Description { get; set; }
    public string? Status { get; set; }
}

public class UpdateProductMatrixRequest
{
    public string? Description { get; set; }
    public string? Status { get; set; }
}

public class CreateProductMatrixSheetRequest
{
    public string DisplayName { get; set; } = string.Empty;
}

public class UpdateProductMatrixSheetRequest
{
    public string? DisplayName { get; set; }
    public List<ProductMatrixCharacteristicDto>? Characteristics { get; set; }
    public List<ProductMatrixRowDto>? Rows { get; set; }
}

/// <summary>
/// Request to add a characteristic to a sheet.
/// </summary>
public class AddCharacteristicRequest
{
    public string CharacteristicName { get; set; } = null!;
    public string ClassName { get; set; } = null!;
    public string DisplayMode { get; set; } = "Standard";  // "Standard" or "Column"
    public string? ResultCharacteristicName { get; set; }
    public string? ResultClassName { get; set; }
}

public class GenerateVariantTableFromMatrixRequest
{
    public string? VariantTableName { get; set; }
    public string? Description { get; set; }
}
