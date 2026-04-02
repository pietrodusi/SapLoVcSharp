namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops a boolean from the stack and jumps to the target offset if it's false.
    /// Example: Stack [false] -> JUMP_IF_FALSE 10 -> IP = 10
    /// Example: Stack [true] -> JUMP_IF_FALSE 10 -> IP unchanged
    /// </summary>
    public class JumpIfFalseInstruction : Instruction
    {
        public int TargetOffset { get; set; }

        public JumpIfFalseInstruction(int targetOffset, int position)
            : base(OpCode.JumpIfFalse, position)
        {
            TargetOffset = targetOffset;
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var condition = Convert.ToBoolean(vm.Stack.Pop());
            if (!condition)
            {
                vm.InstructionPointer = TargetOffset;
            }
            return Task.CompletedTask;
        }

        public override string ToString() => $"JUMP_IF_FALSE {TargetOffset}";
    }
}
