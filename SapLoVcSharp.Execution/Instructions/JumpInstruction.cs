namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Unconditionally jumps to the specified instruction offset.
    /// Example: JUMP 10 -> Set IP to 10
    /// </summary>
    public class JumpInstruction : Instruction
    {
        public int TargetOffset { get; set; }

        public JumpInstruction(int targetOffset, int position)
            : base(OpCode.Jump, position)
        {
            TargetOffset = targetOffset;
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            vm.InstructionPointer = TargetOffset;
            return Task.CompletedTask;
        }

        public override string ToString() => $"JUMP {TargetOffset}";
    }
}
