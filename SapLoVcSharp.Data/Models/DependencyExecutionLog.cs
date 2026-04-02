
/// <summary>
/// Execution log entry.
/// </summary>
public class DependencyExecutionLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DependencyId { get; set; } = null!;
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string ContextJson { get; set; } = null!;
    public string ResultJson { get; set; } = null!;
    public long DurationMs { get; set; }
    public bool Success { get; set; }
    public bool IsConsistent { get; set; } = true;
}