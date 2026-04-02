import axios from 'axios';
import type {
  MaterialDto,
  MaterialDetailDto,
  CreateMaterialRequest,
  UpdateMaterialRequest,
  AssignClassRequest,
  ClassDto,
  ClassDetailDto,
  CreateClassRequest,
  UpdateClassRequest,
  CreateCharacteristicRequest,
  UpdateCharacteristicRequest,
  ReorderCharacteristicsRequest,
  ConfigurationResultDto,
  VariantTableDto,
  CreateVariantTableRequest,
  VariantTableDataDto,
  UploadVariantTableDataRequest,
  ConfigurationProfileDto,
  CreateConfigurationProfileRequest,
  ConstraintNetDto,
  CreateConstraintNetRequest,
  DependencyDto,
  CreateDependencyRequest,
  ProcedureDto,
  CreateProcedureRequest,
  ProductMatrixListDto,
  ProductMatrixDto,
  CreateProductMatrixRequest,
  UpdateProductMatrixRequest,
  CreateProductMatrixSheetRequest,
  UpdateProductMatrixSheetRequest,
  GenerateVariantTableFromMatrixRequest,
} from '../types/api';

// Use Vite proxy in development, or full URL in production
const API_BASE_URL = import.meta.env.PROD ? 'https://localhost:5001/api' : '/api';

// Create axios instance with default config
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// ============================================================================
// Material API
// ============================================================================

export const materialApi = {
  getAll: async (): Promise<MaterialDto[]> => {
    const response = await apiClient.get<MaterialDto[]>('/materials');
    return response.data;
  },

  getById: async (materialNumber: string): Promise<MaterialDetailDto> => {
    const response = await apiClient.get<MaterialDetailDto>(`/materials/${materialNumber}`);
    return response.data;
  },

  create: async (request: CreateMaterialRequest): Promise<void> => {
    await apiClient.post('/materials', request);
  },

  update: async (materialNumber: string, request: UpdateMaterialRequest): Promise<void> => {
    await apiClient.put(`/materials/${materialNumber}`, request);
  },

  delete: async (materialNumber: string): Promise<void> => {
    await apiClient.delete(`/materials/${materialNumber}`);
  },

  assignClass: async (materialNumber: string, request: AssignClassRequest): Promise<void> => {
    await apiClient.post(`/materials/${materialNumber}/classes`, request);
  },

  removeClass: async (materialNumber: string, className: string): Promise<void> => {
    await apiClient.delete(`/materials/${materialNumber}/classes/${className}`);
  },
};

// ============================================================================
// Class API
// ============================================================================

export const classApi = {
  getAll: async (): Promise<ClassDto[]> => {
    const response = await apiClient.get<ClassDto[]>('/classes');
    return response.data;
  },

  getById: async (className: string): Promise<ClassDetailDto> => {
    const response = await apiClient.get<ClassDetailDto>(`/classes/${className}`);
    return response.data;
  },

  create: async (request: CreateClassRequest): Promise<void> => {
    await apiClient.post('/classes', request);
  },

  update: async (className: string, request: UpdateClassRequest): Promise<void> => {
    await apiClient.put(`/classes/${className}`, request);
  },

  delete: async (className: string): Promise<void> => {
    await apiClient.delete(`/classes/${className}`);
  },
};

// ============================================================================
// Characteristic API
// ============================================================================

export const characteristicApi = {
  create: async (className: string, request: CreateCharacteristicRequest): Promise<void> => {
    await apiClient.post(`/classes/${className}/characteristics`, request);
  },

  update: async (
    className: string,
    characteristicName: string,
    request: UpdateCharacteristicRequest
  ): Promise<void> => {
    await apiClient.put(`/classes/${className}/characteristics/${characteristicName}`, request);
  },

  delete: async (className: string, characteristicName: string): Promise<void> => {
    await apiClient.delete(`/classes/${className}/characteristics/${characteristicName}`);
  },

  reorder: async (className: string, request: ReorderCharacteristicsRequest): Promise<void> => {
    await apiClient.put(`/classes/${className}/characteristics/reorder`, request);
  },
};

// ============================================================================
// Configuration API
// ============================================================================

export const configurationApi = {
  execute: async (
    materialNumber: string,
    initialValues?: Record<string, any>
  ): Promise<ConfigurationResultDto> => {
    const response = await apiClient.post<ConfigurationResultDto>(
      `/configurations/${materialNumber}/execute`,
      initialValues || {}
    );
    return response.data;
  },
};

// ============================================================================
// Configuration Profile API
// ============================================================================

export const configurationProfileApi = {
  create: async (request: CreateConfigurationProfileRequest): Promise<ConfigurationProfileDto> => {
    const response = await apiClient.post<ConfigurationProfileDto>('/configuration-profiles', request);
    return response.data;
  },

  getByMaterial: async (materialNumber: string): Promise<ConfigurationProfileDto | null> => {
    try {
      const response = await apiClient.get<ConfigurationProfileDto>(`/configuration-profiles/material/${materialNumber}`);
      return response.data;
    } catch {
      return null;
    }
  },
};

// ============================================================================
// Procedure API (assigned to Configuration Profile)
// ============================================================================

export const procedureApi = {
  create: async (profileId: string, request: CreateProcedureRequest): Promise<ProcedureDto> => {
    const response = await apiClient.post<ProcedureDto>(`/configuration-profiles/${profileId}/procedures`, request);
    return response.data;
  },

  update: async (procedureId: string, request: Partial<CreateProcedureRequest>): Promise<void> => {
    await apiClient.put(`/procedures/${procedureId}`, request);
  },

  delete: async (procedureId: string): Promise<void> => {
    await apiClient.delete(`/procedures/${procedureId}`);
  },
};

// ============================================================================
// Constraint Net API
// ============================================================================

export const constraintNetApi = {
  getByMaterial: async (materialNumber: string): Promise<ConstraintNetDto[]> => {
    const response = await apiClient.get<ConstraintNetDto[]>(`/constraint-nets/material/${materialNumber}`);
    return response.data;
  },

  create: async (materialNumber: string, request: CreateConstraintNetRequest): Promise<ConstraintNetDto> => {
    const response = await apiClient.post<ConstraintNetDto>(`/constraint-nets/material/${materialNumber}`, request);
    return response.data;
  },

  delete: async (netId: string): Promise<void> => {
    await apiClient.delete(`/constraint-nets/${netId}`);
  },
};

// ============================================================================
// Dependency API
// ============================================================================

export const dependencyApi = {
  create: async (netId: string, request: CreateDependencyRequest): Promise<DependencyDto> => {
    const response = await apiClient.post<DependencyDto>(`/constraint-nets/${netId}/dependencies`, request);
    return response.data;
  },

  update: async (dependencyId: string, request: Partial<CreateDependencyRequest>): Promise<void> => {
    await apiClient.put(`/dependencies/${dependencyId}`, request);
  },

  delete: async (dependencyId: string): Promise<void> => {
    await apiClient.delete(`/dependencies/${dependencyId}`);
  },
};

// ============================================================================
// Variant Table API
// ============================================================================

export const variantTableApi = {
  getAll: async (): Promise<VariantTableDto[]> => {
    const response = await apiClient.get<VariantTableDto[]>('/variant-tables');
    return response.data;
  },

  create: async (request: CreateVariantTableRequest): Promise<void> => {
    await apiClient.post('/variant-tables', request);
  },

  update: async (tableName: string, request: CreateVariantTableRequest): Promise<void> => {
    await apiClient.put(`/variant-tables/${tableName}`, request);
  },

  delete: async (tableName: string): Promise<void> => {
    await apiClient.delete(`/variant-tables/${tableName}`);
  },

  getData: async (tableName: string): Promise<VariantTableDataDto> => {
    const response = await apiClient.get<VariantTableDataDto>(`/variant-tables/${tableName}/data`);
    return response.data;
  },

  uploadData: async (tableName: string, request: UploadVariantTableDataRequest): Promise<void> => {
    await apiClient.post(`/variant-tables/${tableName}/upload`, request);
  },
};

// ============================================================================
// Product Matrix API
// ============================================================================

export const productMatrixApi = {
  getByMaterial: async (materialNumber: string): Promise<ProductMatrixListDto[]> => {
    const response = await apiClient.get<ProductMatrixListDto[]>(`/materials/${materialNumber}/product-matrices`);
    return response.data;
  },

  getById: async (matrixName: string): Promise<ProductMatrixDto> => {
    const response = await apiClient.get<ProductMatrixDto>(`/product-matrices/${matrixName}`);
    return response.data;
  },

  create: async (request: CreateProductMatrixRequest): Promise<ProductMatrixListDto> => {
    const response = await apiClient.post<ProductMatrixListDto>('/product-matrices', request);
    return response.data;
  },

  update: async (matrixName: string, request: UpdateProductMatrixRequest): Promise<void> => {
    await apiClient.put(`/product-matrices/${matrixName}`, request);
  },

  delete: async (matrixName: string): Promise<void> => {
    await apiClient.delete(`/product-matrices/${matrixName}`);
  },

  addSheet: async (matrixName: string, request: CreateProductMatrixSheetRequest): Promise<{ sheetIndex: number }> => {
    const response = await apiClient.post<{ message: string; sheetIndex: number }>(`/product-matrices/${matrixName}/sheets`, request);
    return { sheetIndex: response.data.sheetIndex };
  },

  updateSheet: async (matrixName: string, sheetIndex: number, request: UpdateProductMatrixSheetRequest): Promise<void> => {
    await apiClient.put(`/product-matrices/${matrixName}/sheets/${sheetIndex}`, request);
  },

  deleteSheet: async (matrixName: string, sheetIndex: number): Promise<void> => {
    await apiClient.delete(`/product-matrices/${matrixName}/sheets/${sheetIndex}`);
  },

  generateVariantTable: async (
    matrixName: string,
    request?: GenerateVariantTableFromMatrixRequest
  ): Promise<{ variantTableName: string; rowCount: number; columnCount: number }> => {
    const response = await apiClient.post<{
      message: string;
      variantTableName: string;
      rowCount: number;
      columnCount: number;
    }>(`/product-matrices/${matrixName}/generate`, request || {});
    return {
      variantTableName: response.data.variantTableName,
      rowCount: response.data.rowCount,
      columnCount: response.data.columnCount,
    };
  },
};

export default apiClient;
