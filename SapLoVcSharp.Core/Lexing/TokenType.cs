namespace SapLoVcSharp.Core.Lexing
{
    /// <summary>
    /// Defines all token types recognized by the SAP LO-VC lexer.
    /// </summary>
    public enum TokenType
    {
        // End of file
        EOF,

        // Constraint sections (case-insensitive keywords)
        Objects,        // OBJECTS:
        Condition,      // CONDITION:
        Restrictions,   // RESTRICTIONS:
        Inferences,     // INFERENCES:

        // Punctuation
        Colon,          // :
        Comma,          // ,
        Semicolon,      // ;
        Dot,            // .
        OpenParen,      // (
        CloseParen,     // )
        OpenBracket,    // [
        CloseBracket,   // ]

        // Arithmetic operators
        Plus,           // +
        Minus,          // - (also unary minus)
        Multiply,       // *
        Divide,         // /

        // Comparison operators (multi-character checked first!)
        LessEqual,      // <= or =
        GreaterEqual,   // >= or =>
        NotEqual,       // <> or >
        Less,           // 
        Greater,        // >
        Equal,          // =

        // Comparison operator keywords (case-insensitive)
        EQ,             // EQ (equal)
        NE,             // NE (not equal)
        LT,             // LT (less than)
        LE,             // LE (less than or equal)
        GT,             // GT (greater than)
        GE,             // GE (greater than or equal)

        // Logical operators
        And,            // AND
        Or,             // OR
        Not,            // NOT

        // Special operators/keywords
        In,             // IN
        If,             // IF
        Specified,      // SPECIFIED
        TypeOf,         // TYPE_OF
        PartOf,         // PART_OF
        SubpartOf,      // SUBPART_OF

        // Object variables
        Self,           // $SELF
        Parent,         // $PARENT
        Root,           // $ROOT
        MData,          // MDATA

        // Constraint object declaration
        IsA,            // IS_A
        IsObject,       // IS_OBJECT
        Where,          // WHERE

        // Table operations
        Table,          // TABLE

        // Built-in functions - mathematical
        Sin,            // SIN
        Cos,            // COS
        Tan,            // TAN
        Exp,            // EXP
        Ln,             // LN
        Log10,          // LOG10
        Abs,            // ABS
        Sqrt,           // SQRT
        ArcSin,         // ARCSIN
        ArcCos,         // ARCCOS
        ArcTan,         // ARCTAN
        Sign,           // SIGN
        Frac,           // FRAC
        Ceil,           // CEIL
        Trunc,          // TRUNC
        Floor,          // FLOOR


        // Built-in functions - string
        LC,             // LC (lowercase)
        UC,             // UC (uppercase)
        Concat,         // || (string concatenation)

        // Built-in functions - special
        SetDefault,         // $SET_DEFAULT
        DelDefault,         // $DEL_DEFAULT
        SumParts,           // $SUM_PARTS
        CountParts,         // $COUNT_PARTS
        SetPricingFactor,   // $SET_PRICING_FACTOR

        // Literals
        String,         // 'value' or "value"
        Number,         // 123, 123.45, 314E-2

        // Boolean literals
        True,           // TRUE
        False,          // FALSE

        // Identifiers (characteristics, variable names, etc.)
        Identifier,     // Any valid identifier

        // Special
        QuestionMark,   // ? (for variable prefixes like ?PC)
    }
}