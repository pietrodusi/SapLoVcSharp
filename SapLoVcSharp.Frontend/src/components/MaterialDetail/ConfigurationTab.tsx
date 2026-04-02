import { useState, useEffect, useMemo } from 'react';
import {
  CheckCircle2,
  AlertTriangle,
  XCircle,
  User,
  Play,
  Link2,
  Lightbulb,
  Ban,
  RefreshCw,
  Activity,
  EyeOff,
  Loader2,
  Settings2,
  ListTree,
  Table2,
} from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { ScrollArea } from '@/components/ui/scroll-area';
import { cn } from '@/lib/utils';
import { configurationApi } from '../../api/client';
import type { MaterialDetailDto, ConfigurationResultDto } from '../../types/api';

interface ConfigurationTabProps {
  material: MaterialDetailDto;
}

const UNSET_VALUE = '__UNSET__';

const actionColors = {
  UserInput: { main: 'text-indigo-500', bg: 'bg-indigo-500/10', border: 'border-l-indigo-500' },
  Procedure: { main: 'text-emerald-500', bg: 'bg-emerald-500/10', border: 'border-l-emerald-500' },
  Constraint: { main: 'text-amber-500', bg: 'bg-amber-500/10', border: 'border-l-amber-500' },
  Inference: { main: 'text-violet-500', bg: 'bg-violet-500/10', border: 'border-l-violet-500' },
  Restriction: { main: 'text-red-500', bg: 'bg-red-500/10', border: 'border-l-red-500' },
};

const actionBadgeColors = {
  UserInput: 'bg-indigo-500',
  Procedure: 'bg-emerald-500',
  Constraint: 'bg-amber-500',
  Inference: 'bg-violet-500',
  Restriction: 'bg-red-500',
};

export default function ConfigurationTab({ material }: ConfigurationTabProps) {
  const [displayValues, setDisplayValues] = useState<Record<string, string>>({});
  const [userValues, setUserValues] = useState<Record<string, string>>({});
  const [allowedValues, setAllowedValues] = useState<Record<string, string[]>>({});
  const [executing, setExecuting] = useState(false);
  const [result, setResult] = useState<ConfigurationResultDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [showExecutionLog, setShowExecutionLog] = useState(true);
  const [showHidden, setShowHidden] = useState(false);

  const { visibleCharacteristics, hiddenCharacteristics } = useMemo(() => {
    const visible = material.characteristics.filter(c => !c.isHidden);
    const hidden = material.characteristics.filter(c => c.isHidden);
    return { visibleCharacteristics: visible, hiddenCharacteristics: hidden };
  }, [material.characteristics]);

  // Track characteristics for change detection
  const characteristicsKey = useMemo(() => {
    return material.characteristics.map(c => `${c.name}:${c.allowedValues.length}`).join(',');
  }, [material.characteristics]);

  useEffect(() => {
    const initialAllowed: Record<string, string[]> = {};
    material.characteristics.forEach((char) => {
      initialAllowed[char.name] = char.allowedValues.map((v) => v.value);
    });
    setAllowedValues(initialAllowed);

    // Reset user values that no longer exist in characteristics
    setUserValues(prev => {
      const charNames = new Set(material.characteristics.map(c => c.name));
      return Object.fromEntries(
        Object.entries(prev).filter(([k]) => charNames.has(k))
      );
    });
    setDisplayValues(prev => {
      const charNames = new Set(material.characteristics.map(c => c.name));
      return Object.fromEntries(
        Object.entries(prev).filter(([k]) => charNames.has(k))
      );
    });
  }, [material.characteristics]);

  // Re-execute configuration when characteristics change
  useEffect(() => {
    executeConfiguration({});
  }, [characteristicsKey]); // eslint-disable-line react-hooks/exhaustive-deps

  const handleValueChange = async (charName: string, value: string) => {
    const actualValue = value === UNSET_VALUE ? '' : value;
    const newUserValues = actualValue
      ? { ...userValues, [charName]: actualValue }
      : Object.fromEntries(Object.entries(userValues).filter(([k]) => k !== charName));

    setUserValues(newUserValues);

    if (actualValue) {
      setDisplayValues(prev => ({ ...prev, [charName]: actualValue }));
    } else {
      setDisplayValues(prev => {
        const newDisplay = { ...prev };
        delete newDisplay[charName];
        return newDisplay;
      });
    }

    await executeConfiguration(newUserValues);
  };

  const handleReset = async () => {
    setUserValues({});
    setDisplayValues({});
    const initialAllowed: Record<string, string[]> = {};
    material.characteristics.forEach((char) => {
      initialAllowed[char.name] = char.allowedValues.map((v) => v.value);
    });
    setAllowedValues(initialAllowed);
    await executeConfiguration({});
  };

  const executeConfiguration = async (userSelectedValues: Record<string, string>) => {
    try {
      setExecuting(true);
      setError(null);
      const configResult = await configurationApi.execute(material.materialNumber, userSelectedValues);
      setResult(configResult);

      if (configResult.success && configResult.finalValues) {
        const newDisplayValues: Record<string, string> = {};
        Object.entries(configResult.finalValues).forEach(([key, value]) => {
          if (value !== null && value !== undefined && value !== '') {
            newDisplayValues[key] = String(value);
          }
        });
        setDisplayValues(newDisplayValues);
      }

      if (configResult.success && configResult.allowedValues) {
        setAllowedValues(configResult.allowedValues);
      }
    } catch (err) {
      setError('Failed to execute configuration');
      console.error(err);
    } finally {
      setExecuting(false);
    }
  };

  const getStatusIcon = () => {
    if (!result) return null;
    if (!result.success) return <XCircle className="h-5 w-5 text-red-500" />;
    if (!result.isComplete || !result.isStable) return <AlertTriangle className="h-5 w-5 text-amber-500" />;
    return <CheckCircle2 className="h-5 w-5 text-emerald-500" />;
  };

  const getActionIcon = (actionType: string) => {
    const colorClass = actionColors[actionType as keyof typeof actionColors]?.main || 'text-gray-500';
    const iconProps = { className: cn('h-4 w-4', colorClass) };
    switch (actionType) {
      case 'UserInput': return <User {...iconProps} />;
      case 'Procedure': return <Play {...iconProps} />;
      case 'Constraint': return <Link2 {...iconProps} />;
      case 'Inference': return <Lightbulb {...iconProps} />;
      case 'Restriction': return <Ban {...iconProps} />;
      default: return null;
    }
  };

  const groupLogByCycle = (log: any[]) => {
    const grouped = new Map<number, any[]>();
    log.forEach(entry => {
      if (!grouped.has(entry.cycle)) {
        grouped.set(entry.cycle, []);
      }
      grouped.get(entry.cycle)!.push(entry);
    });
    return Array.from(grouped.entries()).sort((a, b) => a[0] - b[0]);
  };

  const renderCharacteristicSelect = (char: any, isHiddenSection = false) => {
    const isRestricted = (allowedValues[char.name]?.length || 0) === 1;
    const isDisabled = executing || (allowedValues[char.name]?.length || 0) === 0;
    const availableValues = allowedValues[char.name] || char.allowedValues.map((v: any) => v.value);
    const currentValue = displayValues[char.name] || UNSET_VALUE;

    return (
      <div key={char.name} className={cn('space-y-2', isHiddenSection && 'opacity-60')}>
        <div className="space-y-1.5">
          <Label className="text-sm font-medium flex items-center gap-2">
            {char.name}
            {char.isRequired && <span className="text-amber-500">*</span>}
          </Label>
          <Select
            value={currentValue}
            onValueChange={(value) => handleValueChange(char.name, value)}
            disabled={isDisabled}
          >
            <SelectTrigger className="w-full h-10">
              <SelectValue placeholder="Not selected" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={UNSET_VALUE}>
                <span className="text-gray-400">Not selected</span>
              </SelectItem>
              {availableValues.map((value: string) => (
                <SelectItem key={value} value={value}>
                  {value}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="flex flex-wrap gap-1.5">
          {isHiddenSection && (
            <Badge variant="secondary" className="text-[10px] h-5 gap-1">
              <EyeOff className="h-3 w-3" />
              Hidden
            </Badge>
          )}
          {isRestricted && (
            <Badge className="text-[10px] h-5 bg-red-100 text-red-700 border-red-200">
              Restricted
            </Badge>
          )}
          {userValues[char.name] && (
            <Badge className="text-[10px] h-5 bg-indigo-100 text-indigo-700 border-indigo-200">
              User set
            </Badge>
          )}
        </div>
      </div>
    );
  };

  return (
    <div className="space-y-4">
      {error && (
        <div className="rounded-lg border border-red-300 bg-red-50 p-4">
          <p className="text-sm text-red-700">{error}</p>
          <Button
            variant="ghost"
            size="sm"
            className="mt-2 h-auto p-0 text-red-700 hover:text-red-800"
            onClick={() => setError(null)}
          >
            Dismiss
          </Button>
        </div>
      )}

      {!material.hasConfigurationProfile && (
        <div className="rounded-lg border border-amber-300 bg-amber-50 p-4">
          <p className="text-sm text-amber-700">
            This material does not have a configuration profile. Configuration execution may not work as expected.
          </p>
        </div>
      )}

      {material.characteristics.length === 0 ? (
        <div className="rounded-lg border border-blue-300 bg-blue-50 p-8 text-center">
          <Settings2 className="h-12 w-12 text-blue-400 mx-auto mb-4" />
          <h3 className="text-lg font-semibold text-blue-800 mb-2">No Characteristics</h3>
          <p className="text-sm text-blue-600">
            Add characteristics in the Classification tab to start configuring.
          </p>
        </div>
      ) : (
        <div className={cn(
          "grid grid-cols-1 gap-6",
          showExecutionLog ? "lg:grid-cols-5" : "lg:grid-cols-1"
        )}>
          {/* Left Panel - Configuration Input */}
          <div className={showExecutionLog ? "lg:col-span-2" : "lg:col-span-1"}>
            <Card>
              <CardHeader className="pb-4">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-base font-semibold">Configure Values</CardTitle>
                  <div className="flex items-center gap-3">
                    <div className="flex items-center gap-2">
                      <ListTree className="h-4 w-4 text-gray-400" />
                      <Label htmlFor="show-log" className="text-xs text-gray-500">
                        Log
                      </Label>
                      <Switch
                        id="show-log"
                        checked={showExecutionLog}
                        onCheckedChange={setShowExecutionLog}
                      />
                    </div>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={handleReset}
                      disabled={executing}
                      className="h-8 gap-1.5 text-xs"
                    >
                      <RefreshCw className="h-3.5 w-3.5" />
                      Reset
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-5">
                {/* Show hidden toggle */}
                {hiddenCharacteristics.length > 0 && (
                  <div className="flex items-center justify-between py-2 px-3 bg-gray-50 rounded-lg">
                    <div className="flex items-center gap-2">
                      <EyeOff className="h-4 w-4 text-gray-400" />
                      <Label htmlFor="show-hidden" className="text-sm text-gray-600">
                        Show hidden ({hiddenCharacteristics.length})
                      </Label>
                    </div>
                    <Switch
                      id="show-hidden"
                      checked={showHidden}
                      onCheckedChange={setShowHidden}
                    />
                  </div>
                )}

                {/* Visible Characteristics */}
                <div className="space-y-4">
                  {visibleCharacteristics.map((char) => renderCharacteristicSelect(char))}
                </div>

                {/* Hidden Characteristics */}
                {showHidden && hiddenCharacteristics.length > 0 && (
                  <div className="border-t pt-4 mt-4">
                    <div className="flex items-center gap-2 mb-4">
                      <EyeOff className="h-4 w-4 text-gray-400" />
                      <span className="text-sm font-medium text-gray-500">
                        Hidden Characteristics
                      </span>
                    </div>
                    <div className="space-y-4">
                      {hiddenCharacteristics.map((char) => renderCharacteristicSelect(char, true))}
                    </div>
                  </div>
                )}

                {executing && (
                  <div className="flex items-center justify-center py-4 bg-gray-50 rounded-lg">
                    <Loader2 className="h-5 w-5 animate-spin text-gray-400 mr-2" />
                    <span className="text-sm text-gray-500">Executing...</span>
                  </div>
                )}
              </CardContent>
            </Card>
          </div>

          {/* Right Panel - Results */}
          {showExecutionLog && (
            <div className="lg:col-span-3 flex flex-col min-h-[calc(100vh-320px)]">
              {result && !executing ? (
                <Card className="flex-1 flex flex-col">
                  <CardHeader className="pb-3 shrink-0">
                    <div className="flex items-center gap-2">
                      {getStatusIcon()}
                      <CardTitle className="text-base font-semibold">Execution Result</CardTitle>
                      <Badge className={cn(
                        'text-xs',
                        result.success
                          ? 'bg-emerald-100 text-emerald-700 border-emerald-200'
                          : 'bg-red-100 text-red-700 border-red-200'
                      )}>
                        {result.success ? 'Success' : 'Failed'}
                      </Badge>
                    </div>
                  </CardHeader>

                  <CardContent className="flex-1 flex flex-col space-y-4 pt-0 overflow-hidden">
                    {/* Status Chips */}
                    <div className="flex flex-wrap gap-2">
                      <Badge
                        variant="outline"
                        className={cn(
                          'text-xs',
                          result.isComplete
                            ? 'border-emerald-300 text-emerald-700 bg-emerald-50'
                            : 'border-amber-300 text-amber-700 bg-amber-50'
                        )}
                      >
                        {result.isComplete ? 'Complete' : 'Incomplete'}
                      </Badge>
                      <Badge
                        variant="outline"
                        className={cn(
                          'text-xs',
                          result.isStable
                            ? 'border-emerald-300 text-emerald-700 bg-emerald-50'
                            : 'border-amber-300 text-amber-700 bg-amber-50'
                        )}
                      >
                        {result.isStable ? 'Stable' : 'Unstable'}
                      </Badge>
                      <Badge variant="outline" className="text-xs gap-1">
                        <Activity className="h-3 w-3" />
                        {result.cyclesExecuted} cycles
                      </Badge>
                      <Badge variant="outline" className="text-xs">
                        {result.durationMs}ms
                      </Badge>
                    </div>

                    {/* Dependencies with Tables */}
                    {result.dependencyExecutions && result.dependencyExecutions.some(d => d.tablesUsed.length > 0) && (
                      <div className="border-t pt-4 shrink-0">
                        <h4 className="text-sm font-semibold mb-2">Dependencies Using Tables</h4>
                        <div className="space-y-1.5">
                          {result.dependencyExecutions
                            .filter(dep => dep.tablesUsed.length > 0)
                            .map((dep, index) => (
                              <div
                                key={index}
                                className={cn(
                                  'flex items-center gap-2 p-2 rounded-md text-xs',
                                  dep.dependencyType === 'Procedure'
                                    ? 'bg-emerald-50 border-l-2 border-l-emerald-500'
                                    : 'bg-amber-50 border-l-2 border-l-amber-500'
                                )}
                              >
                                <Badge
                                  variant="outline"
                                  className={cn(
                                    'text-[10px] h-4 px-1.5',
                                    dep.dependencyType === 'Procedure'
                                      ? 'border-emerald-300 text-emerald-700'
                                      : 'border-amber-300 text-amber-700'
                                  )}
                                >
                                  {dep.dependencyType}
                                </Badge>
                                <span className="font-medium text-gray-700">{dep.dependencyName}</span>
                                <span className="text-gray-400">→</span>
                                <div className="flex items-center gap-1">
                                  <Table2 className="h-3 w-3 text-blue-500" />
                                  {dep.tablesUsed.map((table, tIdx) => (
                                    <code
                                      key={tIdx}
                                      className="text-[10px] bg-blue-100 text-blue-700 px-1 py-0.5 rounded font-mono"
                                    >
                                      {table}
                                    </code>
                                  ))}
                                </div>
                              </div>
                            ))}
                        </div>
                      </div>
                    )}

                    <div className="border-t pt-4 flex-1 flex flex-col min-h-0">
                      <h4 className="text-sm font-semibold mb-3 shrink-0">Execution Log</h4>
                      <ScrollArea className="flex-1">
                        {result.executionLog && result.executionLog.length > 0 ? (
                          <div className="space-y-3 pr-4">
                            {groupLogByCycle(result.executionLog).map(([cycle, entries]) => (
                              <div key={cycle}>
                                <div className="flex items-center gap-3 mb-2">
                                  <div className="flex-1 h-px bg-gray-200" />
                                  <Badge variant="secondary" className="text-[10px] h-5">
                                    Cycle {cycle}
                                  </Badge>
                                  <div className="flex-1 h-px bg-gray-200" />
                                </div>
                                <div className="space-y-2">
                                  {entries.map((entry, index) => {
                                    const colors = actionColors[entry.actionType as keyof typeof actionColors] || {
                                      main: 'text-gray-500',
                                      bg: 'bg-gray-50',
                                      border: 'border-l-gray-300'
                                    };
                                    const badgeColor = actionBadgeColors[entry.actionType as keyof typeof actionBadgeColors] || 'bg-gray-500';
                                    return (
                                      <div
                                        key={index}
                                        className={cn(
                                          'p-2.5 rounded-md border-l-[3px]',
                                          colors.bg,
                                          colors.border
                                        )}
                                      >
                                        <div className="flex items-start gap-2.5">
                                          <div className="mt-0.5 shrink-0">
                                            {getActionIcon(entry.actionType)}
                                          </div>
                                          <div className="flex-1 min-w-0">
                                            <div className="flex items-center gap-1.5 flex-wrap mb-1">
                                              <Badge className={cn('text-[10px] h-4 px-1.5 text-white', badgeColor)}>
                                                {entry.actionType}
                                              </Badge>
                                              <span className="text-[10px] text-gray-400">
                                                {entry.source}
                                              </span>
                                            </div>
                                            <p className="text-xs text-gray-700 mb-1">{entry.description}</p>
                                            <div className="flex items-center gap-1.5 flex-wrap">
                                              <code className="text-[10px] bg-gray-200 px-1 py-0.5 rounded font-mono font-medium">
                                                {entry.characteristicName}
                                              </code>
                                              <span className="text-[10px] text-gray-500">
                                                {entry.actionType === 'Restriction'
                                                  ? `→ [${entry.newValue}]`
                                                  : `${entry.oldValue !== null ? `"${entry.oldValue}"` : '∅'} → ${entry.newValue !== null ? `"${entry.newValue}"` : '∅'}`
                                                }
                                              </span>
                                            </div>
                                          </div>
                                        </div>
                                      </div>
                                    );
                                  })}
                                </div>
                              </div>
                            ))}
                          </div>
                        ) : (
                          <div className="rounded-lg border border-blue-200 bg-blue-50 p-4 text-center">
                            <p className="text-sm text-blue-600">
                              No actions logged. Select values to see the execution log.
                            </p>
                          </div>
                        )}
                      </ScrollArea>
                    </div>

                    {result.errors && result.errors.length > 0 && (
                    <div className="border-t pt-4 shrink-0">
                      <h4 className="text-sm font-semibold text-red-700 mb-2">Errors</h4>
                      <div className="space-y-2">
                        {result.errors.map((err, index) => (
                          <div
                            key={index}
                            className="rounded-md border border-red-200 bg-red-50 p-2.5"
                          >
                            <p className="text-xs text-red-700">{err}</p>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>
            ) : executing ? (
              <Card>
                <CardContent className="py-12 flex flex-col items-center justify-center">
                  <Loader2 className="h-8 w-8 animate-spin text-gray-400 mb-3" />
                  <p className="text-sm text-gray-500">Running configuration...</p>
                </CardContent>
              </Card>
            ) : (
              <Card>
                <CardContent className="py-12 flex flex-col items-center justify-center">
                  <Settings2 className="h-10 w-10 text-gray-300 mb-3" />
                  <p className="text-sm text-gray-500">Select values to see execution results</p>
                </CardContent>
              </Card>
            )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
