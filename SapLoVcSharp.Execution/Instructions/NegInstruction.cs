namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops a value from the stack, negates it, and pushes the result.
    /// Example: Stack [5] -> NEG -> Stack [-5]
    /// </summary>
    public class NegInstruction : Instruction
    {
        public NegInstruction(int position)
            : base(OpCode.Neg, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var value = Convert.ToDouble(vm.Stack.Pop());
            vm.Stack.Push(-value);
            return Task.CompletedTask;
        }

        public override string ToString() => "NEG";
    }
}
