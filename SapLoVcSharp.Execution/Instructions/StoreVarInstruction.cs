namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops a value from the stack and stores it in a characteristic.
    /// Example: STORE_VAR "COLOR" pops the stack and assigns it to COLOR characteristic.
    /// </summary>
    public class StoreVarInstruction : Instruction
    {
        public string VariableName { get; }

        public StoreVarInstruction(string variableName, int position)
            : base(OpCode.StoreVar, position)
        {
            VariableName = variableName;
        }

        public override async Task ExecuteAsync(VirtualMachine vm)
        {
            var value = vm.Stack.Pop();

            // If executing within a constraint transaction, store to constraint context
            if (vm.ConstraintContext.InTransaction)
            {
                // Store to local variable in constraint context
                vm.ConstraintContext.SetLocalVariable(VariableName, value);
            }
            else
            {
                // Store to configuration context (procedures, etc.)
                await vm.Context.SetValueAsync(VariableName, value);
            }
        }

        public override string ToString() => $"STORE_VAR {VariableName}";
    }
}
