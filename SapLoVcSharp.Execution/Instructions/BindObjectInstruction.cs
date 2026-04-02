using SapLoVcSharp.Execution.Contexts;

namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Binds a constraint variable to a class instance.
    /// Used in OBJECTS section: PC IS_A (300) BIKE
    /// </summary>
    public class BindObjectInstruction : Instruction
    {
        public string Variable { get; set; }
        public string ClassName { get; set; }
        public string? ClassType { get; set; }
        public Dictionary<string, string>? WhereClause { get; set; }

        public BindObjectInstruction(
            string variable,
            string className,
            string? classType,
            Dictionary<string, string>? whereClause,
            int position)
            : base(OpCode.BindObject, position)
        {
            Variable = variable;
            ClassName = className;
            ClassType = classType;
            WhereClause = whereClause;
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            // Bind the variable in the constraint context
            vm.ConstraintContext.BindVariable(Variable, ClassName, ClassType, WhereClause);
            return Task.CompletedTask;
        }

        public override string ToString()
        {
            var typeStr = ClassType != null ? $"({ClassType}) " : "";
            var whereStr = WhereClause != null && WhereClause.Count > 0
                ? $" WHERE {string.Join(", ", WhereClause.Select(kv => $"{kv.Key} = {kv.Value}"))}"
                : "";
            return $"BIND_OBJECT {Variable} IS_A {typeStr}{ClassName}{whereStr}";
        }
    }
}
