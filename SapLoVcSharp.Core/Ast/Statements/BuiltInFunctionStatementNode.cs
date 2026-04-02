using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Statements
{
    /// <summary>
    /// Represents a built-in function call as a statement.
    /// Examples: $SET_DEFAULT($SELF, HEIGHT, 100), $SUM_PARTS($PARENT, WEIGHT)
    /// </summary>
    public class BuiltInFunctionCallNode : StatementNode
    {
        public BuiltInFunctionType FunctionType { get; }
        public List<ExpressionNode> Arguments { get; }

        public BuiltInFunctionCallNode(
            BuiltInFunctionType functionType,
            List<ExpressionNode> arguments,
            SourcePosition position)
            : base(position)
        {
            FunctionType = functionType;
            Arguments = arguments;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitBuiltInFunctionCall(this);
        }

        public override string ToString() =>
            $"${FunctionType}({string.Join(", ", Arguments)})";
    }

    /// <summary>
    /// Types of built-in functions available in procedures.
    /// </summary>
    public enum BuiltInFunctionType
    {
        SetDefault,
        DelDefault,
        SumParts,
        CountParts,
        SetPricingFactor,
    }
}