namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops two string values from the stack, concatenates them, and pushes the result.
    /// Example: Stack ["Hello", " World"] -> CONCAT -> Stack ["Hello World"]
    /// </summary>
    public class ConcatInstruction : Instruction
    {
        public ConcatInstruction(int position)
            : base(OpCode.Concat, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var right = vm.Stack.Pop()?.ToString() ?? "";
            var left = vm.Stack.Pop()?.ToString() ?? "";
            vm.Stack.Push(left + right);
            return Task.CompletedTask;
        }

        public override string ToString() => "CONCAT";
    }
}
