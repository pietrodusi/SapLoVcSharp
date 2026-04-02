using FluentAssertions;
using NSubstitute;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Execution.Contexts;
using SapLoVcSharp.Execution.Instructions;

namespace SapLoVcSharp.Execution.Tests.Instructions;

/// <summary>
/// Tests for individual instruction execution.
/// Each test verifies that an instruction correctly modifies the VM state.
/// </summary>
public class InstructionExecutionTests
{
    private readonly VirtualMachine _vm;
    private readonly IConfigurationContext _mockContext;
    private readonly ITableResolver _mockTableResolver;
    private readonly IConstraintContext _mockConstraintContext;

    public InstructionExecutionTests()
    {
        _mockContext = Substitute.For<IConfigurationContext>();
        _mockTableResolver = Substitute.For<ITableResolver>();
        _mockConstraintContext = Substitute.For<IConstraintContext>();
        _vm = new VirtualMachine(_mockContext, _mockTableResolver, _mockConstraintContext);
    }

    #region LoadConstInstruction Tests

    [Fact]
    public async Task LoadConstInstruction_Number_PushesValueOntoStack()
    {
        // Arrange
        var instruction = new LoadConstInstruction(42.0, LiteralType.Number, 0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(42.0);
    }

    [Fact]
    public async Task LoadConstInstruction_String_PushesValueOntoStack()
    {
        // Arrange
        var instruction = new LoadConstInstruction("Red", LiteralType.String, 0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be("Red");
    }

    [Fact]
    public async Task LoadConstInstruction_Boolean_PushesValueOntoStack()
    {
        // Arrange
        var instruction = new LoadConstInstruction(true, LiteralType.Boolean, 0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(true);
    }

    [Fact]
    public async Task LoadConstInstruction_MultipleValues_StacksCorrectly()
    {
        // Arrange
        var instruction1 = new LoadConstInstruction(10.0, LiteralType.Number, 0);
        var instruction2 = new LoadConstInstruction(20.0, LiteralType.Number, 1);

        // Act
        await instruction1.ExecuteAsync(_vm);
        await instruction2.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(2);
        _vm.Stack.Pop().Should().Be(20.0); // Top of stack (last pushed)
        _vm.Stack.Pop().Should().Be(10.0); // Bottom of stack (first pushed)
    }

    #endregion

    #region LoadVarInstruction Tests

    [Fact]
    public async Task LoadVarInstruction_LoadsValueFromContext()
    {
        // Arrange
        _mockContext.GetValueAsync("COLOR").Returns(Task.FromResult<object?>("Blue"));
        var instruction = new LoadVarInstruction("COLOR", 0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be("Blue");
        await _mockContext.Received(1).GetValueAsync("COLOR");
    }

    [Fact]
    public async Task LoadVarInstruction_NullValue_PushesNull()
    {
        // Arrange
        _mockContext.GetValueAsync("EMPTY").Returns(Task.FromResult<object?>(null));
        var instruction = new LoadVarInstruction("EMPTY", 0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().BeNull();
    }

    #endregion

    #region StoreVarInstruction Tests

    [Fact]
    public async Task StoreVarInstruction_PopsAndStoresValue()
    {
        // Arrange
        _vm.Stack.Push("Red");
        var instruction = new StoreVarInstruction("COLOR", 0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().BeEmpty();
        await _mockContext.Received(1).SetValueAsync("COLOR", "Red");
    }

    [Fact]
    public async Task StoreVarInstruction_NullValue_StoresNull()
    {
        // Arrange
        _vm.Stack.Push(null);
        var instruction = new StoreVarInstruction("EMPTY", 0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().BeEmpty();
        await _mockContext.Received(1).SetValueAsync("EMPTY", null);
    }

    #endregion

    #region AddInstruction Tests

    [Fact]
    public async Task AddInstruction_AddsTopTwoValues()
    {
        // Arrange
        _vm.Stack.Push(10.0);
        _vm.Stack.Push(20.0);
        var instruction = new AddInstruction(0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(30.0);
    }

    [Fact]
    public async Task AddInstruction_NegativeNumbers_AddsCorrectly()
    {
        // Arrange
        _vm.Stack.Push(-5.0);
        _vm.Stack.Push(3.0);
        var instruction = new AddInstruction(0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(-2.0);
    }

    [Fact]
    public async Task AddInstruction_Decimals_AddsCorrectly()
    {
        // Arrange
        _vm.Stack.Push(1.5);
        _vm.Stack.Push(2.3);
        var instruction = new AddInstruction(0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        Convert.ToDouble(_vm.Stack.Peek()).Should().BeApproximately(3.8, 1e-10);
    }

    #endregion

    #region SubInstruction Tests

    [Fact]
    public async Task SubInstruction_SubtractsTopTwoValues()
    {
        // Arrange
        _vm.Stack.Push(20.0);
        _vm.Stack.Push(10.0);
        var instruction = new SubInstruction(0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(10.0); // 20 - 10
    }

    [Fact]
    public async Task SubInstruction_NegativeResult_SubtractsCorrectly()
    {
        // Arrange
        _vm.Stack.Push(5.0);
        _vm.Stack.Push(10.0);
        var instruction = new SubInstruction(0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(-5.0); // 5 - 10
    }

    #endregion

    #region EqInstruction Tests

    [Fact]
    public async Task EqInstruction_EqualNumbers_PushesTrue()
    {
        // Arrange
        _vm.Stack.Push(42.0);
        _vm.Stack.Push(42.0);
        var instruction = new EqInstruction(0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(true);
    }

    [Fact]
    public async Task EqInstruction_DifferentNumbers_PushesFalse()
    {
        // Arrange
        _vm.Stack.Push(42.0);
        _vm.Stack.Push(43.0);
        var instruction = new EqInstruction(0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(false);
    }

    [Fact]
    public async Task EqInstruction_EqualStrings_PushesTrue()
    {
        // Arrange
        _vm.Stack.Push("Red");
        _vm.Stack.Push("Red");
        var instruction = new EqInstruction(0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(true);
    }

    [Fact]
    public async Task EqInstruction_DifferentStrings_PushesFalse()
    {
        // Arrange
        _vm.Stack.Push("Red");
        _vm.Stack.Push("Blue");
        var instruction = new EqInstruction(0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(false);
    }

    [Fact]
    public async Task EqInstruction_BothNull_PushesTrue()
    {
        // Arrange
        _vm.Stack.Push(null);
        _vm.Stack.Push(null);
        var instruction = new EqInstruction(0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(true);
    }

    [Fact]
    public async Task EqInstruction_OneNull_PushesFalse()
    {
        // Arrange
        _vm.Stack.Push("Red");
        _vm.Stack.Push(null);
        var instruction = new EqInstruction(0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(false);
    }

    [Fact]
    public async Task EqInstruction_NearlyEqualNumbers_PushesTrue()
    {
        // Arrange - Numbers differing by less than epsilon should be equal
        _vm.Stack.Push(1.0);
        _vm.Stack.Push(1.0 + 1e-11);
        var instruction = new EqInstruction(0);

        // Act
        await instruction.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(true);
    }

    #endregion

    #region Integration Tests (Multiple Instructions)

    [Fact]
    public async Task MultipleInstructions_SimpleArithmetic_ExecutesCorrectly()
    {
        // Arrange: Calculate 5 + 3
        var loadConst1 = new LoadConstInstruction(5.0, LiteralType.Number, 0);
        var loadConst2 = new LoadConstInstruction(3.0, LiteralType.Number, 1);
        var add = new AddInstruction(2);

        // Act
        await loadConst1.ExecuteAsync(_vm);
        await loadConst2.ExecuteAsync(_vm);
        await add.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(8.0);
    }

    [Fact]
    public async Task MultipleInstructions_ComplexArithmetic_ExecutesCorrectly()
    {
        // Arrange: Calculate (10 + 5) - 3 = 12
        var loadConst1 = new LoadConstInstruction(10.0, LiteralType.Number, 0);
        var loadConst2 = new LoadConstInstruction(5.0, LiteralType.Number, 1);
        var add = new AddInstruction(2);
        var loadConst3 = new LoadConstInstruction(3.0, LiteralType.Number, 3);
        var sub = new SubInstruction(4);

        // Act
        await loadConst1.ExecuteAsync(_vm);
        await loadConst2.ExecuteAsync(_vm);
        await add.ExecuteAsync(_vm);
        await loadConst3.ExecuteAsync(_vm);
        await sub.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(12.0);
    }

    [Fact]
    public async Task MultipleInstructions_LoadAndStore_ExecutesCorrectly()
    {
        // Arrange: Load "Red", store in COLOR
        var loadConst = new LoadConstInstruction("Red", LiteralType.String, 0);
        var storeVar = new StoreVarInstruction("COLOR", 1);

        // Act
        await loadConst.ExecuteAsync(_vm);
        await storeVar.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().BeEmpty();
        await _mockContext.Received(1).SetValueAsync("COLOR", "Red");
    }

    [Fact]
    public async Task MultipleInstructions_ComparisonWithLoad_ExecutesCorrectly()
    {
        // Arrange: Load MODEL, compare with "Racing"
        _mockContext.GetValueAsync("MODEL").Returns(Task.FromResult<object?>("Racing"));
        var loadVar = new LoadVarInstruction("MODEL", 0);
        var loadConst = new LoadConstInstruction("Racing", LiteralType.String, 1);
        var eq = new EqInstruction(2);

        // Act
        await loadVar.ExecuteAsync(_vm);
        await loadConst.ExecuteAsync(_vm);
        await eq.ExecuteAsync(_vm);

        // Assert
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(true);
    }

    #endregion
}
