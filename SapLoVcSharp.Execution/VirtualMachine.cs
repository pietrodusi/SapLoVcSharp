using SapLoVcSharp.Execution.Contexts;
using SapLoVcSharp.Execution.Instructions;

namespace SapLoVcSharp.Execution
{
    /// <summary>
    /// Virtual machine for executing compiled instruction lists.
    /// Stack-based execution model with async support.
    /// </summary>
    public class VirtualMachine
    {
        // Execution state
        public Stack<object?> Stack { get; } = new();
        public int InstructionPointer { get; set; }
        public ExecutionResult? Result { get; set; }

        // Tracking
        public HashSet<string> TablesUsed { get; } = new();

        // Dependencies (injected)
        public IConfigurationContext Context { get; }
        public ITableResolver TableResolver { get; }
        public IConstraintContext ConstraintContext { get; }

        public VirtualMachine(
            IConfigurationContext context,
            ITableResolver tableResolver,
            IConstraintContext constraintContext)
        {
            Context = context;
            TableResolver = tableResolver;
            ConstraintContext = constraintContext;
        }

        /// <summary>
        /// Executes a compiled instruction list.
        /// </summary>
        public async Task<ExecutionResult> ExecuteAsync(InstructionList instructionList)
        {
            try
            {
                InstructionPointer = 0;
                Stack.Clear();
                TablesUsed.Clear();
                Result = null;

                while (InstructionPointer < instructionList.Instructions.Count)
                {
                    var instruction = instructionList.Instructions[InstructionPointer];
                    var previousIp = InstructionPointer;

                    // Execute instruction (may modify IP for jumps)
                    await instruction.ExecuteAsync(this);

                    // Check if instruction set a result (early return)
                    if (Result != null)
                    {
                        Result.TablesUsed = TablesUsed.ToList();
                        return Result;
                    }

                    // Auto-increment IP if instruction didn't jump
                    if (InstructionPointer == previousIp)
                    {
                        InstructionPointer++;
                    }
                }

                return new ExecutionResult { Success = true, TablesUsed = TablesUsed.ToList() };
            }
            catch (InconsistencyException ex)
            {
                // Rollback constraint transaction if active
                if (ConstraintContext.InTransaction)
                {
                    ConstraintContext.Rollback();
                }

                return new ExecutionResult
                {
                    Success = false,
                    Inconsistent = true,
                    Message = ex.Message,
                    TablesUsed = TablesUsed.ToList()
                };
            }
            catch (Exception ex)
            {
                // Rollback constraint transaction if active
                if (ConstraintContext.InTransaction)
                {
                    ConstraintContext.Rollback();
                }

                return new ExecutionResult
                {
                    Success = false,
                    Message = $"Execution error: {ex.Message}",
                    Exception = ex,
                    TablesUsed = TablesUsed.ToList()
                };
            }
        }
    }

    /// <summary>
    /// Exception thrown when RETURN_INCONSISTENCY instruction is executed.
    /// </summary>
    public class InconsistencyException : Exception
    {
        public InconsistencyException(string message) : base(message) { }
    }
}
