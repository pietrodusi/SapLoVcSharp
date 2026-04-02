import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import {
  ArrowLeft,
  Package,
  Layers,
  GitBranch,
  Play,
  Table2,
  Loader2,
  Home,
  ChevronRight,
  FileCode2,
  Grid3X3,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { materialApi } from '../../api/client';
import type { MaterialDetailDto } from '../../types/api';
import ClassificationTab from './ClassificationTab';
import DependenciesTab from './DependenciesTab';
import ConfigurationTab from './ConfigurationTab';
import VariantTablesTab from './VariantTablesTab';
import ModelcodeDefinitionTab from './ModelcodeDefinitionTab';
import ProductMatrixTab from './ProductMatrixTab';

export default function MaterialDetail() {
  const { materialNumber } = useParams<{ materialNumber: string }>();
  const navigate = useNavigate();
  const [material, setMaterial] = useState<MaterialDetailDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState('classification');

  useEffect(() => {
    if (materialNumber) {
      loadMaterial();
    }
  }, [materialNumber]);

  const loadMaterial = async () => {
    if (!materialNumber) return;

    try {
      setLoading(true);
      setError(null);
      const data = await materialApi.getById(materialNumber);
      setMaterial(data);
    } catch (err) {
      setError('Failed to load material details');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-80">
        <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
      </div>
    );
  }

  if (error || !material) {
    return (
      <div className="space-y-4">
        <Button variant="ghost" onClick={() => navigate('/')} className="gap-2 text-gray-600">
          <ArrowLeft className="h-4 w-4" />
          Back to Materials
        </Button>
        <div className="rounded-lg border border-red-200 bg-red-50 p-4">
          <p className="text-sm text-red-700">{error || 'Material not found'}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-5">
      {/* Breadcrumbs */}
      <nav className="flex items-center gap-2 text-sm">
        <Link to="/" className="flex items-center gap-1 text-gray-500 hover:text-gray-700 transition-colors">
          <Home className="h-3.5 w-3.5" />
          Materials
        </Link>
        <ChevronRight className="h-3.5 w-3.5 text-gray-400" />
        <span className="text-gray-900 font-medium flex items-center gap-1">
          <Package className="h-3.5 w-3.5" />
          {material.materialNumber}
        </span>
      </nav>

      {/* Header Card */}
      <Card className="bg-white border-gray-200">
        <CardContent className="p-5">
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
            <div className="flex items-center gap-3">
              <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-indigo-100 text-indigo-600">
                <Package className="h-6 w-6" />
              </div>
              <div>
                <div className="flex items-center gap-2.5">
                  <h1 className="text-xl font-bold text-gray-900">{material.materialNumber}</h1>
                  <Badge className={material.status === 'Active'
                    ? 'bg-emerald-100 text-emerald-700 border-emerald-200'
                    : 'bg-gray-100 text-gray-600 border-gray-200'
                  }>
                    {material.status}
                  </Badge>
                </div>
                <p className="text-gray-500 text-sm mt-0.5">
                  {material.description || 'No description'}
                </p>
              </div>
            </div>
            <Button variant="outline" onClick={() => navigate('/')} className="gap-2 h-9 text-sm border-gray-300">
              <ArrowLeft className="h-3.5 w-3.5" />
              Back
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList className="bg-white border border-gray-200 p-1 h-auto inline-flex">
          <TabsTrigger
            value="classification"
            className="gap-1.5 px-3 py-2 text-sm data-[state=active]:bg-indigo-50 data-[state=active]:text-indigo-700"
          >
            <Layers className="h-4 w-4" />
            <span className="hidden sm:inline">Classification</span>
          </TabsTrigger>
          <TabsTrigger
            value="variant-tables"
            className="gap-1.5 px-3 py-2 text-sm data-[state=active]:bg-indigo-50 data-[state=active]:text-indigo-700"
          >
            <Table2 className="h-4 w-4" />
            <span className="hidden sm:inline">Variant Tables</span>
          </TabsTrigger>
          <TabsTrigger
            value="dependencies"
            className="gap-1.5 px-3 py-2 text-sm data-[state=active]:bg-indigo-50 data-[state=active]:text-indigo-700"
          >
            <GitBranch className="h-4 w-4" />
            <span className="hidden sm:inline">Dependencies</span>
          </TabsTrigger>
          <TabsTrigger
            value="configuration"
            className="gap-1.5 px-3 py-2 text-sm data-[state=active]:bg-indigo-50 data-[state=active]:text-indigo-700"
          >
            <Play className="h-4 w-4" />
            <span className="hidden sm:inline">Configuration</span>
          </TabsTrigger>
          <TabsTrigger
            value="modelcode"
            className="gap-1.5 px-3 py-2 text-sm data-[state=active]:bg-indigo-50 data-[state=active]:text-indigo-700"
          >
            <FileCode2 className="h-4 w-4" />
            <span className="hidden sm:inline">Modelcode</span>
          </TabsTrigger>
          <TabsTrigger
            value="product-matrix"
            className="gap-1.5 px-3 py-2 text-sm data-[state=active]:bg-indigo-50 data-[state=active]:text-indigo-700"
          >
            <Grid3X3 className="h-4 w-4" />
            <span className="hidden sm:inline">Product Matrix</span>
          </TabsTrigger>
        </TabsList>

        <TabsContent value="classification" className="mt-4">
          <ClassificationTab material={material} onUpdate={loadMaterial} />
        </TabsContent>
        <TabsContent value="variant-tables" className="mt-4">
          <VariantTablesTab material={material} />
        </TabsContent>
        <TabsContent value="dependencies" className="mt-4">
          <DependenciesTab material={material} />
        </TabsContent>
        <TabsContent value="configuration" className="mt-4">
          <ConfigurationTab material={material} />
        </TabsContent>
        <TabsContent value="modelcode" className="mt-4">
          <ModelcodeDefinitionTab material={material} onUpdate={loadMaterial} />
        </TabsContent>
        <TabsContent value="product-matrix" className="mt-4">
          <ProductMatrixTab material={material} />
        </TabsContent>
      </Tabs>
    </div>
  );
}
