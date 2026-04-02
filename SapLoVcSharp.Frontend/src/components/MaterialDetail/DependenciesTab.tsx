import { useState, useEffect } from 'react';
import {
  Plus,
  Trash2,
  Pencil,
  GitBranch,
  Network,
  Play,
  Loader2,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import Editor from '@monaco-editor/react';
import { constraintNetApi, dependencyApi, configurationProfileApi, procedureApi } from '../../api/client';
import type {
  MaterialDetailDto,
  ConstraintNetDto,
  CreateConstraintNetRequest,
  CreateDependencyRequest,
  ConfigurationProfileDto,
  CreateProcedureRequest,
} from '../../types/api';

interface DependenciesTabProps {
  material: MaterialDetailDto;
}

const SAMPLE_CONSTRAINT = `* Sample Constraint
OBJECTS:
KMAT IS_A (300) CL_KMAT WHERE
    lv_CHAR1 = CHAR1;
    lv_CHAR2 = CHAR2;
    lv_CHAR3 = CHAR3.


CONDITION:
lv_CHAR1 = 'X'.

RESTRICTIONS:
TABLE T_KMAT_01 (
    CHAR2 = lv_CHAR2,
    CHAR3 = lv_CHAR3
)

INFERENCES:
lv_CHAR2,
lv_CHAR3.
`;

const SAMPLE_PROCEDURE = `* Sample Procedure
$SELF.CHAR1 = 'X'
IF $SELF.CHAR2 = 'Y'
`;

export default function DependenciesTab({ material }: DependenciesTabProps) {
  const [configurationProfile, setConfigurationProfile] = useState<ConfigurationProfileDto | null>(null);
  const [constraintNets, setConstraintNets] = useState<ConstraintNetDto[]>([]);
  const [openNetDialog, setOpenNetDialog] = useState(false);
  const [openDepDialog, setOpenDepDialog] = useState(false);
  const [openProcDialog, setOpenProcDialog] = useState(false);
  const [selectedNetId, setSelectedNetId] = useState<string | null>(null);
  const [editingProcedure, setEditingProcedure] = useState<string | null>(null);
  const [editingDependency, setEditingDependency] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const [newNet, setNewNet] = useState<CreateConstraintNetRequest>({
    name: '',
    description: '',
  });

  const [newDependency, setNewDependency] = useState<CreateDependencyRequest>({
    name: '',
    type: 'Constraint',
    sourceCode: SAMPLE_CONSTRAINT,
    description: '',
    status: 'Active',
  });

  const [newProcedure, setNewProcedure] = useState<CreateProcedureRequest>({
    name: '',
    sourceCode: SAMPLE_PROCEDURE,
    description: '',
    status: 'Active',
  });

  useEffect(() => {
    loadConfigurationProfile();
    loadConstraintNets();
  }, [material.materialNumber]);

  const loadConfigurationProfile = async () => {
    try {
      const profile = await configurationProfileApi.getByMaterial(material.materialNumber);
      setConfigurationProfile(profile);
    } catch (err) {
      console.error('Failed to load configuration profile:', err);
      setConfigurationProfile(null);
    }
  };

  const loadConstraintNets = async () => {
    try {
      setLoading(true);
      const nets = await constraintNetApi.getByMaterial(material.materialNumber);
      setConstraintNets(nets);
    } catch (err) {
      console.error('Failed to load constraint nets:', err);
      setConstraintNets([]);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateNet = async () => {
    try {
      setError(null);
      await constraintNetApi.create(material.materialNumber, newNet);
      setOpenNetDialog(false);
      setNewNet({ name: '', description: '' });
      await loadConstraintNets();
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to create constraint net: ${errorMsg}`);
      console.error('Constraint net creation error:', err);
    }
  };

  const handleDeleteNet = async (netId: string) => {
    if (!confirm('Delete this constraint net and all its dependencies?')) return;

    try {
      setError(null);
      await constraintNetApi.delete(netId);
      await loadConstraintNets();
    } catch (err) {
      setError('Failed to delete constraint net');
      console.error(err);
    }
  };

  const handleCreateDependency = async () => {
    if (!selectedNetId && !editingDependency) return;

    try {
      setError(null);
      if (editingDependency) {
        await dependencyApi.update(editingDependency, newDependency);
      } else {
        await dependencyApi.create(selectedNetId!, newDependency);
      }
      setOpenDepDialog(false);
      setEditingDependency(null);
      setNewDependency({
        name: '',
        type: 'Constraint',
        sourceCode: SAMPLE_CONSTRAINT,
        description: '',
        status: 'Active',
      });
      await loadConstraintNets();
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      const action = editingDependency ? 'update' : 'create';
      setError(`Failed to ${action} dependency: ${errorMsg}`);
      console.error(`Dependency ${action} error:`, err);
    }
  };

  const handleDeleteDependency = async (depId: string) => {
    if (!confirm('Delete this dependency?')) return;

    try {
      setError(null);
      await dependencyApi.delete(depId);
      await loadConstraintNets();
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to delete dependency: ${errorMsg}`);
      console.error('Dependency deletion error:', err);
    }
  };

  const handleCreateProcedure = async () => {
    if (!configurationProfile) {
      setError('No configuration profile found. Please ensure the material has a configuration profile.');
      return;
    }

    try {
      setError(null);
      if (editingProcedure) {
        await procedureApi.update(editingProcedure, newProcedure);
      } else {
        await procedureApi.create(configurationProfile.id, newProcedure);
      }
      setOpenProcDialog(false);
      setEditingProcedure(null);
      setNewProcedure({
        name: '',
        sourceCode: SAMPLE_PROCEDURE,
        description: '',
        status: 'Active',
      });
      await loadConfigurationProfile();
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      const action = editingProcedure ? 'update' : 'create';
      setError(`Failed to ${action} procedure: ${errorMsg}`);
      console.error(`Procedure ${action} error:`, err);
    }
  };

  const handleEditProcedure = (proc: any) => {
    setEditingProcedure(proc.id);
    setNewProcedure({
      name: proc.name,
      sourceCode: proc.sourceCode,
      description: proc.description || '',
      status: proc.status || 'Active',
    });
    setOpenProcDialog(true);
  };

  const handleEditDependency = (dep: any) => {
    setEditingDependency(dep.id);
    setNewDependency({
      name: dep.name,
      type: dep.type,
      sourceCode: dep.sourceCode,
      description: dep.description || '',
      status: dep.status || 'Active',
    });
    setOpenDepDialog(true);
  };

  const handleDeleteProcedure = async (procId: string) => {
    if (!confirm('Delete this procedure?')) return;

    try {
      setError(null);
      await procedureApi.delete(procId);
      await loadConfigurationProfile();
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || err.message || 'Unknown error';
      setError(`Failed to delete procedure: ${errorMsg}`);
      console.error('Procedure deletion error:', err);
    }
  };

  const openDependencyDialog = (netId: string) => {
    setSelectedNetId(netId);
    setOpenDepDialog(true);
  };

  return (
    <div className="space-y-8">
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

      {!material.hasConfigurationProfile && (
        <div className="rounded-lg border border-amber-500/50 bg-amber-500/10 p-4">
          <p className="text-sm text-amber-600">
            This material does not have a configuration profile. Create one to manage procedures and dependencies.
          </p>
        </div>
      )}

      {/* Procedures Section */}
      <div>
        <div className="flex justify-between items-center mb-4">
          <div className="flex items-center gap-2">
            <Play className="h-5 w-5 text-muted-foreground" />
            <h2 className="text-lg font-semibold">Procedures</h2>
          </div>
          <Button
            onClick={() => setOpenProcDialog(true)}
            disabled={!configurationProfile}
            className="gap-2"
          >
            <Plus className="h-4 w-4" />
            Create Procedure
          </Button>
        </div>

        {!configurationProfile ? (
          <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-4">
            <p className="text-sm text-blue-600">
              No configuration profile found. Procedures will be available once a profile is created.
            </p>
          </div>
        ) : !configurationProfile.procedures || configurationProfile.procedures.length === 0 ? (
          <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-4">
            <p className="text-sm text-blue-600">
              No procedures defined. Procedures are assigned to the configuration profile and execute sequentially.
            </p>
          </div>
        ) : (
          <div className="space-y-2">
            {configurationProfile.procedures.map((proc) => (
              <Card key={proc.id}>
                <CardContent className="py-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-emerald-500/10 text-emerald-600">
                        <Play className="h-4 w-4" />
                      </div>
                      <div>
                        <p className="font-medium">{proc.name}</p>
                        <p className="text-sm text-muted-foreground">
                          Status: <Badge variant="outline" className="ml-1 text-xs">{proc.status}</Badge>
                        </p>
                      </div>
                    </div>
                    <div className="flex items-center gap-1">
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8"
                        onClick={() => handleEditProcedure(proc)}
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-destructive hover:text-destructive"
                        onClick={() => handleDeleteProcedure(proc.id)}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>

      <div className="border-t pt-8">
        {/* Constraint Nets Section */}
        <div className="flex justify-between items-center mb-4">
          <div className="flex items-center gap-2">
            <Network className="h-5 w-5 text-muted-foreground" />
            <h2 className="text-lg font-semibold">Constraint Nets</h2>
          </div>
          <Button onClick={() => setOpenNetDialog(true)} className="gap-2">
            <Plus className="h-4 w-4" />
            Create Constraint Net
          </Button>
        </div>

        {loading ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground mr-2" />
            <span className="text-muted-foreground">Loading constraint nets...</span>
          </div>
        ) : constraintNets.length === 0 ? (
          <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-4">
            <p className="text-sm text-blue-600">
              No constraint nets defined. Create a constraint net to start adding constraints.
            </p>
          </div>
        ) : (
          <Accordion type="multiple" defaultValue={constraintNets.map(n => n.id)} className="space-y-4">
            {constraintNets.map((net) => (
              <AccordionItem key={net.id} value={net.id} className="border rounded-lg">
                <AccordionTrigger className="px-4 hover:no-underline">
                  <div className="flex justify-between items-center w-full mr-4">
                    <div className="text-left">
                      <p className="font-medium">{net.name}</p>
                      <p className="text-sm text-muted-foreground">
                        {net.description || 'No description'}
                      </p>
                    </div>
                    <Badge variant="secondary">
                      {net.dependencies?.length || 0} constraints
                    </Badge>
                  </div>
                </AccordionTrigger>
                <AccordionContent className="px-4 pb-4">
                  <div className="space-y-4">
                    <div className="flex justify-between items-center">
                      <span className="text-sm font-medium">Constraints</span>
                      <div className="flex items-center gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => openDependencyDialog(net.id)}
                          className="gap-2"
                        >
                          <Plus className="h-4 w-4" />
                          Add Constraint
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-destructive hover:text-destructive"
                          onClick={() => handleDeleteNet(net.id)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>

                    {net.dependencies && net.dependencies.length > 0 ? (
                      <div className="space-y-2">
                        {net.dependencies.map((dep) => (
                          <Card key={dep.id}>
                            <CardContent className="py-3">
                              <div className="flex items-center justify-between">
                                <div className="flex items-center gap-3">
                                  <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-amber-500/10 text-amber-600">
                                    <GitBranch className="h-4 w-4" />
                                  </div>
                                  <div>
                                    <p className="font-medium">{dep.name}</p>
                                    <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                      <span>Type: <Badge variant="outline" className="ml-1 text-xs">{dep.type}</Badge></span>
                                      <span>Status: <Badge variant="outline" className="ml-1 text-xs">{dep.status}</Badge></span>
                                    </div>
                                  </div>
                                </div>
                                <div className="flex items-center gap-1">
                                  <Button
                                    variant="ghost"
                                    size="icon"
                                    className="h-8 w-8"
                                    onClick={() => handleEditDependency(dep)}
                                  >
                                    <Pencil className="h-4 w-4" />
                                  </Button>
                                  <Button
                                    variant="ghost"
                                    size="icon"
                                    className="h-8 w-8 text-destructive hover:text-destructive"
                                    onClick={() => handleDeleteDependency(dep.id)}
                                  >
                                    <Trash2 className="h-4 w-4" />
                                  </Button>
                                </div>
                              </div>
                            </CardContent>
                          </Card>
                        ))}
                      </div>
                    ) : (
                      <p className="text-sm text-muted-foreground py-2">
                        No constraints yet. Click "Add Constraint" to create one.
                      </p>
                    )}
                  </div>
                </AccordionContent>
              </AccordionItem>
            ))}
          </Accordion>
        )}
      </div>

      {/* Create Constraint Net Dialog */}
      <Dialog open={openNetDialog} onOpenChange={setOpenNetDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create Constraint Net</DialogTitle>
            <DialogDescription>
              A constraint net groups related constraints together.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="netName">Net Name</Label>
              <Input
                id="netName"
                placeholder="e.g., BIKE_CONSTRAINTS"
                value={newNet.name}
                onChange={(e) => setNewNet({ ...newNet, name: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="netDesc">Description</Label>
              <Input
                id="netDesc"
                placeholder="Enter a description..."
                value={newNet.description}
                onChange={(e) => setNewNet({ ...newNet, description: e.target.value })}
              />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setOpenNetDialog(false)}>
              Cancel
            </Button>
            <Button onClick={handleCreateNet} disabled={!newNet.name}>
              Create
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Create/Edit Dependency Dialog */}
      <Dialog
        open={openDepDialog}
        onOpenChange={(open) => {
          if (!open) {
            setOpenDepDialog(false);
            setEditingDependency(null);
            setNewDependency({
              name: '',
              type: 'Constraint',
              sourceCode: SAMPLE_CONSTRAINT,
              description: '',
              status: 'Active',
            });
          }
        }}
      >
        <DialogContent className="max-w-4xl max-h-[90vh] overflow-hidden flex flex-col">
          <DialogHeader>
            <DialogTitle>{editingDependency ? 'Edit Constraint' : 'Create New Constraint'}</DialogTitle>
            <DialogDescription>
              Write SAP constraint code to define configuration rules.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4 flex-1 overflow-y-auto">
            <div className="space-y-2">
              <Label htmlFor="depName">Constraint Name</Label>
              <Input
                id="depName"
                placeholder="e.g., CONSTRAINT_001"
                value={newDependency.name}
                onChange={(e) => setNewDependency({ ...newDependency, name: e.target.value })}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="depDesc">Description</Label>
                <Input
                  id="depDesc"
                  placeholder="Enter a description..."
                  value={newDependency.description}
                  onChange={(e) => setNewDependency({ ...newDependency, description: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="depStatus">Status</Label>
                <Select
                  value={newDependency.status || 'Active'}
                  onValueChange={(value) => setNewDependency({ ...newDependency, status: value })}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Select status" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Active">Active</SelectItem>
                    <SelectItem value="Inactive">Inactive</SelectItem>
                    <SelectItem value="Locked">Locked</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="space-y-2">
              <Label>SAP Constraint Code</Label>
              <div className="border rounded-lg overflow-hidden" style={{ height: '350px' }}>
                <Editor
                  height="100%"
                  defaultLanguage="plaintext"
                  value={newDependency.sourceCode}
                  onChange={(value) =>
                    setNewDependency({ ...newDependency, sourceCode: value || '' })
                  }
                  theme="vs-dark"
                  options={{
                    minimap: { enabled: false },
                    fontSize: 14,
                    lineNumbers: 'on',
                    scrollBeyondLastLine: false,
                    automaticLayout: true,
                    tabSize: 4,
                    wordWrap: 'on',
                  }}
                />
              </div>
            </div>

            <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-4">
              <p className="text-sm font-medium text-blue-700 mb-2">Constraint Syntax:</p>
              <ul className="text-xs text-blue-600 space-y-1">
                <li>Sections: OBJECTS / CONDITION / RESTRICTIONS / INFERENCES</li>
                <li>Comments: Lines starting with *</li>
                <li>End with a period (.)</li>
              </ul>
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setOpenDepDialog(false);
                setEditingDependency(null);
                setNewDependency({
                  name: '',
                  type: 'Constraint',
                  sourceCode: SAMPLE_CONSTRAINT,
                  description: '',
                  status: 'Active',
                });
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleCreateDependency}
              disabled={!newDependency.name || !newDependency.sourceCode}
            >
              {editingDependency ? 'Update' : 'Create'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Create/Edit Procedure Dialog */}
      <Dialog
        open={openProcDialog}
        onOpenChange={(open) => {
          if (!open) {
            setOpenProcDialog(false);
            setEditingProcedure(null);
            setNewProcedure({
              name: '',
              sourceCode: SAMPLE_PROCEDURE,
              description: '',
              status: 'Active',
            });
          }
        }}
      >
        <DialogContent className="max-w-4xl max-h-[90vh] overflow-hidden flex flex-col">
          <DialogHeader>
            <DialogTitle>{editingProcedure ? 'Edit Procedure' : 'Create New Procedure'}</DialogTitle>
            <DialogDescription>
              Write SAP procedure code for imperative configuration logic.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4 flex-1 overflow-y-auto">
            <div className="space-y-2">
              <Label htmlFor="procName">Procedure Name</Label>
              <Input
                id="procName"
                placeholder="e.g., PROCEDURE_001"
                value={newProcedure.name}
                onChange={(e) => setNewProcedure({ ...newProcedure, name: e.target.value })}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="procDesc">Description</Label>
                <Input
                  id="procDesc"
                  placeholder="Enter a description..."
                  value={newProcedure.description}
                  onChange={(e) => setNewProcedure({ ...newProcedure, description: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="procStatus">Status</Label>
                <Select
                  value={newProcedure.status || 'Active'}
                  onValueChange={(value) => setNewProcedure({ ...newProcedure, status: value })}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Select status" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Active">Active</SelectItem>
                    <SelectItem value="Inactive">Inactive</SelectItem>
                    <SelectItem value="Locked">Locked</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="space-y-2">
              <Label>SAP Procedure Code</Label>
              <div className="border rounded-lg overflow-hidden" style={{ height: '350px' }}>
                <Editor
                  height="100%"
                  defaultLanguage="plaintext"
                  value={newProcedure.sourceCode}
                  onChange={(value) =>
                    setNewProcedure({ ...newProcedure, sourceCode: value || '' })
                  }
                  theme="vs-dark"
                  options={{
                    minimap: { enabled: false },
                    fontSize: 14,
                    lineNumbers: 'on',
                    scrollBeyondLastLine: false,
                    automaticLayout: true,
                    tabSize: 4,
                    wordWrap: 'on',
                  }}
                />
              </div>
            </div>

            <div className="rounded-lg border border-blue-500/50 bg-blue-500/10 p-4">
              <p className="text-sm font-medium text-blue-700 mb-2">Procedure Syntax:</p>
              <ul className="text-xs text-blue-600 space-y-1">
                <li>Imperative statements with $SELF, $PARENT, $ROOT references</li>
                <li>IF-THEN-ELSE conditional logic</li>
                <li>Comments: Lines starting with *</li>
                <li>End with a period (.)</li>
              </ul>
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setOpenProcDialog(false);
                setEditingProcedure(null);
                setNewProcedure({
                  name: '',
                  sourceCode: SAMPLE_PROCEDURE,
                  description: '',
                  status: 'Active',
                });
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleCreateProcedure}
              disabled={!newProcedure.name || !newProcedure.sourceCode}
            >
              {editingProcedure ? 'Update' : 'Create'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
