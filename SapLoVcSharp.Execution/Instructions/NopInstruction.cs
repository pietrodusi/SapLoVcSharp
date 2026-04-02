namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// No operation - does nothing. Used as a placeholder or jump target.
    /// </summary>
    public class NopInstruction : Instruction
    {
        public NopInstruction(int position)
            : base(OpCode.Nop, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            return Task.CompletedTask;
        }

        public override string ToString() => "NOP";
    }
}
