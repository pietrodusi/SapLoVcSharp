using FluentAssertions;
using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Tests.Lexing
{
    public class SourcePositionTests
    {
        [Fact]
        public void SourcePosition_ShouldCreateWithCorrectValues()
        {
            // Arrange & Act
            var position = new SourcePosition(5, 10, 42);

            // Assert
            position.Line.Should().Be(5);
            position.Column.Should().Be(10);
            position.AbsolutePosition.Should().Be(42);
        }

        [Fact]
        public void SourcePosition_Default_ShouldReturnStartPosition()
        {
            // Act
            var position = SourcePosition.Default;

            // Assert
            position.Line.Should().Be(1);
            position.Column.Should().Be(1);
            position.AbsolutePosition.Should().Be(0);
        }

        [Fact]
        public void SourcePosition_ToString_ShouldFormatCorrectly()
        {
            // Arrange
            var position = new SourcePosition(10, 25, 100);

            // Act
            var result = position.ToString();

            // Assert
            result.Should().Be("Line 10, Column 25");
        }

        [Fact]
        public void SourcePosition_RecordEquality_ShouldWorkCorrectly()
        {
            // Arrange
            var pos1 = new SourcePosition(5, 10, 42);
            var pos2 = new SourcePosition(5, 10, 42);
            var pos3 = new SourcePosition(5, 11, 43);

            // Assert
            pos1.Should().Be(pos2);
            pos1.Should().NotBe(pos3);
        }
    }
}