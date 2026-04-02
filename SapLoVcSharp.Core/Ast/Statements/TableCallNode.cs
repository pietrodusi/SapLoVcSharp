using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Statements
{
    /// <summary>
    /// Represents a table call (e.g., TABLE T_BIKE (MODEL = MODEL, COLOR = COLOR) or TABLE T_BIKE (...) IF condition).
    /// </summary>
    public class TableCallNode : StatementNode
    {
        public string TableName { get; }
        public List<TableArgument> Arguments { get; }
        public ExpressionNode? Condition { get; }

        public TableCallNode(
            string tableName,
            List<TableArgument> arguments,
            SourcePosition position,
            ExpressionNode? condition = null)
            : base(position)
        {
            TableName = tableName;
            Arguments = arguments;
            Condition = condition;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitTableCall(this);
        }

        public override string ToString() =>
            Condition != null
                ? $"TABLE {TableName}({string.Join(", ", Arguments)}) IF {Condition}"
                : $"TABLE {TableName}({string.Join(", ", Arguments)})";
    }

    /// <summary>
    /// Represents a table argument mapping (e.g., MODEL = MODEL or quantity_1 ?= $self.Quantity_1).
    /// </summary>
    public class TableArgument
    {
        public string TableColumn { get; }
        public ExpressionNode Value { get; }
        public bool IsOptional { get; }  // True for ?= (optional/default value)

        public TableArgument(string tableColumn, ExpressionNode value, bool isOptional = false)
        {
            TableColumn = tableColumn;
            Value = value;
            IsOptional = isOptional;
        }

        public override string ToString() =>
            IsOptional ? $"{TableColumn} ?= {Value}" : $"{TableColumn} = {Value}";
    }
}