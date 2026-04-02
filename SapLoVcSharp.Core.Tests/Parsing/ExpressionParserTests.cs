using FluentAssertions;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Lexing;
using SapLoVcSharp.Core.Parsing;
using SapLoVcSharp.Core.Tests.Helpers;

namespace SapLoVcSharp.Core.Tests.Parsing
{
    public class ExpressionParserTests
    {
        #region Literals

        [Fact]
        public void ExpressionParser_Number_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("123");

            result.Should().BeOfType<LiteralNode>();
            var literal = (LiteralNode)result;
            literal.Type.Should().Be(LiteralType.Number);
            literal.Value.Should().Be(123.0);
        }

        [Fact]
        public void ExpressionParser_DecimalNumber_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("123.45");

            var literal = result.Should().BeOfType<LiteralNode>().Subject;
            literal.Type.Should().Be(LiteralType.Number);
            literal.Value.Should().Be(123.45);
        }

        [Fact]
        public void ExpressionParser_NegativeNumber_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("-123");

            var literal = result.Should().BeOfType<LiteralNode>().Subject;
            literal.Type.Should().Be(LiteralType.Number);
            literal.Value.Should().Be(-123.0);
        }

        [Fact]
        public void ExpressionParser_String_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("'Hello World'");

            var literal = result.Should().BeOfType<LiteralNode>().Subject;
            literal.Type.Should().Be(LiteralType.String);
            literal.Value.Should().Be("Hello World");
        }

        [Fact]
        public void ExpressionParser_True_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("TRUE");

            var literal = result.Should().BeOfType<LiteralNode>().Subject;
            literal.Type.Should().Be(LiteralType.Boolean);
            literal.Value.Should().Be(true);
        }

        [Fact]
        public void ExpressionParser_False_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("FALSE");

            var literal = result.Should().BeOfType<LiteralNode>().Subject;
            literal.Type.Should().Be(LiteralType.Boolean);
            literal.Value.Should().Be(false);
        }

        #endregion

        #region Identifiers

        [Fact]
        public void ExpressionParser_Identifier_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("COLOR");

            var identifier = result.Should().BeOfType<IdentifierNode>().Subject;
            identifier.Name.Should().Be("COLOR");
        }

        [Fact]
        public void ExpressionParser_IdentifierWithUnderscore_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("BIKE_TYPE");

            var identifier = result.Should().BeOfType<IdentifierNode>().Subject;
            identifier.Name.Should().Be("BIKE_TYPE");
        }

        #endregion

        #region Member Access

        [Theory]
        [InlineData("$SELF.COLOR", ObjectReference.Self, "COLOR")]
        [InlineData("$PARENT.MODEL", ObjectReference.Parent, "MODEL")]
        [InlineData("$ROOT.WEIGHT", ObjectReference.Root, "WEIGHT")]
        public void ExpressionParser_MemberAccess_ShouldParse(string source, ObjectReference expectedObj, string expectedMember)
        {
            var result = ParserTestHelper.ParseExpression(source);

            var memberAccess = result.Should().BeOfType<MemberAccessNode>().Subject;
            memberAccess.Obj.Should().Be(expectedObj);
            memberAccess.MemberName.Should().Be(expectedMember);
        }

        #endregion

        #region Binary Expressions

        [Theory]
        [InlineData("5 + 3", BinaryOperator.Add)]
        [InlineData("5 - 3", BinaryOperator.Subtract)]
        [InlineData("5 * 3", BinaryOperator.Multiply)]
        [InlineData("5 / 3", BinaryOperator.Divide)]
        public void ExpressionParser_ArithmeticOperators_ShouldParse(string source, BinaryOperator expectedOp)
        {
            var result = ParserTestHelper.ParseExpression(source);

            var binary = result.Should().BeOfType<BinaryExpressionNode>().Subject;
            binary.Op.Should().Be(expectedOp);
            binary.Left.Should().BeOfType<LiteralNode>();
            binary.Right.Should().BeOfType<LiteralNode>();
        }

        [Theory]
        [InlineData("A = B", BinaryOperator.Equal)]
        [InlineData("A <> B", BinaryOperator.NotEqual)]
        [InlineData("A < B", BinaryOperator.Less)]
        [InlineData("A <= B", BinaryOperator.LessEqual)]
        [InlineData("A > B", BinaryOperator.Greater)]
        [InlineData("A >= B", BinaryOperator.GreaterEqual)]
        [InlineData("A EQ B", BinaryOperator.Equal)]
        [InlineData("A NE B", BinaryOperator.NotEqual)]
        [InlineData("A LT B", BinaryOperator.Less)]
        public void ExpressionParser_ComparisonOperators_ShouldParse(string source, BinaryOperator expectedOp)
        {
            var result = ParserTestHelper.ParseExpression(source);

            var binary = result.Should().BeOfType<BinaryExpressionNode>().Subject;
            binary.Op.Should().Be(expectedOp);
        }

        [Theory]
        [InlineData("A AND B", BinaryOperator.And)]
        [InlineData("A OR B", BinaryOperator.Or)]
        public void ExpressionParser_LogicalOperators_ShouldParse(string source, BinaryOperator expectedOp)
        {
            var result = ParserTestHelper.ParseExpression(source);

            var binary = result.Should().BeOfType<BinaryExpressionNode>().Subject;
            binary.Op.Should().Be(expectedOp);
        }

        [Fact]
        public void ExpressionParser_StringConcatenation_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("'Hello' || 'World'");

            var binary = result.Should().BeOfType<BinaryExpressionNode>().Subject;
            binary.Op.Should().Be(BinaryOperator.Concat);
        }

        #endregion

        #region Operator Precedence

        [Fact]
        public void ExpressionParser_Precedence_MultiplicationBeforeAddition()
        {
            // 2 + 3 * 4 should be parsed as 2 + (3 * 4)
            var result = ParserTestHelper.ParseExpression("2 + 3 * 4");

            var add = result.Should().BeOfType<BinaryExpressionNode>().Subject;
            add.Op.Should().Be(BinaryOperator.Add);

            add.Left.Should().BeOfType<LiteralNode>();

            var multiply = add.Right.Should().BeOfType<BinaryExpressionNode>().Subject;
            multiply.Op.Should().Be(BinaryOperator.Multiply);
        }

        [Fact]
        public void ExpressionParser_Precedence_ParenthesesOverridePrecedence()
        {
            // (2 + 3) * 4 should be parsed as (2 + 3) * 4
            var result = ParserTestHelper.ParseExpression("(2 + 3) * 4");

            var multiply = result.Should().BeOfType<BinaryExpressionNode>().Subject;
            multiply.Op.Should().Be(BinaryOperator.Multiply);

            var add = multiply.Left.Should().BeOfType<BinaryExpressionNode>().Subject;
            add.Op.Should().Be(BinaryOperator.Add);

            multiply.Right.Should().BeOfType<LiteralNode>();
        }

        [Fact]
        public void ExpressionParser_Precedence_AndBeforeOr()
        {
            // A OR B AND C should be parsed as A OR (B AND C)
            var result = ParserTestHelper.ParseExpression("A OR B AND C");

            var or = result.Should().BeOfType<BinaryExpressionNode>().Subject;
            or.Op.Should().Be(BinaryOperator.Or);

            or.Left.Should().BeOfType<IdentifierNode>();

            var and = or.Right.Should().BeOfType<BinaryExpressionNode>().Subject;
            and.Op.Should().Be(BinaryOperator.And);
        }

        [Fact]
        public void ExpressionParser_Precedence_ComparisonBeforeLogical()
        {
            // A = 1 AND B = 2 should be parsed as (A = 1) AND (B = 2)
            var result = ParserTestHelper.ParseExpression("A = 1 AND B = 2");

            var and = result.Should().BeOfType<BinaryExpressionNode>().Subject;
            and.Op.Should().Be(BinaryOperator.And);

            and.Left.Should().BeOfType<BinaryExpressionNode>();
            and.Right.Should().BeOfType<BinaryExpressionNode>();
        }

        #endregion

        #region Unary Expressions

        [Fact]
        public void ExpressionParser_UnaryNot_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("NOT A");

            var unary = result.Should().BeOfType<UnaryExpressionNode>().Subject;
            unary.Op.Should().Be(UnaryOperator.Not);
            unary.Operand.Should().BeOfType<IdentifierNode>();
        }

        [Fact]
        public void ExpressionParser_UnaryMinus_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("-A");

            var unary = result.Should().BeOfType<UnaryExpressionNode>().Subject;
            unary.Op.Should().Be(UnaryOperator.Negate);
            unary.Operand.Should().BeOfType<IdentifierNode>();
        }

        #endregion

        #region Function Calls

        [Theory]
        [InlineData("SIN(X)")]
        [InlineData("COS(X)")]
        [InlineData("SQRT(25)")]
        [InlineData("ABS(-5)")]
        public void ExpressionParser_MathFunction_ShouldParse(string source)
        {
            var result = ParserTestHelper.ParseExpression(source);

            var call = result.Should().BeOfType<CallExpressionNode>().Subject;
            call.Arguments.Should().HaveCount(1);
        }

        [Fact]
        public void ExpressionParser_FunctionWithMultipleArguments_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("MAX(A, B, C)");

            var call = result.Should().BeOfType<CallExpressionNode>().Subject;
            call.FunctionName.Should().Be("MAX");
            call.Arguments.Should().HaveCount(3);
        }

        [Fact]
        public void ExpressionParser_NestedFunctionCalls_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("SQRT(ABS(-25))");

            var outerCall = result.Should().BeOfType<CallExpressionNode>().Subject;
            outerCall.FunctionName.Should().Be("SQRT");

            var innerCall = outerCall.Arguments[0].Should().BeOfType<CallExpressionNode>().Subject;
            innerCall.FunctionName.Should().Be("ABS");
        }

        #endregion

        #region IN Expression

        [Fact]
        public void ExpressionParser_InExpression_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("COLOR IN ('Red', 'Blue', 'Green')");

            var inExpr = result.Should().BeOfType<InExpressionNode>().Subject;
            inExpr.Expression.Should().BeOfType<IdentifierNode>();
            inExpr.Values.Should().HaveCount(3);
            inExpr.Values.Should().AllBeOfType<LiteralNode>();
        }

        [Fact]
        public void ExpressionParser_InExpressionWithNumbers_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("SIZE IN (10, 20, 30)");

            var inExpr = result.Should().BeOfType<InExpressionNode>().Subject;
            inExpr.Values.Should().HaveCount(3);
        }

        #endregion

        #region SPECIFIED

        [Fact]
        public void ExpressionParser_Specified_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("SPECIFIED COLOR");

            var specified = result.Should().BeOfType<SpecifiedNode>().Subject;
            specified.Expression.Should().BeOfType<IdentifierNode>();
        }

        [Fact]
        public void ExpressionParser_SpecifiedWithMemberAccess_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("SPECIFIED $SELF.COLOR");

            var specified = result.Should().BeOfType<SpecifiedNode>().Subject;
            specified.Expression.Should().BeOfType<MemberAccessNode>();
        }

        [Fact]
        public void ExpressionParser_Specified_Postfix_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("COLOR SPECIFIED");

            var specified = result.Should().BeOfType<SpecifiedNode>().Subject;
            var identifier = specified.Expression.Should().BeOfType<IdentifierNode>().Subject;
            identifier.Name.Should().Be("COLOR");
        }

        [Fact]
        public void ExpressionParser_Specified_PostfixWithMemberAccess_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("$SELF.COLOR SPECIFIED");

            var specified = result.Should().BeOfType<SpecifiedNode>().Subject;
            specified.Expression.Should().BeOfType<MemberAccessNode>();
        }

        [Fact]
        public void ExpressionParser_Specified_BothFormsEquivalent_ShouldParse()
        {
            var prefix = ParserTestHelper.ParseExpression("SPECIFIED MODEL");
            var postfix = ParserTestHelper.ParseExpression("MODEL SPECIFIED");

            prefix.Should().BeOfType<SpecifiedNode>();
            postfix.Should().BeOfType<SpecifiedNode>();

            // Both should have the same structure (MODEL as the expression)
            var prefixSpec = (SpecifiedNode)prefix;
            var postfixSpec = (SpecifiedNode)postfix;

            prefixSpec.Expression.Should().BeOfType<IdentifierNode>();
            postfixSpec.Expression.Should().BeOfType<IdentifierNode>();
        }

        [Fact]
        public void ExpressionParser_Specified_InComplexExpression_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("MODEL = 'Racing' AND COLOR SPECIFIED");

            var binary = result.Should().BeOfType<BinaryExpressionNode>().Subject;
            binary.Op.Should().Be(BinaryOperator.And);
            binary.Right.Should().BeOfType<SpecifiedNode>();
        }

        #endregion

        #region Table Calls

        [Fact]
        public void ExpressionParser_TableCall_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("TABLE T_BIKE (MODEL = MODEL, COLOR = COLOR)");

            var tableCall = result.Should().BeOfType<TableCallExpressionNode>().Subject;
            tableCall.TableName.Should().Be("T_BIKE");
            tableCall.Arguments.Should().HaveCount(2);

            tableCall.Arguments[0].TableColumn.Should().Be("MODEL");
            tableCall.Arguments[1].TableColumn.Should().Be("COLOR");
        }

        [Fact]
        public void ExpressionParser_TableCallWithOptionalParameter_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("TABLE T_QTY (qty ?= $SELF.QTY)");

            var tableCall = result.Should().BeOfType<TableCallExpressionNode>().Subject;
            tableCall.Arguments.Should().HaveCount(1);
            tableCall.Arguments[0].IsOptional.Should().BeTrue();
        }

        [Fact]
        public void ExpressionParser_TableCallInBinaryExpression_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression("TABLE T_VALID (MODEL = MODEL) AND COLOR = 'Red'");

            var binary = result.Should().BeOfType<BinaryExpressionNode>().Subject;
            binary.Left.Should().BeOfType<TableCallExpressionNode>();
            binary.Op.Should().Be(BinaryOperator.And);
        }

        #endregion

        #region Complex Expressions

        [Fact]
        public void ExpressionParser_ComplexExpression_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression(
                "$SELF.PRICE = ($SELF.BASE_PRICE + $SELF.EXTRAS) * 1.2 IF MODEL = 'Premium'");

            // Should parse as a complex binary expression
            result.Should().BeOfType<BinaryExpressionNode>();
        }

        [Fact]
        public void ExpressionParser_MultipleConditions_ShouldParse()
        {
            var result = ParserTestHelper.ParseExpression(
                "MODEL = 'Racing' AND (COLOR = 'Red' OR COLOR = 'Blue') AND WEIGHT < 10");

            var and1 = result.Should().BeOfType<BinaryExpressionNode>().Subject;
            and1.Op.Should().Be(BinaryOperator.And);

            var and2 = and1.Left.Should().BeOfType<BinaryExpressionNode>().Subject;
            and2.Op.Should().Be(BinaryOperator.And);
        }

        #endregion
    }
}