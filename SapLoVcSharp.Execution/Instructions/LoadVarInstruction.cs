namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Loads a characteristic value from the configuration context onto the stack.
    /// Example: LOAD_VAR "COLOR" pushes the value of COLOR characteristic onto stack.
    /// </summary>
    public class LoadVarInstruction : Instruction
    {
        public string VariableName { get; }

        public LoadVarInstruction(string variableName, int position)
            : base(OpCode.LoadVar, position)
        {
            VariableName = variableName;
        }

        public override async Task ExecuteAsync(VirtualMachine vm)
        {
            object? value;

            // If executing within a constraint transaction, load from constraint context first
            if (vm.ConstraintContext.InTransaction)
            {
                // Try to get local variable from constraint context
                value = vm.ConstraintContext.GetLocalVariable(VariableName);
            }
            else
            {
                // Load from configuration context (procedures, etc.)
                value = await vm.Context.GetValueAsync(VariableName);
            }

            vm.Stack.Push(value);
        }

        public override string ToString() => $"LOAD_VAR {VariableName}";
    }
}
