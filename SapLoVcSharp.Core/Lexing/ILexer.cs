namespace SapLoVcSharp.Core.Lexing
{
    /// <summary>
    /// Interface for lexical analysis of SAP LO-VC source code.
    /// </summary>
    public interface ILexer
    {
        /// <summary>
        /// Tokenizes the source code into a list of tokens.
        /// </summary>
        /// <returns>List of tokens including an EOF token at the end.</returns>
        /// <exception cref="LexerException">Thrown when invalid characters or malformed tokens are encountered.</exception>
        List<Token> Tokenize();
    }
}