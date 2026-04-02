namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops two values from the stack, checks if left >= right, and pushes the result.
    /// Example: Stack [10, 10] -> GE -> Stack [true]
    /// </summary>
    public class GeInstruction : Instruction
    {
        public GeInstruction(int position)
            : base(OpCode.Ge, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var right = Convert.ToDouble(vm.Stack.Pop());
            var left = Convert.ToDouble(vm.Stack.Pop());
            vm.Stack.Push(left >= right);
            return Task.CompletedTask;
        }

        public override string ToString() => "GE";
    }
}
