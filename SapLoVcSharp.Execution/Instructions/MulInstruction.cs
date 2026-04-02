namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops two values from the stack, multiplies them, and pushes the result.
    /// Example: Stack [5, 3] -> MUL -> Stack [15]
    /// </summary>
    public class MulInstruction : Instruction
    {
        public MulInstruction(int position)
            : base(OpCode.Mul, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var right = Convert.ToDouble(vm.Stack.Pop());
            var left = Convert.ToDouble(vm.Stack.Pop());
            vm.Stack.Push(left * right);
            return Task.CompletedTask;
        }

        public override string ToString() => "MUL";
    }
}
