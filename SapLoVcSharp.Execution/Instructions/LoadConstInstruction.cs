using SapLoVcSharp.Core.Ast.Expressions;

namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Loads a constant literal value onto the stack.
    /// Example: LOAD_CONST 42 pushes 42 onto the stack.
    /// </summary>
    public class LoadConstInstruction : Instruction
    {
        public object Value { get; }
        public LiteralType Type { get; }

        public LoadConstInstruction(object value, LiteralType type, int position)
            : base(OpCode.LoadConst, position)
        {
            Value = value;
            Type = type;
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            vm.Stack.Push(Value);
            return Task.CompletedTask;
        }

        public override string ToString() => $"LOAD_CONST {Value} ({Type})";
    }
}
