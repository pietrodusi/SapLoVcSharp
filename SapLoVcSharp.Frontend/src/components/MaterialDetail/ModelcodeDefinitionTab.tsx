import { useState, useEffect } from 'react';
import {
  Plus,
  Trash2,
  ArrowUp,
  ArrowDown,
  Save,
  Loader2,
  ChevronDown,
  ChevronRight,
  FileCode2,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Card } from '@/components/ui/card';
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
import { classApi, characteristicApi, materialApi } from '../../api/client';
import type { MaterialDetailDto, CharacteristicValueDto } from '../../types/api';

interface ModelcodeDefinitionTabProps {
  material: MaterialDetailDto;
  onUpdate?: () => void;
}

interface ModelcodeValue {
  value: string;
  description: string;
  longDescription: string;
  isDefault: boolean;
}

interface ModelcodeCharacteristic {
  shortName: string;
  length: number;
  description: string;
  longDescription: string;
  values: ModelcodeValue[];
  isExisting: boolean;
  existingName?: string;
  isExpanded: boolean;
}

export default function ModelcodeDefinitionTab({ material, onUpdate }: ModelcodeDefinitionTabProps) {
  const [characteristics, setCharacteristics] = useState<ModelcodeCharacteristic[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasChanges, setHasChanges] = useState(false);

  const className = `CL_${material.materialNumber}`;
  const prefix = `${material.materialNumber}_`;

  useEffect(() => {
    loadExistingClass();
  }, [material.materialNumber]);

  const loadExistingClass = async () => {
    try {
      setLoading(true);
      setError(null);

      const classData = await classApi.getById(className);

      // Convert existing characteristics to modelcode format
      const modelcodeChars: ModelcodeCharacteristic[] = classData.characteristics
        .sort((a, b) => a.sortOrder - b.sortOrder)
        .map(char => {
          // Extract shortName by removing the material prefix
          let shortName = char.name;
          if (char.name.toUpperCase().startsWith(prefix.toUpperCase())) {
            shortName = char.name.substring(prefix.length);
          }

          return {
            shortName: shortName.toLowerCase(),
            length: char.length,
            description: char.description,
            longDescription: char.longDescription || '',
            values: char.allowedValues.map(v => ({
              value: v.value,
              description: v.description,
              longDescription: v.longDescription || '',
              isDefault: v.isDefault,
            })),
            isExisting: true,
            existingName: char.name,
            isExpanded: false,
          };
        });

      setCharacteristics(modelcodeChars);
      setHasChanges(false);
    } catch (err: any) {
      // Class doesn't exist yet - start with empty list
      if (err.response?.status === 404) {
        setCharacteristics([]);
        setHasChanges(false);
      } else {
        setError('Failed to load class data');
        console.error(err);
      }
    } finally {
      setLoading(false);
    }
  };

  const addCharacteristic = () => {
    setCharacteristics([
      ...characteristics,
      {
        shortName: '',
        length: 30,
        description: '',
        longDescription: '',
        values: [],
        isExisting: false,
        isExpanded: true,
      },
    ]);
    setHasChanges(true);
  };

  const removeCharacteristic = (index: number) => {
    setCharacteristics(characteristics.filter((_, i) => i !== index));
    setHasChanges(true);
  };

  const updateCharacteristic = (index: number, updates: Partial<ModelcodeCharacteristic>) => {
    const updated = [...characteristics];
    updated[index] = { ...updated[index], ...updates };
    setCharacteristics(updated);
    setHasChanges(true);
  };

  const moveCharacteristic = (index: number, direction: 'up' | 'down') => {
    const newIndex = direction === 'up' ? index - 1 : index + 1;
    if (newIndex < 0 || newIndex >= characteristics.length) return;

    const updated = [...characteristics];
    [updated[index], updated[newIndex]] = [updated[newIndex], updated[index]];
    setCharacteristics(updated);
    setHasChanges(true);
  };

  const addValue = (charIndex: number) => {
    const updated = [...characteristics];
    updated[charIndex].values.push({
      value: '',
      description: '',
      longDescription: '',
      isDefault: false,
    });
    setCharacteristics(updated);
    setHasChanges(true);
  };

  const removeValue = (charIndex: number, valueIndex: number) => {
    const updated = [...characteristics];
    updated[charIndex].values = updated[charIndex].values.filter((_, i) => i !== valueIndex);
    setCharacteristics(updated);
    setHasChanges(true);
  };

  const updateValue = (charIndex: number, valueIndex: number, updates: Partial<ModelcodeValue>) => {
    const updated = [...characteristics];
    updated[charIndex].values[valueIndex] = {
      ...updated[charIndex].values[valueIndex],
      ...updates,
    };
    setCharacteristics(updated);
    setHasChanges(true);
  };

  const moveValue = (charIndex: number, valueIndex: number, direction: 'up' | 'down') => {
    const newIndex = direction === 'up' ? valueIndex - 1 : valueIndex + 1;
    const char = characteristics[charIndex];
    if (newIndex < 0 || newIndex >= char.values.length) return;

    const updated = [...characteristics];
    const values = [...updated[charIndex].values];
    [values[valueIndex], values[newIndex]] = [values[newIndex], values[valueIndex]];
    updated[charIndex].values = values;
    setCharacteristics(updated);
    setHasChanges(true);
  };

  const toggleCharacteristicExpanded = (index: number) => {
    const updated = [...characteristics];
    updated[index].isExpanded = !updated[index].isExpanded;
    setCharacteristics(updated);
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      setError(null);

      // Check if class exists
      let classExists = false;
      try {
        await classApi.getById(className);
        classExists = true;
      } catch {
        classExists = false;
      }

      // Create class if it doesn't exist
      if (!classExists) {
        await classApi.create({
          name: className,
          classType: '300',
          description: `Modelcode class for ${material.materialNumber}`,
          status: 'Active',
        });
      }

      // Process each characteristic
      for (let i = 0; i < characteristics.length; i++) {
        const char = characteristics[i];
        const fullName = `${prefix}${char.shortName.toUpperCase()}`;

        const allowedValues: CharacteristicValueDto[] = char.values.map(v => ({
          value: v.value,
          description: v.description,
          longDescription: v.longDescription,
          isDefault: v.isDefault,
        }));

        if (char.isExisting && char.existingName) {
          // Update existing characteristic
          await characteristicApi.update(className, char.existingName, {
            description: char.description,
            longDescription: char.longDescription,
            length: char.length,
            allowedValues,
          });
        } else {
          // Create new characteristic - Required and Restrictable by default
          await characteristicApi.create(className, {
            name: fullName,
            description: char.description,
            longDescription: char.longDescription,
            dataType: 'CHAR',
            length: char.length,
            isRequired: true,
            isRestrictable: true,
            isHidden: false,
            sortOrder: i,
            allowedValues,
          });
        }
      }

      // Reorder all characteristics
      const reorderItems = characteristics.map((char, index) => ({
        name: char.isExisting && char.existingName
          ? char.existingName
          : `${prefix}${char.shortName.toUpperCase()}`,
        sortOrder: index,
      }));
      await characteristicApi.reorder(className, { order: reorderItems });

      // Assign class to material if not already assigned
      if (!material.classes?.includes(className)) {
        await materialApi.assignClass(material.materialNumber, { className });
      }

      // Reload to get updated data
      await loadExistingClass();
      setHasChanges(false);

      // Notify parent to refresh material data (updates Configuration tab)
      onUpdate?.();
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to save: ${errorMsg}`);
      console.error('Save error:', err);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-40">
        <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
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

      {/* Header */}
      <div className="flex justify-between items-center">
        <div className="flex items-center gap-2">
          <FileCode2 className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold">Modelcode Definition</h2>
          <Badge variant="secondary" className="ml-2">{className}</Badge>
        </div>
        <div className="flex items-center gap-2">
          <Button onClick={addCharacteristic} variant="outline" className="gap-2">
            <Plus className="h-4 w-4" />
            Add Characteristic
          </Button>
          <Button
            onClick={handleSave}
            disabled={saving || !hasChanges}
            className="gap-2"
          >
            {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
            Save
          </Button>
        </div>
      </div>

      {/* Info message */}
      <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-4">
        <p className="text-sm text-blue-600">
          Define characteristics with short names. The full characteristic name will be <strong>{prefix}SHORTNAME</strong>.
          Values are limited to the length defined for each characteristic.
        </p>
      </div>

      {/* Characteristics list */}
      {characteristics.length === 0 ? (
        <Card className="p-8 text-center">
          <p className="text-muted-foreground">No characteristics defined. Click "Add Characteristic" to get started.</p>
        </Card>
      ) : (
        <div className="space-y-4">
          {characteristics.map((char, charIndex) => (
            <Card key={charIndex} className="overflow-hidden">
              {/* Characteristic header */}
              <div
                className="flex items-center gap-4 p-4 bg-muted/30 cursor-pointer"
                onClick={() => toggleCharacteristicExpanded(charIndex)}
              >
                <div className="flex flex-col items-center gap-0.5">
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-6 w-6"
                    onClick={(e) => { e.stopPropagation(); moveCharacteristic(charIndex, 'up'); }}
                    disabled={charIndex === 0}
                  >
                    <ArrowUp className="h-3 w-3" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-6 w-6"
                    onClick={(e) => { e.stopPropagation(); moveCharacteristic(charIndex, 'down'); }}
                    disabled={charIndex === characteristics.length - 1}
                  >
                    <ArrowDown className="h-3 w-3" />
                  </Button>
                </div>

                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    {char.isExpanded ? (
                      <ChevronDown className="h-4 w-4 text-muted-foreground" />
                    ) : (
                      <ChevronRight className="h-4 w-4 text-muted-foreground" />
                    )}
                    <span className="font-medium">
                      {char.shortName ? `${prefix}${char.shortName.toUpperCase()}` : '(New Characteristic)'}
                    </span>
                    {char.isExisting && (
                      <Badge variant="secondary" className="text-xs">Existing</Badge>
                    )}
                  </div>
                  <p className="text-sm text-muted-foreground ml-6">
                    {char.description || 'No description'} - {char.values.length} values
                  </p>
                </div>

                <Button
                  variant="ghost"
                  size="icon"
                  className="h-8 w-8 text-destructive hover:text-destructive"
                  onClick={(e) => { e.stopPropagation(); removeCharacteristic(charIndex); }}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>

              {/* Characteristic details (expanded) */}
              {char.isExpanded && (
                <div className="p-4 space-y-4 border-t">
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label>Short Name</Label>
                      <Input
                        placeholder="e.g., COLOR"
                        value={char.shortName}
                        onChange={(e) => updateCharacteristic(charIndex, { shortName: e.target.value })}
                        disabled={char.isExisting}
                      />
                      {char.isExisting && (
                        <p className="text-xs text-muted-foreground">Name cannot be changed for existing characteristics</p>
                      )}
                    </div>
                    <div className="space-y-2">
                      <Label>Length</Label>
                      <Input
                        type="number"
                        min={1}
                        value={char.length}
                        onChange={(e) => updateCharacteristic(charIndex, { length: parseInt(e.target.value) || 1 })}
                      />
                    </div>
                  </div>

                  <div className="space-y-2">
                    <Label>Description</Label>
                    <Input
                      placeholder="Short description"
                      value={char.description}
                      onChange={(e) => updateCharacteristic(charIndex, { description: e.target.value })}
                    />
                  </div>

                  <div className="space-y-2">
                    <Label>Long Description</Label>
                    <Textarea
                      placeholder="Detailed description..."
                      value={char.longDescription}
                      onChange={(e) => updateCharacteristic(charIndex, { longDescription: e.target.value })}
                      rows={2}
                    />
                  </div>

                  {/* Values section */}
                  <div className="space-y-3 pt-2">
                    <div className="flex items-center justify-between">
                      <Label>Values ({char.values.length})</Label>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => addValue(charIndex)}
                        className="gap-2"
                      >
                        <Plus className="h-3 w-3" />
                        Add Value
                      </Button>
                    </div>

                    {char.values.length === 0 ? (
                      <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-3">
                        <p className="text-sm text-blue-600">No values defined. Click "Add Value" to add allowed values.</p>
                      </div>
                    ) : (
                      <ScrollArea className="h-64">
                        <Table>
                          <TableHeader>
                            <TableRow>
                              <TableHead className="w-12">Order</TableHead>
                              <TableHead>Value (max {char.length})</TableHead>
                              <TableHead>Description (max 30)</TableHead>
                              <TableHead>Long Description</TableHead>
                              <TableHead className="w-12">Actions</TableHead>
                            </TableRow>
                          </TableHeader>
                          <TableBody>
                            {char.values.map((val, valIndex) => (
                              <TableRow key={valIndex}>
                                <TableCell>
                                  <div className="flex flex-col items-center gap-0.5">
                                    <Button
                                      variant="ghost"
                                      size="icon"
                                      className="h-5 w-5"
                                      onClick={() => moveValue(charIndex, valIndex, 'up')}
                                      disabled={valIndex === 0}
                                    >
                                      <ArrowUp className="h-2.5 w-2.5" />
                                    </Button>
                                    <Button
                                      variant="ghost"
                                      size="icon"
                                      className="h-5 w-5"
                                      onClick={() => moveValue(charIndex, valIndex, 'down')}
                                      disabled={valIndex === char.values.length - 1}
                                    >
                                      <ArrowDown className="h-2.5 w-2.5" />
                                    </Button>
                                  </div>
                                </TableCell>
                                <TableCell>
                                  <Input
                                    value={val.value}
                                    maxLength={char.length}
                                    onChange={(e) => updateValue(charIndex, valIndex, { value: e.target.value })}
                                    className="h-8"
                                  />
                                </TableCell>
                                <TableCell>
                                  <Input
                                    value={val.description}
                                    maxLength={30}
                                    onChange={(e) => updateValue(charIndex, valIndex, { description: e.target.value })}
                                    className="h-8"
                                  />
                                </TableCell>
                                <TableCell>
                                  <Input
                                    value={val.longDescription}
                                    onChange={(e) => updateValue(charIndex, valIndex, { longDescription: e.target.value })}
                                    className="h-8"
                                  />
                                </TableCell>
                                <TableCell>
                                  <Button
                                    variant="ghost"
                                    size="icon"
                                    className="h-7 w-7 text-destructive hover:text-destructive"
                                    onClick={() => removeValue(charIndex, valIndex)}
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
              )}
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
