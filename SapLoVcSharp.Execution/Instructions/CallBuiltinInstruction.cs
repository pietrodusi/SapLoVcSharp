using SapLoVcSharp.Core.Ast.Statements;

namespace SapLoVcSharp.Execution.Instructions
{
    /// <summary>
    /// Calls a built-in configuration function like $SET_DEFAULT, $SUM_PARTS, etc.
    /// Pops arguments from stack (in reverse order), executes function, may push result.
    /// Example: $SET_DEFAULT(COLOR, 'Red')
    /// </summary>
    public class CallBuiltinInstruction : Instruction
    {
        public BuiltInFunctionType FunctionType { get; set; }
        public int ArgumentCount { get; set; }

        public CallBuiltinInstruction(BuiltInFunctionType functionType, int argumentCount, int position)
            : base(OpCode.CallBuiltin, position)
        {
            FunctionType = functionType;
            ArgumentCount = argumentCount;
        }

        public override async Task ExecuteAsync(VirtualMachine vm)
        {
            // Pop arguments from stack (in reverse order)
            var arguments = new object?[ArgumentCount];
            for (int i = ArgumentCount - 1; i >= 0; i--)
            {
                arguments[i] = vm.Stack.Pop();
            }

            // Execute the built-in function
            var result = await ExecuteBuiltInFunctionAsync(vm, arguments);

            // Some functions return values (like $SUM_PARTS), others don't
            if (result != null)
            {
                vm.Stack.Push(result);
            }
        }

        private async Task<object?> ExecuteBuiltInFunctionAsync(VirtualMachine vm, object?[] arguments)
        {
            return FunctionType switch
            {
                BuiltInFunctionType.SetDefault => await ExecuteSetDefaultAsync(vm, arguments),
                BuiltInFunctionType.DelDefault => await ExecuteDelDefaultAsync(vm, arguments),
                BuiltInFunctionType.SumParts => await ExecuteSumPartsAsync(vm, arguments),
                BuiltInFunctionType.CountParts => await ExecuteCountPartsAsync(vm, arguments),
                BuiltInFunctionType.SetPricingFactor => await ExecuteSetPricingFactorAsync(vm, arguments),
                _ => throw new InvalidOperationException($"Unknown built-in function: {FunctionType}")
            };
        }

        private async Task<object?> ExecuteSetDefaultAsync(VirtualMachine vm, object?[] arguments)
        {
            // $SET_DEFAULT($SELF, characteristic_name, default_value)
            if (arguments.Length != 3)
                throw new ArgumentException($"$SET_DEFAULT requires 3 arguments, got {arguments.Length}");

            var objectRef = arguments[0]?.ToString(); // "$SELF", "$PARENT", or "$ROOT"
            var characteristicName = arguments[1]?.ToString() ?? throw new ArgumentNullException("Characteristic name cannot be null");
            var defaultValue = arguments[2];

            // For now, we only support $SELF (operating on current context)
            if (objectRef != "$SELF")
                throw new NotSupportedException($"Only $SELF is currently supported for $SET_DEFAULT, got {objectRef}");

            // Set the default value for the characteristic
            await vm.Context.SetValueAsync(characteristicName, defaultValue);
            return null; // No return value
        }

        private async Task<object?> ExecuteDelDefaultAsync(VirtualMachine vm, object?[] arguments)
        {
            // $DEL_DEFAULT($SELF, characteristic_name)
            if (arguments.Length != 2)
                throw new ArgumentException($"$DEL_DEFAULT requires 2 arguments, got {arguments.Length}");

            var objectRef = arguments[0]?.ToString(); // "$SELF", "$PARENT", or "$ROOT"
            var characteristicName = arguments[1]?.ToString() ?? throw new ArgumentNullException("Characteristic name cannot be null");

            // For now, we only support $SELF (operating on current context)
            if (objectRef != "$SELF")
                throw new NotSupportedException($"Only $SELF is currently supported for $DEL_DEFAULT, got {objectRef}");

            // Delete the default value (set to null)
            await vm.Context.SetValueAsync(characteristicName, null);
            return null; // No return value
        }

        private async Task<object?> ExecuteSumPartsAsync(VirtualMachine vm, object?[] arguments)
        {
            // $SUM_PARTS($SELF, characteristic_name) - Sum values from all component parts
            if (arguments.Length != 2)
                throw new ArgumentException($"$SUM_PARTS requires 2 arguments, got {arguments.Length}");

            var objectRef = arguments[0]?.ToString(); // "$SELF", "$PARENT", or "$ROOT"
            var characteristicName = arguments[1]?.ToString() ?? throw new ArgumentNullException("Characteristic name cannot be null");

            // For now, we only support $SELF (operating on current context)
            if (objectRef != "$SELF")
                throw new NotSupportedException($"Only $SELF is currently supported for $SUM_PARTS, got {objectRef}");

            // This would require access to component parts - implementation depends on Context
            // For now, return 0 as a placeholder
            await Task.CompletedTask;
            return 0.0; // Placeholder - would sum from parts
        }

        private async Task<object?> ExecuteCountPartsAsync(VirtualMachine vm, object?[] arguments)
        {
            // $COUNT_PARTS() - Count number of component parts
            if (arguments.Length != 0)
                throw new ArgumentException($"$COUNT_PARTS requires 0 arguments, got {arguments.Length}");

            // This would require access to component parts - implementation depends on Context
            await Task.CompletedTask;
            return 0; // Placeholder - would count parts
        }

        private async Task<object?> ExecuteSetPricingFactorAsync(VirtualMachine vm, object?[] arguments)
        {
            // $SET_PRICING_FACTOR(factor_value)
            if (arguments.Length != 1)
                throw new ArgumentException($"$SET_PRICING_FACTOR requires 1 argument, got {arguments.Length}");

            var factorValue = Convert.ToDouble(arguments[0]);

            // This would set a pricing factor in the configuration context
            // For now, just acknowledge the call
            await Task.CompletedTask;
            return null; // No return value
        }

        public override string ToString() => $"CALL_BUILTIN ${FunctionType}(argc:{ArgumentCount})";
    }
}
