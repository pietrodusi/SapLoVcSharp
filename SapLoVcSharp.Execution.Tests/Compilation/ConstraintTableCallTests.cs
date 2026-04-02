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
    /// Tests for table calls within constraints.
    /// </summary>
    public class ConstraintTableCallTests
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

                    return configContext.GetValueAsync(configName)
                        .ContinueWith(t =>
                        {
                            localVariables[localName] = t.Result;
                        });
                });

            return context;
        }

        [Fact]
        public void Compile_ConstraintWithTableCall_EmitsCallTableInstruction()
        {
            // Arrange - Your exact example
            var source = @"
                OBJECTS: PC IS_A (300) PC_CLASS WHERE lv_CASE = CASE; lv_MONITOR = MONITOR; lv_MOUSE = MOUSE
                CONDITION: lv_MOUSE = 'Y'
                RESTRICTIONS: TABLE PC_TABLE (CASE = lv_CASE, MONITOR = lv_MONITOR)
                INFERENCES: lv_MONITOR";
            var constraint = ParseConstraint(source);

            // Act
            var result = _compiler.Compile(constraint);

            // Assert
            result.Instructions.Should().Contain(i => i is CallTableInstruction);
            result.Metadata.HasTableCalls.Should().BeTrue();

            var tableCall = result.Instructions.OfType<CallTableInstruction>().First();
            tableCall.TableName.Should().Be("PC_TABLE");
            tableCall.Arguments.Should().HaveCount(2);
        }

        [Fact]
        public void Compile_ConstraintWithConditionalTableCall_EmitsJumps()
        {
            // Arrange - Table call with IF clause
            var source = @"
                OBJECTS: PC IS_A (300) PC_CLASS WHERE lv_CASE = CASE
                RESTRICTIONS: TABLE PC_TABLE (CASE = lv_CASE) IF lv_CASE = 'A'";
            var constraint = ParseConstraint(source);

            // Act
            var result = _compiler.Compile(constraint);

            // Assert
            result.Instructions.Should().Contain(i => i is CallTableInstruction);
            // Should have a JumpIfFalse for the IF clause
            var jumps = result.Instructions.OfType<JumpIfFalseInstruction>().ToList();
            jumps.Should().NotBeEmpty();
        }

        [Fact]
        public void Compile_ConstraintWithMultipleTableCalls_CompilesAll()
        {
            // Arrange - Multiple table calls like your example
            var source = @"
                OBJECTS: PC IS_A (300) PC_CLASS WHERE lv_CASE = CASE
                RESTRICTIONS:
                    TABLE PC_TABLE (CASE = lv_CASE),
                    TABLE PC_OTHER_TABLE (CASE = lv_CASE) IF lv_CASE = 'A'";
            var constraint = ParseConstraint(source);

            // Act
            var result = _compiler.Compile(constraint);

            // Assert
            var tableCalls = result.Instructions.OfType<CallTableInstruction>().ToList();
            tableCalls.Should().HaveCount(2);
            tableCalls[0].TableName.Should().Be("PC_TABLE");
            tableCalls[1].TableName.Should().Be("PC_OTHER_TABLE");
        }

        [Fact]
        public void Compile_ConstraintWithWhereClause_BindsCorrectly()
        {
            // Arrange
            var source = @"
                OBJECTS: PC IS_A (300) PC_CLASS WHERE lv_CASE = CASE; lv_MONITOR = MONITOR; lv_MOUSE = MOUSE
                RESTRICTIONS: PC.COLOR = 'Red'";
            var constraint = ParseConstraint(source);

            // Act
            var result = _compiler.Compile(constraint);

            // Assert
            var bindInst = result.Instructions.OfType<BindObjectInstruction>().First();
            bindInst.Variable.Should().Be("PC");
            bindInst.ClassName.Should().Be("PC_CLASS");
            bindInst.ClassType.Should().Be("300");
            bindInst.WhereClause.Should().NotBeNull();
            bindInst.WhereClause.Should().HaveCount(3);
            bindInst.WhereClause!["lv_CASE"].Should().Be("CASE");
            bindInst.WhereClause["lv_MONITOR"].Should().Be("MONITOR");
            bindInst.WhereClause["lv_MOUSE"].Should().Be("MOUSE");
        }

        [Fact]
        public async Task Execute_ConstraintWithTableCall_UsesLocalVariable()
        {
            // Arrange
            var source = @"
                OBJECTS: PC IS_A (300) PC_CLASS WHERE lv_CASE = CASE
                RESTRICTIONS: TABLE PC_TABLE (CASE = lv_CASE)";
            var constraint = ParseConstraint(source);
            var instructions = _compiler.Compile(constraint);

            var context = Substitute.For<IConfigurationContext>();
            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = CreateMockConstraintContext();

            // WHERE clause initializes lv_CASE from CASE characteristic
            context.GetValueAsync("CASE").Returns("CaseA");

            // Mock table lookup to return true
            tableResolver.ResolveAsync(
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, (object? Value, bool IsOptional)>>())
                .Returns(true);

            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            // Transactional flow
            constraintContext.Received(1).BeginTransaction();
            await constraintContext.Received(1).CommitAsync(context);
            // WHERE clause initialization
            await constraintContext.Received(1).InitializeLocalVariableAsync("lv_CASE", "CASE", context);
            // Table call with local variable value
            await tableResolver.Received(1).ResolveAsync(
                "PC_TABLE",
                Arg.Is<Dictionary<string, (object? Value, bool IsOptional)>>(d =>
                    d.ContainsKey("CASE") && d["CASE"].Value!.Equals("CaseA")));
        }

        [Fact]
        public async Task Execute_ConstraintWithConditionalTableCall_SkipsWhenConditionFalse()
        {
            // Arrange
            var source = @"
                OBJECTS: PC IS_A (300) PC_CLASS WHERE lv_CASE = CASE
                RESTRICTIONS: TABLE PC_TABLE (CASE = lv_CASE) IF lv_CASE = 'A'";
            var constraint = ParseConstraint(source);
            var instructions = _compiler.Compile(constraint);

            var context = Substitute.For<IConfigurationContext>();
            var tableResolver = Substitute.For<ITableResolver>();
            var constraintContext = CreateMockConstraintContext();

            // lv_CASE is initialized to 'B', not 'A', so condition is false
            context.GetValueAsync("CASE").Returns("B");

            var vm = new VirtualMachine(context, tableResolver, constraintContext);

            // Act
            var result = await vm.ExecuteAsync(instructions);

            // Assert
            result.Success.Should().BeTrue();
            constraintContext.Received(1).BeginTransaction();
            await constraintContext.Received(1).InitializeLocalVariableAsync("lv_CASE", "CASE", context);
            // Table resolver should NOT be called because IF condition was false
            await tableResolver.DidNotReceive().ResolveAsync(
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, (object? Value, bool IsOptional)>>());
            // Should still commit
            await constraintContext.Received(1).CommitAsync(context);
        }
    }
}
