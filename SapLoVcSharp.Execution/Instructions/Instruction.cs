namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Base class for all virtual machine instructions.
    /// Each instruction knows how to execute itself on the VM.
    /// </summary>
    public abstract class Instruction
    {
        /// <summary>
        /// The operation code for this instruction.
        /// </summary>
        public OpCode OpCode { get; }

        /// <summary>
        /// Original AST position for debugging purposes.
        /// </summary>
        public int Position { get; }

        protected Instruction(OpCode opCode, int position)
        {
            OpCode = opCode;
            Position = position;
        }

        /// <summary>
        /// Executes this instruction on the provided virtual machine.
        /// </summary>
        /// <param name="vm">The virtual machine to execute on</param>
        /// <returns>Task representing the async execution</returns>
        public abstract Task ExecuteAsync(VirtualMachine vm);

        public override string ToString() => $"{OpCode} (pos: {Position})";
    }
}
