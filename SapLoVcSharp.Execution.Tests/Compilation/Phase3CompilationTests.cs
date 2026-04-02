using FluentAssertions;
using NSubstitute;
using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Ast.Statements;
using SapLoVcSharp.Core.Lexing;
using SapLoVcSharp.Core.Parsing.DependencyParsers;
using SapLoVcSharp.Execution.Compilation;
using SapLoVcSharp.Execution.Contexts;
using SapLoVcSharp.Execution.Instructions;

namespace SapLoVcSharp.Execution.Tests.Compilation
{
    /// <summary>
    /// Tests for Phase 3 functionality: table calls and built-in functions.
    /// </summary>
    public class Phase3CompilationTests
    {
        private readonly InstructionCompiler _compiler = new();

        private static ProcedureNode ParseProcedure(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new ProcedureParser(tokens);
            return parser.Parse();
        }

        #region Table Call Compilation Tests

        [Fact]
        public void Compile_SimpleTableCall_EmitsCallTableInstruction()
        {
            // Arrange: TABLE T_PRICING (MODEL = $SELF.MODEL)
            var source = "TABLE T_PRICING (MODEL = $SELF.MODEL).";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions.Should().Contain(i => i is CallTableInstruction);
            result.Metadata.HasTableCalls.Should().BeTrue();
        }

        [Fact]
        public void Compile_TableCallWithMultipleArguments_EmitsCorrectSequence()
        {
            // Arrange: TABLE T_PRICING (MODEL = $SELF.MODEL, SIZE = $SELF.SIZE)
            var source = "TABLE T_PRICING (MODEL = $SELF.MODEL, SIZE = $SELF.SIZE).";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert - Should have: LOAD_MEMBER MODEL, LOAD_MEMBER SIZE, CALL_TABLE
            result.Instructions.OfType<LoadMemberInstruction>().Should().HaveCount(2);
            result.Instructions.Should().Contain(i => i is CallTableInstruction);

            var tableCall = result.Instructions.OfType<CallTableInstruction>().First();
            tableCall.TableName.Should().Be("T_PRICING");
            tableCall.Arguments.Should().HaveCount(2);
        }

        [Fact]
        public void Compile_TableCallWithOptionalArgument_MarksArgumentAsOptional()
        {
            // Arrange: TABLE T_DATA (COL1 = 'A', COL2 ?= $SELF.VALUE)
            var source = "TABLE T_DATA (COL1 = 'A', COL2 ?= $SELF.VALUE).";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            var tableCall = result.Instructions.OfType<CallTableInstruction>().First();
            tableCall.Arguments[0].IsOptional.Should().BeFalse();
            tableCall.Arguments[1].IsOptional.Should().BeTrue();
        }

        [Fact]
        public void Compile_ConditionalTableCall_EmitsJumpIfFalse()
        {
            // Arrange: TABLE T_DATA (COL1 = 'A') IF $SELF.PRICE > 100
            var source = "TABLE T_DATA (COL1 = 'A') IF $SELF.PRICE > 100.";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert - Should have condition evaluation, jump, table call
            result.Instructions.Should().Contain(i => i is JumpIfFalseInstruction);
            result.Instructions.Should().Contain(i => i is CallTableInstruction);
        }

        #endregion

        #region Built-in Function Compilation Tests

        [Fact]
        public void Compile_SetDefaultFunction_EmitsCallBuiltinInstruction()
        {
            // Arrange: $SET_DEFAULT ($SELF, HEIGHT, 'Red')
            var source = "$SET_DEFAULT ($SELF, HEIGHT, 'Red')";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            result.Instructions.Should().Contain(i => i is CallBuiltinInstruction);
            result.Metadata.HasBuiltInFunctions.Should().BeTrue();

            var builtinCall = result.Instructions.OfType<CallBuiltinInstruction>().First();
            builtinCall.FunctionType.Should().Be(BuiltInFunctionType.SetDefault);
            builtinCall.ArgumentCount.Should().Be(3);
        }

        [Fact]
        public void Compile_DelDefaultFunction_EmitsCorrectInstruction()
        {
            // Arrange: $DEL_DEFAULT ($SELF, HEIGHT)
            var source = "$DEL_DEFAULT ($SELF, HEIGHT)";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            var builtinCall = result.Instructions.OfType<CallBuiltinInstruction>().First();
            builtinCall.FunctionType.Should().Be(BuiltInFunctionType.DelDefault);
            builtinCall.ArgumentCount.Should().Be(2);
        }

        [Fact]
        public void Compile_SumPartsFunction_EmitsCorrectInstruction()
        {
            // Arrange: $SUM_PARTS ($SELF, HEIGHT)
            var source = "$SUM_PARTS ($SELF, HEIGHT)";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert
            var builtinCall = result.Instructions.OfType<CallBuiltinInstruction>().First();
            builtinCall.FunctionType.Should().Be(BuiltInFunctionType.SumParts);
            builtinCall.ArgumentCount.Should().Be(2);
        }

        [Fact]
        public void Compile_BuiltinFunctionWithExpressionArguments_EvaluatesArguments()
        {
            // Arrange: $SET_DEFAULT ($SELF, TOTAL, 10 + 20)
            var source = "$SET_DEFAULT ($SELF, TOTAL, 10 + 20)";
            var procedure = ParseProcedure(source);

            // Act
            var result = _compiler.Compile(procedure);

            // Assert - Should evaluate 10 + 20 before calling function
            result.Instructions.Should().Contain(i => i is LoadConstInstruction);
            result.Instructions.Should().Contain(i => i is AddInstruction);
            result.Instructions.Should().Contain(i => i is CallBuiltinInstruction);
        }

        #endregion

        #region Execution Tests

        [Fact]
        public async Task Execute_TableCall_CallsTableResolver()
        {
            // Arrange
            var source = "TABLE T_TEST (COL1 = 'A').";
            var procedure = ParseProcedure(source);
            var instructions = _compiler.Compile(procedure);

            var context = Substitute.For<IConfigurationContext>();
            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();

            tableResolver.ResolveAsync(
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, (object? Value, bool IsOptional)>>())
                .Returns(true);

            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            await tableResolver.Received(1).ResolveAsync(
                "T_TEST",
                Arg.Is<Dictionary<string, (object? Value, bool IsOptional)>>(d =>
                    d.ContainsKey("COL1") && d["COL1"].Value.Equals("A")));
        }

        [Fact]
        public async Task Execute_SetDefaultFunction_CallsContextSetValue()
        {
            // Arrange
            var source = "$SET_DEFAULT ($SELF, HEIGHT, 'Red')";
            var procedure = ParseProcedure(source);
            var instructions = _compiler.Compile(procedure);

            var context = Substitute.For<IConfigurationContext>();
            var self = Substitute.For<IConfigurationObject>();
            context.Self.Returns(self);

            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();

            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            await context.Received(1).SetValueAsync("HEIGHT", "Red");
        }

        [Fact]
        public async Task Execute_ConditionalTableCall_SkipsWhenConditionFalse()
        {
            // Arrange
            var source = "TABLE T_TEST (COL1 = 'A') IF $SELF.ENABLED = TRUE.";
            var procedure = ParseProcedure(source);
            var instructions = _compiler.Compile(procedure);

            var context = Substitute.For<IConfigurationContext>();
            var self = Substitute.For<IConfigurationObject>();
            context.Self.Returns(self);
            self.GetCharacteristicValueAsync("ENABLED").Returns(false); // Condition is false

            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();

            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            // Table resolver should NOT be called because condition is false
            await tableResolver.DidNotReceive().ResolveAsync(
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, (object? Value, bool IsOptional)>>());
        }

        #endregion
    }
}
