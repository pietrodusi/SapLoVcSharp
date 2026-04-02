namespace SapLoVcSharp.Core.Parsing
{
    /// <summary>
    /// Operator precedence levels for Pratt parsing.
    /// Higher number = higher precedence.
    /// </summary>
    public enum Precedence
    {
        None = 0,
        Assignment = 1,     // =
        Or = 2,             // OR
        And = 3,            // AND
        Equality = 4,       // = <> EQ NE
        Comparison = 5,     // < <= > >= LT LE GT GE
        Term = 6,           // + -
        Factor = 7,         // * /
        Unary = 8,          // NOT -
        Call = 9,           // Function calls
        Primary = 10,       // Literals, identifiers, ()
    }
}