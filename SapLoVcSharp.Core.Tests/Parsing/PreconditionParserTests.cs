using FluentAssertions;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Tests.Helpers;

namespace SapLoVcSharp.Core.Tests.Parsing
{
    public class PreconditionParserTests
    {
        [Fact]
        public void PreconditionParser_SimpleCondition_ShouldParse()
        {
            var result = ParserTestHelper.ParsePrecondition("MODEL = 'Racing'");

            result.Condition.Should().BeOfType<BinaryExpressionNode>();
        }

        [Fact]
        public void PreconditionParser_AndCondition_ShouldParse()
        {
            var result = ParserTestHelper.ParsePrecondition("MODEL = 'Racing' AND COLOR = 'Red'");

            var binary = result.Condition.Should().BeOfType<BinaryExpressionNode>().Subject;
            binary.Op.Should().Be(BinaryOperator.And);
        }

        [Fact]
        public void PreconditionParser_ComplexCondition_ShouldParse()
        {
            var result = ParserTestHelper.ParsePrecondition(
                "MODEL = 'Tandem' AND SPECIFIED MODEL");

            result.Condition.Should().BeOfType<BinaryExpressionNode>();
        }

        [Fact]
        public void PreconditionParser_TableCall_ShouldParse()
        {
            var result = ParserTestHelper.ParsePrecondition("TABLE T_BIKE (MODEL = MODEL)");

            result.Condition.Should().BeOfType<TableCallExpressionNode>();
        }

        [Fact]
        public void PreconditionParser_TableCallWithAnd_ShouldParse()
        {
            var result = ParserTestHelper.ParsePrecondition(
                "TABLE T_VALID (MODEL = MODEL) AND COLOR = 'Red'");

            var binary = result.Condition.Should().BeOfType<BinaryExpressionNode>().Subject;
            binary.Left.Should().BeOfType<TableCallExpressionNode>();
        }

        [Fact]
        public void PreconditionParser_InExpression_ShouldParse()
        {
            var result = ParserTestHelper.ParsePrecondition("COLOR IN ('Red', 'Blue', 'Green')");

            result.Condition.Should().BeOfType<InExpressionNode>();
        }

        [Fact]
        public void PreconditionParser_RealWorldExample_ShouldParse()
        {
            var source = "MODEL eq 'Racing' and Specified MODEL";

            var result = ParserTestHelper.ParsePrecondition(source);

            result.Condition.Should().BeOfType<BinaryExpressionNode>();
        }
    }
}