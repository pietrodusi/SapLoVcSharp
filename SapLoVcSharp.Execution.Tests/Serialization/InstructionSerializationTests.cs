using FluentAssertions;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Execution.Instructions;
using SapLoVcSharp.Execution.Serialization;
using Xunit.Abstractions;

namespace SapLoVcSharp.Execution.Tests.Serialization;

/// <summary>
/// Tests for instruction list JSON serialization and deserialization.
/// Verifies round-trip preservation of instruction lists.
/// </summary>
public class InstructionSerializationTests
{
    private readonly IInstructionSerializer _serializer;
    private readonly ITestOutputHelper _output;

    public InstructionSerializationTests(ITestOutputHelper output)
    {
        _serializer = new InstructionSerializer();
        _output = output;
    }

    #region Single Instruction Round-Trip Tests

    [Fact]
    public void RoundTrip_LoadConstInstruction_Number_PreservesValue()
    {
        // Arrange
        var original = new InstructionList
        {
            DependencyId = "TEST_001",
            DependencyType = DependencyType.Procedure,
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction(42.0, LiteralType.Number, 0)
            }
        };

        // Act
        var json = _serializer.Serialize(original);
        _output.WriteLine("Serialized JSON:");
        _output.WriteLine(json);

        var deserialized = _serializer.Deserialize(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.DependencyId.Should().Be("TEST_001");
        deserialized.DependencyType.Should().Be(DependencyType.Procedure);
        deserialized.Instructions.Should().HaveCount(1);

        var instruction = deserialized.Instructions[0].Should().BeOfType<LoadConstInstruction>().Subject;
        Convert.ToDouble(instruction.Value).Should().Be(42.0);
        instruction.Type.Should().Be(LiteralType.Number);
        instruction.Position.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_LoadConstInstruction_String_PreservesValue()
    {
        // Arrange
        var original = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction("Red", LiteralType.String, 0)
            }
        };

        // Act
        var json = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize(json);

        // Assert
        var instruction = deserialized.Instructions[0].Should().BeOfType<LoadConstInstruction>().Subject;
        instruction.Value.Should().Be("Red");
        instruction.Type.Should().Be(LiteralType.String);
    }

    [Fact]
    public void RoundTrip_LoadConstInstruction_Boolean_PreservesValue()
    {
        // Arrange
        var original = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction(true, LiteralType.Boolean, 0)
            }
        };

        // Act
        var json = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize(json);

        // Assert
        var instruction = deserialized.Instructions[0].Should().BeOfType<LoadConstInstruction>().Subject;
        instruction.Value.Should().Be(true);
        instruction.Type.Should().Be(LiteralType.Boolean);
    }

    [Fact]
    public void RoundTrip_LoadVarInstruction_PreservesVariableName()
    {
        // Arrange
        var original = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadVarInstruction("COLOR", 0)
            }
        };

        // Act
        var json = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize(json);

        // Assert
        var instruction = deserialized.Instructions[0].Should().BeOfType<LoadVarInstruction>().Subject;
        instruction.VariableName.Should().Be("COLOR");
        instruction.Position.Should().Be(0);
    }

    [Fact]
    public void RoundTrip_StoreVarInstruction_PreservesVariableName()
    {
        // Arrange
        var original = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new StoreVarInstruction("MODEL", 5)
            }
        };

        // Act
        var json = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize(json);

        // Assert
        var instruction = deserialized.Instructions[0].Should().BeOfType<StoreVarInstruction>().Subject;
        instruction.VariableName.Should().Be("MODEL");
        instruction.Position.Should().Be(5);
    }

    [Fact]
    public void RoundTrip_AddInstruction_PreservesStructure()
    {
        // Arrange
        var original = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new AddInstruction(2)
            }
        };

        // Act
        var json = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize(json);

        // Assert
        var instruction = deserialized.Instructions[0].Should().BeOfType<AddInstruction>().Subject;
        instruction.OpCode.Should().Be(OpCode.Add);
        instruction.Position.Should().Be(2);
    }

    [Fact]
    public void RoundTrip_SubInstruction_PreservesStructure()
    {
        // Arrange
        var original = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new SubInstruction(3)
            }
        };

        // Act
        var json = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize(json);

        // Assert
        var instruction = deserialized.Instructions[0].Should().BeOfType<SubInstruction>().Subject;
        instruction.OpCode.Should().Be(OpCode.Sub);
        instruction.Position.Should().Be(3);
    }

    [Fact]
    public void RoundTrip_EqInstruction_PreservesStructure()
    {
        // Arrange
        var original = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new EqInstruction(4)
            }
        };

        // Act
        var json = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize(json);

        // Assert
        var instruction = deserialized.Instructions[0].Should().BeOfType<EqInstruction>().Subject;
        instruction.OpCode.Should().Be(OpCode.Eq);
        instruction.Position.Should().Be(4);
    }

    #endregion

    #region Multiple Instructions Round-Trip Tests

    [Fact]
    public void RoundTrip_MultipleInstructions_PreservesOrder()
    {
        // Arrange
        var original = new InstructionList
        {
            DependencyId = "MULTI_001",
            DependencyType = DependencyType.Procedure,
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction(5.0, LiteralType.Number, 0),
                new LoadConstInstruction(3.0, LiteralType.Number, 1),
                new AddInstruction(2),
                new StoreVarInstruction("RESULT", 3)
            }
        };

        // Act
        var json = _serializer.Serialize(original);
        _output.WriteLine("Serialized JSON:");
        _output.WriteLine(json);

        var deserialized = _serializer.Deserialize(json);

        // Assert
        deserialized.Instructions.Should().HaveCount(4);
        deserialized.Instructions[0].Should().BeOfType<LoadConstInstruction>();
        deserialized.Instructions[1].Should().BeOfType<LoadConstInstruction>();
        deserialized.Instructions[2].Should().BeOfType<AddInstruction>();
        deserialized.Instructions[3].Should().BeOfType<StoreVarInstruction>();
    }

    [Fact]
    public void RoundTrip_ComplexInstructionList_PreservesAllData()
    {
        // Arrange
        var original = new InstructionList
        {
            DependencyId = "COMPLEX_001",
            DependencyType = DependencyType.Constraint,
            Instructions = new List<Instruction>
            {
                new LoadVarInstruction("MODEL", 0),
                new LoadConstInstruction("Racing", LiteralType.String, 1),
                new EqInstruction(2),
                new LoadConstInstruction("Red", LiteralType.String, 3),
                new StoreVarInstruction("COLOR", 4)
            },
            Metadata = new InstructionMetadata
            {
                HasTableCalls = false,
                HasBuiltInFunctions = false,
                EstimatedComplexity = 5
            }
        };

        // Act
        var json = _serializer.Serialize(original);
        _output.WriteLine("Serialized JSON:");
        _output.WriteLine(json);

        var deserialized = _serializer.Deserialize(json);

        // Assert
        deserialized.DependencyId.Should().Be("COMPLEX_001");
        deserialized.DependencyType.Should().Be(DependencyType.Constraint);
        deserialized.Instructions.Should().HaveCount(5);
        deserialized.Metadata.Should().NotBeNull();
        deserialized.Metadata.HasTableCalls.Should().BeFalse();
        deserialized.Metadata.HasBuiltInFunctions.Should().BeFalse();
        deserialized.Metadata.EstimatedComplexity.Should().Be(5);
    }

    #endregion

    #region Metadata Serialization Tests

    [Fact]
    public void RoundTrip_Metadata_WithObjectBindings_PreservesData()
    {
        // Arrange
        var original = new InstructionList
        {
            DependencyType = DependencyType.Constraint,
            Instructions = new List<Instruction>(),
            Metadata = new InstructionMetadata
            {
                ObjectBindings = new Dictionary<string, ObjectBinding>
                {
                    ["PC"] = new ObjectBinding
                    {
                        Variable = "PC",
                        ClassName = "BIKE",
                        ClassType = "300",
                        WhereClause = new Dictionary<string, string>
                        {
                            ["MOD"] = "MODEL",
                            ["COL"] = "COLOR"
                        }
                    }
                },
                InferenceCharacteristics = new List<string> { "COLOR", "WEIGHT" }
            }
        };

        // Act
        var json = _serializer.Serialize(original);
        _output.WriteLine("Serialized JSON:");
        _output.WriteLine(json);

        var deserialized = _serializer.Deserialize(json);

        // Assert
        deserialized.Metadata.ObjectBindings.Should().NotBeNull();
        deserialized.Metadata.ObjectBindings.Should().ContainKey("PC");

        var binding = deserialized.Metadata.ObjectBindings!["PC"];
        binding.Variable.Should().Be("PC");
        binding.ClassName.Should().Be("BIKE");
        binding.ClassType.Should().Be("300");
        binding.WhereClause.Should().ContainKey("MOD");
        binding.WhereClause!["MOD"].Should().Be("MODEL");

        deserialized.Metadata.InferenceCharacteristics.Should().Contain("COLOR");
        deserialized.Metadata.InferenceCharacteristics.Should().Contain("WEIGHT");
    }

    [Fact]
    public void RoundTrip_Metadata_Flags_PreservesData()
    {
        // Arrange
        var original = new InstructionList
        {
            Instructions = new List<Instruction>(),
            Metadata = new InstructionMetadata
            {
                HasTableCalls = true,
                HasBuiltInFunctions = true,
                EstimatedComplexity = 42
            }
        };

        // Act
        var json = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize(json);

        // Assert
        deserialized.Metadata.HasTableCalls.Should().BeTrue();
        deserialized.Metadata.HasBuiltInFunctions.Should().BeTrue();
        deserialized.Metadata.EstimatedComplexity.Should().Be(42);
    }

    #endregion

    #region Dependency Type Tests

    [Theory]
    [InlineData(DependencyType.Procedure)]
    [InlineData(DependencyType.Constraint)]
    [InlineData(DependencyType.Precondition)]
    [InlineData(DependencyType.SelectionCondition)]
    public void RoundTrip_DependencyTypes_PreservesType(DependencyType type)
    {
        // Arrange
        var original = new InstructionList
        {
            DependencyType = type,
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction(1.0, LiteralType.Number, 0)
            }
        };

        // Act
        var json = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize(json);

        // Assert
        deserialized.DependencyType.Should().Be(type);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void RoundTrip_EmptyInstructionList_WorksCorrectly()
    {
        // Arrange
        var original = new InstructionList
        {
            DependencyId = "EMPTY_001",
            DependencyType = DependencyType.Procedure,
            Instructions = new List<Instruction>()
        };

        // Act
        var json = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize(json);

        // Assert
        deserialized.DependencyId.Should().Be("EMPTY_001");
        deserialized.Instructions.Should().BeEmpty();
    }

    [Fact]
    public void Serialize_NullInstructionList_ThrowsException()
    {
        // Arrange
        InstructionList? nullList = null;

        // Act & Assert
        var act = () => _serializer.Serialize(nullList!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Deserialize_EmptyString_ThrowsException()
    {
        // Act & Assert
        var act = () => _serializer.Deserialize("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsException()
    {
        // Act & Assert
        var act = () => _serializer.Deserialize("{ invalid json }");
        act.Should().Throw<System.Text.Json.JsonException>();
    }

    #endregion

    #region JSON Format Validation

    [Fact]
    public void Serialize_ProducesValidJson_WithCorrectDiscriminator()
    {
        // Arrange
        var original = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new LoadConstInstruction(42.0, LiteralType.Number, 0)
            }
        };

        // Act
        var json = _serializer.Serialize(original);
        _output.WriteLine(json);

        // Assert
        json.Should().Contain("\"$opCode\"");
        json.Should().Contain("\"LoadConst\"");
        json.Should().Contain("\"value\"");
        json.Should().Contain("\"type\"");
    }

    [Fact]
    public void Serialize_UsesWriteIndented_ForReadability()
    {
        // Arrange
        var original = new InstructionList
        {
            Instructions = new List<Instruction>
            {
                new AddInstruction(0)
            }
        };

        // Act
        var json = _serializer.Serialize(original);

        // Assert - Indented JSON should have newlines
        json.Should().Contain("\n");
    }

    #endregion
}
