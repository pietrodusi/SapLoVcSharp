namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops two values from the stack, divides them, and pushes the result.
    /// Example: Stack [15, 3] -> DIV -> Stack [5]
    /// </summary>
    public class DivInstruction : Instruction
    {
        public DivInstruction(int position)
            : base(OpCode.Div, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var right = Convert.ToDouble(vm.Stack.Pop());
            var left = Convert.ToDouble(vm.Stack.Pop());

            if (Math.Abs(right) < 1e-10)
                throw new DivideByZeroException("Division by zero");

            vm.Stack.Push(left / right);
            return Task.CompletedTask;
        }

        public override string ToString() => "DIV";
    }
}
