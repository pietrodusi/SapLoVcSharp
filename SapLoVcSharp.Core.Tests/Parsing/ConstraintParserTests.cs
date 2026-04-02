using FluentAssertions;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Ast.Statements;
using SapLoVcSharp.Core.Tests.Helpers;

namespace SapLoVcSharp.Core.Tests.Parsing
{
    public class ConstraintParserTests
    {
        #region Basic Structure

        [Fact]
        public void ConstraintParser_MinimalConstraint_ShouldParse()
        {
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                RESTRICTIONS: PC.COLOR = 'Red'";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Objects.Should().HaveCount(1);
            result.Condition.Should().BeNull();
            result.Restrictions.Should().HaveCount(1);
            result.Inferences.Should().BeEmpty();
        }

        [Fact]
        public void ConstraintParser_FullConstraint_ShouldParse()
        {
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                CONDITION: PC.MODEL = 'Racing'
                RESTRICTIONS: PC.COLOR = 'Red'
                INFERENCES: PC.COLOR";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Objects.Should().HaveCount(1);
            result.Condition.Should().NotBeNull();
            result.Restrictions.Should().HaveCount(1);
            result.Inferences.Should().HaveCount(1);
        }

        [Fact]
        public void ConstraintParser_WithDots_ShouldParse()
        {
            var source = @"
                OBJECTS: PC IS_A (300) BIKE.
                CONDITION: PC.MODEL = 'Racing'.
                RESTRICTIONS: PC.COLOR = 'Red'.
                INFERENCES: PC.COLOR.";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Should().NotBeNull();
        }

        [Fact]
        public void ConstraintParser_WithoutDots_ShouldParse()
        {
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                CONDITION: PC.MODEL = 'Racing'
                RESTRICTIONS: PC.COLOR = 'Red'
                INFERENCES: PC.COLOR";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Should().NotBeNull();
        }

        #endregion

        #region Objects Section

        [Fact]
        public void ConstraintParser_SingleObject_ShouldParse()
        {
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                RESTRICTIONS: FALSE";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Objects.Should().HaveCount(1);
            result.Objects[0].Variable.Should().Be("PC");
            result.Objects[0].ClassType.Should().Be("300");
            result.Objects[0].ClassName.Should().Be("BIKE");
        }

        [Fact]
        public void ConstraintParser_MultipleObjects_ShouldParse()
        {
            var source = @"
                OBJECTS: 
                    PC IS_A (300) BIKE,
                    PT IS_A (001) PRINTER
                RESTRICTIONS: FALSE";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Objects.Should().HaveCount(2);
            result.Objects[0].Variable.Should().Be("PC");
            result.Objects[1].Variable.Should().Be("PT");
        }

        [Fact]
        public void ConstraintParser_ObjectWithWhereClause_ShouldParse()
        {
            var source = @"
                OBJECTS: PC IS_A (300) BIKE WHERE MOD = MODEL; COL = COLOR
                RESTRICTIONS: FALSE";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Objects[0].VariableMappings.Should().NotBeNull();
            result.Objects[0].VariableMappings.Should().ContainKey("MOD");
            result.Objects[0].VariableMappings.Should().ContainKey("COL");
            result.Objects[0].VariableMappings!["MOD"].Should().Be("MODEL");
            result.Objects[0].VariableMappings["COL"].Should().Be("COLOR");
        }

        #endregion

        #region Condition Section

        [Fact]
        public void ConstraintParser_SimpleCondition_ShouldParse()
        {
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                CONDITION: PC.MODEL = 'Racing'
                RESTRICTIONS: FALSE";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Condition.Should().BeOfType<BinaryExpressionNode>();
        }

        [Fact]
        public void ConstraintParser_ComplexCondition_ShouldParse()
        {
            var source = @"
        OBJECTS: PC IS_A (300) BIKE
        CONDITION: PC.MODEL = 'Racing' AND PC.WEIGHT > 10
        RESTRICTIONS: PC.COLOR = 'Red' IF PC.PRICE < 1000, PC.COLOR = 'Blue' IF PC.PRICE >= 1000";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Condition.Should().BeOfType<BinaryExpressionNode>();
            var and = (BinaryExpressionNode)result.Condition!;
            and.Op.Should().Be(BinaryOperator.And);

            // Left side should be: PC.MODEL = 'Racing'
            and.Left.Should().BeOfType<BinaryExpressionNode>();
            var leftBinary = (BinaryExpressionNode)and.Left;
            leftBinary.Left.Should().BeOfType<ConstraintMemberAccessNode>();

            // Right side should be: PC.WEIGHT > 10
            and.Right.Should().BeOfType<BinaryExpressionNode>();
        }

        [Fact]
        public void ConstraintParser_ConditionWithPartOf_ShouldParse()
        {
            var source = @"
                OBJECTS: 
                    SERVER IS_A (300) C_SERVER,
                    NET IS_A (300) C_NET
                CONDITION: PART_OF(SERVER, NET)
                RESTRICTIONS: FALSE";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Condition.Should().NotBeNull();
        }

        #endregion

        #region Restrictions Section

        [Fact]
        public void ConstraintParser_SingleRestriction_ShouldParse()
        {
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                RESTRICTIONS: PC.COLOR = 'Red'";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Restrictions.Should().HaveCount(1);
        }

        [Fact]
        public void ConstraintParser_MultipleRestrictions_ShouldParse()
        {
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                RESTRICTIONS: 
                    PC.COLOR = 'Red',
                    PC.WEIGHT = 10,
                    PC.PRICE = 1000";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Restrictions.Should().HaveCount(3);
        }

        [Fact]
        public void ConstraintParser_RestrictionWithTableCall_ShouldParse()
        {
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                RESTRICTIONS: TABLE T_BIKE (MODEL = PC.MODEL, COLOR = PC.COLOR)";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Restrictions.Should().HaveCount(1);
            result.Restrictions[0].Should().BeOfType<TableCallNode>();
        }

        [Fact]
        public void ConstraintParser_RestrictionWithFalse_ShouldParse()
        {
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                CONDITION: PC.CPU = '486' AND PC.EXTRAS = 'CO_processor'
                RESTRICTIONS: FALSE";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Restrictions.Should().HaveCount(1);
            result.Restrictions[0].Should().BeOfType<ReturnInconsistencyNode>();
        }

        #endregion

        #region Inferences Section

        [Fact]
        public void ConstraintParser_SingleInference_ShouldParse()
        {
            var source = @"
                OBJECTS: PC IS_A (300) BIKE WHERE COL = COLOR
                RESTRICTIONS: FALSE
                INFERENCES: COL";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Inferences.Should().HaveCount(1);
            result.Inferences[0].Should().Be("COL");
        }

        [Fact]
        public void ConstraintParser_MultipleInferences_ShouldParse()
        {
            var source = @"
                OBJECTS: PC IS_A (300) BIKE WHERE MOD = MODEL; COL = COLOR; W = WEIGHT
                RESTRICTIONS: FALSE
                INFERENCES: MOD, COL, W";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Inferences.Should().HaveCount(3);
            result.Inferences.Should().Contain("MOD");
            result.Inferences.Should().Contain("COL");
            result.Inferences.Should().Contain("W");
        }

        #endregion

        #region Real World Examples

        [Fact]
        public void ConstraintParser_RealWorldExample1_ShouldParse()
        {
            var source = @"
                OBJECTS: PC IS_A (300) PC where MOD = Model; Typ = Bike_type; FH = Frame_height
                CONDITION: MOD = 'Racing'
                RESTRICTIONS: FH = 46
                INFERENCES: FH";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Objects.Should().HaveCount(1);
            result.Condition.Should().NotBeNull();
            result.Restrictions.Should().HaveCount(1);
            result.Restrictions[0].Should().BeOfType<AssignmentNode>();
            result.Inferences.Should().HaveCount(1);
        }

        [Fact]
        public void ConstraintParser_RealWorldExample2_WithMultilineTableCall()
        {
            var source = @"
                OBJECTS:
                PC IS_A (300) PC where
                 lv_CHAR1 = CHAR1;
                 lv_CHAR2 = CHAR2;
                 lv_CHAR3 = CHAR3;
                 lv_CHAR4 = CHAR4;
                 lv_CHAR5 = CHAR5;
                 lv_CHAR6 = CHAR6.

                CONDITION:
                lv_CHAR6 = 'TRUE'.

                RESTRICTIONS:
                TABLE MY_TABLE
                 (CHAR1=lv_CHAR1,
                   CHAR2 = lv_CHAR2,
                   CHAR3 = lv_CHAR3,
                   CHAR4 = lv_CHAR4,
                   CHAR5 = lv_CHAR5  ).

                INFERENCES:
                lv_CHAR1,
                lv_CHAR2,
                lv_CHAR3,
                lv_CHAR4,
                lv_CHAR5.";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Objects.Should().HaveCount(1);
            result.Objects[0].Variable.Should().Be("PC");
            result.Objects[0].VariableMappings.Should().HaveCount(6);

            result.Condition.Should().NotBeNull();

            result.Restrictions.Should().HaveCount(1);
            var tableCall = result.Restrictions[0].Should().BeOfType<TableCallNode>().Subject;
            tableCall.TableName.Should().Be("MY_TABLE");
            tableCall.Arguments.Should().HaveCount(5);

            result.Inferences.Should().HaveCount(5);
        }

        [Fact]
        public void ConstraintParser_NetworkExample_ShouldParse()
        {
            var source = @"
                OBJECTS: 
                    Server IS_A (300) c_server WHERE server_os = c_operating_system,
                    Workstation IS_A (300) c_workstation WHERE workstation_os = c_operating_system,
                    Net IS_A (300) c_net
                CONDITION:
                    PART_OF(server, net) AND PART_OF(workstation, net) AND server_os = 'OS1'
                RESTRICTIONS:
                    workstation_os = server_os
                INFERENCES:
                    workstation_os";

            var result = ParserTestHelper.ParseConstraint(source);

            result.Objects.Should().HaveCount(3);
            result.Condition.Should().NotBeNull();
            result.Restrictions.Should().HaveCount(1);
            result.Inferences.Should().HaveCount(1);
        }

        #endregion
    }
}