using Microsoft.EntityFrameworkCore;
using SapLoVcSharp.Data.Models;
using SapLoVcSharp.Data.Serialization;
using SapLoVcSharp.Data.Sqlite.Contexts;
using SapLoVcSharp.Execution;
using SapLoVcSharp.Execution.Compilation;
using System.Diagnostics;
using System.Text.Json;

namespace SapLoVcSharp.Data.Sqlite.Services
{
    /// <summary>
    /// High-level service for executing configuration dependencies.
    /// Orchestrates the entire dependency execution process.
    /// </summary>
    public class ExecutionService
    {
        private readonly SqliteDbContext _dbContext;
        private readonly IInstructionCompiler _compiler;
        private readonly AstSerializer _astSerializer;

        public ExecutionService(SqliteDbContext dbContext)
        {
            _dbContext = dbContext;
            _compiler = new InstructionCompiler();
            _astSerializer = new AstSerializer();
        }

        /// <summary>
        /// Execute all dependencies for a material configuration
        /// </summary>
        public async Task<ConfigurationResult> ExecuteConfigurationAsync(
            string materialNumber,
            Dictionary<string, object?>? initialValues = null)
        {
            var sw = Stopwatch.StartNew();
            var result = new ConfigurationResult
            {
                MaterialNumber = materialNumber,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // Get initial allowed values for all characteristics (before execution)
                result.InitialAllowedValues = await GetAllCharacteristicsWithValuesAsync(materialNumber);

                // Load material with configuration profile
                var material = await LoadMaterialAsync(materialNumber);
                if (material?.ConfigurationProfile == null)
                {
                    throw new InvalidOperationException(
                        $"Material {materialNumber} has no configuration profile");
                }

                // Create configuration context
                var configContext = new ConfigurationContext(_dbContext, materialNumber);
                await configContext.InitializeWithDefaultsAsync();

                // Apply initial values if provided
                if (initialValues != null)
                {
                    foreach (var (name, value) in initialValues)
                    {
                        // Get old value to check if it actually changed
                        var oldValue = await configContext.GetValueAsync(name);

                        // Only log user input if the value actually changed
                        var oldStr = oldValue?.ToString();
                        var newStr = value?.ToString();
                        if (oldStr != newStr)
                        {
                            configContext.LogUserInput(name, oldValue, value);
                        }

                        // Set value without logging (we already logged above if needed)
                        await configContext.SetValueAsync(name, value, skipLogging: true);
                    }
                }

                // Create constraint context and table resolver
                var constraintContext = new ConstraintContext();
                var tableResolver = new TableResolver(_dbContext);

                // Create virtual machine
                var vm = new VirtualMachine(configContext, tableResolver, constraintContext);

                // Execute configuration cycle until stable or max iterations
                await ExecuteConfigurationCycleAsync(
                    vm,
                    material.ConfigurationProfile,
                    configContext,
                    result);

                // Get final configuration values
                result.FinalValues = await configContext.GetAllValuesAsync();
                result.MissingRequired = await configContext.GetMissingRequiredCharacteristicsAsync();

                // Copy action logs from context
                result.ActionLogs = configContext.ActionLogs.ToList();

                // Get value restrictions
                var restrictedChars = await configContext.GetRestrictedCharacteristicsAsync();
                foreach (var charName in restrictedChars)
                {
                    var availableValues = await configContext.GetAvailableValuesAsync(charName);
                    if (availableValues != null)
                    {
                        result.ValueRestrictions[charName] = availableValues;
                    }
                }

                // Build final allowed values (combining initial values with restrictions)
                foreach (var (charName, initialVals) in result.InitialAllowedValues)
                {
                    // If restricted, use restricted values; otherwise use initial values
                    if (result.ValueRestrictions.TryGetValue(charName, out var restrictedValues))
                    {
                        result.FinalAllowedValues[charName] = restrictedValues;
                    }
                    else
                    {
                        result.FinalAllowedValues[charName] = initialVals;
                    }
                }

                result.Success = true;
                result.IsComplete = result.MissingRequired.Count == 0;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                result.ErrorDetails = ex.ToString();
            }
            finally
            {
                sw.Stop();
                result.EndTime = DateTime.UtcNow;
                result.DurationMs = sw.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// Execute configuration dependencies in cycles until stable or max iterations.
        /// Each cycle executes all procedures, then all constraints, then checks for changes.
        /// </summary>
        private async Task ExecuteConfigurationCycleAsync(
            VirtualMachine vm,
            ConfigurationProfileEntity profile,
            ConfigurationContext configContext,
            ConfigurationResult result)
        {
            var cycleState = new Execution.Contexts.CycleState
            {
                MaxCycles = 20 // Default max cycles
            };

            // Main cycle loop
            while (!cycleState.IsStable && !cycleState.MaxCyclesExceeded)
            {
                var cycleSw = Stopwatch.StartNew();
                cycleState.BeginCycle();

                // Take snapshot before cycle
                var snapshotBefore = configContext.CreateSnapshot();

                var cycleMetrics = new CycleMetrics
                {
                    CycleNumber = cycleState.CurrentCycle
                };

                // Execute all procedures (in sequence)
                var procedures = profile.Procedures
                    .Where(p => p.Dependency!.Status == "Active")
                    .OrderBy(p => p.SequenceNumber).ToList();
                foreach (var procedure in procedures)
                {
                    // Set current dependency context for logging
                    configContext.CurrentCycle = cycleState.CurrentCycle;
                    configContext.CurrentDependencyName = procedure.Dependency!.Name;
                    configContext.CurrentDependencyType = "Procedure";

                    await ExecuteDependencyAsync(vm, procedure.Dependency!, result);
                    cycleState.RecordDependencyExecution();
                    cycleMetrics.ProceduresExecuted++;
                }

                // Execute all dependency nets (constraints in nets, in sequence)
                foreach (var net in profile.DependencyNets.Where(n => n.Status == "Active"))
                {
                    foreach (var constraint in net.Constraints
                        .Where(c => c.Dependency!.Status == "Active")
                        .OrderBy(c => c.SequenceNumber))
                    {
                        // Set current dependency context for logging
                        configContext.CurrentCycle = cycleState.CurrentCycle;
                        configContext.CurrentDependencyName = constraint.Dependency!.Name;
                        configContext.CurrentDependencyType = "Constraint";

                        await ExecuteDependencyAsync(vm, constraint.Dependency!, result);
                        cycleState.RecordDependencyExecution();
                        cycleMetrics.ConstraintsExecuted++;
                    }
                }

                cycleMetrics.TotalDependenciesExecuted = cycleMetrics.ProceduresExecuted + cycleMetrics.ConstraintsExecuted;

                // Detect changes
                var changedChars = configContext.DetectChanges(snapshotBefore);
                foreach (var charName in changedChars)
                {
                    cycleState.RecordChange(charName);
                }

                cycleMetrics.ChangedCharacteristics = new HashSet<string>(changedChars);

                // Check stability
                cycleState.CheckStability();
                cycleMetrics.IsStable = cycleState.IsStable;

                cycleSw.Stop();
                cycleMetrics.DurationMs = cycleSw.ElapsedMilliseconds;

                result.CycleDetails.Add(cycleMetrics);

                // Debug output
                Console.WriteLine($"[CYCLE {cycleState.CurrentCycle}] " +
                    $"Executed {cycleMetrics.TotalDependenciesExecuted} dependencies, " +
                    $"{changedChars.Count} changes, " +
                    $"stable={cycleState.IsStable}");
            }

            // Record final cycle state
            result.CyclesExecuted = cycleState.CurrentCycle;
            result.IsStable = cycleState.IsStable;
            result.ChangedCharacteristics = cycleState.AllChangedCharacteristics;

            // Check for max cycles exceeded
            if (cycleState.MaxCyclesExceeded)
            {
                result.Success = false;
                result.Error = $"Configuration exceeded maximum cycles ({cycleState.MaxCycles}) without reaching stability";
            }
        }

        private async Task<MaterialEntity?> LoadMaterialAsync(string materialNumber)
        {
            return await _dbContext.Materials
                .Include(m => m.ConfigurationProfile)
                    .ThenInclude(p => p!.Procedures.OrderBy(proc => proc.SequenceNumber))
                        .ThenInclude(p => p.Dependency)
                .Include(m => m.ConfigurationProfile)
                    .ThenInclude(p => p!.DependencyNets)
                        .ThenInclude(n => n.Constraints)
                            .ThenInclude(c => c.Dependency)
                .FirstOrDefaultAsync(m => m.MaterialNumber == materialNumber);
        }

        private async Task ExecuteDependencyAsync(
            VirtualMachine vm,
            DependencyEntity dependency,
            ConfigurationResult result)
        {
            var sw = Stopwatch.StartNew();
            var executionLog = new DependencyExecutionLogEntry
            {
                DependencyId = dependency.Id,
                DependencyName = dependency.Name,
                DependencyType = dependency.Type,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // Deserialize AST
                var ast = _astSerializer.Deserialize(dependency.AstJson);

                // Compile to instructions
                var instructions = _compiler.Compile(ast);

                // Execute
                var execResult = await vm.ExecuteAsync(instructions);

                executionLog.Success = execResult.Success;
                executionLog.IsConsistent = !execResult.Inconsistent;
                executionLog.Message = execResult.Message;
                executionLog.TablesUsed = execResult.TablesUsed;

                if (execResult.ReturnValue != null)
                {
                    executionLog.ReturnValue = execResult.ReturnValue.ToString();
                }

                result.ExecutionLogs.Add(executionLog);

                // If execution failed or inconsistent, track it
                if (!execResult.Success)
                {
                    result.FailedDependencies.Add(dependency.Name);
                }
            }
            catch (Exception ex)
            {
                executionLog.Success = false;
                executionLog.Message = ex.Message;
                executionLog.ErrorDetails = ex.ToString();

                result.ExecutionLogs.Add(executionLog);
                result.FailedDependencies.Add(dependency.Name);
            }
            finally
            {
                sw.Stop();
                executionLog.EndTime = DateTime.UtcNow;
                executionLog.DurationMs = sw.ElapsedMilliseconds;

                // Clear constraint context for next execution
                // Each constraint should execute with a clean context
                vm.ConstraintContext.Clear();
            }
        }

        private async Task ExecuteDependencyNetAsync(
            VirtualMachine vm,
            DependencyNetEntity net,
            ConfigurationResult result)
        {
            // Execute all constraints in the net in sequence order
            foreach (var constraint in net.Constraints.OrderBy(c => c.SequenceNumber))
            {
                if (constraint.Dependency != null)
                {
                    await ExecuteDependencyAsync(vm, constraint.Dependency, result);
                }
            }
        }

        /// <summary>
        /// Gets all characteristics for a material with their allowed values from the database
        /// </summary>
        public async Task<Dictionary<string, List<string>>> GetAllCharacteristicsWithValuesAsync(string materialNumber)
        {
            var material = await _dbContext.Materials
                .Include(m => m.ClassAssignments)
                    .ThenInclude(ca => ca.Class)
                        .ThenInclude(c => c!.Characteristics)
                            .ThenInclude(ch => ch.Values)
                .FirstOrDefaultAsync(m => m.MaterialNumber == materialNumber);

            var result = new Dictionary<string, List<string>>();

            if (material != null)
            {
                foreach (var assignment in material.ClassAssignments)
                {
                    foreach (var characteristic in assignment.Class!.Characteristics)
                    {
                        var values = characteristic.Values
                            .Select(v => v.Value)
                            .Where(v => !string.IsNullOrEmpty(v))
                            .ToList();

                        result[characteristic.Name] = values;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Persist execution logs to database
        /// </summary>
        public async Task SaveExecutionLogsAsync(ConfigurationResult result)
        {
            foreach (var log in result.ExecutionLogs)
            {
                var dbLog = new DependencyExecutionLog
                {
                    DependencyId = log.DependencyId,
                    ExecutedAt = log.StartTime,
                    ContextJson = JsonSerializer.Serialize(new
                    {
                        MaterialNumber = result.MaterialNumber,
                        StartTime = result.StartTime
                    }),
                    ResultJson = JsonSerializer.Serialize(new
                    {
                        log.Success,
                        log.IsConsistent,
                        log.Message,
                        log.ReturnValue,
                        log.ErrorDetails
                    }),
                    DurationMs = log.DurationMs,
                    Success = log.Success,
                    IsConsistent = log.IsConsistent
                };

                _dbContext.ExecutionLogs.Add(dbLog);
            }

            await _dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Result of a complete configuration execution
    /// </summary>
    public class ConfigurationResult
    {
        public string MaterialNumber { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long DurationMs { get; set; }

        public bool Success { get; set; }
        public bool IsComplete { get; set; }

        public string? Error { get; set; }
        public string? ErrorDetails { get; set; }

        public Dictionary<string, object?> FinalValues { get; set; } = new();
        public List<string> MissingRequired { get; set; } = new();
        public List<string> FailedDependencies { get; set; } = new();

        // ===== Cycle Execution Metrics =====

        /// <summary>
        /// Number of cycles executed before reaching stability or max iterations
        /// </summary>
        public int CyclesExecuted { get; set; }

        /// <summary>
        /// Whether the configuration reached a stable state (no changes)
        /// </summary>
        public bool IsStable { get; set; }

        /// <summary>
        /// All characteristics that changed across all cycles
        /// </summary>
        public HashSet<string> ChangedCharacteristics { get; set; } = new();

        /// <summary>
        /// Detailed metrics for each cycle (for debugging/analysis)
        /// </summary>
        public List<CycleMetrics> CycleDetails { get; set; } = new();

        /// <summary>
        /// Initial allowed values for all characteristics (before execution).
        /// Maps characteristic name to list of all possible values from database.
        /// </summary>
        public Dictionary<string, List<string>> InitialAllowedValues { get; set; } = new();

        /// <summary>
        /// Final allowed values for all characteristics (after execution and restrictions).
        /// Maps characteristic name to list of available values.
        /// If no restrictions applied, will match InitialAllowedValues.
        /// </summary>
        public Dictionary<string, List<string>> FinalAllowedValues { get; set; } = new();

        /// <summary>
        /// Value restrictions applied by constraints.
        /// Maps characteristic name to list of available values.
        /// Empty dictionary means no restrictions were applied.
        /// </summary>
        public Dictionary<string, List<string>> ValueRestrictions { get; set; } = new();

        public List<DependencyExecutionLogEntry> ExecutionLogs { get; set; } = new();

        /// <summary>
        /// Detailed action logs from configuration context (value assignments, restrictions)
        /// </summary>
        public List<Contexts.ConfigurationActionLog> ActionLogs { get; set; } = new();

        public void PrintSummary()
        {
            Console.WriteLine($"\n=== Configuration Result for {MaterialNumber} ===");
            Console.WriteLine($"Success: {Success}");
            Console.WriteLine($"Complete: {IsComplete}");
            Console.WriteLine($"Duration: {DurationMs}ms");
            Console.WriteLine($"Cycles Executed: {CyclesExecuted}");
            Console.WriteLine($"Stable: {IsStable}");
            Console.WriteLine($"Dependencies Executed: {ExecutionLogs.Count}");
            Console.WriteLine($"Failed Dependencies: {FailedDependencies.Count}");

            if (Error != null)
            {
                Console.WriteLine($"Error: {Error}");
            }

            // Print cycle details
            if (CycleDetails.Any())
            {
                Console.WriteLine($"\n=== Cycle Execution Details ===");
                foreach (var cycle in CycleDetails)
                {
                    var status = cycle.IsStable ? "STABLE" : $"{cycle.ChangedCharacteristics.Count} changes";
                    Console.WriteLine($"  Cycle {cycle.CycleNumber}: " +
                        $"{cycle.TotalDependenciesExecuted} deps " +
                        $"({cycle.ProceduresExecuted} proc, {cycle.ConstraintsExecuted} constr), " +
                        $"{status}, {cycle.DurationMs}ms");

                    if (cycle.ChangedCharacteristics.Any())
                    {
                        Console.WriteLine($"    Changed: {string.Join(", ", cycle.ChangedCharacteristics)}");
                    }
                }
            }

            // Print initial state
            if (InitialAllowedValues.Any())
            {
                Console.WriteLine($"\n=== Initial State (All Characteristics) ===");
                foreach (var (name, values) in InitialAllowedValues.OrderBy(kv => kv.Key))
                {
                    var valueStr = values.Any() ? string.Join(", ", values) : "(no values)";
                    Console.WriteLine($"  {name}: [{valueStr}]");
                }
            }

            // Print final assigned values
            Console.WriteLine($"\n=== Assigned Values (After Execution) ===");
            if (FinalValues.Any())
            {
                foreach (var (name, value) in FinalValues.OrderBy(kv => kv.Key))
                {
                    Console.WriteLine($"  {name} = {value ?? "(null)"}");
                }
            }
            else
            {
                Console.WriteLine($"  (no values assigned)");
            }

            // Print value restrictions
            if (ValueRestrictions.Any())
            {
                Console.WriteLine($"\n=== Value Restrictions Applied ===");
                foreach (var (name, values) in ValueRestrictions.OrderBy(kv => kv.Key))
                {
                    var initial = InitialAllowedValues.TryGetValue(name, out var initialVals)
                        ? initialVals : new List<string>();
                    var removed = initial.Except(values).ToList();

                    Console.WriteLine($"  {name}:");
                    Console.WriteLine($"    Before: [{string.Join(", ", initial)}]");
                    Console.WriteLine($"    After:  [{string.Join(", ", values)}]");
                    if (removed.Any())
                    {
                        Console.WriteLine($"    Removed: [{string.Join(", ", removed)}]");
                    }
                }
            }

            // Print final state
            if (FinalAllowedValues.Any())
            {
                Console.WriteLine($"\n=== Final State (All Characteristics) ===");
                foreach (var (name, values) in FinalAllowedValues.OrderBy(kv => kv.Key))
                {
                    var valueStr = values.Any() ? string.Join(", ", values) : "(no values)";
                    var restricted = ValueRestrictions.ContainsKey(name) ? " [RESTRICTED]" : "";
                    var assigned = FinalValues.ContainsKey(name) ? $" = {FinalValues[name]}" : "";
                    Console.WriteLine($"  {name}{assigned}: [{valueStr}]{restricted}");
                }
            }

            if (MissingRequired.Any())
            {
                Console.WriteLine($"\n=== Missing Required Characteristics ===");
                foreach (var name in MissingRequired)
                {
                    Console.WriteLine($"  - {name}");
                }
            }

            Console.WriteLine($"\n=== Execution Log ===");
            foreach (var log in ExecutionLogs)
            {
                var status = log.Success ? "✓" : "✗";
                var consistency = log.IsConsistent ? "" : " [INCONSISTENT]";
                Console.WriteLine($"  {status} {log.DependencyName} ({log.DependencyType}) {log.DurationMs}ms{consistency}");
                if (!string.IsNullOrEmpty(log.Message))
                {
                    Console.WriteLine($"      {log.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Log entry for a single dependency execution
    /// </summary>
    public class DependencyExecutionLogEntry
    {
        public string DependencyId { get; set; } = null!;
        public string DependencyName { get; set; } = null!;
        public string DependencyType { get; set; } = null!;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long DurationMs { get; set; }

        public bool Success { get; set; }
        public bool IsConsistent { get; set; } = true;

        public string? Message { get; set; }
        public string? ReturnValue { get; set; }
        public string? ErrorDetails { get; set; }

        /// <summary>
        /// List of variant tables accessed during this dependency execution
        /// </summary>
        public List<string> TablesUsed { get; set; } = new();
    }

    /// <summary>
    /// Metrics for a single cycle execution
    /// </summary>
    public class CycleMetrics
    {
        /// <summary>
        /// Cycle number (1-based)
        /// </summary>
        public int CycleNumber { get; set; }

        /// <summary>
        /// Number of procedures executed this cycle
        /// </summary>
        public int ProceduresExecuted { get; set; }

        /// <summary>
        /// Number of constraints executed this cycle
        /// </summary>
        public int ConstraintsExecuted { get; set; }

        /// <summary>
        /// Total dependencies executed this cycle
        /// </summary>
        public int TotalDependenciesExecuted { get; set; }

        /// <summary>
        /// Characteristics that changed in this cycle
        /// </summary>
        public HashSet<string> ChangedCharacteristics { get; set; } = new();

        /// <summary>
        /// Duration of this cycle in milliseconds
        /// </summary>
        public long DurationMs { get; set; }

        /// <summary>
        /// Whether this cycle resulted in stability (no changes)
        /// </summary>
        public bool IsStable { get; set; }
    }
}
