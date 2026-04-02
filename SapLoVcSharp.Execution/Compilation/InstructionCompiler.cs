using SapLoVcSharp.Core.Ast;
using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Ast.Statements;
using SapLoVcSharp.Execution.Instructions;

namespace SapLoVcSharp.Execution.Compilation
{
    /// <summary>
    /// Instruction compiler that implements the visitor pattern to convert AST nodes to instructions.
    /// Uses a stack-based approach where expressions leave their results on the stack.
    /// </summary>
    public class InstructionCompiler : IInstructionCompiler, IAstVisitor
    {
        private readonly List<Instruction> _instructions = new();
        private readonly InstructionMetadata _metadata = new();
        private int _position = 0;
        private string? _currentDependencyId;

        public InstructionList Compile(DependencyNode dependency)
        {
            // Reset state
            _instructions.Clear();
            _metadata.HasTableCalls = false;
            _metadata.HasBuiltInFunctions = false;
            _metadata.EstimatedComplexity = 0;
            _position = 0;
            _currentDependencyId = dependency.Name;

            // Visit the dependency node (will dispatch to specific visitor method)
            dependency.Accept(this);

            // Build result
            var result = new InstructionList
            {
                DependencyId = _currentDependencyId ?? "UNKNOWN",
                DependencyType = GetDependencyType(dependency),
                Instructions = new List<Instruction>(_instructions),
                Metadata = _metadata,
                Version = 1
            };

            _metadata.EstimatedComplexity = _instructions.Count;
            return result;
        }

        #region Dependency Visitors

        public void VisitProcedure(ProcedureNode node)
        {
            // Procedures: Execute statements sequentially
            foreach (var statement in node.Statements)
            {
                statement.Accept(this);
            }
        }

        public void VisitConstraint(ConstraintNode node)
        {
            // Transactional constraint execution flow:
            // 1. Begin transaction (create isolated context)
            // 2. Bind object variables (OBJECTS section)
            // 3. Initialize local variables from config context (WHERE clause)
            // 4. Evaluate condition (CONDITION section) - short-circuit if false
            // 5. Execute restrictions (RESTRICTIONS section)
            // 6. Commit constraint context to config context (if successful)

            // 1. Begin constraint transaction
            Emit(new BeginConstraintTransactionInstruction(_position++));

            // 2. Bind all object variables
            foreach (var obj in node.Objects)
            {
                Emit(new BindObjectInstruction(
                    obj.Variable,
                    obj.ClassName,
                    obj.ClassType,
                    obj.VariableMappings,
                    _position++));

                // 3. Initialize WHERE clause local variables
                if (obj.VariableMappings != null)
                {
                    foreach (var mapping in obj.VariableMappings)
                    {
                        // mapping.Key = local variable (lv_CASE)
                        // mapping.Value = config characteristic (CASE)
                        Emit(new InitializeConstraintVarInstruction(
                            mapping.Key,
                            mapping.Value,
                            _position++));
                    }
                }
            }

            // 4. Evaluate condition if present
            JumpIfFalseInstruction? skipJump = null;
            if (node.Condition != null)
            {
                // Evaluate the condition expression
                node.Condition.Accept(this);  // Leaves boolean on stack

                // If condition is false, jump to end (skip restrictions and commit)
                skipJump = new JumpIfFalseInstruction(-1, _position++);
                Emit(skipJump);
            }

            // 5. Execute restrictions (in isolated constraint context)
            foreach (var restriction in node.Restrictions)
            {
                restriction.Accept(this);
            }

            // 5.5. Apply inferences if present
            if (node.Inferences != null && node.Inferences.Any())
            {
                Emit(new CollectInferencesInstruction(node.Inferences, _position++));
            }

            // 6. Commit constraint context to configuration context
            Emit(new CommitConstraintContextInstruction(_position++));

            // Patch the conditional jump if present
            if (skipJump != null)
            {
                skipJump.TargetOffset = _instructions.Count;
            }
        }

        public void VisitPrecondition(PreconditionNode node)
        {
            // Precondition: Evaluate condition expression and return boolean
            node.Condition.Accept(this);  // Leaves boolean on stack
            Emit(new ReturnBoolInstruction(_position++));
        }

        public void VisitSelectionCondition(SelectionConditionNode node)
        {
            // Selection condition: Same as precondition
            node.Condition.Accept(this);  // Leaves boolean on stack
            Emit(new ReturnBoolInstruction(_position++));
        }

        #endregion

        #region Expression Visitors

        public void VisitLiteral(LiteralNode node)
        {
            Emit(new LoadConstInstruction(node.Value, node.Type, _position++));
        }

        public void VisitIdentifier(IdentifierNode node)
        {
            // Identifier = variable load
            Emit(new LoadVarInstruction(node.Name, _position++));
        }

        public void VisitBinaryExpression(BinaryExpressionNode node)
        {
            // Stack-based evaluation: left, right, then operator
            node.Left.Accept(this);   // Push left value
            node.Right.Accept(this);  // Push right value

            // Emit operator instruction (pops 2, pushes 1)
            var instruction = GetBinaryOperatorInstruction(node.Op);
            Emit(instruction);
        }

        public void VisitUnaryExpression(UnaryExpressionNode node)
        {
            // Evaluate operand first
            node.Operand.Accept(this);

            // Emit unary operator
            var instruction = GetUnaryOperatorInstruction(node.Op);
            Emit(instruction);
        }

        public void VisitMemberAccess(MemberAccessNode node)
        {
            // Member access: $SELF.COLOR, $PARENT.MODEL, $ROOT.PRICE
            Emit(new LoadMemberInstruction(ObjectRefToString(node.Obj), node.MemberName, _position++));
        }

        public void VisitConstraintMemberAccess(ConstraintMemberAccessNode node)
        {
            // Constraint member access: PC.COLOR
            // Load the value from the constraint variable
            Emit(new LoadConstraintMemberInstruction(node.Variable, node.MemberName, _position++));
        }

        public void VisitObjectReference(ObjectReferenceNode node)
        {
            // Object reference: $SELF, $PARENT, $ROOT (used as arguments to built-in functions)
            // Push the object reference as a string constant
            var objectRefString = ObjectRefToString(node.Obj);
            Emit(new LoadConstInstruction(objectRefString, LiteralType.String, _position++));
        }

        public void VisitCallExpression(CallExpressionNode node)
        {
            // Mathematical/string function calls (e.g., SIN(x), SQRT(25), MAX(a, b, c))
            // Evaluate all arguments and push onto stack
            foreach (var arg in node.Arguments)
            {
                arg.Accept(this);
            }

            // Emit function call instruction
            Emit(new CallFunctionInstruction(node.FunctionName, node.Arguments.Count, _position++));
        }

        public void VisitInExpression(InExpressionNode node)
        {
            // IN expression: expr IN (value1, value2, ...)
            // 1. Evaluate the left expression (leaves value on stack)
            node.Expression.Accept(this);

            // 2. Extract constant values from the list
            var values = new List<object?>();
            foreach (var valueExpr in node.Values)
            {
                if (valueExpr is LiteralNode literal)
                {
                    // Most common case: literal values
                    values.Add(literal.Value);
                }
                else
                {
                    // Complex expressions in IN list not yet supported
                    throw new CompilationException(
                        $"IN expression currently only supports literal values, got {valueExpr.GetType().Name}");
                }
            }

            // 3. Emit instruction that checks membership
            Emit(new InListInstruction(values, _position++));
        }

        public void VisitSpecified(SpecifiedNode node)
        {
            // SPECIFIED checks if a characteristic has been assigned a value
            // Extract characteristic name from the expression
            string characteristicName;

            if (node.Expression is IdentifierNode identifier)
            {
                characteristicName = identifier.Name;
            }
            else if (node.Expression is MemberAccessNode member)
            {
                // SPECIFIED $SELF.COLOR -> check "COLOR"
                characteristicName = member.MemberName;
            }
            else if (node.Expression is ConstraintMemberAccessNode constraintMember)
            {
                // SPECIFIED PC.COLOR -> check "COLOR" in constraint context
                characteristicName = constraintMember.MemberName;
            }
            else
            {
                throw new CompilationException(
                    $"SPECIFIED requires an identifier or member access, got {node.Expression.GetType().Name}");
            }

            Emit(new SpecifiedInstruction(characteristicName, _position++));
        }

        public void VisitTableCallExpression(TableCallExpressionNode node)
        {
            _metadata.HasTableCalls = true;

            // Table call as expression (e.g., IF TABLE T_BIKE(...))
            // Same as statement version but used in boolean expressions

            // Evaluate all argument expressions and push onto stack
            var argumentSpecs = new List<TableArgumentSpec>();
            foreach (var arg in node.Arguments)
            {
                // Compile the argument value expression
                arg.Value.Accept(this);

                // Extract the variable name if it's an identifier (for both optional and required params)
                // This enables restriction mode where all params are bidirectional
                string? varName = null;
                if (arg.Value is IdentifierNode idNode)
                {
                    varName = idNode.Name;
                }

                // Record the argument specification
                argumentSpecs.Add(new TableArgumentSpec(arg.TableColumn, arg.IsOptional, varName));
            }

            // Emit the table call instruction (pushes boolean result)
            Emit(new CallTableInstruction(node.TableName, argumentSpecs, _position++));
        }

        #endregion

        #region Statement Visitors

        public void VisitAssignment(AssignmentNode node)
        {
            if (node.Condition != null)
            {
                // Conditional assignment: value = expr IF condition
                // Compile condition
                node.Condition.Accept(this);  // Leaves boolean on stack

                // Create jump instruction (will be patched)
                var skipJump = new JumpIfFalseInstruction(-1, _position++);
                var jumpIndex = _instructions.Count;
                Emit(skipJump);

                // Compile value
                node.Value.Accept(this);  // Leaves value on stack

                // Compile store
                CompileStore(node.Target);

                // Patch jump target to point after the assignment
                skipJump.TargetOffset = _instructions.Count;
            }
            else
            {
                // Simple assignment: target = value
                node.Value.Accept(this);  // Leaves value on stack
                CompileStore(node.Target);
            }
        }

        public void VisitTableCall(TableCallNode node)
        {
            _metadata.HasTableCalls = true;

            // Handle conditional table call: TABLE ... IF condition
            JumpIfFalseInstruction? skipJump = null;
            if (node.Condition != null)
            {
                // Evaluate condition
                node.Condition.Accept(this);

                // Jump past table call if condition is false
                skipJump = new JumpIfFalseInstruction(-1, _position++);
                Emit(skipJump);
            }

            // Evaluate all argument expressions and push onto stack
            // Arguments will be popped in reverse order by the instruction
            var argumentSpecs = new List<TableArgumentSpec>();
            foreach (var arg in node.Arguments)
            {
                // Compile the argument value expression
                arg.Value.Accept(this);

                // Extract the variable name if it's an identifier (for both optional and required params)
                // This enables restriction mode where all params are bidirectional
                string? varName = null;
                if (arg.Value is IdentifierNode idNode)
                {
                    varName = idNode.Name;
                }

                // Record the argument specification
                argumentSpecs.Add(new TableArgumentSpec(arg.TableColumn, arg.IsOptional, varName));
            }

            // Emit the table call instruction
            Emit(new CallTableInstruction(node.TableName, argumentSpecs, _position++));

            // Patch conditional jump if present
            if (skipJump != null)
            {
                skipJump.TargetOffset = _instructions.Count;
            }
        }

        public void VisitBuiltInFunctionCall(BuiltInFunctionCallNode node)
        {
            _metadata.HasBuiltInFunctions = true;

            // Evaluate all argument expressions and push onto stack
            // Special handling: identifiers in built-in functions represent characteristic NAMES (as strings),
            // not their values. For example, $SET_DEFAULT($SELF, HEIGHT, 'Red') - HEIGHT is the name "HEIGHT"
            foreach (var arg in node.Arguments)
            {
                if (arg is IdentifierNode identifier)
                {
                    // Convert identifier to string constant (characteristic name)
                    Emit(new LoadConstInstruction(identifier.Name, LiteralType.String, _position++));
                }
                else
                {
                    // Normal expression evaluation
                    arg.Accept(this);
                }
            }

            // Emit the built-in function call instruction
            Emit(new CallBuiltinInstruction(node.FunctionType, node.Arguments.Count, _position++));
        }

        public void VisitReturnInconsistency(ReturnInconsistencyNode node)
        {
            Emit(new ReturnInconsistencyInstruction(null, _position++));
        }

        #endregion

        #region Helper Methods

        private void CompileStore(ExpressionNode target)
        {
            if (target is MemberAccessNode member)
            {
                // Procedure member access: $SELF.COLOR
                Emit(new StoreMemberInstruction(
                    ObjectRefToString(member.Obj),
                    member.MemberName,
                    _position++
                ));
            }
            else if (target is ConstraintMemberAccessNode constraintMember)
            {
                // Constraint member access: PC.COLOR
                Emit(new StoreConstraintMemberInstruction(
                    constraintMember.Variable,
                    constraintMember.MemberName,
                    _position++
                ));
            }
            else if (target is IdentifierNode identifier)
            {
                // Simple variable: HEIGHT
                Emit(new StoreVarInstruction(identifier.Name, _position++));
            }
            else
            {
                throw new CompilationException($"Invalid assignment target: {target.GetType().Name}");
            }
        }

        private Instruction GetBinaryOperatorInstruction(BinaryOperator op)
        {
            return op switch
            {
                BinaryOperator.Add => new AddInstruction(_position++),
                BinaryOperator.Subtract => new SubInstruction(_position++),
                BinaryOperator.Multiply => new MulInstruction(_position++),
                BinaryOperator.Divide => new DivInstruction(_position++),
                BinaryOperator.Equal => new EqInstruction(_position++),
                BinaryOperator.NotEqual => new NeInstruction(_position++),
                BinaryOperator.Less => new LtInstruction(_position++),
                BinaryOperator.LessEqual => new LeInstruction(_position++),
                BinaryOperator.Greater => new GtInstruction(_position++),
                BinaryOperator.GreaterEqual => new GeInstruction(_position++),
                BinaryOperator.And => new AndInstruction(_position++),
                BinaryOperator.Or => new OrInstruction(_position++),
                BinaryOperator.Concat => new ConcatInstruction(_position++),
                _ => throw new CompilationException($"Unsupported binary operator: {op}")
            };
        }

        private Instruction GetUnaryOperatorInstruction(UnaryOperator op)
        {
            return op switch
            {
                UnaryOperator.Not => new NotInstruction(_position++),
                UnaryOperator.Negate => new NegInstruction(_position++),
                _ => throw new CompilationException($"Unsupported unary operator: {op}")
            };
        }

        private static Instructions.DependencyType GetDependencyType(DependencyNode node)
        {
            return node switch
            {
                ProcedureNode => Instructions.DependencyType.Procedure,
                ConstraintNode => Instructions.DependencyType.Constraint,
                PreconditionNode => Instructions.DependencyType.Precondition,
                SelectionConditionNode => Instructions.DependencyType.SelectionCondition,
                _ => throw new CompilationException($"Unknown dependency type: {node.GetType().Name}")
            };
        }

        private static string ObjectRefToString(ObjectReference objRef)
        {
            return objRef switch
            {
                ObjectReference.Self => "$SELF",
                ObjectReference.Parent => "$PARENT",
                ObjectReference.Root => "$ROOT",
                _ => throw new CompilationException($"Unknown object reference: {objRef}")
            };
        }

        private void Emit(Instruction instruction)
        {
            _instructions.Add(instruction);
        }

        #endregion
    }

    /// <summary>
    /// Exception thrown during compilation.
    /// </summary>
    public class CompilationException : Exception
    {
        public CompilationException(string message) : base(message) { }
        public CompilationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
