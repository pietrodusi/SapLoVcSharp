namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Duplicates the top value on the stack.
    /// Example: Stack [1, 2, 3] -> DUP -> Stack [1, 2, 3, 3]
    /// </summary>
    public class DupInstruction : Instruction
    {
        public DupInstruction(int position)
            : base(OpCode.Dup, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var value = vm.Stack.Peek();
            vm.Stack.Push(value);
            return Task.CompletedTask;
        }

        public override string ToString() => "DUP";
    }
}
