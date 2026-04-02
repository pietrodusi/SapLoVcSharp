namespace SapLoVcSharp.Core.Common
{
    /// <summary>
    /// Base exception for all SAP LO-VC parsing and runtime errors.
    /// </summary>
    public abstract class SapLoVcException : Exception
    {
        public SourcePosition Position { get; }

        protected SapLoVcException(string message, SourcePosition position)
            : base($"{message} at {position}")
        {
            Position = position;
        }

        protected SapLoVcException(string message, SourcePosition position, Exception innerException)
            : base($"{message} at {position}", innerException)
        {
            Position = position;
        }
    }
}