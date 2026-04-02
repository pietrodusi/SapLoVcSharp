namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Calls a mathematical or string function (e.g., SIN, COS, SQRT, MAX, MIN, etc.).
    /// Pops arguments from stack, computes result, pushes result back.
    /// Example: SQRT(25) pushes 5.0
    /// </summary>
    public class CallFunctionInstruction : Instruction
    {
        public string FunctionName { get; }
        public int ArgumentCount { get; }

        public CallFunctionInstruction(string functionName, int argumentCount, int position)
            : base(OpCode.CallFunction, position)
        {
            FunctionName = functionName;
            ArgumentCount = argumentCount;
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            // Pop arguments from stack (in reverse order)
            var arguments = new object?[ArgumentCount];
            for (int i = ArgumentCount - 1; i >= 0; i--)
            {
                arguments[i] = vm.Stack.Pop();
            }

            // Execute the function
            var result = ExecuteFunction(arguments);

            // Push result onto stack
            vm.Stack.Push(result);
            return Task.CompletedTask;
        }

        private object? ExecuteFunction(object?[] arguments)
        {
            return FunctionName.ToUpperInvariant() switch
            {
                // Trigonometric functions
                "SIN" => Math.Sin(ToDouble(arguments[0])),
                "COS" => Math.Cos(ToDouble(arguments[0])),
                "TAN" => Math.Tan(ToDouble(arguments[0])),
                "ASIN" => Math.Asin(ToDouble(arguments[0])),
                "ACOS" => Math.Acos(ToDouble(arguments[0])),
                "ATAN" => Math.Atan(ToDouble(arguments[0])),

                // Mathematical functions
                "SQRT" => Math.Sqrt(ToDouble(arguments[0])),
                "ABS" => Math.Abs(ToDouble(arguments[0])),
                "EXP" => Math.Exp(ToDouble(arguments[0])),
                "LOG" => Math.Log(ToDouble(arguments[0])),
                "LOG10" => Math.Log10(ToDouble(arguments[0])),
                "CEIL" => Math.Ceiling(ToDouble(arguments[0])),
                "FLOOR" => Math.Floor(ToDouble(arguments[0])),
                "ROUND" => Math.Round(ToDouble(arguments[0])),

                // Aggregate functions
                "MAX" => arguments.Select(a => ToDouble(a)).Max(),
                "MIN" => arguments.Select(a => ToDouble(a)).Min(),

                // String functions
                "LC" => arguments[0]?.ToString()?.ToLowerInvariant(),
                "UC" => arguments[0]?.ToString()?.ToUpperInvariant(),
                "LEN" => arguments[0]?.ToString()?.Length,

                _ => throw new InvalidOperationException($"Unknown function: {FunctionName}")
            };
        }

        private static double ToDouble(object? value)
        {
            if (value == null)
                throw new ArgumentNullException("Cannot convert null to number");

            try
            {
                return Convert.ToDouble(value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot convert {value} to number", ex);
            }
        }

        public override string ToString() => $"CALL {FunctionName}({ArgumentCount} args)";
    }
}
