namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops two values from the stack, adds them, and pushes the result.
    /// Example: Stack [5, 3] -> ADD -> Stack [8]
    /// </summary>
    public class AddInstruction : Instruction
    {
        public AddInstruction(int position)
            : base(OpCode.Add, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var right = Convert.ToDouble(vm.Stack.Pop());
            var left = Convert.ToDouble(vm.Stack.Pop());
            vm.Stack.Push(left + right);
            return Task.CompletedTask;
        }

        public override string ToString() => "ADD";
    }
}
