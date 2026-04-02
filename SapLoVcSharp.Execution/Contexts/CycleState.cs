namespace SapLoVcSharp.Execution.Contexts
{
    /// <summary>
    /// Tracks the state of configuration cycle execution.
    /// Used to detect stability and prevent infinite loops.
    /// </summary>
    public class CycleState
    {
        /// <summary>
        /// Current cycle number (starts at 1)
        /// </summary>
        public int CurrentCycle { get; set; } = 0;

        /// <summary>
        /// Maximum number of cycles allowed before failing (default: 20)
        /// </summary>
        public int MaxCycles { get; set; } = 20;

        /// <summary>
        /// Characteristics that changed in the last cycle
        /// </summary>
        public HashSet<string> ChangedCharacteristics { get; set; } = new();

        /// <summary>
        /// Total number of characteristics that have changed across all cycles
        /// </summary>
        public HashSet<string> AllChangedCharacteristics { get; set; } = new();

        /// <summary>
        /// Whether the configuration has reached a stable state (no changes)
        /// </summary>
        public bool IsStable { get; set; } = false;

        /// <summary>
        /// Whether the maximum number of cycles has been exceeded
        /// </summary>
        public bool MaxCyclesExceeded => CurrentCycle >= MaxCycles;

        /// <summary>
        /// Number of dependencies executed in the current cycle
        /// </summary>
        public int DependenciesExecutedThisCycle { get; set; } = 0;

        /// <summary>
        /// Total number of dependencies executed across all cycles
        /// </summary>
        public int TotalDependenciesExecuted { get; set; } = 0;

        /// <summary>
        /// Reset state for a new cycle
        /// </summary>
        public void BeginCycle()
        {
            CurrentCycle++;
            ChangedCharacteristics.Clear();
            DependenciesExecutedThisCycle = 0;
        }

        /// <summary>
        /// Mark that a characteristic changed in this cycle
        /// </summary>
        public void RecordChange(string characteristicName)
        {
            ChangedCharacteristics.Add(characteristicName);
            AllChangedCharacteristics.Add(characteristicName);
            IsStable = false;
        }

        /// <summary>
        /// Check if the cycle is stable (no changes detected)
        /// </summary>
        public void CheckStability()
        {
            IsStable = ChangedCharacteristics.Count == 0;
        }

        /// <summary>
        /// Record that a dependency was executed
        /// </summary>
        public void RecordDependencyExecution()
        {
            DependenciesExecutedThisCycle++;
            TotalDependenciesExecuted++;
        }
    }
}
