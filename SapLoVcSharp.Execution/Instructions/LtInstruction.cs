namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops two values from the stack, checks if left < right, and pushes the result.
    /// Example: Stack [5, 10] -> LT -> Stack [true]
    /// </summary>
    public class LtInstruction : Instruction
    {
        public LtInstruction(int position)
            : base(OpCode.Lt, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var right = Convert.ToDouble(vm.Stack.Pop());
            var left = Convert.ToDouble(vm.Stack.Pop());
            vm.Stack.Push(left < right);
            return Task.CompletedTask;
        }

        public override string ToString() => "LT";
    }
}
