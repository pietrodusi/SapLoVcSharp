namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops two values from the stack, compares for equality, and pushes the result.
    /// Example: Stack ["Red", "Red"] -> EQ -> Stack [true]
    /// </summary>
    public class EqInstruction : Instruction
    {
        public EqInstruction(int position)
            : base(OpCode.Eq, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var right = vm.Stack.Pop();
            var left = vm.Stack.Pop();

            // Unwrap JsonElement values to their native types
            left = UnwrapJsonElement(left);
            right = UnwrapJsonElement(right);

            // Handle null comparisons
            if (left == null && right == null)
            {
                vm.Stack.Push(true);
            }
            else if (left == null || right == null)
            {
                vm.Stack.Push(false);
            }
            else
            {
                // For numbers, convert and compare
                if (IsNumeric(left) && IsNumeric(right))
                {
                    var leftNum = Convert.ToDouble(left);
                    var rightNum = Convert.ToDouble(right);
                    vm.Stack.Push(Math.Abs(leftNum - rightNum) < 1e-10);
                }
                else
                {
                    // For everything else, use Equals
                    vm.Stack.Push(left.Equals(right));
                }
            }

            return Task.CompletedTask;
        }

        private static object? UnwrapJsonElement(object? value)
        {
            if (value is System.Text.Json.JsonElement jsonElement)
            {
                return jsonElement.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => jsonElement.GetString(),
                    System.Text.Json.JsonValueKind.Number => jsonElement.GetDouble(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    System.Text.Json.JsonValueKind.Null => null,
                    _ => value
                };
            }
            return value;
        }

        private static bool IsNumeric(object? obj)
        {
            return obj is int or long or float or double or decimal;
        }

        public override string ToString() => "EQ";
    }
}
