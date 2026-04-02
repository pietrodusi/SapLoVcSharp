namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Loads a member value from a constraint variable onto the stack.
    /// Example: PC.COLOR pushes the value of the COLOR characteristic onto the stack.
    /// </summary>
    public class LoadConstraintMemberInstruction : Instruction
    {
        public string Variable { get; }
        public string MemberName { get; }

        public LoadConstraintMemberInstruction(string variable, string memberName, int position)
            : base(OpCode.LoadConstraintMember, position)
        {
            Variable = variable;
            MemberName = memberName;
        }

        public override async Task ExecuteAsync(VirtualMachine vm)
        {
            var value = await vm.ConstraintContext.GetMemberValueAsync(Variable, MemberName);
            vm.Stack.Push(value);
        }

        public override string ToString() => $"LOAD_CONSTRAINT_MEMBER {Variable}.{MemberName}";
    }
}
