using SapLoVcSharp.Execution.Instructions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SapLoVcSharp.Execution.Serialization
{
    /// <summary>
    /// JSON serializer for instruction lists using System.Text.Json.
    /// Follows the same pattern as AstSerializer with polymorphic support.
    /// </summary>
    public class InstructionSerializer : IInstructionSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new ObjectJsonConverter()
            },
            TypeInfoResolver = new InstructionTypeResolver(),
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        public string Serialize(InstructionList instructionList)
        {
            if (instructionList == null)
                throw new ArgumentNullException(nameof(instructionList));

            return JsonSerializer.Serialize(instructionList, Options);
        }

        public InstructionList Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON cannot be empty", nameof(json));

            return JsonSerializer.Deserialize<InstructionList>(json, Options)
                ?? throw new InvalidOperationException("Deserialization returned null");
        }
    }

    /// <summary>
    /// Custom type resolver for polymorphic instruction serialization.
    /// Handles all concrete instruction types with $opCode discriminator.
    /// </summary>
    public class InstructionTypeResolver : DefaultJsonTypeInfoResolver
    {
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var typeInfo = base.GetTypeInfo(type, options);

            if (type == typeof(Instruction))
            {
                typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "$opCode",
                    IgnoreUnrecognizedTypeDiscriminators = true,
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor,
                    DerivedTypes =
                    {
                        // Stack operations
                        new JsonDerivedType(typeof(LoadConstInstruction), "LoadConst"),
                        new JsonDerivedType(typeof(LoadVarInstruction), "LoadVar"),
                        new JsonDerivedType(typeof(StoreVarInstruction), "StoreVar"),
                        new JsonDerivedType(typeof(PopInstruction), "Pop"),
                        new JsonDerivedType(typeof(DupInstruction), "Dup"),

                        // Arithmetic operations
                        new JsonDerivedType(typeof(AddInstruction), "Add"),
                        new JsonDerivedType(typeof(SubInstruction), "Sub"),
                        new JsonDerivedType(typeof(MulInstruction), "Mul"),
                        new JsonDerivedType(typeof(DivInstruction), "Div"),
                        new JsonDerivedType(typeof(NegInstruction), "Neg"),

                        // Comparison operations
                        new JsonDerivedType(typeof(EqInstruction), "Eq"),
                        new JsonDerivedType(typeof(NeInstruction), "Ne"),
                        new JsonDerivedType(typeof(LtInstruction), "Lt"),
                        new JsonDerivedType(typeof(LeInstruction), "Le"),
                        new JsonDerivedType(typeof(GtInstruction), "Gt"),
                        new JsonDerivedType(typeof(GeInstruction), "Ge"),

                        // Logical operations
                        new JsonDerivedType(typeof(AndInstruction), "And"),
                        new JsonDerivedType(typeof(OrInstruction), "Or"),
                        new JsonDerivedType(typeof(NotInstruction), "Not"),

                        // String operations
                        new JsonDerivedType(typeof(ConcatInstruction), "Concat"),

                        // Member access
                        new JsonDerivedType(typeof(LoadMemberInstruction), "LoadMember"),
                        new JsonDerivedType(typeof(StoreMemberInstruction), "StoreMember"),

                        // Control flow
                        new JsonDerivedType(typeof(JumpInstruction), "Jump"),
                        new JsonDerivedType(typeof(JumpIfFalseInstruction), "JumpIfFalse"),
                        new JsonDerivedType(typeof(JumpIfTrueInstruction), "JumpIfTrue"),

                        // Return instructions
                        new JsonDerivedType(typeof(ReturnBoolInstruction), "ReturnBool"),
                        new JsonDerivedType(typeof(ReturnInconsistencyInstruction), "ReturnInconsistency"),

                        // Utility
                        new JsonDerivedType(typeof(NopInstruction), "Nop"),

                        // Function calls (Phase 3)
                        new JsonDerivedType(typeof(CallFunctionInstruction), "CallFunction"),
                        new JsonDerivedType(typeof(CallBuiltinInstruction), "CallBuiltin"),
                        new JsonDerivedType(typeof(CallTableInstruction), "CallTable"),

                        // Constraint operations (Phase 4)
                        new JsonDerivedType(typeof(BindObjectInstruction), "BindObject"),
                        new JsonDerivedType(typeof(LoadConstraintMemberInstruction), "LoadConstraintMember"),
                        new JsonDerivedType(typeof(StoreConstraintMemberInstruction), "StoreConstraintMember"),

                        // Constraint transaction operations (Phase 4 - Transactional)
                        new JsonDerivedType(typeof(InitializeConstraintVarInstruction), "InitializeConstraintVar"),
                        new JsonDerivedType(typeof(BeginConstraintTransactionInstruction), "BeginConstraintTransaction"),
                        new JsonDerivedType(typeof(CommitConstraintContextInstruction), "CommitConstraintContext"),

                        // Expression operations (Phase 5)
                        new JsonDerivedType(typeof(SpecifiedInstruction), "Specified"),
                        new JsonDerivedType(typeof(InListInstruction), "InList"),
                        new JsonDerivedType(typeof(CollectInferencesInstruction), "ApplyInferences")
                    }
                };
            }

            return typeInfo;
        }
    }
}
