import { useState, useEffect } from 'react';
import {
  Plus,
  Trash2,
  Pencil,
  List,
  ArrowUp,
  ArrowDown,
  EyeOff,
  Layers,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Card } from '@/components/ui/card';
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
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/ui/accordion';
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
import { classApi, characteristicApi, materialApi } from '../../api/client';
import type {
  MaterialDetailDto,
  CreateCharacteristicRequest,
  CreateClassRequest,
  ClassDetailDto,
} from '../../types/api';

interface ClassificationTabProps {
  material: MaterialDetailDto;
  onUpdate: () => void;
}

export default function ClassificationTab({ material, onUpdate }: ClassificationTabProps) {
  const [assignedClasses, setAssignedClasses] = useState<ClassDetailDto[]>([]);
  const [openCharDialog, setOpenCharDialog] = useState(false);
  const [openClassDialog, setOpenClassDialog] = useState(false);
  const [openValuesDialog, setOpenValuesDialog] = useState(false);
  const [selectedClass, setSelectedClass] = useState<string | null>(null);
  const [editingCharClass, setEditingCharClass] = useState<string | null>(null);
  const [editingCharName, setEditingCharName] = useState<string | null>(null);
  const [editingClassName, setEditingClassName] = useState<string | null>(null);
  const [managingValuesFor, setManagingValuesFor] = useState<{ className: string; char: any } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [newCharacteristic, setNewCharacteristic] = useState<CreateCharacteristicRequest>({
    name: '',
    description: '',
    dataType: 'CHAR',
    length: 255,
    isRequired: false,
    isRestrictable: true,
    isHidden: false,
    sortOrder: 0,
    allowedValues: [],
  });
  const [newClass, setNewClass] = useState<CreateClassRequest>({
    name: '',
    classType: '300',
    description: '',
    status: 'Active',
  });
  const [valueInput, setValueInput] = useState({ value: '', description: '' });
  const [managedValues, setManagedValues] = useState<{ value: string; description: string; longDescription: string; isDefault: boolean }[]>([]);
  const [valuesInput, setValuesInput] = useState({ value: '', description: '' });

  useEffect(() => {
    loadAssignedClasses();
  }, [material.materialNumber, material.classes]);

  const loadAssignedClasses = async () => {
    try {
      const materialData = await materialApi.getById(material.materialNumber);
      const classesMap = new Map<string, ClassDetailDto>();
      const assignedClassNames = materialData.classes || [];

      for (const className of assignedClassNames) {
        try {
          const classDetail = await classApi.getById(className);
          const filteredChars = materialData.characteristics.filter(c =>
            classDetail.characteristics.some(cc => cc.name === c.name)
          );
          classesMap.set(className, {
            ...classDetail,
            characteristics: filteredChars,
          });
        } catch (err) {
          console.error(`Failed to load class ${className}:`, err);
        }
      }

      const assignedCharNames = new Set<string>();
      classesMap.forEach(cls => {
        cls.characteristics.forEach(char => assignedCharNames.add(char.name));
      });

      const unassignedChars = materialData.characteristics.filter(
        char => !assignedCharNames.has(char.name)
      );

      if (unassignedChars.length > 0) {
        classesMap.set('Unassigned', {
          name: 'Unassigned Characteristics',
          classType: '300',
          description: 'Characteristics not assigned to any class',
          status: 'Active',
          characteristics: unassignedChars,
        });
      }

      setAssignedClasses(Array.from(classesMap.values()));
    } catch (err) {
      setError('Failed to load classes');
      console.error(err);
    }
  };

  const handleCreateClass = async () => {
    try {
      setError(null);
      if (editingClassName) {
        await classApi.update(editingClassName, {
          description: newClass.description,
          status: newClass.status,
          classType: newClass.classType,
        });
      } else {
        await classApi.create(newClass);
        await materialApi.assignClass(material.materialNumber, { className: newClass.name });
      }
      setOpenClassDialog(false);
      setEditingClassName(null);
      setNewClass({ name: '', classType: '300', description: '', status: 'Active' });
      await loadAssignedClasses();
      onUpdate();
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      const action = editingClassName ? 'update' : 'create';
      setError(`Failed to ${action} class: ${errorMsg}`);
      console.error(`Class ${action} error:`, err);
    }
  };

  const handleDeleteClass = async (className: string) => {
    if (!confirm(`Delete class ${className} and all its characteristics?`)) return;

    try {
      setError(null);
      await classApi.delete(className);
      await loadAssignedClasses();
      onUpdate();
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to delete class: ${errorMsg}`);
      console.error('Class deletion error:', err);
    }
  };

  const handleEditClass = (classData: ClassDetailDto) => {
    setEditingClassName(classData.name);
    setNewClass({
      name: classData.name,
      classType: classData.classType,
      description: classData.description || '',
      status: classData.status || 'Active',
    });
    setOpenClassDialog(true);
  };

  const handleAddCharacteristic = async () => {
    if (!selectedClass && !editingCharClass) return;

    try {
      setError(null);
      if (editingCharClass && editingCharName) {
        await characteristicApi.update(editingCharClass, editingCharName, newCharacteristic);
      } else if (selectedClass) {
        await characteristicApi.create(selectedClass, newCharacteristic);
      }
      setOpenCharDialog(false);
      resetCharacteristicForm();
      onUpdate();
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      const action = editingCharClass ? 'update' : 'create';
      setError(`Failed to ${action} characteristic: ${errorMsg}`);
      console.error(`Characteristic ${action} error:`, err);
    }
  };

  const handleDeleteCharacteristic = async (className: string, charName: string) => {
    if (!confirm(`Delete characteristic ${charName}?`)) return;

    try {
      setError(null);
      await characteristicApi.delete(className, charName);
      onUpdate();
    } catch (err) {
      setError('Failed to delete characteristic');
      console.error(err);
    }
  };

  const handleEditCharacteristic = (className: string, char: any) => {
    setEditingCharClass(className);
    setEditingCharName(char.name);
    setNewCharacteristic({
      name: char.name,
      description: char.description || '',
      dataType: char.dataType,
      length: 255,
      isRequired: char.isRequired,
      isRestrictable: char.isRestrictable,
      isHidden: char.isHidden || false,
      sortOrder: char.sortOrder || 0,
      allowedValues: char.allowedValues || [],
    });
    setOpenCharDialog(true);
  };

  const addAllowedValue = () => {
    if (!valueInput.value) return;

    setNewCharacteristic({
      ...newCharacteristic,
      allowedValues: [
        ...(newCharacteristic.allowedValues || []),
        {
          value: valueInput.value,
          description: valueInput.description || valueInput.value,
          longDescription: '',
          isDefault: false,
        },
      ],
    });
    setValueInput({ value: '', description: '' });
  };

  const removeAllowedValue = (index: number) => {
    setNewCharacteristic({
      ...newCharacteristic,
      allowedValues: newCharacteristic.allowedValues?.filter((_, i) => i !== index),
    });
  };

  const resetCharacteristicForm = () => {
    setNewCharacteristic({
      name: '',
      description: '',
      dataType: 'CHAR',
      length: 255,
      isRequired: false,
      isRestrictable: true,
      isHidden: false,
      sortOrder: 0,
      allowedValues: [],
    });
    setValueInput({ value: '', description: '' });
    setSelectedClass(null);
    setEditingCharClass(null);
    setEditingCharName(null);
  };

  const openCharacteristicDialog = (className: string) => {
    setSelectedClass(className);
    setOpenCharDialog(true);
  };

  const openManageValuesDialog = (className: string, char: any) => {
    setManagingValuesFor({ className, char });
    setManagedValues(char.allowedValues || []);
    setOpenValuesDialog(true);
  };

  const addManagedValue = () => {
    if (!valuesInput.value) return;

    setManagedValues([
      ...managedValues,
      {
        value: valuesInput.value,
        description: valuesInput.description || valuesInput.value,
        longDescription: '',
        isDefault: false,
      },
    ]);
    setValuesInput({ value: '', description: '' });
  };

  const removeManagedValue = (index: number) => {
    setManagedValues(managedValues.filter((_, i) => i !== index));
  };

  const toggleDefaultValue = (index: number) => {
    setManagedValues(
      managedValues.map((val, i) => ({
        ...val,
        isDefault: i === index ? !val.isDefault : val.isDefault,
      }))
    );
  };

  const saveValues = async () => {
    if (!managingValuesFor) return;

    try {
      setError(null);
      await characteristicApi.update(
        managingValuesFor.className,
        managingValuesFor.char.name,
        {
          allowedValues: managedValues,
        }
      );
      setOpenValuesDialog(false);
      setManagingValuesFor(null);
      setManagedValues([]);
      setValuesInput({ value: '', description: '' });
      await loadAssignedClasses();
      onUpdate();
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to update values: ${errorMsg}`);
      console.error('Values update error:', err);
    }
  };

  const handleMoveCharacteristic = async (className: string, charName: string, direction: 'up' | 'down') => {
    try {
      setError(null);
      const classData = assignedClasses.find(c => c.name === className);
      if (!classData) return;

      const sortedChars = [...classData.characteristics].sort((a, b) =>
        (a.sortOrder || 0) - (b.sortOrder || 0)
      );

      const currentIndex = sortedChars.findIndex(c => c.name === charName);
      if (currentIndex === -1) return;

      const newIndex = direction === 'up' ? currentIndex - 1 : currentIndex + 1;
      if (newIndex < 0 || newIndex >= sortedChars.length) return;

      const newOrder = sortedChars.map((char, idx) => {
        if (idx === currentIndex) {
          return { name: char.name, sortOrder: newIndex };
        }
        if (idx === newIndex) {
          return { name: char.name, sortOrder: currentIndex };
        }
        return { name: char.name, sortOrder: idx };
      });

      await characteristicApi.reorder(className, { order: newOrder });
      await loadAssignedClasses();
      onUpdate();
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to reorder characteristics: ${errorMsg}`);
      console.error('Reorder error:', err);
    }
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
          <Layers className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold">Classes & Characteristics</h2>
        </div>
        <Button onClick={() => setOpenClassDialog(true)} className="gap-2">
          <Plus className="h-4 w-4" />
          Create Class
        </Button>
      </div>

      {assignedClasses.length === 0 ? (
        <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-4">
          <p className="text-sm text-blue-600">
            No classes assigned. Create a class and add characteristics to get started.
          </p>
        </div>
      ) : (
        <Accordion type="multiple" defaultValue={assignedClasses.map(c => c.name)} className="space-y-4">
          {assignedClasses.map((classData) => (
            <AccordionItem key={classData.name} value={classData.name} className="border rounded-lg">
              <AccordionTrigger className="px-4 hover:no-underline">
                <div className="flex justify-between items-center w-full mr-4">
                  <div className="text-left">
                    <p className="font-medium">{classData.name}</p>
                    <p className="text-sm text-muted-foreground">
                      {classData.description || 'No description'}
                    </p>
                  </div>
                  <Badge variant="secondary" className="ml-4">
                    {classData.characteristics.length} characteristics
                  </Badge>
                </div>
              </AccordionTrigger>
              <AccordionContent className="px-4 pb-4">
                <div className="space-y-4">
                  <div className="flex justify-between items-center">
                    <span className="text-sm font-medium">Characteristics</span>
                    <div className="flex items-center gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => openCharacteristicDialog(classData.name)}
                        className="gap-2"
                      >
                        <Plus className="h-4 w-4" />
                        Add Characteristic
                      </Button>
                      {classData.name !== 'Material Characteristics' && classData.name !== 'Unassigned Characteristics' && (
                        <>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8"
                            onClick={() => handleEditClass(classData)}
                          >
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-destructive hover:text-destructive"
                            onClick={() => handleDeleteClass(classData.name)}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </>
                      )}
                    </div>
                  </div>

                  <Card>
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead className="w-16 text-center">Order</TableHead>
                          <TableHead>Name</TableHead>
                          <TableHead>Description</TableHead>
                          <TableHead className="w-20">Type</TableHead>
                          <TableHead className="w-20 text-center">Required</TableHead>
                          <TableHead className="w-24 text-center">Restrictable</TableHead>
                          <TableHead className="w-20 text-center">Hidden</TableHead>
                          <TableHead>Allowed Values</TableHead>
                          <TableHead className="w-28">Actions</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {[...classData.characteristics]
                          .sort((a, b) => (a.sortOrder || 0) - (b.sortOrder || 0))
                          .map((char, index, arr) => (
                            <TableRow key={char.name} className={cn(char.isHidden && 'opacity-60')}>
                              <TableCell className="text-center">
                                <div className="flex flex-col items-center gap-0.5">
                                  <Button
                                    variant="ghost"
                                    size="icon"
                                    className="h-6 w-6"
                                    onClick={() => handleMoveCharacteristic(classData.name, char.name, 'up')}
                                    disabled={index === 0}
                                  >
                                    <ArrowUp className="h-3 w-3" />
                                  </Button>
                                  <Button
                                    variant="ghost"
                                    size="icon"
                                    className="h-6 w-6"
                                    onClick={() => handleMoveCharacteristic(classData.name, char.name, 'down')}
                                    disabled={index === arr.length - 1}
                                  >
                                    <ArrowDown className="h-3 w-3" />
                                  </Button>
                                </div>
                              </TableCell>
                              <TableCell>
                                <div className="flex items-center gap-1.5">
                                  {char.name}
                                  {char.isHidden && (
                                    <EyeOff className="h-3.5 w-3.5 text-muted-foreground" />
                                  )}
                                </div>
                              </TableCell>
                              <TableCell className="text-muted-foreground text-sm">
                                {char.description || '-'}
                              </TableCell>
                              <TableCell>
                                <Badge variant="outline" className="text-xs">
                                  {char.dataType}
                                </Badge>
                              </TableCell>
                              <TableCell className="text-center">
                                {char.isRequired ? (
                                  <Badge className="bg-amber-500/10 text-amber-600 text-xs">Yes</Badge>
                                ) : (
                                  <span className="text-muted-foreground text-sm">No</span>
                                )}
                              </TableCell>
                              <TableCell className="text-center">
                                {char.isRestrictable ? (
                                  <Badge className="bg-blue-500/10 text-blue-600 text-xs">Yes</Badge>
                                ) : (
                                  <span className="text-muted-foreground text-sm">No</span>
                                )}
                              </TableCell>
                              <TableCell className="text-center">
                                {char.isHidden ? (
                                  <Badge variant="secondary" className="text-xs">Yes</Badge>
                                ) : (
                                  <span className="text-muted-foreground text-sm">No</span>
                                )}
                              </TableCell>
                              <TableCell>
                                <div className="flex flex-wrap gap-1">
                                  {char.allowedValues.slice(0, 3).map((val) => (
                                    <Badge
                                      key={val.value}
                                      variant={val.isDefault ? 'default' : 'outline'}
                                      className="text-xs"
                                    >
                                      {val.value}
                                    </Badge>
                                  ))}
                                  {char.allowedValues.length > 3 && (
                                    <Badge variant="secondary" className="text-xs">
                                      +{char.allowedValues.length - 3}
                                    </Badge>
                                  )}
                                </div>
                              </TableCell>
                              <TableCell>
                                <div className="flex items-center gap-1">
                                  <Button
                                    variant="ghost"
                                    size="icon"
                                    className="h-7 w-7"
                                    onClick={() => openManageValuesDialog(classData.name, char)}
                                  >
                                    <List className="h-3.5 w-3.5" />
                                  </Button>
                                  <Button
                                    variant="ghost"
                                    size="icon"
                                    className="h-7 w-7"
                                    onClick={() => handleEditCharacteristic(classData.name, char)}
                                  >
                                    <Pencil className="h-3.5 w-3.5" />
                                  </Button>
                                  <Button
                                    variant="ghost"
                                    size="icon"
                                    className="h-7 w-7 text-destructive hover:text-destructive"
                                    onClick={() => handleDeleteCharacteristic(classData.name, char.name)}
                                  >
                                    <Trash2 className="h-3.5 w-3.5" />
                                  </Button>
                                </div>
                              </TableCell>
                            </TableRow>
                          ))}
                      </TableBody>
                    </Table>
                  </Card>
                </div>
              </AccordionContent>
            </AccordionItem>
          ))}
        </Accordion>
      )}

      {/* Add/Edit Characteristic Dialog */}
      <Dialog
        open={openCharDialog}
        onOpenChange={(open) => {
          if (!open) {
            setOpenCharDialog(false);
            resetCharacteristicForm();
          }
        }}
      >
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>
              {editingCharClass ? 'Edit Characteristic' : 'Add New Characteristic'}
            </DialogTitle>
            <DialogDescription>
              {editingCharClass
                ? 'Update the characteristic properties below.'
                : 'Create a new characteristic with allowed values.'}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="charName">Characteristic Name</Label>
              <Input
                id="charName"
                placeholder="e.g., COLOR"
                value={newCharacteristic.name}
                onChange={(e) => setNewCharacteristic({ ...newCharacteristic, name: e.target.value })}
                disabled={!!editingCharClass}
              />
              {editingCharClass && (
                <p className="text-xs text-muted-foreground">Name cannot be changed when editing</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="charDesc">Description</Label>
              <Input
                id="charDesc"
                placeholder="Enter a description..."
                value={newCharacteristic.description}
                onChange={(e) => setNewCharacteristic({ ...newCharacteristic, description: e.target.value })}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="dataType">Data Type</Label>
                <Input
                  id="dataType"
                  value={newCharacteristic.dataType}
                  onChange={(e) => setNewCharacteristic({ ...newCharacteristic, dataType: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="length">Length</Label>
                <Input
                  id="length"
                  type="number"
                  value={newCharacteristic.length}
                  onChange={(e) => setNewCharacteristic({ ...newCharacteristic, length: parseInt(e.target.value) })}
                />
              </div>
            </div>

            <div className="flex flex-wrap gap-6 py-2">
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="isRequired"
                  checked={newCharacteristic.isRequired}
                  onCheckedChange={(checked) =>
                    setNewCharacteristic({ ...newCharacteristic, isRequired: checked === true })
                  }
                />
                <Label htmlFor="isRequired" className="font-normal">Required</Label>
              </div>
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="isRestrictable"
                  checked={newCharacteristic.isRestrictable}
                  onCheckedChange={(checked) =>
                    setNewCharacteristic({ ...newCharacteristic, isRestrictable: checked === true })
                  }
                />
                <Label htmlFor="isRestrictable" className="font-normal">Restrictable</Label>
              </div>
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="isHidden"
                  checked={newCharacteristic.isHidden}
                  onCheckedChange={(checked) =>
                    setNewCharacteristic({ ...newCharacteristic, isHidden: checked === true })
                  }
                />
                <Label htmlFor="isHidden" className="font-normal flex items-center gap-1">
                  Hidden
                  <EyeOff className="h-3.5 w-3.5 text-muted-foreground" />
                </Label>
              </div>
            </div>

            <div className="space-y-3 pt-2">
              <Label>Allowed Values ({newCharacteristic.allowedValues?.length || 0})</Label>
              <div className="flex gap-2">
                <Input
                  placeholder="Value"
                  value={valueInput.value}
                  onChange={(e) => setValueInput({ ...valueInput, value: e.target.value })}
                  onKeyDown={(e) => e.key === 'Enter' && addAllowedValue()}
                  className="flex-1"
                />
                <Input
                  placeholder="Description"
                  value={valueInput.description}
                  onChange={(e) => setValueInput({ ...valueInput, description: e.target.value })}
                  onKeyDown={(e) => e.key === 'Enter' && addAllowedValue()}
                  className="flex-1"
                />
                <Button variant="outline" onClick={addAllowedValue} className="gap-2">
                  <Plus className="h-4 w-4" />
                  Add
                </Button>
              </div>

              {newCharacteristic.allowedValues && newCharacteristic.allowedValues.length > 0 ? (
                <ScrollArea className="h-48">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Value</TableHead>
                        <TableHead>Description</TableHead>
                        <TableHead className="w-20 text-center">Default</TableHead>
                        <TableHead className="w-16">Actions</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {newCharacteristic.allowedValues.map((val, index) => (
                        <TableRow key={index}>
                          <TableCell>
                            <Input
                              value={val.value}
                              onChange={(e) => {
                                const updated = [...(newCharacteristic.allowedValues || [])];
                                updated[index] = { ...updated[index], value: e.target.value };
                                setNewCharacteristic({ ...newCharacteristic, allowedValues: updated });
                              }}
                              className="h-8"
                            />
                          </TableCell>
                          <TableCell>
                            <Input
                              value={val.description}
                              onChange={(e) => {
                                const updated = [...(newCharacteristic.allowedValues || [])];
                                updated[index] = { ...updated[index], description: e.target.value };
                                setNewCharacteristic({ ...newCharacteristic, allowedValues: updated });
                              }}
                              className="h-8"
                            />
                          </TableCell>
                          <TableCell className="text-center">
                            <Checkbox
                              checked={val.isDefault}
                              onCheckedChange={() => {
                                const updated = [...(newCharacteristic.allowedValues || [])];
                                updated[index] = { ...updated[index], isDefault: !updated[index].isDefault };
                                setNewCharacteristic({ ...newCharacteristic, allowedValues: updated });
                              }}
                            />
                          </TableCell>
                          <TableCell>
                            <Button
                              variant="ghost"
                              size="icon"
                              className="h-7 w-7 text-destructive hover:text-destructive"
                              onClick={() => removeAllowedValue(index)}
                            >
                              <Trash2 className="h-3.5 w-3.5" />
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </ScrollArea>
              ) : (
                <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-4">
                  <p className="text-sm text-blue-600">
                    No values defined yet. Add some values above.
                  </p>
                </div>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setOpenCharDialog(false);
                resetCharacteristicForm();
              }}
            >
              Cancel
            </Button>
            <Button onClick={handleAddCharacteristic} disabled={!newCharacteristic.name}>
              {editingCharClass ? 'Update' : 'Create'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Create/Edit Class Dialog */}
      <Dialog
        open={openClassDialog}
        onOpenChange={(open) => {
          if (!open) {
            setOpenClassDialog(false);
            setEditingClassName(null);
            setNewClass({ name: '', classType: '300', description: '', status: 'Active' });
          }
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {editingClassName ? 'Edit Class' : 'Create New Class'}
            </DialogTitle>
            <DialogDescription>
              {editingClassName
                ? 'Update the class properties below.'
                : 'Create a new class to organize characteristics.'}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="className">Class Name</Label>
              <Input
                id="className"
                placeholder="e.g., BIKE_CLASS"
                value={newClass.name}
                onChange={(e) => setNewClass({ ...newClass, name: e.target.value })}
                disabled={!!editingClassName}
              />
              {editingClassName && (
                <p className="text-xs text-muted-foreground">Name cannot be changed when editing</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="classType">Class Type</Label>
              <Input
                id="classType"
                placeholder="e.g., 300"
                value={newClass.classType}
                onChange={(e) => setNewClass({ ...newClass, classType: e.target.value })}
              />
              <p className="text-xs text-muted-foreground">Common: 300 (Material), 200 (Equipment)</p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="classDesc">Description</Label>
              <Input
                id="classDesc"
                placeholder="Enter a description..."
                value={newClass.description}
                onChange={(e) => setNewClass({ ...newClass, description: e.target.value })}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="classStatus">Status</Label>
              <Input
                id="classStatus"
                value={newClass.status}
                onChange={(e) => setNewClass({ ...newClass, status: e.target.value })}
              />
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setOpenClassDialog(false);
                setEditingClassName(null);
                setNewClass({ name: '', classType: '300', description: '', status: 'Active' });
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleCreateClass}
              disabled={!newClass.name || !newClass.classType}
            >
              {editingClassName ? 'Update' : 'Create & Assign'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Manage Values Dialog */}
      <Dialog
        open={openValuesDialog}
        onOpenChange={(open) => {
          if (!open) {
            setOpenValuesDialog(false);
            setManagingValuesFor(null);
            setManagedValues([]);
            setValuesInput({ value: '', description: '' });
          }
        }}
      >
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Manage Values - {managingValuesFor?.char.name}</DialogTitle>
            <DialogDescription>
              Add, edit, remove, or set default values for this characteristic.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="flex gap-2">
              <Input
                placeholder="Value"
                value={valuesInput.value}
                onChange={(e) => setValuesInput({ ...valuesInput, value: e.target.value })}
                onKeyDown={(e) => e.key === 'Enter' && addManagedValue()}
                className="flex-1"
              />
              <Input
                placeholder="Description"
                value={valuesInput.description}
                onChange={(e) => setValuesInput({ ...valuesInput, description: e.target.value })}
                onKeyDown={(e) => e.key === 'Enter' && addManagedValue()}
                className="flex-1"
              />
              <Button variant="outline" onClick={addManagedValue} className="gap-2">
                <Plus className="h-4 w-4" />
                Add
              </Button>
            </div>

            <div className="space-y-2">
              <Label>Current Values ({managedValues.length})</Label>
              {managedValues.length === 0 ? (
                <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-4">
                  <p className="text-sm text-blue-600">
                    No values defined yet. Add some values above.
                  </p>
                </div>
              ) : (
                <ScrollArea className="h-64">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Value</TableHead>
                        <TableHead>Description</TableHead>
                        <TableHead className="w-20 text-center">Default</TableHead>
                        <TableHead className="w-16">Actions</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {managedValues.map((val, index) => (
                        <TableRow key={index}>
                          <TableCell>
                            <Input
                              value={val.value}
                              onChange={(e) => {
                                const updated = [...managedValues];
                                updated[index] = { ...updated[index], value: e.target.value };
                                setManagedValues(updated);
                              }}
                              className="h-8"
                            />
                          </TableCell>
                          <TableCell>
                            <Input
                              value={val.description}
                              onChange={(e) => {
                                const updated = [...managedValues];
                                updated[index] = { ...updated[index], description: e.target.value };
                                setManagedValues(updated);
                              }}
                              className="h-8"
                            />
                          </TableCell>
                          <TableCell className="text-center">
                            <Checkbox
                              checked={val.isDefault}
                              onCheckedChange={() => toggleDefaultValue(index)}
                            />
                          </TableCell>
                          <TableCell>
                            <Button
                              variant="ghost"
                              size="icon"
                              className="h-7 w-7 text-destructive hover:text-destructive"
                              onClick={() => removeManagedValue(index)}
                            >
                              <Trash2 className="h-3.5 w-3.5" />
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </ScrollArea>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setOpenValuesDialog(false);
                setManagingValuesFor(null);
                setManagedValues([]);
                setValuesInput({ value: '', description: '' });
              }}
            >
              Cancel
            </Button>
            <Button onClick={saveValues}>Save Values</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
