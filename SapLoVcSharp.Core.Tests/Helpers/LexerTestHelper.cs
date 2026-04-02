using SapLoVcSharp.Core.Lexing;

namespace SapLoVcSharp.Core.Tests.Helpers
{
    /// <summary>
    /// Helper methods for lexer testing.
    /// </summary>
    public static class LexerTestHelper
    {
        /// <summary>
        /// Tokenizes source code and returns the list of tokens (excluding EOF).
        /// </summary>
        public static List<Token> TokenizeWithoutEof(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            return tokens.Where(t => t.Type != TokenType.EOF).ToList();
        }

        /// <summary>
        /// Tokenizes source code and returns all tokens (including EOF).
        /// </summary>
        public static List<Token> Tokenize(string source)
        {

            var lexer = new Lexer(source);
            return lexer.Tokenize();
        }

        /// <summary>
        /// Creates a simple assertion helper for token sequences.
        /// </summary>
        public static void AssertTokenSequence(string source, params (TokenType Type, string Value)[] expectedTokens)
        {
            var tokens = TokenizeWithoutEof(source);

            if (tokens.Count != expectedTokens.Length)
            {
                throw new Exception(
                    $"Expected {expectedTokens.Length} tokens but got {tokens.Count}. " +
                    $"Tokens: {string.Join(", ", tokens.Select(t => t.Type))}");
            }

            for (int i = 0; i < expectedTokens.Length; i++)
            {
                if (tokens[i].Type != expectedTokens[i].Type)
                {
                    throw new Exception(
                        $"Token {i}: Expected type {expectedTokens[i].Type} but got {tokens[i].Type}");
                }

                if (tokens[i].Value != expectedTokens[i].Value)
                {
                    throw new Exception(
                        $"Token {i}: Expected value '{expectedTokens[i].Value}' but got '{tokens[i].Value}'");
                }
            }
        }
    }
}