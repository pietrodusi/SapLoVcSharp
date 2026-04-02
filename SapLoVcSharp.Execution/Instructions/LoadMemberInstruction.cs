using SapLoVcSharp.Execution.Contexts;

namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Loads a characteristic value from an object in the context using member access.
    /// Example: LOAD_MEMBER "$SELF" "COLOR" -> Push value of $SELF.COLOR
    /// </summary>
    public class LoadMemberInstruction : Instruction
    {
        public string ObjectName { get; set; }
        public string MemberName { get; set; }

        public LoadMemberInstruction(string objectName, string memberName, int position)
            : base(OpCode.LoadMember, position)
        {
            ObjectName = objectName;
            MemberName = memberName;
        }

        public override async Task ExecuteAsync(VirtualMachine vm)
        {
            var obj = GetObject(vm, ObjectName);
            var value = await obj.GetCharacteristicValueAsync(MemberName);
            vm.Stack.Push(value);
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

        public override string ToString() => $"LOAD_MEMBER {ObjectName}.{MemberName}";
    }
}
