using SapLoVcSharp.Core.Ast.Statements;
using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Expressions
{
    /// <summary>
    /// Represents a table call used as an expression (in constraints, preconditions, etc.).
    /// Example: TABLE T_BIKE (MODEL = MODEL, COLOR = COLOR)
    /// </summary>
    public class TableCallExpressionNode : ExpressionNode
    {
        public string TableName { get; }
        public List<TableArgument> Arguments { get; }

        public TableCallExpressionNode(
            string tableName,
            List<TableArgument> arguments,
            SourcePosition position)
            : base(position)
        {
            TableName = tableName;
            Arguments = arguments;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitTableCallExpression(this);
        }

        public override string ToString() =>
            $"TABLE {TableName}({string.Join(", ", Arguments)})";
    }
}