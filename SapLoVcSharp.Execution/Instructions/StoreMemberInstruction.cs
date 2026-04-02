using SapLoVcSharp.Execution.Contexts;

namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops a value from the stack and stores it in a characteristic using member access.
    /// Example: Stack ["Red"] -> STORE_MEMBER "$SELF" "COLOR" -> $SELF.COLOR = "Red"
    /// </summary>
    public class StoreMemberInstruction : Instruction
    {
        public string ObjectName { get; set; }
        public string MemberName { get; set; }

        public StoreMemberInstruction(string objectName, string memberName, int position)
            : base(OpCode.StoreMember, position)
        {
            ObjectName = objectName;
            MemberName = memberName;
        }

        public override async Task ExecuteAsync(VirtualMachine vm)
        {
            var value = vm.Stack.Pop();
            var obj = GetObject(vm, ObjectName);
            await obj.SetCharacteristicValueAsync(MemberName, value);
        }

        private IConfigurationObject GetObject(VirtualMachine vm, string objectName)
        {
            return objectName switch
            {
                "$SELF" => vm.Context.Self,
                "$PARENT" => vm.Context.Parent ?? throw new InvalidOperationException("$PARENT is not available in this context"),
                "$ROOT" => vm.Context.Root ?? throw new InvalidOperationException("$ROOT is not available in this context"),
                _ => throw new InvalidOperationException($"Unknown object reference: {objectName}")
            };
        }

        public override string ToString() => $"STORE_MEMBER {ObjectName}.{MemberName}";
    }
}
