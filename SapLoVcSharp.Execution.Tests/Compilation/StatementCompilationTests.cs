using FluentAssertions;
using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Lexing;
using SapLoVcSharp.Core.Parsing.DependencyParsers;
using SapLoVcSharp.Execution.Compilation;
using SapLoVcSharp.Execution.Instructions;

namespace SapLoVcSharp.Execution.Tests.Compilation
{
    /// <summary>
    /// Tests for statement compilation in the instruction compiler.
    /// Verifies that statements are correctly compiled to instruction sequences.
    /// </summary>
    public class StatementCompilationTests
    {
        private readonly InstructionCompiler _compiler = new();

        private static ProcedureNode ParseProcedure(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new ProcedureParser(tokens);
            return parser.Parse();
        }

        private static PreconditionNode ParsePrecondition(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new PreconditionParser(tokens);
            return parser.Parse();
        }

        private static SelectionConditionNode ParseSelectionCondition(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new SelectionConditionParser(tokens);
            return parser.Parse();
        }

        #region Simple Assignment Tests

        [Fact]
        public void Compile_SimpleAssignment_EmitsCorrectSequence()
        {
            // Arrange: $SELF.COLOR = 'Red'
            var source = "$SELF.COLOR = 'Red'.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert - Should be: LOAD_CONST 'Red', STORE_MEMBER $SELF.COLOR
            result.Instructions.Should().HaveCount(2);
            result.Instructions[0].Should().BeOfType<LoadConstInstruction>();
            result.Instructions[1].Should().BeOfType<StoreMemberInstruction>()
                .Which.MemberName.Should().Be("COLOR");
        }


        #endregion

        #region Conditional Assignment Tests

        [Fact]
        public void Compile_ConditionalAssignment_EmitsJumpIfFalse()
        {
            // Arrange: $SELF.COLOR = 'Red' IF $SELF.MODEL = 'Racing'
            var source = "$SELF.COLOR = 'Red' IF $SELF.MODEL = 'Racing'.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert - Should contain JumpIfFalse
            result.Instructions.Should().Contain(i => i is JumpIfFalseInstruction);
        }

        [Fact]
        public void Compile_ConditionalAssignment_EmitsCorrectSequence()
        {
            // Arrange: $SELF.COLOR = 'Red' IF $SELF.MODEL = 'Racing'
            var source = "$SELF.COLOR = 'Red' IF $SELF.MODEL = 'Racing'.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert - Sequence should be:
            // 1. Evaluate condition: LOAD_MEMBER MODEL, LOAD_CONST 'Racing', EQ
            // 2. JUMP_IF_FALSE to skip assignment
            // 3. LOAD_CONST 'Red', STORE_MEMBER COLOR
            var instructions = result.Instructions;

            // Condition evaluation
            instructions.OfType<LoadMemberInstruction>()
                .Should().Contain(lm => lm.MemberName == "MODEL");
            instructions.OfType<LoadConstInstruction>()
                .Should().Contain(lc => lc.Value.Equals("Racing"));
            instructions.Should().Contain(i => i is EqInstruction);

            // Jump
            var jumpInstruction = instructions.OfType<JumpIfFalseInstruction>().First();
            jumpInstruction.Should().NotBeNull();

            // Assignment value
            instructions.OfType<LoadConstInstruction>()
                .Should().Contain(lc => lc.Value.Equals("Red"));
            instructions.OfType<StoreMemberInstruction>()
                .Should().Contain(sm => sm.MemberName == "COLOR");
        }

        [Fact]
        public void Compile_ConditionalAssignment_JumpTargetIsCorrect()
        {
            // Arrange: $SELF.COLOR = 'Red' IF $SELF.MODEL = 'Racing'
            var source = "$SELF.COLOR = 'Red' IF $SELF.MODEL = 'Racing'.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert - Jump target should point past the assignment
            var jumpInstruction = result.Instructions.OfType<JumpIfFalseInstruction>().First();
            jumpInstruction.TargetOffset.Should().Be(result.Instructions.Count,
                "Jump should point to the end of the instruction list");
        }


        #endregion

        #region Precondition and Selection Condition Tests

        [Fact]
        public void Compile_Precondition_EmitsReturnBool()
        {
            // Arrange: $SELF.PRICE > 0
            var source = "$SELF.PRICE > 0.";
            var precondition = ParsePrecondition(source);

            // Act
            var result = _compiler.Compile(precondition);

            // Assert - Should end with RETURN_BOOL
            result.Instructions.Last().Should().BeOfType<ReturnBoolInstruction>();
        }

        [Fact]
        public void Compile_Precondition_EmitsCorrectSequence()
        {
            // Arrange: $SELF.PRICE > 100
            var source = "$SELF.PRICE > 100.";
            var precondition = ParsePrecondition(source);

            // Act
            var result = _compiler.Compile(precondition);

            // Assert - Sequence: LOAD_MEMBER PRICE, LOAD_CONST 100, GT, RETURN_BOOL
            result.Instructions[0].Should().BeOfType<LoadMemberInstruction>();
            result.Instructions[1].Should().BeOfType<LoadConstInstruction>();
            result.Instructions[2].Should().BeOfType<GtInstruction>();
            result.Instructions[3].Should().BeOfType<ReturnBoolInstruction>();
        }

        [Fact]
        public void Compile_SelectionCondition_EmitsReturnBool()
        {
            // Arrange: $SELF.MODEL = 'Racing'
            var source = "$SELF.MODEL = 'Racing'.";
            var selectionCondition = ParseSelectionCondition(source);

            // Act
            var result = _compiler.Compile(selectionCondition);

            // Assert - Should end with RETURN_BOOL
            result.Instructions.Last().Should().BeOfType<ReturnBoolInstruction>();
        }

        [Fact]
        public void Compile_ComplexPrecondition_EmitsCorrectSequence()
        {
            // Arrange: ($SELF.PRICE > 0 AND $SELF.PRICE < 1000)
            var source = "($SELF.PRICE > 0 AND $SELF.PRICE < 1000).";
            var precondition = ParsePrecondition(source);

            // Act
            var result = _compiler.Compile(precondition);

            // Assert - Should have: 2 LOAD_MEMBER, 2 LOAD_CONST, GT, LT, AND, RETURN_BOOL
            result.Instructions.OfType<LoadMemberInstruction>().Should().HaveCount(2);
            result.Instructions.OfType<LoadConstInstruction>().Should().HaveCount(2);
            result.Instructions.Should().Contain(i => i is GtInstruction);
            result.Instructions.Should().Contain(i => i is LtInstruction);
            result.Instructions.Should().Contain(i => i is AndInstruction);
            result.Instructions.Last().Should().BeOfType<ReturnBoolInstruction>();
        }

        #endregion

        #region Metadata Tests

        [Fact]
        public void Compile_SimpleAssignment_SetsCorrectMetadata()
        {
            // Arrange
            var source = "$SELF.COLOR = 'Red'.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.DependencyType.Should().Be(SapLoVcSharp.Execution.Instructions.DependencyType.Procedure);
            result.DependencyId.Should().Be("UNKNOWN"); // Default value during compilation
            result.Metadata.Should().NotBeNull();
        }

        [Fact]
        public void Compile_PreconditionWithTableCall_SetsTableCallMetadata()
        {
            // Arrange: This will be tested more thoroughly in Phase 3
            // For now, just verify metadata structure exists
            var source = "$SELF.PRICE > 0.";
            var precondition = ParsePrecondition(source);

            // Act
            var result = _compiler.Compile(precondition);

            // Assert
            result.Metadata.HasTableCalls.Should().BeFalse();
            result.Metadata.HasBuiltInFunctions.Should().BeFalse();
        }

        #endregion
    }
}
