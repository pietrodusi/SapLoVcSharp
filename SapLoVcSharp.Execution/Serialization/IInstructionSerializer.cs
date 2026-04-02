using SapLoVcSharp.Execution.Instructions;

namespace SapLoVcSharp.Execution.Serialization
{
    /// <summary>
    /// Serializer for instruction lists to/from JSON.
    /// </summary>
    public interface IInstructionSerializer
    {
        /// <summary>
        /// Serializes an instruction list to JSON
        /// </summary>
        string Serialize(InstructionList instructionList);

        /// <summary>
        /// Deserializes an instruction list from JSON
        /// </summary>
        InstructionList Deserialize(string json);
    }
}
