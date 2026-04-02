import { useState, useEffect } from 'react';
import {
  Plus,
  Trash2,
  Grid3X3,
  Loader2,
  Zap,
  Check,
  X,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { ScrollArea, ScrollBar } from '@/components/ui/scroll-area';
import { cn } from '@/lib/utils';
import { productMatrixApi } from '../../api/client';
import type {
  MaterialDetailDto,
  ProductMatrixListDto,
  ProductMatrixDto,
  ProductMatrixSheetDto,
  ProductMatrixCharacteristicDto,
  ProductMatrixRowDto,
  CharacteristicDto,
} from '../../types/api';

interface ProductMatrixTabProps {
  material: MaterialDetailDto;
}

export default function ProductMatrixTab({ material }: ProductMatrixTabProps) {
  const [matrices, setMatrices] = useState<ProductMatrixListDto[]>([]);
  const [selectedMatrix, setSelectedMatrix] = useState<ProductMatrixDto | null>(null);
  const [activeSheetIndex, setActiveSheetIndex] = useState<number>(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Create matrix dialog
  const [openCreateDialog, setOpenCreateDialog] = useState(false);
  const [newMatrixName, setNewMatrixName] = useState('');
  const [newMatrixDescription, setNewMatrixDescription] = useState('');

  // Add sheet dialog
  const [openAddSheetDialog, setOpenAddSheetDialog] = useState(false);
  const [newSheetName, setNewSheetName] = useState('');

  // Add characteristic dialog
  const [openAddCharDialog, setOpenAddCharDialog] = useState(false);
  const [newCharName, setNewCharName] = useState('');
  const [newCharDisplayMode, setNewCharDisplayMode] = useState<'Standard' | 'Column'>('Standard');
  const [newCharResultName, setNewCharResultName] = useState('');

  // Add row dialog
  const [openAddRowDialog, setOpenAddRowDialog] = useState(false);
  const [newRowLabel, setNewRowLabel] = useState('');

  // Generate VT dialog
  const [openGenerateDialog, setOpenGenerateDialog] = useState(false);
  const [generateVtName, setGenerateVtName] = useState('');
  const [generating, setGenerating] = useState(false);

  // All available characteristics from material
  const allCharacteristics: CharacteristicDto[] = material.characteristics || [];

  useEffect(() => {
    loadMatrices();
  }, [material.materialNumber]);

  const loadMatrices = async () => {
    try {
      setLoading(true);
      const data = await productMatrixApi.getByMaterial(material.materialNumber);
      setMatrices(data);
    } catch (err) {
      setError('Failed to load product matrices');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const loadMatrix = async (matrixName: string) => {
    try {
      setLoading(true);
      const data = await productMatrixApi.getById(matrixName);
      setSelectedMatrix(data);
      setActiveSheetIndex(data.sheets.length > 0 ? data.sheets[0].sheetIndex : 0);
    } catch (err) {
      setError('Failed to load matrix details');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectMatrix = async (matrix: ProductMatrixListDto) => {
    await loadMatrix(matrix.name);
  };

  const handleCreateMatrix = async () => {
    if (!newMatrixName) return;

    try {
      setError(null);
      await productMatrixApi.create({
        name: newMatrixName,
        materialNumber: material.materialNumber,
        description: newMatrixDescription,
      });
      setOpenCreateDialog(false);
      setNewMatrixName('');
      setNewMatrixDescription('');
      await loadMatrices();
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to create matrix: ${errorMsg}`);
    }
  };

  const handleDeleteMatrix = async (matrixName: string) => {
    if (!confirm(`Delete product matrix "${matrixName}"?`)) return;

    try {
      setError(null);
      await productMatrixApi.delete(matrixName);
      if (selectedMatrix?.name === matrixName) {
        setSelectedMatrix(null);
      }
      await loadMatrices();
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to delete matrix: ${errorMsg}`);
    }
  };

  const handleAddSheet = async () => {
    if (!selectedMatrix) return;

    try {
      setError(null);
      await productMatrixApi.addSheet(selectedMatrix.name, {
        displayName: newSheetName,
      });
      setOpenAddSheetDialog(false);
      setNewSheetName('');
      await loadMatrix(selectedMatrix.name);
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to add sheet: ${errorMsg}`);
    }
  };

  const handleDeleteSheet = async (sheetIndex: number) => {
    if (!selectedMatrix) return;
    if (!confirm('Delete this sheet?')) return;

    try {
      setError(null);
      await productMatrixApi.deleteSheet(selectedMatrix.name, sheetIndex);
      await loadMatrix(selectedMatrix.name);
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to delete sheet: ${errorMsg}`);
    }
  };

  const getCurrentSheet = (): ProductMatrixSheetDto | undefined => {
    return selectedMatrix?.sheets.find(s => s.sheetIndex === activeSheetIndex);
  };

  const handleAddCharacteristic = async () => {
    if (!selectedMatrix || !newCharName) return;

    const sheet = getCurrentSheet();
    if (!sheet) return;

    // Find the characteristic to get className
    const char = allCharacteristics.find(c => c.name === newCharName);
    if (!char) {
      setError('Characteristic not found');
      return;
    }

    // Validation for Column style - cannot have result characteristic
    if (newCharDisplayMode === 'Column' && newCharResultName) {
      setError('Column style cannot have a result characteristic');
      return;
    }

    // Find result characteristic to get its className
    const resultChar = newCharResultName
      ? allCharacteristics.find(c => c.name === newCharResultName)
      : null;

    try {
      setError(null);
      const newChar: ProductMatrixCharacteristicDto = {
        characteristicIndex: sheet.characteristics.length,
        characteristicName: newCharName,
        className: char.className,
        displayMode: newCharDisplayMode,
        resultCharacteristicName: newCharDisplayMode === 'Standard' && newCharResultName ? newCharResultName : null,
        resultClassName: newCharDisplayMode === 'Standard' && resultChar ? resultChar.className : null,
        allowedValues: char.allowedValues.map(v => v.value),
        resultAllowedValues: resultChar ? resultChar.allowedValues.map(v => v.value) : null,
      };

      await productMatrixApi.updateSheet(selectedMatrix.name, activeSheetIndex, {
        characteristics: [...sheet.characteristics, newChar],
      });

      setOpenAddCharDialog(false);
      resetAddCharForm();
      await loadMatrix(selectedMatrix.name);
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to add characteristic: ${errorMsg}`);
    }
  };

  const resetAddCharForm = () => {
    setNewCharName('');
    setNewCharDisplayMode('Standard');
    setNewCharResultName('');
  };

  const handleAddRow = async () => {
    if (!selectedMatrix) return;

    const sheet = getCurrentSheet();
    if (!sheet) return;

    try {
      setError(null);
      const newRow: ProductMatrixRowDto = {
        rowIndex: sheet.rows.length,
        rowLabel: newRowLabel || null,
        cells: [],  // Empty cells, user will populate
      };

      await productMatrixApi.updateSheet(selectedMatrix.name, activeSheetIndex, {
        rows: [...sheet.rows, newRow],
      });

      setOpenAddRowDialog(false);
      setNewRowLabel('');
      await loadMatrix(selectedMatrix.name);
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to add row: ${errorMsg}`);
    }
  };

  const handleCellClick = async (rowIndex: number, charIndex: number, valueIndex: number, char: ProductMatrixCharacteristicDto) => {
    if (!selectedMatrix) return;

    const sheet = getCurrentSheet();
    if (!sheet) return;

    const row = sheet.rows.find(r => r.rowIndex === rowIndex);
    if (!row) return;

    // Find existing cell
    const cell = row.cells.find(c => c.characteristicIndex === charIndex && c.valueIndex === valueIndex);
    let newValue: string | null;

    if (char.displayMode === 'Standard' && !char.resultCharacteristicName) {
      // Standard mode without result: toggle X
      newValue = cell?.value === 'X' ? null : 'X';
    } else {
      // Column mode or Standard with result: do nothing on click (dropdown will handle it)
      return;
    }

    // Update cell
    const updatedRows = sheet.rows.map(r => {
      if (r.rowIndex === rowIndex) {
        const updatedCells = r.cells.filter(c => !(c.characteristicIndex === charIndex && c.valueIndex === valueIndex));
        if (newValue !== null) {
          updatedCells.push({ characteristicIndex: charIndex, valueIndex, value: newValue });
        }
        return { ...r, cells: updatedCells };
      }
      return r;
    });

    try {
      await productMatrixApi.updateSheet(selectedMatrix.name, activeSheetIndex, {
        rows: updatedRows,
      });
      await loadMatrix(selectedMatrix.name);
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to update cell: ${errorMsg}`);
    }
  };

  const handleCellValueChange = async (rowIndex: number, charIndex: number, valueIndex: number, newValue: string | null) => {
    if (!selectedMatrix) return;

    const sheet = getCurrentSheet();
    if (!sheet) return;

    const updatedRows = sheet.rows.map(r => {
      if (r.rowIndex === rowIndex) {
        const updatedCells = r.cells.filter(c => !(c.characteristicIndex === charIndex && c.valueIndex === valueIndex));
        if (newValue) {
          updatedCells.push({ characteristicIndex: charIndex, valueIndex, value: newValue });
        }
        return { ...r, cells: updatedCells };
      }
      return r;
    });

    try {
      await productMatrixApi.updateSheet(selectedMatrix.name, activeSheetIndex, {
        rows: updatedRows,
      });
      await loadMatrix(selectedMatrix.name);
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to update cell: ${errorMsg}`);
    }
  };

  const handleDeleteRow = async (rowIndex: number) => {
    if (!selectedMatrix) return;

    const sheet = getCurrentSheet();
    if (!sheet) return;

    if (!confirm('Delete this row?')) return;

    try {
      setError(null);
      const updatedRows = sheet.rows
        .filter(r => r.rowIndex !== rowIndex)
        .map((r, idx) => ({ ...r, rowIndex: idx }));

      await productMatrixApi.updateSheet(selectedMatrix.name, activeSheetIndex, {
        rows: updatedRows,
      });
      await loadMatrix(selectedMatrix.name);
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to delete row: ${errorMsg}`);
    }
  };

  const handleDeleteCharacteristic = async (charIndex: number) => {
    if (!selectedMatrix) return;

    const sheet = getCurrentSheet();
    if (!sheet) return;

    if (!confirm('Delete this characteristic?')) return;

    try {
      setError(null);
      const updatedChars = sheet.characteristics
        .filter(c => c.characteristicIndex !== charIndex)
        .map((c, idx) => ({ ...c, characteristicIndex: idx }));

      // Also remove cells for this characteristic from rows
      const updatedRows = sheet.rows.map(r => ({
        ...r,
        cells: r.cells.filter(c => c.characteristicIndex !== charIndex),
      }));

      await productMatrixApi.updateSheet(selectedMatrix.name, activeSheetIndex, {
        characteristics: updatedChars,
        rows: updatedRows,
      });
      await loadMatrix(selectedMatrix.name);
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to delete characteristic: ${errorMsg}`);
    }
  };

  const handleMoveCharacteristic = async (charIndex: number, direction: 'left' | 'right') => {
    if (!selectedMatrix) return;

    const sheet = getCurrentSheet();
    if (!sheet) return;

    const newIndex = direction === 'left' ? charIndex - 1 : charIndex + 1;

    // Validate bounds
    if (newIndex < 0 || newIndex >= sheet.characteristics.length) return;

    try {
      setError(null);

      // Swap characteristics
      const updatedChars = [...sheet.characteristics];
      const temp = updatedChars[charIndex];
      updatedChars[charIndex] = updatedChars[newIndex];
      updatedChars[newIndex] = temp;

      // Reassign characteristic indices
      const reindexedChars = updatedChars.map((c, idx) => ({
        ...c,
        characteristicIndex: idx,
      }));

      // Update cell characteristic indices to match new order
      const updatedRows = sheet.rows.map(r => ({
        ...r,
        cells: r.cells.map(cell => {
          if (cell.characteristicIndex === charIndex) {
            return { ...cell, characteristicIndex: newIndex };
          } else if (cell.characteristicIndex === newIndex) {
            return { ...cell, characteristicIndex: charIndex };
          }
          return cell;
        }),
      }));

      await productMatrixApi.updateSheet(selectedMatrix.name, activeSheetIndex, {
        characteristics: reindexedChars,
        rows: updatedRows,
      });
      await loadMatrix(selectedMatrix.name);
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to move characteristic: ${errorMsg}`);
    }
  };

  const handleGenerateVariantTable = async () => {
    if (!selectedMatrix) return;

    try {
      setGenerating(true);
      setError(null);
      const result = await productMatrixApi.generateVariantTable(selectedMatrix.name, {
        variantTableName: generateVtName || undefined,
      });
      setOpenGenerateDialog(false);
      setGenerateVtName('');
      alert(`Variant table generated: ${JSON.stringify(result)}`);
      await loadMatrix(selectedMatrix.name);
      await loadMatrices();
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to generate variant table: ${errorMsg}`);
    } finally {
      setGenerating(false);
    }
  };

  // Calculate total column count for a sheet
  const getTotalColumns = (sheet: ProductMatrixSheetDto): number => {
    return sheet.characteristics.reduce((sum, char) => {
      return sum + (char.displayMode === 'Column' ? 1 : char.allowedValues.length);
    }, 0);
  };

  const sheet = getCurrentSheet();

  return (
    <div className="space-y-4">
      {error && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-4">
          <p className="text-sm text-destructive">{error}</p>
          <Button
            variant="ghost"
            size="sm"
            className="mt-2 h-auto p-0 text-destructive hover:text-destructive"
            onClick={() => setError(null)}
          >
            Dismiss
          </Button>
        </div>
      )}

      {/* Matrix Selection Bar */}
      <div className="flex items-center gap-2 flex-wrap">
        <Grid3X3 className="h-5 w-5 text-muted-foreground" />
        {loading && matrices.length === 0 ? (
          <div className="flex items-center">
            <Loader2 className="h-4 w-4 animate-spin text-muted-foreground mr-2" />
            <span className="text-sm text-muted-foreground">Loading...</span>
          </div>
        ) : (
          <>
            {matrices.map((matrix) => (
              <div key={matrix.name} className="flex items-center">
                <Button
                  variant={selectedMatrix?.name === matrix.name ? 'default' : 'outline'}
                  size="sm"
                  onClick={() => handleSelectMatrix(matrix)}
                  className="gap-2"
                >
                  {matrix.name}
                  {matrix.generatedVariantTableName && (
                    <Badge variant="secondary" className="text-[10px] px-1">VT</Badge>
                  )}
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7 ml-1 text-destructive hover:text-destructive"
                  onClick={(e) => {
                    e.stopPropagation();
                    handleDeleteMatrix(matrix.name);
                  }}
                >
                  <X className="h-3 w-3" />
                </Button>
              </div>
            ))}
          </>
        )}
        <Button
          variant="outline"
          size="sm"
          onClick={() => setOpenCreateDialog(true)}
          className="gap-1"
        >
          <Plus className="h-4 w-4" />
          New
        </Button>
      </div>

      {/* Matrix Editor - Full Width */}
      {selectedMatrix ? (
        <Card>
          <CardHeader className="pb-3">
            <div className="flex justify-between items-center">
              <div className="flex items-center gap-3">
                <CardTitle className="text-base">{selectedMatrix.name}</CardTitle>
                <Badge variant="outline" className="text-xs">
                  {selectedMatrix.status || 'Active'}
                </Badge>
                {selectedMatrix.description && (
                  <span className="text-sm text-muted-foreground">
                    {selectedMatrix.description}
                  </span>
                )}
              </div>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setOpenAddSheetDialog(true)}
                  className="gap-1"
                >
                  <Plus className="h-4 w-4" />
                  Sheet
                </Button>
                <Button
                  size="sm"
                  onClick={() => {
                    setGenerateVtName(`VT_${selectedMatrix.name}`);
                    setOpenGenerateDialog(true);
                  }}
                  className="gap-1"
                >
                  <Zap className="h-4 w-4" />
                  Generate VT
                </Button>
              </div>
            </div>
          </CardHeader>
          <CardContent className="pt-0">
            {/* Sheet Tabs */}
            {selectedMatrix.sheets.length > 0 ? (
              <Tabs
                value={String(activeSheetIndex)}
                onValueChange={(v) => setActiveSheetIndex(Number(v))}
              >
                <div className="flex items-center justify-between mb-4">
                  <TabsList>
                    {selectedMatrix.sheets.map((s) => (
                      <TabsTrigger key={s.sheetIndex} value={String(s.sheetIndex)}>
                        {s.displayName || `Sheet ${s.sheetIndex + 1}`}
                      </TabsTrigger>
                    ))}
                  </TabsList>
                  {sheet && (
                    <div className="flex gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setOpenAddCharDialog(true)}
                      >
                        <Plus className="h-4 w-4 mr-1" />
                        Characteristic
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="text-destructive"
                        onClick={() => handleDeleteSheet(activeSheetIndex)}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  )}
                    </div>

                    {selectedMatrix.sheets.map((s) => (
                      <TabsContent key={s.sheetIndex} value={String(s.sheetIndex)}>
                        {/* Matrix Grid */}
                        {s.characteristics.length === 0 ? (
                          <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-4">
                            <p className="text-sm text-blue-600">
                              No characteristics defined. Add characteristics to start building the matrix.
                            </p>
                          </div>
                        ) : (
                          <ScrollArea className="w-full whitespace-nowrap">
                            <Table className="border">
                              <TableHeader>
                                {/* Row 1: Characteristic names (with colspan) */}
                                <TableRow className="bg-primary/10 border-b-2 border-primary/30">
                                  {s.characteristics.map((char, idx) => (
                                    <TableHead
                                      key={char.characteristicIndex}
                                      colSpan={char.displayMode === 'Column' ? 1 : Math.max(char.allowedValues.length, 1)}
                                      className={cn(
                                        "text-center font-semibold py-1",
                                        idx < s.characteristics.length - 1 && "border-r-2 border-primary/30",
                                        char.displayMode === 'Column' ? "bg-blue-50" : "bg-primary/10"
                                      )}
                                    >
                                      <div className="flex flex-col items-center gap-0.5">
                                        <div className="flex items-center gap-1">
                                          <span className="font-bold text-sm">{char.characteristicName}</span>
                                          {char.displayMode === 'Column' && (
                                            <Badge variant="outline" className="text-[9px] px-1 py-0 bg-blue-100">
                                              Col
                                            </Badge>
                                          )}
                                        </div>
                                        <div className="flex items-center gap-0.5">
                                          <Button
                                            variant="ghost"
                                            size="icon"
                                            className="h-5 w-5 text-muted-foreground hover:text-primary disabled:opacity-30"
                                            onClick={() => handleMoveCharacteristic(char.characteristicIndex, 'left')}
                                            disabled={idx === 0}
                                            title="Move left"
                                          >
                                            <ChevronLeft className="h-3 w-3" />
                                          </Button>
                                          <Button
                                            variant="ghost"
                                            size="icon"
                                            className="h-5 w-5 text-muted-foreground hover:text-primary disabled:opacity-30"
                                            onClick={() => handleMoveCharacteristic(char.characteristicIndex, 'right')}
                                            disabled={idx === s.characteristics.length - 1}
                                            title="Move right"
                                          >
                                            <ChevronRight className="h-3 w-3" />
                                          </Button>
                                          <Button
                                            variant="ghost"
                                            size="icon"
                                            className="h-5 w-5 text-muted-foreground hover:text-destructive"
                                            onClick={() => handleDeleteCharacteristic(char.characteristicIndex)}
                                            title="Delete"
                                          >
                                            <X className="h-3 w-3" />
                                          </Button>
                                        </div>
                                      </div>
                                    </TableHead>
                                  ))}
                                  <TableHead className="w-10 bg-muted/50"></TableHead>
                                </TableRow>

                                {/* Row 2: Values (for Standard style) */}
                                <TableRow className="bg-muted/30">
                                  {s.characteristics.flatMap((char, charIdx) =>
                                    char.displayMode === 'Column'
                                      ? [
                                          <TableHead
                                            key={`${char.characteristicIndex}-0`}
                                            className={cn(
                                              "text-center text-[11px] text-muted-foreground py-1 bg-blue-50/50",
                                              charIdx < s.characteristics.length - 1 && "border-r-2 border-primary/30"
                                            )}
                                          >
                                          </TableHead>,
                                        ]
                                      : char.allowedValues.map((val, idx) => (
                                          <TableHead
                                            key={`${char.characteristicIndex}-${idx}`}
                                            className={cn(
                                              "text-center text-[11px] font-medium py-1 min-w-[60px] border-r",
                                              idx === char.allowedValues.length - 1 && charIdx < s.characteristics.length - 1 && "border-r-2 border-primary/30"
                                            )}
                                          >
                                            {val}
                                          </TableHead>
                                        ))
                                  )}
                                  <TableHead className="bg-muted/50"></TableHead>
                                </TableRow>

                                {/* Row 3: Result characteristics (if any) - only show if at least one char has result */}
                                {s.characteristics.some(c => c.resultCharacteristicName) && (
                                  <TableRow className="bg-amber-50/50 border-t">
                                    {s.characteristics.map((char, idx) => (
                                      <TableHead
                                        key={char.characteristicIndex}
                                        colSpan={char.displayMode === 'Column' ? 1 : Math.max(char.allowedValues.length, 1)}
                                        className={cn(
                                          "text-center text-[11px] text-amber-700 font-medium py-1",
                                          idx < s.characteristics.length - 1 && "border-r-2 border-primary/30"
                                        )}
                                      >
                                        {char.resultCharacteristicName || ''}
                                      </TableHead>
                                    ))}
                                    <TableHead className="bg-muted/30"></TableHead>
                                  </TableRow>
                                )}
                              </TableHeader>

                              <TableBody>
                                {s.rows.length === 0 ? (
                                  <TableRow>
                                    <TableCell
                                      colSpan={getTotalColumns(s) + 1}
                                      className="text-center py-8 text-muted-foreground"
                                    >
                                      No rows yet. Click the button below to add a row.
                                    </TableCell>
                                  </TableRow>
                                ) : (
                                  s.rows.map((row) => (
                                    <TableRow key={row.rowIndex} className="hover:bg-muted/30">
                                      {s.characteristics.flatMap((char, charIdx) => {
                                        const isLastChar = charIdx === s.characteristics.length - 1;

                                        if (char.displayMode === 'Column') {
                                          // Single cell with dropdown for Column style
                                          const cell = row.cells.find(
                                            (c) => c.characteristicIndex === char.characteristicIndex && c.valueIndex === 0
                                          );
                                          return [
                                            <TableCell
                                              key={`${char.characteristicIndex}-0`}
                                              className={cn(
                                                "text-center p-1 bg-blue-50/30 border-r",
                                                !isLastChar && "border-r-2 border-primary/30"
                                              )}
                                            >
                                              <Select
                                                value={cell?.value || '__clear__'}
                                                onValueChange={(v) =>
                                                  handleCellValueChange(row.rowIndex, char.characteristicIndex, 0, v === '__clear__' ? null : v)
                                                }
                                              >
                                                <SelectTrigger className="h-7 text-xs mx-auto w-full max-w-[100px]">
                                                  <SelectValue placeholder="-" />
                                                </SelectTrigger>
                                                <SelectContent>
                                                  <SelectItem value="__clear__">-</SelectItem>
                                                  {char.allowedValues.map((v) => (
                                                    <SelectItem key={v} value={v}>
                                                      {v}
                                                    </SelectItem>
                                                  ))}
                                                </SelectContent>
                                              </Select>
                                            </TableCell>,
                                          ];
                                        } else if (char.resultCharacteristicName) {
                                          // Standard with result: dropdown for result value
                                          return char.allowedValues.map((_val, idx) => {
                                            const cell = row.cells.find(
                                              (c) => c.characteristicIndex === char.characteristicIndex && c.valueIndex === idx
                                            );
                                            const isLastVal = idx === char.allowedValues.length - 1;
                                            return (
                                              <TableCell
                                                key={`${char.characteristicIndex}-${idx}`}
                                                className={cn(
                                                  "text-center p-1 border-r",
                                                  isLastVal && !isLastChar && "border-r-2 border-primary/30"
                                                )}
                                              >
                                                <Select
                                                  value={cell?.value || '__clear__'}
                                                  onValueChange={(v) =>
                                                    handleCellValueChange(row.rowIndex, char.characteristicIndex, idx, v === '__clear__' ? null : v)
                                                  }
                                                >
                                                  <SelectTrigger className="h-7 text-xs mx-auto w-full max-w-[80px]">
                                                    <SelectValue placeholder="-" />
                                                  </SelectTrigger>
                                                  <SelectContent>
                                                    <SelectItem value="__clear__">-</SelectItem>
                                                    {(char.resultAllowedValues || []).map((v) => (
                                                      <SelectItem key={v} value={v}>
                                                        {v}
                                                      </SelectItem>
                                                    ))}
                                                  </SelectContent>
                                                </Select>
                                              </TableCell>
                                            );
                                          });
                                        } else {
                                          // Standard without result: X toggle
                                          return char.allowedValues.map((_val, idx) => {
                                            const cell = row.cells.find(
                                              (c) => c.characteristicIndex === char.characteristicIndex && c.valueIndex === idx
                                            );
                                            const hasValue = cell?.value === 'X';
                                            const isLastVal = idx === char.allowedValues.length - 1;
                                            return (
                                              <TableCell
                                                key={`${char.characteristicIndex}-${idx}`}
                                                className={cn(
                                                  'text-center cursor-pointer transition-colors border-r',
                                                  hasValue && 'bg-emerald-100',
                                                  !hasValue && 'hover:bg-muted/50',
                                                  isLastVal && !isLastChar && "border-r-2 border-primary/30"
                                                )}
                                                onClick={() =>
                                                  handleCellClick(row.rowIndex, char.characteristicIndex, idx, char)
                                                }
                                              >
                                                {hasValue && <span className="text-emerald-600 font-bold">X</span>}
                                              </TableCell>
                                            );
                                          });
                                        }
                                      })}
                                      <TableCell className="text-center bg-muted/20">
                                        <Button
                                          variant="ghost"
                                          size="icon"
                                          className="h-7 w-7 text-muted-foreground hover:text-destructive"
                                          onClick={() => handleDeleteRow(row.rowIndex)}
                                        >
                                          <Trash2 className="h-3.5 w-3.5" />
                                        </Button>
                                      </TableCell>
                                    </TableRow>
                                  ))
                                )}
                                {/* Add Row button as table row */}
                                <TableRow className="hover:bg-muted/20">
                                  <TableCell
                                    colSpan={getTotalColumns(s) + 1}
                                    className="text-center py-2"
                                  >
                                    <Button
                                      variant="ghost"
                                      size="sm"
                                      className="text-muted-foreground hover:text-primary"
                                      onClick={() => setOpenAddRowDialog(true)}
                                    >
                                      <Plus className="h-4 w-4 mr-1" />
                                      Add Row
                                    </Button>
                                  </TableCell>
                                </TableRow>
                              </TableBody>
                            </Table>
                            <ScrollBar orientation="horizontal" />
                          </ScrollArea>
                        )}
                      </TabsContent>
                    ))}
                  </Tabs>
                ) : (
                  <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-4">
                    <p className="text-sm text-blue-600">
                      No sheets in this matrix. Add a sheet to start defining combinations.
                    </p>
                  </div>
                )}
              </CardContent>
            </Card>
          ) : matrices.length > 0 ? (
            <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-4">
              <p className="text-sm text-blue-600">
                Select a matrix to edit.
              </p>
            </div>
          ) : (
            <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-4">
              <p className="text-sm text-blue-600">
                No product matrices defined yet. Click "New" to create one.
              </p>
            </div>
          )}

      {/* Create Matrix Dialog */}
      <Dialog open={openCreateDialog} onOpenChange={setOpenCreateDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create Product Matrix</DialogTitle>
            <DialogDescription>
              Create a new product matrix to define characteristic combinations.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="matrixName">Matrix Name</Label>
              <Input
                id="matrixName"
                placeholder="e.g., PMX_BIKE_CONFIG"
                value={newMatrixName}
                onChange={(e) => setNewMatrixName(e.target.value.toUpperCase())}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="matrixDesc">Description</Label>
              <Input
                id="matrixDesc"
                placeholder="Enter description..."
                value={newMatrixDescription}
                onChange={(e) => setNewMatrixDescription(e.target.value)}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setOpenCreateDialog(false)}>
              Cancel
            </Button>
            <Button onClick={handleCreateMatrix} disabled={!newMatrixName}>
              Create
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Add Sheet Dialog */}
      <Dialog open={openAddSheetDialog} onOpenChange={setOpenAddSheetDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add Sheet</DialogTitle>
            <DialogDescription>
              Add a new sheet to the matrix. Characteristics can be added after the sheet is created.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="sheetName">Sheet Name</Label>
              <Input
                id="sheetName"
                placeholder="e.g., Frame Sizes"
                value={newSheetName}
                onChange={(e) => setNewSheetName(e.target.value)}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setOpenAddSheetDialog(false)}>
              Cancel
            </Button>
            <Button onClick={handleAddSheet}>Add Sheet</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Add Characteristic Dialog */}
      <Dialog open={openAddCharDialog} onOpenChange={setOpenAddCharDialog}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Add Characteristic</DialogTitle>
            <DialogDescription>
              Add a characteristic to the matrix. All material characteristics are available.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            {allCharacteristics.length === 0 ? (
              <div className="rounded-lg border border-amber-500/50 bg-amber-500/10 p-4">
                <p className="text-sm text-amber-700">
                  No characteristics available. Assign classes with characteristics to this material first.
                </p>
              </div>
            ) : (
              <>
                <div className="space-y-2">
                  <Label>Characteristic</Label>
                  <Select
                    value={newCharName}
                    onValueChange={(v) => setNewCharName(v)}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Select characteristic" />
                    </SelectTrigger>
                    <SelectContent>
                      {allCharacteristics.map((char) => (
                        <SelectItem key={char.name} value={char.name}>
                          {char.name} {char.description && `- ${char.description}`}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label>Display Mode</Label>
              <Select
                value={newCharDisplayMode}
                onValueChange={(v) => setNewCharDisplayMode(v as 'Standard' | 'Column')}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Standard">Standard (all values as columns)</SelectItem>
                  <SelectItem value="Column">Column (single dropdown)</SelectItem>
                </SelectContent>
              </Select>
              <p className="text-xs text-muted-foreground">
                {newCharDisplayMode === 'Standard'
                  ? 'Each value becomes a column. Click cells to mark with X.'
                  : 'Single column with dropdown to select value.'}
              </p>
            </div>
            {newCharDisplayMode === 'Standard' && (
              <div className="space-y-2">
                <Label>Result Characteristic (optional)</Label>
                <Select
                  value={newCharResultName || '__none__'}
                  onValueChange={(v) => setNewCharResultName(v === '__none__' ? '' : v)}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="None" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="__none__">None</SelectItem>
                    {allCharacteristics
                      .filter((c) => c.name !== newCharName)
                      .map((char) => (
                        <SelectItem key={char.name} value={char.name}>
                          {char.name}
                        </SelectItem>
                      ))}
                  </SelectContent>
                </Select>
                <p className="text-xs text-muted-foreground">
                  If set, cells will have dropdowns to select result values instead of X marks.
                </p>
              </div>
            )}
              </>
            )}
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setOpenAddCharDialog(false);
                resetAddCharForm();
              }}
            >
              Cancel
            </Button>
            <Button onClick={handleAddCharacteristic} disabled={!newCharName}>
              Add Characteristic
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Add Row Dialog */}
      <Dialog open={openAddRowDialog} onOpenChange={setOpenAddRowDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add Row</DialogTitle>
            <DialogDescription>Add a new row to the matrix. You can optionally add a label.</DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label>Row Label (optional)</Label>
              <Input
                placeholder="e.g., Row description"
                value={newRowLabel}
                onChange={(e) => setNewRowLabel(e.target.value)}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setOpenAddRowDialog(false)}>
              Cancel
            </Button>
            <Button onClick={handleAddRow}>Add Row</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Generate Variant Table Dialog */}
      <Dialog open={openGenerateDialog} onOpenChange={setOpenGenerateDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Generate Variant Table</DialogTitle>
            <DialogDescription>
              Generate a variant table from the matrix. This will create a table with all valid combinations.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="vtName">Variant Table Name</Label>
              <Input
                id="vtName"
                placeholder="e.g., VT_PMX_BIKE"
                value={generateVtName}
                onChange={(e) => setGenerateVtName(e.target.value.toUpperCase())}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setOpenGenerateDialog(false)}>
              Cancel
            </Button>
            <Button onClick={handleGenerateVariantTable} disabled={generating}>
              {generating ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                  Generating...
                </>
              ) : (
                'Generate'
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
