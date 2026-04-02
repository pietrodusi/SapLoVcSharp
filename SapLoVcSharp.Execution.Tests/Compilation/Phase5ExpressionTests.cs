using FluentAssertions;
using NSubstitute;
using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Lexing;
using SapLoVcSharp.Core.Parsing.DependencyParsers;
using SapLoVcSharp.Execution.Compilation;
using SapLoVcSharp.Execution.Contexts;
using SapLoVcSharp.Execution.Instructions;

namespace SapLoVcSharp.Execution.Tests.Compilation
{
    /// <summary>
    /// Tests for Phase 5: Expression support (SPECIFIED, IN, mathematical functions, table call expressions).
    /// </summary>
    public class Phase5ExpressionTests
    {
        private readonly InstructionCompiler _compiler = new();

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

        #region SPECIFIED Expression Tests

        [Fact]
        public void Compile_SpecifiedExpression_EmitsSpecifiedInstruction()
        {
            // Arrange
            var source = "SPECIFIED COLOR";
            var precondition = ParsePrecondition(source);

            // Act
            var result = _compiler.Compile(precondition);

            // Assert
            result.Instructions.Should().Contain(i => i is SpecifiedInstruction);
            var specifiedInst = result.Instructions.OfType<SpecifiedInstruction>().First();
            specifiedInst.CharacteristicName.Should().Be("COLOR");
        }

        [Fact]
        public void Compile_SpecifiedWithMemberAccess_EmitsSpecifiedInstruction()
        {
            // Arrange
            var source = "SPECIFIED $SELF.HEIGHT";
            var precondition = ParsePrecondition(source);

            // Act
            var result = _compiler.Compile(precondition);

            // Assert
            var specifiedInst = result.Instructions.OfType<SpecifiedInstruction>().First();
            specifiedInst.CharacteristicName.Should().Be("HEIGHT");
        }

        [Fact]
        public void Compile_SpecifiedPostfix_EmitsSpecifiedInstruction()
        {
            // Arrange
            var source = "COLOR SPECIFIED";
            var precondition = ParsePrecondition(source);

            // Act
            var result = _compiler.Compile(precondition);

            // Assert
            var specifiedInst = result.Instructions.OfType<SpecifiedInstruction>().First();
            specifiedInst.CharacteristicName.Should().Be("COLOR");
        }

        [Fact]
        public async Task Execute_SpecifiedExpression_ReturnsTrueWhenValueExists()
        {
            // Arrange
            var source = "SPECIFIED COLOR";
            var precondition = ParsePrecondition(source);
            var instructions = _compiler.Compile(precondition);

            var context = Substitute.For<IConfigurationContext>();
            context.GetValueAsync("COLOR").Returns("Red"); // COLOR has a value

            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();
            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            result.ReturnValue.Should().Be(true);
        }

        [Fact]
        public async Task Execute_SpecifiedExpression_ReturnsFalseWhenValueIsNull()
        {
            // Arrange
            var source = "SPECIFIED COLOR";
            var precondition = ParsePrecondition(source);
            var instructions = _compiler.Compile(precondition);

            var context = Substitute.For<IConfigurationContext>();
            context.GetValueAsync("COLOR").Returns((object?)null); // COLOR is null

            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();
            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            result.ReturnValue.Should().Be(false);
        }

        #endregion

        #region IN Expression Tests

        [Fact]
        public void Compile_InExpression_EmitsInListInstruction()
        {
            // Arrange
            var source = "COLOR IN ('Red', 'Blue', 'Green')";
            var precondition = ParsePrecondition(source);

            // Act
            var result = _compiler.Compile(precondition);

            // Assert
            result.Instructions.Should().Contain(i => i is InListInstruction);
            var inListInst = result.Instructions.OfType<InListInstruction>().First();
            inListInst.Values.Should().HaveCount(3);
            inListInst.Values.Should().Contain("Red");
            inListInst.Values.Should().Contain("Blue");
            inListInst.Values.Should().Contain("Green");
        }

        [Fact]
        public void Compile_InExpressionWithNumbers_EmitsInListInstruction()
        {
            // Arrange
            var source = "SIZE IN (10, 20, 30)";
            var precondition = ParsePrecondition(source);

            // Act
            var result = _compiler.Compile(precondition);

            // Assert
            var inListInst = result.Instructions.OfType<InListInstruction>().First();
            inListInst.Values.Should().HaveCount(3);
            inListInst.Values.Should().Contain(10.0);
            inListInst.Values.Should().Contain(20.0);
            inListInst.Values.Should().Contain(30.0);
        }

        [Fact]
        public async Task Execute_InExpression_ReturnsTrueWhenValueInList()
        {
            // Arrange
            var source = "COLOR IN ('Red', 'Blue', 'Green')";
            var precondition = ParsePrecondition(source);
            var instructions = _compiler.Compile(precondition);

            var context = Substitute.For<IConfigurationContext>();
            context.GetValueAsync("COLOR").Returns("Blue"); // Blue is in list

            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();
            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            result.ReturnValue.Should().Be(true);
        }

        [Fact]
        public async Task Execute_InExpression_ReturnsFalseWhenValueNotInList()
        {
            // Arrange
            var source = "COLOR IN ('Red', 'Blue', 'Green')";
            var precondition = ParsePrecondition(source);
            var instructions = _compiler.Compile(precondition);

            var context = Substitute.For<IConfigurationContext>();
            context.GetValueAsync("COLOR").Returns("Yellow"); // Yellow not in list

            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();
            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            result.ReturnValue.Should().Be(false);
        }

        [Fact]
        public async Task Execute_InExpression_CaseInsensitiveStringComparison()
        {
            // Arrange
            var source = "COLOR IN ('Red', 'Blue')";
            var precondition = ParsePrecondition(source);
            var instructions = _compiler.Compile(precondition);

            var context = Substitute.For<IConfigurationContext>();
            context.GetValueAsync("COLOR").Returns("red"); // lowercase "red"

            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();
            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            result.ReturnValue.Should().Be(true); // Should match case-insensitively
        }

        #endregion

        #region Mathematical Function Tests

        [Fact]
        public void Compile_MathFunctionCall_EmitsCallFunctionInstruction()
        {
            // Arrange
            var source = "SQRT(HEIGHT) > 10";
            var precondition = ParsePrecondition(source);

            // Act
            var result = _compiler.Compile(precondition);

            // Assert
            result.Instructions.Should().Contain(i => i is CallFunctionInstruction);
            var callInst = result.Instructions.OfType<CallFunctionInstruction>().First();
            callInst.FunctionName.Should().Be("SQRT");
            callInst.ArgumentCount.Should().Be(1);
        }

        [Fact]
        public void Compile_NestedFunctionCalls_EmitsMultipleInstructions()
        {
            // Arrange
            var source = "SQRT(ABS(HEIGHT)) > 5";
            var precondition = ParsePrecondition(source);

            // Act
            var result = _compiler.Compile(precondition);

            // Assert
            var callInstructions = result.Instructions.OfType<CallFunctionInstruction>().ToList();
            callInstructions.Should().HaveCount(2);
            callInstructions.Should().Contain(c => c.FunctionName == "ABS");
            callInstructions.Should().Contain(c => c.FunctionName == "SQRT");
        }

        [Fact]
        public async Task Execute_SqrtFunction_ReturnsCorrectValue()
        {
            // Arrange
            var source = "SQRT(HEIGHT) = 5";
            var precondition = ParsePrecondition(source);
            var instructions = _compiler.Compile(precondition);

            var context = Substitute.For<IConfigurationContext>();
            context.GetValueAsync("HEIGHT").Returns(25); // sqrt(25) = 5

            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();
            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            result.ReturnValue.Should().Be(true);
        }

        [Fact]
        public async Task Execute_MaxFunction_ReturnsMaxValue()
        {
            // Arrange
            var source = "MAX(10, 20, 5) = 20";
            var precondition = ParsePrecondition(source);
            var instructions = _compiler.Compile(precondition);

            var context = Substitute.For<IConfigurationContext>();
            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();
            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            result.ReturnValue.Should().Be(true);
        }

        [Fact]
        public async Task Execute_MinFunction_ReturnsMinValue()
        {
            // Arrange
            var source = "MIN(10, 20, 5) = 5";
            var precondition = ParsePrecondition(source);
            var instructions = _compiler.Compile(precondition);

            var context = Substitute.For<IConfigurationContext>();
            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();
            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            result.ReturnValue.Should().Be(true);
        }

        [Fact]
        public async Task Execute_TrigonometricFunctions_WorkCorrectly()
        {
            // Arrange
            var source = "SIN(0) = 0";
            var precondition = ParsePrecondition(source);
            var instructions = _compiler.Compile(precondition);

            var context = Substitute.For<IConfigurationContext>();
            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();
            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            result.ReturnValue.Should().Be(true);
        }

        [Fact]
        public async Task Execute_StringFunction_UpperCase_WorksCorrectly()
        {
            // Arrange
            var source = "UC(COLOR) = 'RED'";
            var precondition = ParsePrecondition(source);
            var instructions = _compiler.Compile(precondition);

            var context = Substitute.For<IConfigurationContext>();
            context.GetValueAsync("COLOR").Returns("red");

            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();
            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            result.ReturnValue.Should().Be(true);
        }

        [Fact]
        public async Task Execute_StringFunction_LowerCase_WorksCorrectly()
        {
            // Arrange
            var source = "LC(COLOR) = 'blue'";
            var precondition = ParsePrecondition(source);
            var instructions = _compiler.Compile(precondition);

            var context = Substitute.For<IConfigurationContext>();
            context.GetValueAsync("COLOR").Returns("BLUE");

            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();
            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            result.ReturnValue.Should().Be(true);
        }

        #endregion

        #region Table Call Expression Tests

        [Fact]
        public void Compile_TableCallExpression_EmitsCallTableInstruction()
        {
            // Arrange
            var source = "TABLE T_BIKE (MODEL = MODEL, COLOR = COLOR)";
            var precondition = ParsePrecondition(source);

            // Act
            var result = _compiler.Compile(precondition);

            // Assert
            result.Instructions.Should().Contain(i => i is CallTableInstruction);
            result.Metadata.HasTableCalls.Should().BeTrue();

            var tableCall = result.Instructions.OfType<CallTableInstruction>().First();
            tableCall.TableName.Should().Be("T_BIKE");
            tableCall.Arguments.Should().HaveCount(2);
        }

        [Fact]
        public void Compile_TableCallExpressionWithOptionalArgs_HandlesOptionalFlag()
        {
            // Arrange
            var source = "TABLE T_QTY (qty ?= QTY)";
            var precondition = ParsePrecondition(source);

            // Act
            var result = _compiler.Compile(precondition);

            // Assert
            var tableCall = result.Instructions.OfType<CallTableInstruction>().First();
            tableCall.Arguments.Should().HaveCount(1);
            tableCall.Arguments[0].IsOptional.Should().BeTrue();
        }

        [Fact]
        public async Task Execute_TableCallExpression_ReturnsTrueWhenMatchFound()
        {
            // Arrange
            var source = "TABLE T_BIKE (MODEL = MODEL)";
            var precondition = ParsePrecondition(source);
            var instructions = _compiler.Compile(precondition);

            var context = Substitute.For<IConfigurationContext>();
            context.GetValueAsync("MODEL").Returns("Racing");

            var tableResolver = Substitute.For<ITableResolver>();
            tableResolver.ResolveAsync(
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, (object? Value, bool IsOptional)>>())
                .Returns(true); // Match found

            var constraintContext = Substitute.For<IConstraintContext>();
            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            result.ReturnValue.Should().Be(true);
        }

        [Fact]
        public async Task Execute_TableCallExpression_ReturnsFalseWhenNoMatch()
        {
            // Arrange
            var source = "TABLE T_BIKE (MODEL = MODEL)";
            var precondition = ParsePrecondition(source);
            var instructions = _compiler.Compile(precondition);

            var context = Substitute.For<IConfigurationContext>();
            context.GetValueAsync("MODEL").Returns("InvalidModel");

            var tableResolver = Substitute.For<ITableResolver>();
            tableResolver.ResolveAsync(
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, (object? Value, bool IsOptional)>>())
                .Returns(false); // No match found

            var constraintContext = Substitute.For<IConstraintContext>();
            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            result.ReturnValue.Should().Be(false);
        }

        #endregion

        #region Combined Expression Tests

        [Fact]
        public async Task Execute_CombinedExpression_SpecifiedAndIn()
        {
            // Arrange
            var source = "SPECIFIED COLOR AND COLOR IN ('Red', 'Blue')";
            var precondition = ParsePrecondition(source);
            var instructions = _compiler.Compile(precondition);

            var context = Substitute.For<IConfigurationContext>();
            context.GetValueAsync("COLOR").Returns("Red");

            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();
            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            result.ReturnValue.Should().Be(true); // COLOR is specified AND in list
        }

        [Fact]
        public async Task Execute_CombinedExpression_FunctionAndComparison()
        {
            // Arrange
            var source = "SQRT(HEIGHT) > 5 AND COLOR = 'Red'";
            var precondition = ParsePrecondition(source);
            var instructions = _compiler.Compile(precondition);

            var context = Substitute.For<IConfigurationContext>();
            context.GetValueAsync("HEIGHT").Returns(36); // sqrt(36) = 6 > 5
            context.GetValueAsync("COLOR").Returns("Red");

            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();
            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            result.ReturnValue.Should().Be(true);
        }

        [Fact]
        public async Task Execute_ComplexExpression_AllPhase5Features()
        {
            // Arrange - combines SPECIFIED, IN, math functions, and logical operators
            var source = @"SPECIFIED HEIGHT AND
                SQRT(HEIGHT) > 5 AND
                COLOR IN ('Red', 'Blue') AND
                MAX(10, 20, 30) = 30";
            var precondition = ParsePrecondition(source);
            var instructions = _compiler.Compile(precondition);

            var context = Substitute.For<IConfigurationContext>();
            context.GetValueAsync("HEIGHT").Returns(36);
            context.GetValueAsync("COLOR").Returns("Red");

            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = Substitute.For<IConstraintContext>();
            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            result.ReturnValue.Should().Be(true);
        }

        #endregion
    }
}
