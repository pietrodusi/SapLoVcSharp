namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Stores a value from the stack into a constraint variable member.
    /// Example: PC.COLOR = 'Red' pops 'Red' from stack and stores it in PC.COLOR.
    /// </summary>
    public class StoreConstraintMemberInstruction : Instruction
    {
        public string Variable { get; }
        public string MemberName { get; }

        public StoreConstraintMemberInstruction(string variable, string memberName, int position)
            : base(OpCode.StoreConstraintMember, position)
        {
            Variable = variable;
            MemberName = memberName;
        }

        public override async Task ExecuteAsync(VirtualMachine vm)
        {
            var value = vm.Stack.Pop();
            await vm.ConstraintContext.SetMemberValueAsync(Variable, MemberName, value);
        }

        public override string ToString() => $"STORE_CONSTRAINT_MEMBER {Variable}.{MemberName}";
    }
}
