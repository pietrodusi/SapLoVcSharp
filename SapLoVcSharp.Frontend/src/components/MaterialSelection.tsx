import { useState, useEffect, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, Trash2, Search, Package, ArrowRight, Loader2, FolderOpen } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { cn } from '@/lib/utils';
import { materialApi, configurationProfileApi } from '../api/client';
import type { MaterialDto, CreateMaterialRequest } from '../types/api';

export default function MaterialSelection() {
  const navigate = useNavigate();
  const [materials, setMaterials] = useState<MaterialDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [openDialog, setOpenDialog] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [newMaterial, setNewMaterial] = useState<CreateMaterialRequest>({
    materialNumber: '',
    description: '',
    status: 'Active',
  });

  useEffect(() => {
    loadMaterials();
  }, []);

  const loadMaterials = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await materialApi.getAll();
      setMaterials(data);
    } catch (err) {
      setError('Failed to load materials. Make sure the API is running.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const filteredMaterials = useMemo(() => {
    if (!searchQuery.trim()) return materials;
    const query = searchQuery.toLowerCase();
    return materials.filter(
      (m) =>
        m.materialNumber.toLowerCase().includes(query) ||
        m.description?.toLowerCase().includes(query) ||
        m.classes.some((c) => c.toLowerCase().includes(query))
    );
  }, [materials, searchQuery]);

  const handleCreateMaterial = async () => {
    try {
      setError(null);
      await materialApi.create(newMaterial);
      try {
        await configurationProfileApi.create({
          materialNumber: newMaterial.materialNumber,
          name: `${newMaterial.materialNumber}_Profile`,
          description: `Configuration profile for ${newMaterial.materialNumber}`,
        });
      } catch (profileErr: unknown) {
        const err = profileErr as { response?: { data?: { error?: string } }; message?: string };
        console.error('Failed to create configuration profile:', profileErr);
        setError(`Material created but failed to create configuration profile: ${err.response?.data?.error || err.message}`);
        return;
      }
      setOpenDialog(false);
      setNewMaterial({ materialNumber: '', description: '', status: 'Active' });
      await loadMaterials();
    } catch (err: unknown) {
      const error = err as { response?: { data?: { error?: string } }; message?: string };
      setError(`Failed to create material: ${error.response?.data?.error || error.message}`);
      console.error(err);
    }
  };

  const handleDeleteMaterial = async (event: React.MouseEvent, materialNumber: string) => {
    event.stopPropagation();
    if (!confirm(`Are you sure you want to delete material ${materialNumber}?`)) {
      return;
    }
    try {
      setError(null);
      await materialApi.delete(materialNumber);
      await loadMaterials();
    } catch (err) {
      setError('Failed to delete material');
      console.error(err);
    }
  };

  const handleSelectMaterial = (materialNumber: string) => {
    navigate(`/material/${materialNumber}`);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-96">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Materials</h1>
          <p className="text-muted-foreground mt-1">
            Select a material to configure or create a new one
          </p>
        </div>
        <Button onClick={() => setOpenDialog(true)} className="gap-2">
          <Plus className="h-4 w-4" />
          Create Material
        </Button>
      </div>

      {/* Search */}
      <div className="relative max-w-md">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          placeholder="Search by material number, description, or class..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="pl-10"
        />
      </div>

      {/* Error Alert */}
      {error && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-destructive">
          <p className="text-sm">{error}</p>
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

      {/* Materials Grid */}
      {filteredMaterials.length > 0 ? (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {filteredMaterials.map((material) => (
            <Card
              key={material.materialNumber}
              className={cn(
                "group cursor-pointer transition-all duration-200 hover:shadow-md hover:border-primary/50",
                "relative overflow-hidden"
              )}
              onClick={() => handleSelectMaterial(material.materialNumber)}
            >
              <CardHeader className="pb-3">
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
                      <Package className="h-5 w-5" />
                    </div>
                    <CardTitle className="text-base">{material.materialNumber}</CardTitle>
                  </div>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 opacity-0 group-hover:opacity-100 transition-opacity hover:bg-destructive hover:text-destructive-foreground"
                    onClick={(e) => handleDeleteMaterial(e, material.materialNumber)}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <CardDescription className="line-clamp-2 min-h-[2.5rem]">
                  {material.description || 'No description'}
                </CardDescription>

                <div className="flex flex-wrap gap-2">
                  <Badge variant={material.status === 'Active' ? 'success' : 'secondary'}>
                    {material.status}
                  </Badge>
                  {material.classes.slice(0, 2).map((cls) => (
                    <Badge key={cls} variant="outline" className="font-normal">
                      {cls}
                    </Badge>
                  ))}
                  {material.classes.length > 2 && (
                    <Badge variant="secondary" className="font-normal">
                      +{material.classes.length - 2}
                    </Badge>
                  )}
                </div>

                <div className="flex items-center justify-end pt-2 border-t">
                  <span className="text-sm text-muted-foreground group-hover:text-primary transition-colors flex items-center gap-1">
                    Configure
                    <ArrowRight className="h-4 w-4" />
                  </span>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : searchQuery ? (
        <Card className="py-16">
          <div className="flex flex-col items-center justify-center text-center">
            <Search className="h-12 w-12 text-muted-foreground/50 mb-4" />
            <h3 className="text-lg font-semibold">No materials found</h3>
            <p className="text-muted-foreground mt-1">
              No materials found matching "{searchQuery}"
            </p>
          </div>
        </Card>
      ) : (
        <Card className="py-16">
          <div className="flex flex-col items-center justify-center text-center">
            <FolderOpen className="h-12 w-12 text-muted-foreground/50 mb-4" />
            <h3 className="text-lg font-semibold">No materials yet</h3>
            <p className="text-muted-foreground mt-1">
              Create your first material to get started
            </p>
            <Button onClick={() => setOpenDialog(true)} className="mt-4 gap-2">
              <Plus className="h-4 w-4" />
              Create Material
            </Button>
          </div>
        </Card>
      )}

      {/* Create Material Dialog */}
      <Dialog open={openDialog} onOpenChange={setOpenDialog}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Create New Material</DialogTitle>
            <DialogDescription>
              A configuration profile will be automatically created for this material.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="materialNumber">Material Number</Label>
              <Input
                id="materialNumber"
                placeholder="e.g., MAT-001"
                value={newMaterial.materialNumber}
                onChange={(e) => setNewMaterial({ ...newMaterial, materialNumber: e.target.value })}
              />
              <p className="text-xs text-muted-foreground">Unique identifier for the material</p>
            </div>
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Input
                id="description"
                placeholder="Enter a description..."
                value={newMaterial.description}
                onChange={(e) => setNewMaterial({ ...newMaterial, description: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="status">Status</Label>
              <Input
                id="status"
                value={newMaterial.status}
                onChange={(e) => setNewMaterial({ ...newMaterial, status: e.target.value })}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setOpenDialog(false)}>
              Cancel
            </Button>
            <Button onClick={handleCreateMaterial} disabled={!newMaterial.materialNumber}>
              Create Material
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
