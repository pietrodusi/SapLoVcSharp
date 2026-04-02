namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Throws an inconsistency exception to signal a constraint violation.
    /// Used in constraint RESTRICTIONS section.
    /// Example: RETURN_INCONSISTENCY "Invalid configuration: COLOR cannot be Red when MODEL is Racing"
    /// </summary>
    public class ReturnInconsistencyInstruction : Instruction
    {
        public string? Message { get; set; }

        public ReturnInconsistencyInstruction(string? message, int position)
            : base(OpCode.ReturnInconsistency, position)
        {
            Message = message;
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            throw new InconsistencyException(Message ?? "Constraint violation");
        }

        public override string ToString() => $"RETURN_INCONSISTENCY \"{Message}\"";
    }
}
