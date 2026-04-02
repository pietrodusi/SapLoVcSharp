namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops a boolean from the stack and returns it as the execution result.
    /// Used for preconditions and selection conditions.
    /// Example: Stack [true] -> RETURN_BOOL -> ExecutionResult.Success = true, BooleanResult = true
    /// </summary>
    public class ReturnBoolInstruction : Instruction
    {
        public ReturnBoolInstruction(int position)
            : base(OpCode.ReturnBool, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var result = Convert.ToBoolean(vm.Stack.Pop());
            vm.Result = new ExecutionResult
            {
                Success = true,
                ReturnValue = result
            };
            return Task.CompletedTask;
        }

        public override string ToString() => "RETURN_BOOL";
    }
}
