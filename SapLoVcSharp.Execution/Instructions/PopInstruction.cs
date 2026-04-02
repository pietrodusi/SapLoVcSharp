namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops and discards the top value from the stack.
    /// Example: Stack [1, 2, 3] -> POP -> Stack [1, 2]
    /// </summary>
    public class PopInstruction : Instruction
    {
        public PopInstruction(int position)
            : base(OpCode.Pop, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            vm.Stack.Pop();
            return Task.CompletedTask;
        }

        public override string ToString() => "POP";
    }
}
