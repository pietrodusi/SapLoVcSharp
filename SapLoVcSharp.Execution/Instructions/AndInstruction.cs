namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops two boolean values from the stack, performs logical AND, and pushes the result.
    /// Example: Stack [true, false] -> AND -> Stack [false]
    /// </summary>
    public class AndInstruction : Instruction
    {
        public AndInstruction(int position)
            : base(OpCode.And, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var right = Convert.ToBoolean(vm.Stack.Pop());
            var left = Convert.ToBoolean(vm.Stack.Pop());
            vm.Stack.Push(left && right);
            return Task.CompletedTask;
        }

        public override string ToString() => "AND";
    }
}
