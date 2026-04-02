namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Pops two values from the stack, compares for inequality, and pushes the result.
    /// Example: Stack ["Red", "Blue"] -> NE -> Stack [true]
    /// </summary>
    public class NeInstruction : Instruction
    {
        public NeInstruction(int position)
            : base(OpCode.Ne, position)
        {
        }

        public override Task ExecuteAsync(VirtualMachine vm)
        {
            var right = vm.Stack.Pop();
            var left = vm.Stack.Pop();

            // Use same logic as EqInstruction but negate result
            bool isEqual;
            if (left == null && right == null)
            {
                isEqual = true;
            }
            else if (left == null || right == null)
            {
                isEqual = false;
            }
            else if (IsNumeric(left) && IsNumeric(right))
            {
                var leftNum = Convert.ToDouble(left);
                var rightNum = Convert.ToDouble(right);
                isEqual = Math.Abs(leftNum - rightNum) < 1e-10;
            }
            else
            {
                isEqual = left.Equals(right);
            }

            vm.Stack.Push(!isEqual);
            return Task.CompletedTask;
        }

        private static bool IsNumeric(object? obj)
        {
            return obj is int or long or float or double or decimal;
        }

        public override string ToString() => "NE";
    }
}
