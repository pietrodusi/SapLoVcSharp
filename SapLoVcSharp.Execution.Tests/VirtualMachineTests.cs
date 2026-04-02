using FluentAssertions;
using NSubstitute;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Execution.Contexts;
using SapLoVcSharp.Execution.Instructions;

namespace SapLoVcSharp.Execution.Tests;

/// <summary>
/// Tests for virtual machine execution of instruction lists.
/// Verifies end-to-end execution of compiled instructions.
/// </summary>
public class VirtualMachineTests
{
    private readonly VirtualMachine _vm;
    private readonly IConfigurationContext _mockContext;
    private readonly ITableResolver _mockTableResolver;
    private readonly IConstraintContext _mockConstraintContext;

    public VirtualMachineTests()
    {
        _mockContext = Substitute.For<IConfigurationContext>();
        _mockTableResolver = Substitute.For<ITableResolver>();
        _mockConstraintContext = Substitute.For<IConstraintContext>();
        _vm = new VirtualMachine(_mockContext, _mockTableResolver, _mockConstraintContext);
    }

    #region Simple Execution Tests

    [Fact]
    public async Task ExecuteAsync_EmptyInstructionList_ReturnsSuccess()
    {
        // Arrange
        var instructionList = new InstructionList
        {
            Instructions = new List<Instruction>()
        };

        // Act
        var result = await _vm.ExecuteAsync(instructionList);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Inconsistent.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_SingleInstruction_ExecutesCorrectly()
    {
        // Arrange
        var instructionList = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction(42.0, LiteralType.Number, 0)
            }
        };

        // Act
        var result = await _vm.ExecuteAsync(instructionList);

        // Assert
        result.Success.Should().BeTrue();
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(42.0);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleInstructions_ExecutesInOrder()
    {
        // Arrange: Calculate 5 + 3 = 8
        var instructionList = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction(5.0, LiteralType.Number, 0),
                new LoadConstInstruction(3.0, LiteralType.Number, 1),
                new AddInstruction(2)
            }
        };

        // Act
        var result = await _vm.ExecuteAsync(instructionList);

        // Assert
        result.Success.Should().BeTrue();
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(8.0);
    }

    #endregion

    #region Arithmetic Expression Tests

    [Fact]
    public async Task ExecuteAsync_ComplexArithmetic_CalculatesCorrectly()
    {
        // Arrange: (10 + 5) - 3 = 12
        var instructionList = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction(10.0, LiteralType.Number, 0),
                new LoadConstInstruction(5.0, LiteralType.Number, 1),
                new AddInstruction(2),
                new LoadConstInstruction(3.0, LiteralType.Number, 3),
                new SubInstruction(4)
            }
        };

        // Act
        var result = await _vm.ExecuteAsync(instructionList);

        // Assert
        result.Success.Should().BeTrue();
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(12.0);
    }

    [Fact]
    public async Task ExecuteAsync_NestedArithmetic_CalculatesCorrectly()
    {
        // Arrange: (2 + 3) + (4 + 5) = 5 + 9 = 14
        var instructionList = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction(2.0, LiteralType.Number, 0),
                new LoadConstInstruction(3.0, LiteralType.Number, 1),
                new AddInstruction(2),
                new LoadConstInstruction(4.0, LiteralType.Number, 3),
                new LoadConstInstruction(5.0, LiteralType.Number, 4),
                new AddInstruction(5),
                new AddInstruction(6)
            }
        };

        // Act
        var result = await _vm.ExecuteAsync(instructionList);

        // Assert
        result.Success.Should().BeTrue();
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(14.0);
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public async Task ExecuteAsync_EqualityComparison_ReturnsTrue()
    {
        // Arrange: "Red" == "Red"
        var instructionList = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction("Red", LiteralType.String, 0),
                new LoadConstInstruction("Red", LiteralType.String, 1),
                new EqInstruction(2)
            }
        };

        // Act
        var result = await _vm.ExecuteAsync(instructionList);

        // Assert
        result.Success.Should().BeTrue();
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(true);
    }

    [Fact]
    public async Task ExecuteAsync_InequalityComparison_ReturnsFalse()
    {
        // Arrange: "Red" == "Blue"
        var instructionList = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction("Red", LiteralType.String, 0),
                new LoadConstInstruction("Blue", LiteralType.String, 1),
                new EqInstruction(2)
            }
        };

        // Act
        var result = await _vm.ExecuteAsync(instructionList);

        // Assert
        result.Success.Should().BeTrue();
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(false);
    }

    #endregion

    #region Context Integration Tests

    [Fact]
    public async Task ExecuteAsync_LoadFromContext_LoadsValueCorrectly()
    {
        // Arrange
        _mockContext.GetValueAsync("COLOR").Returns(Task.FromResult<object?>("Blue"));

        var instructionList = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadVarInstruction("COLOR", 0)
            }
        };

        // Act
        var result = await _vm.ExecuteAsync(instructionList);

        // Assert
        result.Success.Should().BeTrue();
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be("Blue");
        await _mockContext.Received(1).GetValueAsync("COLOR");
    }

    [Fact]
    public async Task ExecuteAsync_StoreToContext_StoresValueCorrectly()
    {
        // Arrange
        var instructionList = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction("Red", LiteralType.String, 0),
                new StoreVarInstruction("COLOR", 1)
            }
        };

        // Act
        var result = await _vm.ExecuteAsync(instructionList);

        // Assert
        result.Success.Should().BeTrue();
        _vm.Stack.Should().BeEmpty();
        await _mockContext.Received(1).SetValueAsync("COLOR", "Red");
    }

    [Fact]
    public async Task ExecuteAsync_LoadCompareStore_WorksEndToEnd()
    {
        // Arrange: Load MODEL, compare with "Racing", store result
        _mockContext.GetValueAsync("MODEL").Returns(Task.FromResult<object?>("Racing"));

        var instructionList = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadVarInstruction("MODEL", 0),
                new LoadConstInstruction("Racing", LiteralType.String, 1),
                new EqInstruction(2),
                new StoreVarInstruction("IS_RACING", 3)
            }
        };

        // Act
        var result = await _vm.ExecuteAsync(instructionList);

        // Assert
        result.Success.Should().BeTrue();
        _vm.Stack.Should().BeEmpty();
        await _mockContext.Received(1).GetValueAsync("MODEL");
        await _mockContext.Received(1).SetValueAsync("IS_RACING", true);
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public async Task ExecuteAsync_ConditionalAssignmentSimulation_WorksCorrectly()
    {
        // Arrange: Simulates: $SELF.COLOR = 'Red' IF MODEL = 'Racing'
        // Compiled to: LOAD MODEL, LOAD 'Racing', EQ, (result on stack), LOAD 'Red', STORE COLOR
        _mockContext.GetValueAsync("MODEL").Returns(Task.FromResult<object?>("Racing"));

        var instructionList = new InstructionList
        {
            DependencyType = DependencyType.Procedure,
            Instructions = new List<Instruction>
            {
                new LoadVarInstruction("MODEL", 0),
                new LoadConstInstruction("Racing", LiteralType.String, 1),
                new EqInstruction(2),
                // Note: In real compiler, we'd have JUMP_IF_FALSE here
                // For now, just continue with assignment
                new LoadConstInstruction("Red", LiteralType.String, 3),
                new StoreVarInstruction("COLOR", 4)
            }
        };

        // Act
        var result = await _vm.ExecuteAsync(instructionList);

        // Assert
        result.Success.Should().BeTrue();
        await _mockContext.Received(1).SetValueAsync("COLOR", "Red");
    }

    [Fact]
    public async Task ExecuteAsync_MultipleAssignments_ExecutesAll()
    {
        // Arrange: Simulates: $SELF.COLOR = 'Red', $SELF.WEIGHT = 10
        var instructionList = new InstructionList
        {
            DependencyType = DependencyType.Procedure,
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction("Red", LiteralType.String, 0),
                new StoreVarInstruction("COLOR", 1),
                new LoadConstInstruction(10.0, LiteralType.Number, 2),
                new StoreVarInstruction("WEIGHT", 3)
            }
        };

        // Act
        var result = await _vm.ExecuteAsync(instructionList);

        // Assert
        result.Success.Should().BeTrue();
        _vm.Stack.Should().BeEmpty();
        await _mockContext.Received(1).SetValueAsync("COLOR", "Red");
        await _mockContext.Received(1).SetValueAsync("WEIGHT", 10.0);
    }

    #endregion

    #region State Management Tests

    [Fact]
    public async Task ExecuteAsync_ClearsStack_BeforeExecution()
    {
        // Arrange - Pollute stack
        _vm.Stack.Push("garbage");
        _vm.Stack.Push(123);

        var instructionList = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction(42.0, LiteralType.Number, 0)
            }
        };

        // Act
        var result = await _vm.ExecuteAsync(instructionList);

        // Assert
        result.Success.Should().BeTrue();
        _vm.Stack.Should().HaveCount(1);
        _vm.Stack.Peek().Should().Be(42.0);
    }

    [Fact]
    public async Task ExecuteAsync_ResetsInstructionPointer_BeforeExecution()
    {
        // Arrange - Set IP to non-zero
        _vm.InstructionPointer = 99;

        var instructionList = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction(1.0, LiteralType.Number, 0),
                new LoadConstInstruction(2.0, LiteralType.Number, 1),
                new AddInstruction(2)
            }
        };

        // Act
        var result = await _vm.ExecuteAsync(instructionList);

        // Assert
        result.Success.Should().BeTrue();
        _vm.Stack.Peek().Should().Be(3.0); // Verify all instructions executed
    }

    [Fact]
    public async Task ExecuteAsync_InstructionPointerIncrement_WorksCorrectly()
    {
        // Arrange
        var instructionList = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction(1.0, LiteralType.Number, 0),
                new LoadConstInstruction(2.0, LiteralType.Number, 1),
                new LoadConstInstruction(3.0, LiteralType.Number, 2)
            }
        };

        // Act
        var result = await _vm.ExecuteAsync(instructionList);

        // Assert
        result.Success.Should().BeTrue();
        _vm.Stack.Should().HaveCount(3);
        _vm.InstructionPointer.Should().Be(3); // Should be at end
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteAsync_Exception_ReturnsFailureResult()
    {
        // Arrange - Mock throws exception
        _mockContext.GetValueAsync(Arg.Any<string>())
            .Returns<Task<object?>>(_ => throw new InvalidOperationException("Test error"));

        var instructionList = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadVarInstruction("NONEXISTENT", 0)
            }
        };

        // Act
        var result = await _vm.ExecuteAsync(instructionList);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Test error");
        result.Exception.Should().NotBeNull();
    }

    #endregion
}
