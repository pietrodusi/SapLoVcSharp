using FluentAssertions;
using SapLoVcSharp.Core.Common;
using SapLoVcSharp.Core.Lexing;

namespace SapLoVcSharp.Core.Tests.Lexing
{
    public class TokenTests
    {
        [Fact]
        public void Token_ShouldCreateWithCorrectValues()
        {
            // Arrange
            var position = new SourcePosition(1, 5, 4);

            // Act
            var token = new Token(TokenType.Identifier, "COLOR", position);

            // Assert
            token.Type.Should().Be(TokenType.Identifier);
            token.Value.Should().Be("COLOR");
            token.Position.Should().Be(position);
        }

        [Fact]
        public void Token_ToString_ShouldFormatCorrectly()
        {
            // Arrange
            var position = new SourcePosition(1, 1, 0);
            var token = new Token(TokenType.Self, "$SELF", position);

            // Act
            var result = token.ToString();

            // Assert
            result.Should().Contain("Self");
            result.Should().Contain("$SELF");
            result.Should().Contain("Line 1, Column 1");
        }

        [Fact]
        public void Token_RecordEquality_ShouldWorkCorrectly()
        {
            // Arrange
            var pos = new SourcePosition(1, 1, 0);
            var token1 = new Token(TokenType.Equal, "=", pos);
            var token2 = new Token(TokenType.Equal, "=", pos);
            var token3 = new Token(TokenType.NotEqual, "<>", pos);

            // Assert
            token1.Should().Be(token2);
            token1.Should().NotBe(token3);
        }
    }
}