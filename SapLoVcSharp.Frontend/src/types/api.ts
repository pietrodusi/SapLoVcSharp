// ============================================================================
// Material Types
// ============================================================================

export interface MaterialDto {
  materialNumber: string;
  description: string;
  status: string;
  classes: string[];
}

export interface MaterialDetailDto {
  materialNumber: string;
  description: string;
  status: string;
  hasConfigurationProfile: boolean;
  classes?: string[];
  characteristics: CharacteristicDto[];
}

export interface CreateMaterialRequest {
  materialNumber: string;
  description?: string;
  status?: string;
}

export interface UpdateMaterialRequest {
  description?: string;
  status?: string;
}

export interface AssignClassRequest {
  className: string;
}

// ============================================================================
// Class Types
// ============================================================================

export interface ClassDto {
  name: string;
  classType: string;
  description: string;
  status: string;
  characteristicCount: number;
}

export interface ClassDetailDto {
  name: string;
  classType: string;
  description: string;
  status: string;
  characteristics: CharacteristicDto[];
}

export interface CreateClassRequest {
  name: string;
  classType: string;
  description?: string;
  status?: string;
}

export interface UpdateClassRequest {
  description?: string;
  status?: string;
  classType?: string;
}

// ============================================================================
// Characteristic Types
// ============================================================================

export interface CharacteristicDto {
  name: string;
  className: string;
  description: string;
  longDescription: string;
  dataType: string;
  length: number;
  isRequired: boolean;
  isRestrictable: boolean;
  isHidden: boolean;
  sortOrder: number;
  allowedValues: CharacteristicValueDto[];
}

export interface CharacteristicValueDto {
  value: string;
  description: string;
  longDescription: string;
  isDefault: boolean;
}

export interface CreateCharacteristicRequest {
  name: string;
  description?: string;
  longDescription?: string;
  dataType?: string;
  length?: number;
  isRequired: boolean;
  isRestrictable: boolean;
  isHidden?: boolean;
  sortOrder?: number;
  allowedValues?: CharacteristicValueDto[];
}

export interface UpdateCharacteristicRequest {
  description?: string;
  longDescription?: string;
  dataType?: string;
  length?: number;
  isRequired?: boolean;
  isRestrictable?: boolean;
  isHidden?: boolean;
  sortOrder?: number;
  allowedValues?: CharacteristicValueDto[];
}

export interface ReorderCharacteristicsRequest {
  order: CharacteristicOrderItem[];
}

export interface CharacteristicOrderItem {
  name: string;
  sortOrder: number;
}

// ============================================================================
// Configuration Types
// ============================================================================

export interface ConfigurationResultDto {
  materialNumber: string;
  success: boolean;
  isComplete: boolean;
  isStable: boolean;
  cyclesExecuted: number;
  durationMs: number;
  finalValues: Record<string, string | null>;
  /** Allowed values per characteristic after applying restrictions */
  allowedValues: Record<string, string[]>;
  errors: (string | null)[];
  executionLog: ExecutionLogEntry[];
  /** Summary of each dependency execution with tables used */
  dependencyExecutions: DependencyExecutionSummary[];
}

export interface ExecutionLogEntry {
  cycle: number;
  actionType: string;  // "UserInput", "Procedure", "Constraint", "Inference"
  source: string;      // Name of procedure/constraint or "User"
  characteristicName: string;
  oldValue: string | null;
  newValue: string | null;
  description: string;
}

export interface DependencyExecutionSummary {
  dependencyName: string;
  dependencyType: string;
  success: boolean;
  isConsistent: boolean;
  tablesUsed: string[];
}

// ============================================================================
// Configuration Profile Types
// ============================================================================

export interface ConfigurationProfileDto {
  id: string;
  materialNumber: string;
  name: string;
  description: string;
  procedures?: ProcedureDto[];
}

export interface ProcedureDto {
  id: string;
  name: string;
  sourceCode: string;
  status: string;
  description?: string;
}

export interface CreateProcedureRequest {
  name: string;
  sourceCode: string;
  description?: string;
  status?: string;
}

export interface CreateConfigurationProfileRequest {
  materialNumber: string;
  name: string;
  description?: string;
}

// ============================================================================
// Constraint Net Types
// ============================================================================

export interface ConstraintNetDto {
  id: string;
  name: string;
  description: string;
  dependencies: DependencyDto[];
}

export interface CreateConstraintNetRequest {
  name: string;
  description?: string;
}

// ============================================================================
// Dependency Types
// ============================================================================

export interface DependencyDto {
  id: string;
  name: string;
  type: 'Constraint' | 'Procedure' | 'SelectionCondition' | 'Precondition';
  sourceCode: string;
  status: string;
}

export interface CreateDependencyRequest {
  name: string;
  type: 'Constraint' | 'Procedure' | 'SelectionCondition' | 'Precondition';
  sourceCode: string;
  description?: string;
  status?: string;
}

// ============================================================================
// Variant Table Types
// ============================================================================

export interface VariantTableDto {
  name: string;
  description: string;
  status: string;
  databaseTableName: string;
  rowCount: number;
  isTableCreated: boolean;
  columns: VariantTableColumnDto[];
}

export interface VariantTableColumnDto {
  characteristicName: string;
  sqlDataType: string;
  isKeyColumn: boolean;
}

export interface CreateVariantTableRequest {
  name: string;
  description?: string;
  status?: string;
  columns: VariantTableColumnDto[];
  rows?: string[][];
}

export interface VariantTableDataDto {
  tableName: string;
  columns: string[];
  rows: string[][];
}

export interface UploadVariantTableDataRequest {
  content: string;
  separator?: string;  // Default: tab
  hasHeader: boolean;
}

// ============================================================================
// Product Matrix Types
// ============================================================================

export interface ProductMatrixListDto {
  name: string;
  description: string;
  status: string;
  materialNumber: string;
  generatedVariantTableName: string | null;
  sheetCount: number;
}

export interface ProductMatrixDto {
  name: string;
  description: string;
  status: string;
  materialNumber: string;
  generatedVariantTableName: string | null;
  sheets: ProductMatrixSheetDto[];
}

export interface ProductMatrixSheetDto {
  sheetIndex: number;
  displayName: string;
  characteristics: ProductMatrixCharacteristicDto[];
  rows: ProductMatrixRowDto[];
}

/**
 * Represents a characteristic in a Product Matrix sheet.
 * Display modes:
 * - "Standard": All characteristic values shown as separate columns. Cells toggle X (no result) or select result value.
 * - "Column": Single column with dropdown to select characteristic value.
 */
export interface ProductMatrixCharacteristicDto {
  characteristicIndex: number;
  characteristicName: string;
  className: string;
  displayMode: 'Standard' | 'Column';
  resultCharacteristicName: string | null;
  resultClassName: string | null;
  /** Allowed values for this characteristic (fetched from characteristic definition) */
  allowedValues: string[];
  /** Allowed values for the result characteristic (if any) */
  resultAllowedValues: string[] | null;
}

export interface ProductMatrixRowDto {
  rowIndex: number;
  rowLabel: string | null;
  cells: ProductMatrixCellDto[];
}

/**
 * Cell in a Product Matrix.
 * CharacteristicIndex + ValueIndex identify the column:
 * - For Standard mode: ValueIndex is the index into AllowedValues
 * - For Column mode: ValueIndex is always 0
 */
export interface ProductMatrixCellDto {
  characteristicIndex: number;
  valueIndex: number;
  value: string | null;
}

export interface CreateProductMatrixRequest {
  name: string;
  materialNumber: string;
  description?: string;
  status?: string;
}

export interface UpdateProductMatrixRequest {
  description?: string;
  status?: string;
}

export interface CreateProductMatrixSheetRequest {
  displayName: string;
}

export interface UpdateProductMatrixSheetRequest {
  displayName?: string;
  characteristics?: ProductMatrixCharacteristicDto[];
  rows?: ProductMatrixRowDto[];
}

/**
 * Request to add a characteristic to a sheet.
 */
export interface AddCharacteristicRequest {
  characteristicName: string;
  className: string;
  displayMode: 'Standard' | 'Column';
  resultCharacteristicName?: string;
  resultClassName?: string;
}

export interface GenerateVariantTableFromMatrixRequest {
  variantTableName?: string;
  description?: string;
}
