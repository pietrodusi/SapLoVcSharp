namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops two values from the stack, subtracts them, and pushes the result.
    /// Example: Stack [10, 3] -> SUB -> Stack [7]
    /// </summary>
    public class SubInstruction : Instruction
    {
        public SubInstruction(int position)
            : base(OpCode.Sub, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var right = Convert.ToDouble(vm.Stack.Pop());
            var left = Convert.ToDouble(vm.Stack.Pop());
            vm.Stack.Push(left - right);
            return Task.CompletedTask;
        }

        public override string ToString() => "SUB";
    }
}
