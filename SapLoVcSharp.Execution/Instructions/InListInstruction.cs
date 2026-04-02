namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Checks if a value is in a list of values.
    /// Pops the value from the stack, checks membership, pushes true/false.
    /// Example: COLOR IN ('Red', 'Blue') checks if COLOR is 'Red' or 'Blue'.
    /// </summary>
    public class InListInstruction : Instruction
    {
        public List<object?> Values { get; }

        public InListInstruction(List<object?> values, int position)
            : base(OpCode.InList, position)
        {
            Values = values;
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var value = vm.Stack.Pop();

            // Check if value is in the list
            bool found = false;
            foreach (var listValue in Values)
            {
                if (AreEqual(value, listValue))
                {
                    found = true;
                    break;
                }
            }

            vm.Stack.Push(found);
            return Task.CompletedTask;
        }

        private static bool AreEqual(object? a, object? b)
        {
            // Handle null equality
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            // String comparison (case-insensitive for SAP compatibility)
            if (a is string strA && b is string strB)
            {
                return string.Equals(strA, strB, StringComparison.OrdinalIgnoreCase);
            }

            // Numeric comparison (convert to double)
            if (IsNumeric(a) && IsNumeric(b))
            {
                try
                {
                    var numA = Convert.ToDouble(a);
                    var numB = Convert.ToDouble(b);
                    return Math.Abs(numA - numB) < 0.0000001; // Epsilon comparison
                }
                catch
                {
                    return false;
                }
            }

            // Default equality
            return a.Equals(b);
        }

        private static bool IsNumeric(object? value)
        {
            return value is int or long or float or double or decimal;
        }

        public override string ToString() =>
            $"IN_LIST ({string.Join(", ", Values.Select(v => v?.ToString() ?? "null"))})";
    }
}
