using FluentAssertions;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Ast.Statements;
using SapLoVcSharp.Core.Tests.Helpers;

namespace SapLoVcSharp.Core.Tests.Parsing
{
    public class ProcedureParserTests
    {
        [Fact]
        public void ProcedureParser_SimpleAssignment_ShouldParse()
        {
            var result = ParserTestHelper.ParseProcedure("$SELF.COLOR = 'Red'");

            result.Statements.Should().HaveCount(1);
            var assignment = result.Statements[0].Should().BeOfType<AssignmentNode>().Subject;
            assignment.Target.Should().NotBeNull();
            assignment.Value.Should().NotBeNull();
        }

        [Fact]
        public void ProcedureParser_ConditionalAssignment_ShouldParse()
        {
            var result = ParserTestHelper.ParseProcedure("$SELF.COLOR = 'Red' IF MODEL = 'Racing'");

            result.Statements.Should().HaveCount(1);
            var assignment = result.Statements[0].Should().BeOfType<AssignmentNode>().Subject;
            assignment.Target.Should().NotBeNull();
            assignment.Value.Should().NotBeNull();
            assignment.Condition.Should().NotBeNull();
        }

        [Fact]
        public void ProcedureParser_MultipleStatements_ShouldParse()
        {
            var result = ParserTestHelper.ParseProcedure(
                "$SELF.COLOR = 'Red', $SELF.WEIGHT = 10, $SELF.PRICE = 1000");

            result.Statements.Should().HaveCount(3);
            result.Statements.Should().AllBeOfType<AssignmentNode>();
        }

        [Fact]
        public void ProcedureParser_MixedStatements_ShouldParse()
        {
            var result = ParserTestHelper.ParseProcedure(
                "$SELF.COLOR = 'Red' IF MODEL = 'Racing', $SELF.WEIGHT = 10");

            result.Statements.Should().HaveCount(2);
            result.Statements[0].Should().BeOfType<AssignmentNode>();
            result.Statements[1].Should().BeOfType<AssignmentNode>();

            var conditional = result.Statements[0] as AssignmentNode;
            conditional!.Condition.Should().NotBeNull();

            var simple = result.Statements[1] as AssignmentNode;
            simple!.Condition.Should().BeNull();
        }

        [Fact]
        public void ProcedureParser_TableCall_ShouldParse()
        {
            var result = ParserTestHelper.ParseProcedure("TABLE T_BIKE (MODEL = MODEL, COLOR = COLOR)");

            result.Statements.Should().HaveCount(1);
            var tableCall = result.Statements[0].Should().BeOfType<TableCallNode>().Subject;
            tableCall.TableName.Should().Be("T_BIKE");
            tableCall.Arguments.Should().HaveCount(2);
        }

        [Fact]
        public void ProcedureParser_AssignmentAndTableCall_ShouldParse()
        {
            var result = ParserTestHelper.ParseProcedure(
                "$SELF.COLOR = 'Red', TABLE T_BIKE (MODEL = MODEL)");

            result.Statements.Should().HaveCount(2);
            result.Statements[0].Should().BeOfType<AssignmentNode>();
            result.Statements[1].Should().BeOfType<TableCallNode>();
        }

        [Fact]
        public void ProcedureParser_ArithmeticInAssignment_ShouldParse()
        {
            var result = ParserTestHelper.ParseProcedure("$SELF.WEIGHT = $SELF.WEIGHT + 1.5");

            result.Statements.Should().HaveCount(1);
            result.Statements[0].Should().BeOfType<AssignmentNode>();
        }

        [Fact]
        public void ProcedureParser_DefaultValueAssignment_ShouldParse()
        {
            var result = ParserTestHelper.ParseProcedure(
                "$SET_DEFAULT ($SELF, HEIGHT, 1.5 * $SELF.WIDTH)");

            result.Statements.Should().HaveCount(1);

            var builtInCall = result.Statements[0].Should().BeOfType<BuiltInFunctionCallNode>().Subject;
            builtInCall.FunctionType.Should().Be(BuiltInFunctionType.SetDefault);
            builtInCall.Arguments.Should().HaveCount(3);
        }

        [Fact]
        public void ProcedureParser_RealWorldExample_ShouldParse()
        {
            var source = @"
                $SELF.WEIGHT = 10 IF FRAME = 'Aluminum',
                $SELF.WEIGHT = 14 IF FRAME = 'Steel',
                $SELF.WEIGHT = $SELF.WEIGHT + 0.5 IF EXTRAS = 'Mudguard'";

            var result = ParserTestHelper.ParseProcedure(source);

            result.Statements.Should().HaveCount(3);
            result.Statements.Should().AllBeOfType<AssignmentNode>();

            foreach (var stmt in result.Statements.Cast<AssignmentNode>())
            {
                stmt.Condition.Should().NotBeNull();
            }
        }

        [Fact]
        public void ProcedureParser_ConditionalTableCall_ShouldParse()
        {
            var result = ParserTestHelper.ParseProcedure(
                "TABLE T_BIKE (MODEL = MODEL, COLOR = COLOR) IF MODEL = 'Racing'");

            result.Statements.Should().HaveCount(1);
            var tableCall = result.Statements[0].Should().BeOfType<TableCallNode>().Subject;
            tableCall.TableName.Should().Be("T_BIKE");
            tableCall.Arguments.Should().HaveCount(2);
            tableCall.Condition.Should().NotBeNull();
        }

        [Fact]
        public void ProcedureParser_ComplexRealWorldExample_ShouldParse()
        {
            var source = @"
                $SELF.WEIGHT = 10,
                $SELF.WEIGHT = 14 IF FRAME = 'Steel',
                TABLE T_BIKE_01 (
                    FRAME = $SELF.FRAME,
                    COLOR = $SELF.COLOR,
                    MODEL = $SELF.MODEL,
                    WEIGHT = $SELF.WEIGHT
                ) IF TABLE T_BIKE_01 (
                    FRAME = $SELF.FRAME,
                    COLOR = $SELF.COLOR
                ),
                TABLE T_BIKE_01 (
                    FRAME = $SELF.FRAME,
                    COLOR = $SELF.COLOR,
                    MODEL = $SELF.MODEL,
                    WEIGHT = $SELF.WEIGHT
                ) IF FRAME = 'Aluminum'";

            var result = ParserTestHelper.ParseProcedure(source);

            result.Statements.Should().HaveCount(4);
            var assignmentWithoutCondition = result.Statements[0].Should().BeOfType<AssignmentNode>().Subject;
            var assignmentWithCondition = result.Statements[1].Should().BeOfType<AssignmentNode>().Subject;
            var tableCallWithTableCondition = result.Statements[2].Should().BeOfType<TableCallNode>().Subject;
            var tableCallWithExpressionCondition = result.Statements[3].Should().BeOfType<TableCallNode>().Subject;

            assignmentWithoutCondition.Condition.Should().BeNull();
            assignmentWithCondition.Condition.Should().BeOfType<BinaryExpressionNode>();
            tableCallWithTableCondition.Condition.Should().BeOfType<TableCallExpressionNode>();
            tableCallWithExpressionCondition.Condition.Should().BeOfType<BinaryExpressionNode>();
        }
    }
}