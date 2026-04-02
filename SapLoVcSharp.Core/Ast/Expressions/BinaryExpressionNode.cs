using SapLoVcSharp.Core.Common;
using System.Text.Json.Serialization;

namespace SapLoVcSharp.Core.Ast.Expressions
{
    /// <summary>
    /// Represents a binary expression (e.g., a + b, a AND b, a = b).
    /// </summary>
    public class BinaryExpressionNode : ExpressionNode
    {
        public ExpressionNode Left { get; }
        public BinaryOperator Op { get; }
        public ExpressionNode Right { get; }

        public BinaryExpressionNode(
            ExpressionNode left,
            BinaryOperator op,
            ExpressionNode right,
            SourcePosition position)
            : base(position)
        {
            Left = left;
            Op = op;
            Right = right;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitBinaryExpression(this);
        }

        public override string ToString() => $"Binary({Left} {Op} {Right})";
    }

    public enum BinaryOperator
    {
        // Arithmetic
        Add,            // +
        Subtract,       // -
        Multiply,       // *
        Divide,         // /

        // Comparison
        Equal,          // = or EQ
        NotEqual,       // <> or NE
        Less,           // < or LT
        LessEqual,      // <= or LE
        Greater,        // > or GT
        GreaterEqual,   // >= or GE

        // Logical
        And,            // AND
        Or,             // OR

        // String
        Concat,         // ||
    }
}