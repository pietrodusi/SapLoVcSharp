using FluentAssertions;
using SapLoVcSharp.Core.Common;
using SapLoVcSharp.Core.Lexing;

namespace SapLoVcSharp.Core.Tests.Lexing
{
    public class LexerExceptionTests
    {
        [Fact]
        public void LexerException_ShouldIncludePositionInMessage()
        {
            // Arrange
            var position = new SourcePosition(5, 10, 42);

            // Act
            var exception = new LexerException("Test error", position);

            // Assert
            exception.Message.Should().Contain("Test error");
            exception.Message.Should().Contain("Line 5, Column 10");
            exception.Position.Should().Be(position);
        }

        [Fact]
        public void LexerException_ShouldSupportInnerException()
        {
            // Arrange
            var position = new SourcePosition(1, 1, 0);
            var inner = new InvalidOperationException("Inner error");

            // Act
            var exception = new LexerException("Outer error", position, inner);

            // Assert
            exception.InnerException.Should().Be(inner);
        }
    }
}