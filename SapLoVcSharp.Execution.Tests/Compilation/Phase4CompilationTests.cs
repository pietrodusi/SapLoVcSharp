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
    /// Tests for Phase 4 functionality: constraint compilation and execution.
    /// </summary>
    public class Phase4CompilationTests
    {
        private readonly InstructionCompiler _compiler = new();

        private static ConstraintNode ParseConstraint(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new ConstraintParser(tokens);
            return parser.Parse();
        }

        private static IConstraintContext CreateMockConstraintContext()
        {
            var context = Substitute.For<IConstraintContext>();

            // Mock transactional behavior
            var isInTransaction = false;
            var localVariables = new Dictionary<string, object?>();

            context.InTransaction.Returns(_ => isInTransaction);

            context.When(x => x.BeginTransaction()).Do(_ => isInTransaction = true);

            context.CommitAsync(Arg.Any<IConfigurationContext>()).Returns(Task.FromResult(true))
                .AndDoes(_ =>
                {
                    isInTransaction = false;
                    localVariables.Clear();
                });

            context.When(x => x.Rollback()).Do(_ =>
            {
                isInTransaction = false;
                localVariables.Clear();
            });

            context.GetLocalVariable(Arg.Any<string>()).Returns(call =>
            {
                var name = call.Arg<string>();
                return localVariables.TryGetValue(name, out var value) ? value : null;
            });

            context.When(x => x.SetLocalVariable(Arg.Any<string>(), Arg.Any<object?>()))
                .Do(call =>
                {
                    var name = call.Arg<string>();
                    var value = call.Arg<object?>();
                    localVariables[name] = value;
                });

            context.InitializeLocalVariableAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IConfigurationContext>())
                .Returns(call =>
                {
                    var localName = call.ArgAt<string>(0);
                    var configName = call.ArgAt<string>(1);
                    var configContext = call.ArgAt<IConfigurationContext>(2);

                    // Simulate reading from config and storing locally
                    return configContext.GetValueAsync(configName)
                        .ContinueWith(t =>
                        {
                            localVariables[localName] = t.Result;
                        });
                });

            return context;
        }

        #region Constraint Compilation Tests

        [Fact]
        public void Compile_MinimalConstraint_EmitsTransactionalInstructions()
        {
            // Arrange
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                RESTRICTIONS: PC.COLOR = 'Red'";
            var constraint = ParseConstraint(source);

            // Act
            var result = _compiler.Compile(constraint);

            // Assert - Should have transactional flow
            result.Instructions.Should().Contain(i => i is BeginConstraintTransactionInstruction);
            result.Instructions.Should().Contain(i => i is BindObjectInstruction);
            result.Instructions.Should().Contain(i => i is CommitConstraintContextInstruction);

            var bindInst = result.Instructions.OfType<BindObjectInstruction>().First();
            bindInst.Variable.Should().Be("PC");
            bindInst.ClassName.Should().Be("BIKE");
            bindInst.ClassType.Should().Be("300");

            // Instructions should be in correct order
            var instructions = result.Instructions;
            instructions[0].Should().BeOfType<BeginConstraintTransactionInstruction>();
            instructions.Last().Should().BeOfType<CommitConstraintContextInstruction>();
        }

        [Fact]
        public void Compile_ConstraintWithCondition_EmitsJumpIfFalse()
        {
            // Arrange
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                CONDITION: PC.MODEL = 'Racing'
                RESTRICTIONS: PC.COLOR = 'Red'";
            var constraint = ParseConstraint(source);

            // Act
            var result = _compiler.Compile(constraint);

            // Assert - Should have: bind, load PC.MODEL, load 'Racing', eq, jumpiffalse, restrictions
            result.Instructions.Should().Contain(i => i is BindObjectInstruction);
            result.Instructions.Should().Contain(i => i is JumpIfFalseInstruction);
            result.Instructions.Should().Contain(i => i is LoadConstraintMemberInstruction);
        }

        [Fact]
        public void Compile_ConstraintMemberAccess_EmitsLoadConstraintMember()
        {
            // Arrange - Constraint with PC.COLOR in condition
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                CONDITION: PC.COLOR = 'Red'
                RESTRICTIONS: PC.PRICE = 100";
            var constraint = ParseConstraint(source);

            // Act
            var result = _compiler.Compile(constraint);

            // Assert
            var loadInstructions = result.Instructions.OfType<LoadConstraintMemberInstruction>().ToList();
            loadInstructions.Should().NotBeEmpty();
            loadInstructions.Should().Contain(i => i.Variable == "PC" && i.MemberName == "COLOR");
        }

        [Fact]
        public void Compile_ConstraintAssignment_EmitsStoreConstraintMember()
        {
            // Arrange
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                RESTRICTIONS: PC.COLOR = 'Red'";
            var constraint = ParseConstraint(source);

            // Act
            var result = _compiler.Compile(constraint);

            // Assert
            result.Instructions.Should().Contain(i => i is StoreConstraintMemberInstruction);

            var storeInst = result.Instructions.OfType<StoreConstraintMemberInstruction>().First();
            storeInst.Variable.Should().Be("PC");
            storeInst.MemberName.Should().Be("COLOR");
        }

        [Fact]
        public void Compile_ConstraintWithMultipleObjects_BindsAll()
        {
            // Arrange
            var source = @"
                OBJECTS:
                    PC IS_A (300) BIKE,
                    FRAME IS_A (100) COMPONENT
                RESTRICTIONS:
                    PC.COLOR = FRAME.COLOR";
            var constraint = ParseConstraint(source);

            // Act
            var result = _compiler.Compile(constraint);

            // Assert
            var bindInstructions = result.Instructions.OfType<BindObjectInstruction>().ToList();
            bindInstructions.Should().HaveCount(2);
            bindInstructions[0].Variable.Should().Be("PC");
            bindInstructions[1].Variable.Should().Be("FRAME");
        }

        [Fact]
        public void Compile_ConstraintWithMultipleRestrictions_CompilesAll()
        {
            // Arrange
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                RESTRICTIONS:
                    PC.COLOR = 'Red',
                    PC.PRICE = 1000,
                    PC.WEIGHT = 15";
            var constraint = ParseConstraint(source);

            // Act
            var result = _compiler.Compile(constraint);

            // Assert
            var storeInstructions = result.Instructions.OfType<StoreConstraintMemberInstruction>().ToList();
            storeInstructions.Should().HaveCount(3);
            storeInstructions.Select(i => i.MemberName).Should().BeEquivalentTo(new[] { "COLOR", "PRICE", "WEIGHT" });
        }

        [Fact]
        public void Compile_ConstraintWithConditionalRestriction_EmitsCorrectFlow()
        {
            // Arrange
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                RESTRICTIONS: PC.COLOR = 'Red' IF PC.MODEL = 'Racing'";
            var constraint = ParseConstraint(source);

            // Act
            var result = _compiler.Compile(constraint);

            // Assert - Should have nested jumps: one for constraint condition (none here), one for restriction IF
            var jumpInstructions = result.Instructions.OfType<JumpIfFalseInstruction>().ToList();
            jumpInstructions.Should().HaveCount(1); // One for the IF clause in the restriction
        }

        #endregion

        #region Execution Tests

        [Fact]
        public async Task Execute_MinimalConstraint_UsesTransactionalExecution()
        {
            // Arrange
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                RESTRICTIONS: PC.COLOR = 'Red'";
            var constraint = ParseConstraint(source);
            var instructions = _compiler.Compile(constraint);

            var context = Substitute.For<IConfigurationContext>();
            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = CreateMockConstraintContext();

            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            constraintContext.Received(1).BeginTransaction();
            constraintContext.Received(1).BindVariable("PC", "BIKE", "300", null);
            await constraintContext.Received(1).SetMemberValueAsync("PC", "COLOR", "Red");
            await constraintContext.Received(1).CommitAsync(context);
        }

        [Fact]
        public async Task Execute_ConstraintWithCondition_SkipsCommitWhenConditionFalse()
        {
            // Arrange
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                CONDITION: PC.MODEL = 'Racing'
                RESTRICTIONS: PC.COLOR = 'Red'";
            var constraint = ParseConstraint(source);
            var instructions = _compiler.Compile(constraint);

            var context = Substitute.For<IConfigurationContext>();
            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = CreateMockConstraintContext();

            // Make condition false
            constraintContext.GetMemberValueAsync("PC", "MODEL").Returns("Touring");

            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            constraintContext.Received(1).BeginTransaction();
            constraintContext.Received(1).BindVariable("PC", "BIKE", "300", null);
            // Should NOT have called SetMemberValueAsync or CommitAsync because condition was false
            await constraintContext.DidNotReceive().SetMemberValueAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object?>());
            await constraintContext.DidNotReceive().CommitAsync(Arg.Any<IConfigurationContext>());
        }

        [Fact]
        public async Task Execute_ConstraintWithCondition_CommitsWhenConditionTrue()
        {
            // Arrange
            var source = @"
                OBJECTS: PC IS_A (300) BIKE
                CONDITION: PC.MODEL = 'Racing'
                RESTRICTIONS: PC.COLOR = 'Red'";
            var constraint = ParseConstraint(source);
            var instructions = _compiler.Compile(constraint);

            var context = Substitute.For<IConfigurationContext>();
            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = CreateMockConstraintContext();

            // Make condition true
            constraintContext.GetMemberValueAsync("PC", "MODEL").Returns("Racing");

            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            constraintContext.Received(1).BeginTransaction();
            constraintContext.Received(1).BindVariable("PC", "BIKE", "300", null);
            await constraintContext.Received(1).SetMemberValueAsync("PC", "COLOR", "Red");
            await constraintContext.Received(1).CommitAsync(context);
        }

        [Fact]
        public async Task Execute_ConstraintWithMultipleObjects_BindsAllVariables()
        {
            // Arrange
            var source = @"
                OBJECTS:
                    PC IS_A (300) BIKE,
                    FRAME IS_A (100) COMPONENT
                RESTRICTIONS:
                    PC.COLOR = 'Red'";
            var constraint = ParseConstraint(source);
            var instructions = _compiler.Compile(constraint);

            var context = Substitute.For<IConfigurationContext>();
            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = CreateMockConstraintContext();

            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            constraintContext.Received(1).BeginTransaction();
            constraintContext.Received(1).BindVariable("PC", "BIKE", "300", null);
            constraintContext.Received(1).BindVariable("FRAME", "COMPONENT", "100", null);
            await constraintContext.Received(1).CommitAsync(context);
        }

        [Fact]
        public async Task Execute_ConstraintCopyingValues_UsesConstraintMembers()
        {
            // Arrange - Copy value from one object to another
            var source = @"
                OBJECTS:
                    PC IS_A (300) BIKE,
                    FRAME IS_A (100) COMPONENT
                RESTRICTIONS:
                    PC.COLOR = FRAME.COLOR";
            var constraint = ParseConstraint(source);
            var instructions = _compiler.Compile(constraint);

            var context = Substitute.For<IConfigurationContext>();
            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = CreateMockConstraintContext();

            // FRAME.COLOR has value "Blue"
            constraintContext.GetMemberValueAsync("FRAME", "COLOR").Returns("Blue");

            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            constraintContext.Received(1).BeginTransaction();
            // Should have read FRAME.COLOR and written it to PC.COLOR
            await constraintContext.Received(1).GetMemberValueAsync("FRAME", "COLOR");
            await constraintContext.Received(1).SetMemberValueAsync("PC", "COLOR", "Blue");
            await constraintContext.Received(1).CommitAsync(context);
        }

        #endregion

        #region WHERE Clause and Local Variable Tests

        [Fact]
        public void Compile_ConstraintWithWhereClause_EmitsInitializeInstructions()
        {
            // Arrange
            var source = @"
                OBJECTS: PC IS_A (300) PC_CLASS WHERE lv_CASE = CASE; lv_MONITOR = MONITOR
                RESTRICTIONS: PC.COLOR = 'Red'";
            var constraint = ParseConstraint(source);

            // Act
            var result = _compiler.Compile(constraint);

            // Assert
            var initInstructions = result.Instructions.OfType<InitializeConstraintVarInstruction>().ToList();
            initInstructions.Should().HaveCount(2);
            initInstructions[0].LocalVarName.Should().Be("lv_CASE");
            initInstructions[0].ConfigCharacteristicName.Should().Be("CASE");
            initInstructions[1].LocalVarName.Should().Be("lv_MONITOR");
            initInstructions[1].ConfigCharacteristicName.Should().Be("MONITOR");
        }

        [Fact]
        public async Task Execute_ConstraintWithWhereClause_InitializesLocalVariables()
        {
            // Arrange
            var source = @"
                OBJECTS: PC IS_A (300) PC_CLASS WHERE lv_CASE = CASE
                RESTRICTIONS: PC.RESULT = lv_CASE";
            var constraint = ParseConstraint(source);
            var instructions = _compiler.Compile(constraint);

            var context = Substitute.For<IConfigurationContext>();
            context.GetValueAsync("CASE").Returns("CaseA");

            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = CreateMockConstraintContext();

            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            // Should have initialized lv_CASE from config context's CASE
            await constraintContext.Received(1).InitializeLocalVariableAsync("lv_CASE", "CASE", context);
            // lv_CASE value should be used in restriction
            await constraintContext.Received(1).SetMemberValueAsync("PC", "RESULT", "CaseA");
        }

        [Fact]
        public async Task Execute_CompleteUserExample_WorksEndToEnd()
        {
            // Arrange - User's exact example
            var source = @"
                OBJECTS: PC IS_A (300) PC_CLASS WHERE lv_CASE = CASE; lv_MONITOR = MONITOR; lv_MOUSE = MOUSE
                CONDITION: lv_MOUSE = 'Y'
                RESTRICTIONS: TABLE PC_TABLE (CASE = lv_CASE, MONITOR = lv_MONITOR)
                INFERENCES: lv_MONITOR";
            var constraint = ParseConstraint(source);
            var instructions = _compiler.Compile(constraint);

            var context = Substitute.For<IConfigurationContext>();
            context.GetValueAsync("CASE").Returns("CaseA");
            context.GetValueAsync("MONITOR").Returns("Monitor1");
            context.GetValueAsync("MOUSE").Returns("Y");

            var tableResolver = Substitute.For<ITableResolver>();
            tableResolver.ResolveAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, (object?, bool)>>())
                .Returns(true);

            var constraintContext = CreateMockConstraintContext();

            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();

            // Transactional flow
            constraintContext.Received(1).BeginTransaction();
            await constraintContext.Received(1).CommitAsync(context);

            // Object binding
            constraintContext.Received(1).BindVariable("PC", "PC_CLASS", "300", Arg.Any<Dictionary<string, string>>());

            // WHERE clause initialization
            await constraintContext.Received(1).InitializeLocalVariableAsync("lv_CASE", "CASE", context);
            await constraintContext.Received(1).InitializeLocalVariableAsync("lv_MONITOR", "MONITOR", context);
            await constraintContext.Received(1).InitializeLocalVariableAsync("lv_MOUSE", "MOUSE", context);

            // Table call with local variables
            await tableResolver.Received(1).ResolveAsync("PC_TABLE",
                Arg.Is<Dictionary<string, (object? Value, bool IsOptional)>>(d =>
                    d.ContainsKey("CASE") && d["CASE"].Value!.Equals("CaseA") &&
                    d.ContainsKey("MONITOR") && d["MONITOR"].Value!.Equals("Monitor1")));
        }

        #endregion
    }
}
