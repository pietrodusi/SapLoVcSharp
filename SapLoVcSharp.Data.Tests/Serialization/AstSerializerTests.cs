using FluentAssertions;
using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Ast.Statements;
using SapLoVcSharp.Core.Lexing;
using SapLoVcSharp.Core.Parsing.DependencyParsers;
using SapLoVcSharp.Data.Serialization;
using Xunit.Abstractions;

namespace SapLoVcSharp.Core.Tests.Data
{
    public class AstSerializerTests
    {
        private readonly AstSerializer _serializer = new();
        private readonly ITestOutputHelper _output;

        public AstSerializerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Serialize_Procedure_ShouldProduceJson()
        {
            // Arrange
            var source = "$SELF.COLOR = 'Red' IF MODEL = 'Racing'";
            var ast = ParseProcedure(source);

            // Act
            var json = _serializer.Serialize(ast);

            // Debug output
            _output.WriteLine("Serialized JSON:");
            _output.WriteLine(json);

            // Assert
            json.Should().NotBeNullOrEmpty();
            json.Should().Contain("$type");
            json.Should().Contain("Procedure"); // Short discriminator, not "ProcedureNode"
            json.Should().Contain("statements");
            json.Should().Contain("Assignment"); // AssignmentNode with condition
            json.Should().Contain("condition"); // Should have condition field for IF clause
        }

        [Fact]
        public void Deserialize_Procedure_ShouldRecreateAst()
        {
            // Arrange
            var source = "$SELF.COLOR = 'Red' IF MODEL = 'Racing'";
            var originalAst = ParseProcedure(source);
            var json = _serializer.Serialize(originalAst);

            _output.WriteLine("Serialized JSON:");
            _output.WriteLine(json);

            // Act
            var deserializedAst = _serializer.Deserialize(json);

            // Assert
            deserializedAst.Should().NotBeNull();
            deserializedAst.Should().BeOfType<ProcedureNode>();

            var procedure = (ProcedureNode)deserializedAst;
            procedure.Statements.Should().HaveCount(originalAst.Statements.Count);
        }

        [Fact]
        public void RoundTrip_Procedure_ShouldPreserveStructure()
        {
            // Arrange
            var source = @"
                $SELF.COLOR = 'Red' IF MODEL = 'Racing',
                $SELF.WEIGHT = 10,
                $SET_DEFAULT($SELF, PRICE, 1000)
            ";
            var originalAst = ParseProcedure(source);

            // Act
            var json = _serializer.Serialize(originalAst);
            _output.WriteLine("Serialized JSON:");
            _output.WriteLine(json);

            var deserializedAst = _serializer.Deserialize<ProcedureNode>(json);

            // Assert
            deserializedAst.Should().NotBeNull();
            deserializedAst.Statements.Should().HaveCount(originalAst.Statements.Count);
            deserializedAst.Statements[0].Should().BeOfType<AssignmentNode>();
            deserializedAst.Statements[1].Should().BeOfType<AssignmentNode>();
            deserializedAst.Statements[2].Should().BeOfType<BuiltInFunctionCallNode>();
        }

        [Fact]
        public void RoundTrip_Constraint_ShouldPreserveStructure()
        {
            // Arrange
            var source = @"
                OBJECTS: PC IS_A (300) BIKE WHERE MODEL = MODEL; COLOR = COLOR.
                CONDITION: MODEL = 'Racing'.
                RESTRICTIONS: COLOR = 'Red'.
                INFERENCES: COLOR.
            ";
            var originalAst = ParseConstraint(source);

            // Act
            var json = _serializer.Serialize(originalAst);
            _output.WriteLine("Serialized JSON:");
            _output.WriteLine(json);

            var deserializedAst = _serializer.Deserialize<ConstraintNode>(json);

            // Assert
            deserializedAst.Should().NotBeNull();
            deserializedAst.Objects.Should().HaveCount(originalAst.Objects.Count);
            deserializedAst.Restrictions.Should().HaveCount(originalAst.Restrictions.Count);
            deserializedAst.Inferences.Should().BeEquivalentTo(originalAst.Inferences);
        }

        [Fact]
        public void RoundTrip_ComplexConstraint_ShouldPreserveAllDetails()
        {
            // Arrange
            var source = @"
                OBJECTS:
                    PC IS_A (300) PC_CLASS WHERE
                        lv_CHAR1 = CHAR1;
                        lv_CHAR2 = CHAR2;
                        lv_CHAR3 = CHAR3.
                CONDITION:
                    lv_CHAR3 = 'TEST' AND lv_CHAR1 IN ('Val1', 'Val2').
                RESTRICTIONS:
                    TABLE TABLE_ID (CHAR1 = lv_CHAR1, CHAR2 = lv_CHAR2).
                INFERENCES:
                    lv_CHAR2.
            ";
            var originalAst = ParseConstraint(source);

            // Act
            var json = _serializer.Serialize(originalAst);
            _output.WriteLine("Serialized JSON:");
            _output.WriteLine(json);

            var deserializedAst = _serializer.Deserialize<ConstraintNode>(json);

            // Assert
            deserializedAst.Should().NotBeNull();
            deserializedAst.Objects.Should().HaveCount(1);

            var obj = deserializedAst.Objects[0];
            obj.VariableMappings.Should().HaveCount(3);
            obj.VariableMappings.Should().ContainKey("lv_CHAR1");
            obj.VariableMappings.Should().ContainKey("lv_CHAR2");
            obj.VariableMappings.Should().ContainKey("lv_CHAR3");

            deserializedAst.Condition.Should().NotBeNull();
            deserializedAst.Restrictions.Should().HaveCount(1);
            // Constraints use TableCallNode (statement) not TableCallExpressionNode (expression)
            // because RESTRICTIONS is parsed as statements by ProcedureParser
            deserializedAst.Restrictions[0].Should().BeOfType<TableCallNode>();
            deserializedAst.Inferences.Should().Contain("lv_CHAR2");
        }

        private ProcedureNode ParseProcedure(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new ProcedureParser(tokens);
            return parser.Parse();
        }

        private ConstraintNode ParseConstraint(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new ConstraintParser(tokens);
            return parser.Parse();
        }
    }
}