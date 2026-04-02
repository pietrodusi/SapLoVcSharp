namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Begins a constraint execution transaction.
    /// Creates an isolated execution context for the constraint.
    /// </summary>
    public class BeginConstraintTransactionInstruction : Instruction
    {
        public BeginConstraintTransactionInstruction(int position)
            : base(OpCode.BeginConstraintTransaction, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            vm.ConstraintContext.BeginTransaction();
            return Task.CompletedTask;
        }

        public override string ToString() => "BEGIN_CONSTRAINT_TRANSACTION";
    }
}
