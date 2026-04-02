namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Checks if a characteristic has been specified (assigned a value).
    /// Pushes true if the characteristic has a non-null value, false otherwise.
    /// Example: SPECIFIED COLOR checks if COLOR has been assigned.
    /// </summary>
    public class SpecifiedInstruction : Instruction
    {
        public string CharacteristicName { get; }

        public SpecifiedInstruction(string characteristicName, int position)
            : base(OpCode.Specified, position)
        {
            CharacteristicName = characteristicName;
        }

        public override async Task ExecuteAsync(VirtualMachine vm)
        {
            object? value;

            // Check if the characteristic has been specified
            if (vm.ConstraintContext.InTransaction)
            {
                // In constraint context, check local variable
                value = vm.ConstraintContext.GetLocalVariable(CharacteristicName);
            }
            else
            {
                // In configuration context
                value = await vm.Context.GetValueAsync(CharacteristicName);
            }

            // Push true if value is not null, false otherwise
            vm.Stack.Push(value != null);
        }

        public override string ToString() => $"SPECIFIED {CharacteristicName}";
    }
}
