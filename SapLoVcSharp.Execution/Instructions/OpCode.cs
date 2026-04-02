namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Instruction opcodes for the SapLoVcSharp virtual machine.
    /// High-level IL-like instructions for executing compiled dependencies.
    /// </summary>
    public enum OpCode
    {
        // Stack operations
        LoadConst,          // Push literal value onto stack
        LoadVar,            // Load characteristic value onto stack
        StoreVar,           // Pop stack and store in characteristic
        Pop,                // Discard top of stack
        Dup,                // Duplicate top of stack

        // Arithmetic operations
        Add,                // Pop 2, push a + b
        Sub,                // Pop 2, push a - b
        Mul,                // Pop 2, push a * b
        Div,                // Pop 2, push a / b
        Neg,                // Pop 1, push -a

        // Comparison operations
        Eq,                 // Pop 2, push a == b
        Ne,                 // Pop 2, push a != b
        Lt,                 // Pop 2, push a < b
        Le,                 // Pop 2, push a <= b
        Gt,                 // Pop 2, push a > b
        Ge,                 // Pop 2, push a >= b

        // Logical operations
        And,                // Pop 2, push a AND b
        Or,                 // Pop 2, push a OR b
        Not,                // Pop 1, push NOT a

        // String operations
        Concat,             // Pop 2 strings, push concatenation

        // Member access operations
        LoadMember,         // Load characteristic from object ($SELF.COLOR)
        StoreMember,        // Store to characteristic in object
        LoadConstraintMember,   // Load from constraint variable (PC.COLOR)
        StoreConstraintMember,  // Store to constraint variable

        // Control flow operations
        Jump,               // Unconditional jump to instruction offset
        JumpIfFalse,        // Pop stack, jump if false
        JumpIfTrue,         // Pop stack, jump if true

        // Function call operations
        CallFunction,       // Call built-in math function (SIN, COS, etc.)
        CallBuiltin,        // Call built-in config function ($SET_DEFAULT, etc.)
        CallTable,          // Call variant table (async)

        // Special operations
        Specified,          // Check if characteristic has value
        InList,             // Check membership in list
        ReturnInconsistency,    // Signal configuration inconsistency
        ReturnBool,         // Return boolean value (for preconditions)
        Nop,                // No operation

        // Constraint-specific operations
        BindObject,         // Bind constraint variable to class instance
        ApplyInferences,    // Apply inferences to characteristics

        // Constraint transaction operations
        InitializeConstraintVar,    // Initialize local variable from config context (WHERE clause)
        BeginConstraintTransaction, // Begin isolated constraint execution
        CommitConstraintContext     // Apply constraint changes to config context
    }
}
