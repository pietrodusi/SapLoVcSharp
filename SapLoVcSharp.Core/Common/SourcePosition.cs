namespace SapLoVcSharp.Core.Common
{
    /// <summary>
    /// Represents a position in the source code for error reporting.
    /// </summary>
    public record SourcePosition(int Line, int Column, int AbsolutePosition)
    {
        /// <summary>
        /// Default position (start of file).
        /// </summary>
        public static SourcePosition Default => new(1, 1, 0);

        public override string ToString() => $"Line {Line}, Column {Column}";
    }
}