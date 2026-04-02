using FluentAssertions;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Tests.Helpers;

namespace SapLoVcSharp.Core.Tests.Parsing
{
    public class SelectionConditionParserTests
    {
        [Fact]
        public void SelectionConditionParser_SimpleCondition_ShouldParse()
        {
            var result = ParserTestHelper.ParseSelectionCondition("HANDLEBAR = 'Racing'");

            result.Condition.Should().BeOfType<BinaryExpressionNode>();
        }

        [Fact]
        public void SelectionConditionParser_AndCondition_ShouldParse()
        {
            var result = ParserTestHelper.ParseSelectionCondition(
                "DOOR_WIDTH = 0.6100 AND DOOR_HEIGHT = 1.980");

            var binary = result.Condition.Should().BeOfType<BinaryExpressionNode>().Subject;
            binary.Op.Should().Be(BinaryOperator.And);
        }

        [Fact]
        public void SelectionConditionParser_OrCondition_ShouldParse()
        {
            var result = ParserTestHelper.ParseSelectionCondition(
                "MODEL = 'Racing' OR MODEL = 'Mountain'");

            var binary = result.Condition.Should().BeOfType<BinaryExpressionNode>().Subject;
            binary.Op.Should().Be(BinaryOperator.Or);
        }

        [Fact]
        public void SelectionConditionParser_NotCondition_ShouldParse()
        {
            var result = ParserTestHelper.ParseSelectionCondition(
                "NOT ((DOOR_WIDTH = 0.6100 AND DOOR_HEIGHT = 1.980))");

            result.Condition.Should().BeOfType<UnaryExpressionNode>();
        }

        [Fact]
        public void SelectionConditionParser_TableCall_ShouldParse()
        {
            var result = ParserTestHelper.ParseSelectionCondition(
                "TABLE T_SEL_SADDLE (COUNTER = '10', SADDLE_SUPPORT = SADDLE_SUPPORT)");

            result.Condition.Should().BeOfType<TableCallExpressionNode>();
        }

        [Fact]
        public void SelectionConditionParser_RealWorldExample_ShouldParse()
        {
            var source = @"NOT ((DOOR_WIDTH = 0.6100 and DOOR_HEIGHT = 1.980) or 
                               (DOOR_WIDTH = 0.7350 and DOOR_HEIGHT = 1.980))";

            var result = ParserTestHelper.ParseSelectionCondition(source);

            result.Condition.Should().BeOfType<UnaryExpressionNode>();
        }
    }
}