using FluentAssertions;
using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Ast.Statements;
using SapLoVcSharp.Core.Lexing;
using SapLoVcSharp.Core.Parsing.DependencyParsers;
using SapLoVcSharp.Execution.Compilation;
using SapLoVcSharp.Execution.Instructions;

namespace SapLoVcSharp.Execution.Tests.Compilation
{
    /// <summary>
    /// Tests for expression compilation in the instruction compiler.
    /// Verifies that expressions are correctly compiled to instruction sequences.
    /// </summary>
    public class ExpressionCompilationTests
    {
        private readonly InstructionCompiler _compiler = new();

        private static ProcedureNode ParseProcedure(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new ProcedureParser(tokens);
            return parser.Parse();
        }

        [Fact]
        public void Compile_SimpleLiteral_EmitsLoadConst()
        {
            // Arrange: $SELF.COLOR = 'Red'
            var source = "$SELF.COLOR = 'Red'.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions.Should().ContainSingle(i => i is LoadConstInstruction);
            var loadConst = result.Instructions.OfType<LoadConstInstruction>().First();
            loadConst.Value.Should().Be("Red");
            loadConst.Type.Should().Be(LiteralType.String);
        }

        [Fact]
        public void Compile_NumericLiteral_EmitsLoadConstWithNumber()
        {
            // Arrange: $SELF.PRICE = 42
            var source = "$SELF.PRICE = 42.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            var loadConst = result.Instructions.OfType<LoadConstInstruction>().First();
            loadConst.Value.Should().Be(42.0);
            loadConst.Type.Should().Be(LiteralType.Number);
        }

        [Fact]
        public void Compile_BooleanLiteral_EmitsLoadConstWithBoolean()
        {
            // Arrange: $SELF.AVAILABLE = TRUE
            var source = "$SELF.AVAILABLE = TRUE.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            var loadConst = result.Instructions.OfType<LoadConstInstruction>().First();
            loadConst.Value.Should().Be(true);
            loadConst.Type.Should().Be(LiteralType.Boolean);
        }

        [Fact]
        public void Compile_IdentifierExpression_EmitsLoadVar()
        {
            // Arrange: $SELF.COLOR = $SELF.MODEL
            var source = "$SELF.COLOR = $SELF.MODEL.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions.Should().ContainSingle(i => i is LoadMemberInstruction);
            var loadMember = result.Instructions.OfType<LoadMemberInstruction>().First();
            loadMember.ObjectName.Should().Be("$SELF");
            loadMember.MemberName.Should().Be("MODEL");
        }

        [Fact]
        public void Compile_Addition_EmitsCorrectSequence()
        {
            // Arrange: $SELF.TOTAL = 10 + 20
            var source = "$SELF.TOTAL = 10 + 20.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert - Should be: LOAD_CONST 10, LOAD_CONST 20, ADD, STORE_MEMBER
            var instructions = result.Instructions;
            instructions[0].Should().BeOfType<LoadConstInstruction>()
                .Which.Value.Should().Be(10.0);
            instructions[1].Should().BeOfType<LoadConstInstruction>()
                .Which.Value.Should().Be(20.0);
            instructions[2].Should().BeOfType<AddInstruction>();
            instructions[3].Should().BeOfType<StoreMemberInstruction>();
        }

        [Fact]
        public void Compile_Subtraction_EmitsCorrectSequence()
        {
            // Arrange: $SELF.RESULT = 100 - 25
            var source = "$SELF.RESULT = 100 - 25.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions[0].Should().BeOfType<LoadConstInstruction>();
            result.Instructions[1].Should().BeOfType<LoadConstInstruction>();
            result.Instructions[2].Should().BeOfType<SubInstruction>();
        }

        [Fact]
        public void Compile_Multiplication_EmitsCorrectSequence()
        {
            // Arrange: $SELF.AREA = 5 * 10
            var source = "$SELF.AREA = 5 * 10.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions[2].Should().BeOfType<MulInstruction>();
        }

        [Fact]
        public void Compile_Division_EmitsCorrectSequence()
        {
            // Arrange: $SELF.AVERAGE = 100 / 4
            var source = "$SELF.AVERAGE = 100 / 4.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions[2].Should().BeOfType<DivInstruction>();
        }

        [Fact]
        public void Compile_ComplexArithmetic_EmitsCorrectSequence()
        {
            // Arrange: $SELF.RESULT = (10 + 20) * 3
            var source = "$SELF.RESULT = (10 + 20) * 3.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert - Should be: LOAD 10, LOAD 20, ADD, LOAD 3, MUL, STORE
            result.Instructions[0].Should().BeOfType<LoadConstInstruction>();
            result.Instructions[1].Should().BeOfType<LoadConstInstruction>();
            result.Instructions[2].Should().BeOfType<AddInstruction>();
            result.Instructions[3].Should().BeOfType<LoadConstInstruction>();
            result.Instructions[4].Should().BeOfType<MulInstruction>();
        }

        [Fact]
        public void Compile_Negation_EmitsNegInstruction()
        {
            // Arrange: $SELF.VALUE = -(42)
            var source = "$SELF.VALUE = -(42).";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions.Should().Contain(i => i is NegInstruction);
        }

        [Fact]
        public void Compile_EqualityComparison_EmitsEqInstruction()
        {
            // Arrange: $SELF.MATCH = ($SELF.MODEL = 'Racing')
            var source = "$SELF.MATCH = ($SELF.MODEL = 'Racing').";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions.Should().Contain(i => i is EqInstruction);
        }

        [Fact]
        public void Compile_NotEqualComparison_EmitsNeInstruction()
        {
            // Arrange: $SELF.DIFFERENT = ($SELF.MODEL <> 'Racing')
            var source = "$SELF.DIFFERENT = ($SELF.MODEL <> 'Racing').";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions.Should().Contain(i => i is NeInstruction);
        }

        [Fact]
        public void Compile_LessThanComparison_EmitsLtInstruction()
        {
            // Arrange: $SELF.CHEAP = ($SELF.PRICE < 100)
            var source = "$SELF.CHEAP = ($SELF.PRICE < 100).";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions.Should().Contain(i => i is LtInstruction);
        }

        [Fact]
        public void Compile_GreaterThanComparison_EmitsGtInstruction()
        {
            // Arrange: $SELF.EXPENSIVE = ($SELF.PRICE > 1000)
            var source = "$SELF.EXPENSIVE = ($SELF.PRICE > 1000).";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions.Should().Contain(i => i is GtInstruction);
        }

        [Fact]
        public void Compile_LogicalAnd_EmitsAndInstruction()
        {
            // Arrange: $SELF.VALID = ($SELF.PRICE > 0 AND $SELF.PRICE < 1000)
            var source = "$SELF.VALID = ($SELF.PRICE > 0 AND $SELF.PRICE < 1000).";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions.Should().Contain(i => i is AndInstruction);
        }

        [Fact]
        public void Compile_LogicalOr_EmitsOrInstruction()
        {
            // Arrange: $SELF.SPECIAL = ($SELF.COLOR = 'Red' OR $SELF.COLOR = 'Blue')
            var source = "$SELF.SPECIAL = ($SELF.COLOR = 'Red' OR $SELF.COLOR = 'Blue').";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions.Should().Contain(i => i is OrInstruction);
        }

        [Fact]
        public void Compile_LogicalNot_EmitsNotInstruction()
        {
            // Arrange: $SELF.UNAVAILABLE = NOT $SELF.AVAILABLE
            var source = "$SELF.UNAVAILABLE = NOT $SELF.AVAILABLE.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions.Should().Contain(i => i is NotInstruction);
        }

        [Fact]
        public void Compile_StringConcatenation_EmitsConcatInstruction()
        {
            // Arrange: $SELF.FULLNAME = $SELF.FIRSTNAME || ' ' || $SELF.LASTNAME
            var source = "$SELF.FULLNAME = $SELF.FIRSTNAME || ' ' || $SELF.LASTNAME.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions.Should().Contain(i => i is ConcatInstruction);
        }

        [Fact]
        public void Compile_MemberAccessLoad_EmitsLoadMemberInstruction()
        {
            // Arrange: $SELF.COPY = $PARENT.COLOR
            var source = "$SELF.COPY = $PARENT.COLOR.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions.Should().ContainSingle(i => i is LoadMemberInstruction);
            var loadMember = result.Instructions.OfType<LoadMemberInstruction>().First();
            loadMember.ObjectName.Should().Be("$PARENT");
            loadMember.MemberName.Should().Be("COLOR");
        }

        [Fact]
        public void Compile_NestedExpressions_EmitsCorrectSequence()
        {
            // Arrange: $SELF.RESULT = (($SELF.A + $SELF.B) * $SELF.C) - $SELF.D
            var source = "$SELF.RESULT = (($SELF.A + $SELF.B) * $SELF.C) - $SELF.D.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert - Should have: 4 LOAD_MEMBER, ADD, MUL, SUB, STORE_MEMBER
            result.Instructions.OfType<LoadMemberInstruction>().Should().HaveCount(4);
            result.Instructions.Should().Contain(i => i is AddInstruction);
            result.Instructions.Should().Contain(i => i is MulInstruction);
            result.Instructions.Should().Contain(i => i is SubInstruction);
            result.Instructions.Should().Contain(i => i is StoreMemberInstruction);
        }
    }
}
