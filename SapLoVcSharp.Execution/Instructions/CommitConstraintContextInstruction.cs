namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Commits constraint context changes to the configuration context.
    /// Called at the end of successful constraint execution (no inconsistencies).
    /// If commit fails due to conflicts, sets Result.Success = false.
    /// </summary>
    public class CommitConstraintContextInstruction : Instruction
    {
        public CommitConstraintContextInstruction(int position)
            : base(OpCode.CommitConstraintContext, position)
        {
        }

        public override async Task ExecuteAsync(VirtualMachine vm)
        {
            var success = await vm.ConstraintContext.CommitAsync(vm.Context);

            if (!success)
            {
                // Commit failed - set execution result to failure
                vm.Result = new ExecutionResult
                {
                    Success = false,
                    Message = "Constraint context commit failed - conflicts detected"
                };
            }
        }

        public override string ToString() => "COMMIT_CONSTRAINT_CONTEXT";
    }
}
