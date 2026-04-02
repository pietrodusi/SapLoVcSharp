namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops a boolean value from the stack, negates it, and pushes the result.
    /// Example: Stack [true] -> NOT -> Stack [false]
    /// </summary>
    public class NotInstruction : Instruction
    {
        public NotInstruction(int position)
            : base(OpCode.Not, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var value = Convert.ToBoolean(vm.Stack.Pop());
            vm.Stack.Push(!value);
            return Task.CompletedTask;
        }

        public override string ToString() => "NOT";
    }
}
