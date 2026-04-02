namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops two values from the stack, checks if left > right, and pushes the result.
    /// Example: Stack [10, 5] -> GT -> Stack [true]
    /// </summary>
    public class GtInstruction : Instruction
    {
        public GtInstruction(int position)
            : base(OpCode.Gt, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var right = Convert.ToDouble(vm.Stack.Pop());
            var left = Convert.ToDouble(vm.Stack.Pop());
            vm.Stack.Push(left > right);
            return Task.CompletedTask;
        }

        public override string ToString() => "GT";
    }
}
