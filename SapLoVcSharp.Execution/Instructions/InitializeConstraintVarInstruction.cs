namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Initializes a local constraint variable from the configuration context.
    /// Used for WHERE clause: WHERE lv_CASE = CASE
    /// Reads CASE from config context and stores in constraint context's lv_CASE.
    /// </summary>
    public class InitializeConstraintVarInstruction : Instruction
    {
        public string LocalVarName { get; }
        public string ConfigCharacteristicName { get; }

        public InitializeConstraintVarInstruction(
            string localVarName,
            string configCharacteristicName,
            int position)
            : base(OpCode.InitializeConstraintVar, position)
        {
            LocalVarName = localVarName;
            ConfigCharacteristicName = configCharacteristicName;
        }

        public override async Task ExecuteAsync(VirtualMachine vm)
        {
            // Initialize local variable from configuration context
            await vm.ConstraintContext.InitializeLocalVariableAsync(
                LocalVarName,
                ConfigCharacteristicName,
                vm.Context);
        }

        public override string ToString() =>
            $"INIT_CONSTRAINT_VAR {LocalVarName} = {ConfigCharacteristicName}";
    }
}
