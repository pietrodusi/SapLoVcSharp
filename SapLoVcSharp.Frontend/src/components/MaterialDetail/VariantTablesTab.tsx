import { useState, useEffect, useRef } from 'react';
import {
  Plus,
  Trash2,
  Pencil,
  Upload,
  Table2,
  Key,
  Loader2,
  GripVertical,
  ChevronUp,
  ChevronDown,
  AlertTriangle,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
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
import { Textarea } from '@/components/ui/textarea';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { ScrollArea } from '@/components/ui/scroll-area';
import { cn } from '@/lib/utils';
import { variantTableApi } from '../../api/client';
import type {
  MaterialDetailDto,
  VariantTableDto,
  VariantTableColumnDto,
  VariantTableDataDto,
} from '../../types/api';

interface VariantTablesTabProps {
  material: MaterialDetailDto;
}

export default function VariantTablesTab({ material }: VariantTablesTabProps) {
  const [tables, setTables] = useState<VariantTableDto[]>([]);
  const [selectedTable, setSelectedTable] = useState<VariantTableDto | null>(null);
  const [tableData, setTableData] = useState<VariantTableDataDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Create table dialog
  const [openCreateDialog, setOpenCreateDialog] = useState(false);
  const [newTableName, setNewTableName] = useState('');
  const [newTableDescription, setNewTableDescription] = useState('');
  const [newTableColumns, setNewTableColumns] = useState<VariantTableColumnDto[]>([]);
  const [newColumnName, setNewColumnName] = useState('');
  const [newColumnIsKey, setNewColumnIsKey] = useState(false);

  // Upload dialog
  const [openUploadDialog, setOpenUploadDialog] = useState(false);
  const [uploadContent, setUploadContent] = useState('');
  const [uploadSeparator, setUploadSeparator] = useState('\t');
  const [uploadHasHeader, setUploadHasHeader] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Edit data dialog
  const [editingRowIndex, setEditingRowIndex] = useState<number | null>(null);
  const [editingRowValues, setEditingRowValues] = useState<string[]>([]);
  const [openEditRowDialog, setOpenEditRowDialog] = useState(false);

  // Edit table metadata dialog
  const [openEditTableDialog, setOpenEditTableDialog] = useState(false);
  const [editTableDescription, setEditTableDescription] = useState('');
  const [editTableStatus, setEditTableStatus] = useState('Active');
  const [editTableColumns, setEditTableColumns] = useState<VariantTableColumnDto[]>([]);
  const [editNewColumnName, setEditNewColumnName] = useState('');
  const [editNewColumnIsKey, setEditNewColumnIsKey] = useState(false);
  const [columnsModified, setColumnsModified] = useState(false);

  useEffect(() => {
    loadTables();
  }, []);

  const loadTables = async () => {
    try {
      setLoading(true);
      const data = await variantTableApi.getAll();
      setTables(data);
    } catch (err) {
      setError('Failed to load variant tables');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const loadTableData = async (tableName: string) => {
    try {
      setLoading(true);
      const data = await variantTableApi.getData(tableName);
      setTableData(data);
    } catch (err) {
      setError('Failed to load table data');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectTable = async (table: VariantTableDto) => {
    setSelectedTable(table);
    await loadTableData(table.name);
  };

  const handleCreateTable = async () => {
    if (!newTableName || newTableColumns.length === 0) return;

    try {
      setError(null);
      await variantTableApi.create({
        name: newTableName,
        description: newTableDescription,
        columns: newTableColumns,
      });
      setOpenCreateDialog(false);
      resetCreateForm();
      await loadTables();
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to create table: ${errorMsg}`);
    }
  };

  const handleDeleteTable = async (tableName: string) => {
    if (!confirm(`Delete variant table "${tableName}"?`)) return;

    try {
      setError(null);
      await variantTableApi.delete(tableName);
      if (selectedTable?.name === tableName) {
        setSelectedTable(null);
        setTableData(null);
      }
      await loadTables();
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to delete table: ${errorMsg}`);
    }
  };

  const handleUpload = async () => {
    if (!selectedTable || !uploadContent) return;

    try {
      setError(null);
      await variantTableApi.uploadData(selectedTable.name, {
        content: uploadContent,
        separator: uploadSeparator,
        hasHeader: uploadHasHeader,
      });
      setOpenUploadDialog(false);
      setUploadContent('');
      await loadTables();
      await loadTableData(selectedTable.name);
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to upload data: ${errorMsg}`);
    }
  };

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (e) => {
        setUploadContent(e.target?.result as string);
      };
      reader.readAsText(file);
    }
  };

  const resetCreateForm = () => {
    setNewTableName('');
    setNewTableDescription('');
    setNewTableColumns([]);
    setNewColumnName('');
    setNewColumnIsKey(false);
  };

  const addColumn = () => {
    if (!newColumnName) return;

    const charExists = material.characteristics.some(c => c.name === newColumnName);
    if (!charExists) {
      setError(`Characteristic "${newColumnName}" not found in material`);
      return;
    }

    setNewTableColumns([
      ...newTableColumns,
      {
        characteristicName: newColumnName,
        sqlDataType: 'VARCHAR(255)',
        isKeyColumn: newColumnIsKey,
      },
    ]);
    setNewColumnName('');
    setNewColumnIsKey(false);
    setError(null);
  };

  const removeColumn = (index: number) => {
    setNewTableColumns(newTableColumns.filter((_, i) => i !== index));
  };

  const handleEditRow = (rowIndex: number) => {
    if (!tableData) return;
    setEditingRowIndex(rowIndex);
    setEditingRowValues([...tableData.rows[rowIndex]]);
    setOpenEditRowDialog(true);
  };

  const handleSaveRow = async () => {
    if (!selectedTable || !tableData || editingRowIndex === null) return;

    try {
      const newRows = [...tableData.rows];
      newRows[editingRowIndex] = editingRowValues;

      await variantTableApi.update(selectedTable.name, {
        name: selectedTable.name,
        description: selectedTable.description,
        columns: selectedTable.columns,
        rows: newRows,
      });

      setOpenEditRowDialog(false);
      setEditingRowIndex(null);
      await loadTables();
      await loadTableData(selectedTable.name);
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to save row: ${errorMsg}`);
    }
  };

  const handleAddRow = async () => {
    if (!selectedTable || !tableData) return;

    const newRow = tableData.columns.map(() => '');
    setEditingRowIndex(tableData.rows.length);
    setEditingRowValues(newRow);
    setOpenEditRowDialog(true);
  };

  const handleDeleteRow = async (rowIndex: number) => {
    if (!selectedTable || !tableData) return;
    if (!confirm('Delete this row?')) return;

    try {
      const newRows = tableData.rows.filter((_, i) => i !== rowIndex);

      await variantTableApi.update(selectedTable.name, {
        name: selectedTable.name,
        description: selectedTable.description,
        columns: selectedTable.columns,
        rows: newRows,
      });

      await loadTables();
      await loadTableData(selectedTable.name);
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to delete row: ${errorMsg}`);
    }
  };

  const handleSaveNewRow = async () => {
    if (!selectedTable || !tableData) return;

    try {
      const newRows = [...tableData.rows, editingRowValues];

      await variantTableApi.update(selectedTable.name, {
        name: selectedTable.name,
        description: selectedTable.description,
        columns: selectedTable.columns,
        rows: newRows,
      });

      setOpenEditRowDialog(false);
      setEditingRowIndex(null);
      await loadTables();
      await loadTableData(selectedTable.name);
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to add row: ${errorMsg}`);
    }
  };

  const handleOpenEditTableDialog = () => {
    if (!selectedTable) return;
    setEditTableDescription(selectedTable.description || '');
    setEditTableStatus(selectedTable.status || 'Active');
    setEditTableColumns([...selectedTable.columns]);
    setEditNewColumnName('');
    setEditNewColumnIsKey(false);
    setColumnsModified(false);
    setOpenEditTableDialog(true);
  };

  const handleSaveTableMetadata = async () => {
    if (!selectedTable) return;

    try {
      setError(null);

      // If columns were modified, clear all data
      const rows = columnsModified ? [] : (tableData?.rows || []);

      await variantTableApi.update(selectedTable.name, {
        name: selectedTable.name,
        description: editTableDescription,
        status: editTableStatus,
        columns: editTableColumns,
        rows: rows,
      });

      setOpenEditTableDialog(false);
      await loadTables();
      // Update selected table to reflect changes
      const updatedTables = await variantTableApi.getAll();
      const updatedTable = updatedTables.find(t => t.name === selectedTable.name);
      if (updatedTable) {
        setSelectedTable(updatedTable);
        await loadTableData(updatedTable.name);
      }
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to update table: ${errorMsg}`);
    }
  };

  // Column editing functions
  const handleAddEditColumn = () => {
    if (!editNewColumnName) return;

    const charExists = material.characteristics.some(c => c.name === editNewColumnName);
    if (!charExists) {
      setError(`Characteristic "${editNewColumnName}" not found in material`);
      return;
    }

    // Check if column already exists
    if (editTableColumns.some(c => c.characteristicName === editNewColumnName)) {
      setError(`Column "${editNewColumnName}" already exists in the table`);
      return;
    }

    setEditTableColumns([
      ...editTableColumns,
      {
        characteristicName: editNewColumnName,
        sqlDataType: 'VARCHAR(255)',
        isKeyColumn: editNewColumnIsKey,
      },
    ]);
    setEditNewColumnName('');
    setEditNewColumnIsKey(false);
    setColumnsModified(true);
    setError(null);
  };

  const handleRemoveEditColumn = (index: number) => {
    setEditTableColumns(editTableColumns.filter((_, i) => i !== index));
    setColumnsModified(true);
  };

  const handleToggleKeyColumn = (index: number) => {
    const newColumns = [...editTableColumns];
    newColumns[index] = {
      ...newColumns[index],
      isKeyColumn: !newColumns[index].isKeyColumn,
    };
    setEditTableColumns(newColumns);
    // Toggling key column doesn't require clearing data
  };

  const handleMoveColumn = (index: number, direction: 'up' | 'down') => {
    const newIndex = direction === 'up' ? index - 1 : index + 1;
    if (newIndex < 0 || newIndex >= editTableColumns.length) return;

    const newColumns = [...editTableColumns];
    [newColumns[index], newColumns[newIndex]] = [newColumns[newIndex], newColumns[index]];
    setEditTableColumns(newColumns);
    setColumnsModified(true);
  };

  return (
    <div>
      {error && (
        <div className="mb-4 rounded-lg border border-destructive/50 bg-destructive/10 p-4">
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

      <div className="flex justify-between items-center mb-6">
        <div className="flex items-center gap-2">
          <Table2 className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold">Variant Tables</h2>
        </div>
        <Button onClick={() => setOpenCreateDialog(true)} className="gap-2">
          <Plus className="h-4 w-4" />
          Create Table
        </Button>
      </div>

      <div className="flex flex-col lg:flex-row gap-6">
        {/* Table List */}
        <div className="w-full lg:w-1/3">
          
          {loading && tables.length === 0 ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground mr-2" />
              <span className="text-muted-foreground">Loading tables...</span>
            </div>
          ) : tables.length === 0 ? (
            <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-4">
              <p className="text-sm text-blue-600">
                No variant tables defined yet. Create one to store configuration data.
              </p>
            </div>
          ) : (
            <div className="space-y-2">
              {tables.map((table) => (
                <Card
                  key={table.name}
                  className={cn(
                    "cursor-pointer transition-all hover:border-primary/50",
                    selectedTable?.name === table.name && "ring-2 ring-primary"
                  )}
                  onClick={() => handleSelectTable(table)}
                >
                  <CardContent className="p-4">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary/10 text-primary">
                          <Table2 className="h-4 w-4" />
                        </div>
                        <div>
                          <p className="font-medium">{table.name}</p>
                          <p className="text-xs text-muted-foreground">
                            {table.columns.length} columns, {table.rowCount} rows
                          </p>
                        </div>
                      </div>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-destructive hover:text-destructive"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleDeleteTable(table.name);
                        }}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </div>

        {/* Table Details & Data */}
        <div className="w-full lg:w-2/3">
          {selectedTable ? (
            <Card>
              <CardHeader className="pb-4">
                <div className="flex justify-between items-start">
                  <div>
                    <div className="flex items-center gap-2">
                      <CardTitle className="text-lg">{selectedTable.name}</CardTitle>
                      <Badge variant="outline" className="text-xs">
                        {selectedTable.status || 'Active'}
                      </Badge>
                    </div>
                    <p className="text-sm text-muted-foreground mt-1">
                      {selectedTable.description || 'No description'}
                    </p>
                  </div>
                  <div className="flex gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={handleOpenEditTableDialog}
                      className="gap-2"
                    >
                      <Pencil className="h-4 w-4" />
                      Edit
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setOpenUploadDialog(true)}
                      className="gap-2"
                    >
                      <Upload className="h-4 w-4" />
                      Upload Data
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={handleAddRow}
                      className="gap-2"
                    >
                      <Plus className="h-4 w-4" />
                      Add Row
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                {/* Column Info */}
                <div>
                  <span className="text-sm font-medium mb-2 block">Columns</span>
                  <div className="flex flex-wrap gap-2">
                    {selectedTable.columns.map((col, index) => (
                      <Badge
                        key={index}
                        variant={col.isKeyColumn ? 'default' : 'outline'}
                        className="gap-1"
                      >
                        {col.isKeyColumn && <Key className="h-3 w-3" />}
                        {col.characteristicName}
                      </Badge>
                    ))}
                  </div>
                </div>

                {/* Table Data */}
                <div>
                  <span className="text-sm font-medium mb-2 block">
                    Data ({tableData?.rows.length || 0} rows)
                  </span>
                  {loading ? (
                    <div className="flex items-center justify-center py-8">
                      <Loader2 className="h-6 w-6 animate-spin text-muted-foreground mr-2" />
                      <span className="text-muted-foreground">Loading data...</span>
                    </div>
                  ) : tableData && tableData.rows.length > 0 ? (
                    <ScrollArea className="h-96">
                      <Table>
                        <TableHeader>
                          <TableRow>
                            {tableData.columns.map((col, index) => (
                              <TableHead key={index}>{col}</TableHead>
                            ))}
                            <TableHead className="w-24 text-center">Actions</TableHead>
                          </TableRow>
                        </TableHeader>
                        <TableBody>
                          {tableData.rows.map((row, rowIndex) => (
                            <TableRow key={rowIndex}>
                              {row.map((cell, cellIndex) => (
                                <TableCell key={cellIndex}>{cell}</TableCell>
                              ))}
                              <TableCell className="text-center">
                                <div className="flex items-center justify-center gap-1">
                                  <Button
                                    variant="ghost"
                                    size="icon"
                                    className="h-7 w-7"
                                    onClick={() => handleEditRow(rowIndex)}
                                  >
                                    <Pencil className="h-3.5 w-3.5" />
                                  </Button>
                                  <Button
                                    variant="ghost"
                                    size="icon"
                                    className="h-7 w-7 text-destructive hover:text-destructive"
                                    onClick={() => handleDeleteRow(rowIndex)}
                                  >
                                    <Trash2 className="h-3.5 w-3.5" />
                                  </Button>
                                </div>
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </ScrollArea>
                  ) : (
                    <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-4">
                      <p className="text-sm text-blue-600">
                        No data in this table. Upload data or add rows manually.
                      </p>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          ) : (
            <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-4">
              <p className="text-sm text-blue-600">
                Select a table from the list to view its data, or create a new table.
              </p>
            </div>
          )}
        </div>
      </div>

      {/* Create Table Dialog */}
      <Dialog
        open={openCreateDialog}
        onOpenChange={(open) => {
          if (!open) {
            setOpenCreateDialog(false);
            resetCreateForm();
          }
        }}
      >
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Create Variant Table</DialogTitle>
            <DialogDescription>
              Define a table with columns mapped to material characteristics.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="tableName">Table Name</Label>
              <Input
                id="tableName"
                placeholder="e.g., T_BIKE_CONFIG"
                value={newTableName}
                onChange={(e) => setNewTableName(e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="tableDesc">Description</Label>
              <Input
                id="tableDesc"
                placeholder="Enter a description..."
                value={newTableDescription}
                onChange={(e) => setNewTableDescription(e.target.value)}
              />
            </div>

            <div className="space-y-3 pt-2">
              <Label>Columns ({newTableColumns.length})</Label>
              <div className="flex gap-2 items-end">
                <div className="flex-1">
                  <Select value={newColumnName} onValueChange={setNewColumnName}>
                    <SelectTrigger>
                      <SelectValue placeholder="Select characteristic" />
                    </SelectTrigger>
                    <SelectContent>
                      {material.characteristics
                        .filter(c => !newTableColumns.some(col => col.characteristicName === c.name))
                        .map((char) => (
                          <SelectItem key={char.name} value={char.name}>
                            {char.name}
                          </SelectItem>
                        ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="flex items-center gap-2">
                  <Checkbox
                    id="isKey"
                    checked={newColumnIsKey}
                    onCheckedChange={(checked) => setNewColumnIsKey(checked === true)}
                  />
                  <Label htmlFor="isKey" className="font-normal">Key</Label>
                </div>
                <Button variant="outline" onClick={addColumn}>
                  Add
                </Button>
              </div>

              {newTableColumns.length > 0 && (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Characteristic</TableHead>
                      <TableHead className="w-24 text-center">Key Column</TableHead>
                      <TableHead className="w-16">Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {newTableColumns.map((col, index) => (
                      <TableRow key={index}>
                        <TableCell>{col.characteristicName}</TableCell>
                        <TableCell className="text-center">
                          {col.isKeyColumn && <Key className="h-4 w-4 text-primary mx-auto" />}
                        </TableCell>
                        <TableCell>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-7 w-7 text-destructive hover:text-destructive"
                            onClick={() => removeColumn(index)}
                          >
                            <Trash2 className="h-3.5 w-3.5" />
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setOpenCreateDialog(false);
                resetCreateForm();
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleCreateTable}
              disabled={!newTableName || newTableColumns.length === 0}
            >
              Create
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Upload Data Dialog */}
      <Dialog
        open={openUploadDialog}
        onOpenChange={(open) => {
          if (!open) {
            setOpenUploadDialog(false);
            setUploadContent('');
          }
        }}
      >
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Upload Data to {selectedTable?.name}</DialogTitle>
            <DialogDescription>
              Upload a CSV or TSV file with data. The columns must match:{' '}
              <span className="font-medium">
                {selectedTable?.columns.map(c => c.characteristicName).join(', ')}
              </span>
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="flex gap-4">
              <div className="space-y-2">
                <Label>Separator</Label>
                <Select value={uploadSeparator} onValueChange={setUploadSeparator}>
                  <SelectTrigger className="w-32">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="\t">Tab</SelectItem>
                    <SelectItem value=";">Semicolon</SelectItem>
                    <SelectItem value=",">Comma</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="flex items-end gap-2">
                <Checkbox
                  id="hasHeader"
                  checked={uploadHasHeader}
                  onCheckedChange={(checked) => setUploadHasHeader(checked === true)}
                />
                <Label htmlFor="hasHeader" className="font-normal">First row is header</Label>
              </div>
            </div>

            <div className="space-y-2">
              <input
                type="file"
                ref={fileInputRef}
                onChange={handleFileSelect}
                accept=".txt,.csv,.tsv"
                className="hidden"
              />
              <Button
                variant="outline"
                onClick={() => fileInputRef.current?.click()}
              >
                Select File
              </Button>
            </div>

            <div className="space-y-2">
              <Label htmlFor="uploadContent">Data Content</Label>
              <textarea
                id="uploadContent"
                className="flex min-h-[200px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 font-mono"
                placeholder="Paste data here or select a file..."
                value={uploadContent}
                onChange={(e) => setUploadContent(e.target.value)}
              />
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setOpenUploadDialog(false);
                setUploadContent('');
              }}
            >
              Cancel
            </Button>
            <Button onClick={handleUpload} disabled={!uploadContent}>
              Upload
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Row Dialog */}
      <Dialog
        open={openEditRowDialog}
        onOpenChange={(open) => {
          if (!open) {
            setOpenEditRowDialog(false);
            setEditingRowIndex(null);
          }
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {editingRowIndex !== null && tableData && editingRowIndex < tableData.rows.length
                ? 'Edit Row'
                : 'Add Row'}
            </DialogTitle>
            <DialogDescription>
              Enter values for each column in the table.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            {tableData && editingRowValues.map((value, index) => (
              <div key={index} className="space-y-2">
                <Label htmlFor={`cell-${index}`}>{tableData.columns[index]}</Label>
                <Input
                  id={`cell-${index}`}
                  value={value}
                  onChange={(e) => {
                    const newValues = [...editingRowValues];
                    newValues[index] = e.target.value;
                    setEditingRowValues(newValues);
                  }}
                />
              </div>
            ))}
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setOpenEditRowDialog(false);
                setEditingRowIndex(null);
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={
                editingRowIndex !== null && tableData && editingRowIndex < tableData.rows.length
                  ? handleSaveRow
                  : handleSaveNewRow
              }
            >
              Save
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Table Metadata Dialog */}
      <Dialog open={openEditTableDialog} onOpenChange={setOpenEditTableDialog}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-hidden flex flex-col">
          <DialogHeader>
            <DialogTitle>Edit Table: {selectedTable?.name}</DialogTitle>
            <DialogDescription>
              Update the table description, status, and column structure.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4 flex-1 overflow-y-auto">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="tableDesc">Description</Label>
                <Textarea
                  id="tableDesc"
                  placeholder="Enter a description..."
                  value={editTableDescription}
                  onChange={(e) => setEditTableDescription(e.target.value)}
                  rows={2}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="tableStatus">Status</Label>
                <Select value={editTableStatus} onValueChange={setEditTableStatus}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select status" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Active">Active</SelectItem>
                    <SelectItem value="Inactive">Inactive</SelectItem>
                    <SelectItem value="Locked">Locked</SelectItem>
                    <SelectItem value="Released">Released</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            {/* Column Structure Section */}
            <div className="space-y-3 pt-2 border-t">
              <div className="flex items-center justify-between">
                <Label className="text-base font-semibold">Columns ({editTableColumns.length})</Label>
                {columnsModified && (
                  <div className="flex items-center gap-1.5 text-amber-600">
                    <AlertTriangle className="h-4 w-4" />
                    <span className="text-xs">Structure changed - data will be cleared</span>
                  </div>
                )}
              </div>

              {/* Add new column */}
              <div className="flex gap-2 items-end">
                <div className="flex-1">
                  <Select value={editNewColumnName} onValueChange={setEditNewColumnName}>
                    <SelectTrigger>
                      <SelectValue placeholder="Select characteristic to add" />
                    </SelectTrigger>
                    <SelectContent>
                      {material.characteristics
                        .filter(c => !editTableColumns.some(col => col.characteristicName === c.name))
                        .map((char) => (
                          <SelectItem key={char.name} value={char.name}>
                            {char.name}
                          </SelectItem>
                        ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="flex items-center gap-2">
                  <Checkbox
                    id="editIsKey"
                    checked={editNewColumnIsKey}
                    onCheckedChange={(checked) => setEditNewColumnIsKey(checked === true)}
                  />
                  <Label htmlFor="editIsKey" className="font-normal text-sm">Key</Label>
                </div>
                <Button variant="outline" size="sm" onClick={handleAddEditColumn}>
                  <Plus className="h-4 w-4 mr-1" />
                  Add
                </Button>
              </div>

              {/* Column list */}
              {editTableColumns.length > 0 ? (
                <div className="border rounded-lg">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead className="w-12">Order</TableHead>
                        <TableHead>Characteristic</TableHead>
                        <TableHead className="w-20 text-center">Key</TableHead>
                        <TableHead className="w-24 text-center">Actions</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {editTableColumns.map((col, index) => (
                        <TableRow key={index}>
                          <TableCell className="py-2">
                            <div className="flex flex-col gap-0.5">
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-5 w-5"
                                disabled={index === 0}
                                onClick={() => handleMoveColumn(index, 'up')}
                              >
                                <ChevronUp className="h-3 w-3" />
                              </Button>
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-5 w-5"
                                disabled={index === editTableColumns.length - 1}
                                onClick={() => handleMoveColumn(index, 'down')}
                              >
                                <ChevronDown className="h-3 w-3" />
                              </Button>
                            </div>
                          </TableCell>
                          <TableCell className="py-2">
                            <div className="flex items-center gap-2">
                              <GripVertical className="h-4 w-4 text-muted-foreground" />
                              <span className="font-medium">{col.characteristicName}</span>
                            </div>
                          </TableCell>
                          <TableCell className="py-2 text-center">
                            <Checkbox
                              checked={col.isKeyColumn}
                              onCheckedChange={() => handleToggleKeyColumn(index)}
                            />
                          </TableCell>
                          <TableCell className="py-2 text-center">
                            <Button
                              variant="ghost"
                              size="icon"
                              className="h-7 w-7 text-destructive hover:text-destructive"
                              onClick={() => handleRemoveEditColumn(index)}
                            >
                              <Trash2 className="h-3.5 w-3.5" />
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              ) : (
                <div className="rounded-lg border border-amber-300 bg-amber-50 p-4 text-center">
                  <p className="text-sm text-amber-700">
                    No columns defined. Add at least one column to save the table.
                  </p>
                </div>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setOpenEditTableDialog(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleSaveTableMetadata}
              disabled={editTableColumns.length === 0}
            >
              {columnsModified ? 'Save (Clear Data)' : 'Save'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
