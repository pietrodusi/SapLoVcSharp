using FluentAssertions;
using SapLoVcSharp.Core.Lexing;
using SapLoVcSharp.Core.Tests.Helpers;

namespace SapLoVcSharp.Core.Tests.Lexing
{
    public class LexerTests
    {
        #region Basic Tokenization

        [Fact]
        public void Lexer_EmptyString_ShouldReturnOnlyEofToken()
        {
            // Arrange
            var lexer = new Lexer("");

            // Act
            var tokens = lexer.Tokenize();

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.EOF);
        }

        [Fact]
        public void Lexer_WhitespaceOnly_ShouldReturnOnlyEofToken()
        {
            // Arrange
            var lexer = new Lexer("   \t\n\r\n  ");

            // Act
            var tokens = lexer.Tokenize();

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.EOF);
        }

        [Fact]
        public void Lexer_NullSource_ShouldThrowArgumentNullException()
        {
            // Act
            Action act = () => new Lexer(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region Identifiers

        [Fact]
        public void Lexer_SimpleIdentifier_ShouldTokenizeCorrectly()
        {
            // Arrange & Act
            var tokens = LexerTestHelper.TokenizeWithoutEof("COLOR");

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.Identifier);
            tokens[0].Value.Should().Be("COLOR");
        }

        [Fact]
        public void Lexer_IdentifierWithUnderscore_ShouldTokenizeCorrectly()
        {
            // Arrange & Act
            var tokens = LexerTestHelper.TokenizeWithoutEof("BIKE_TYPE");

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.Identifier);
            tokens[0].Value.Should().Be("BIKE_TYPE");
        }

        [Fact]
        public void Lexer_IdentifierWithNumbers_ShouldTokenizeCorrectly()
        {
            // Arrange & Act
            var tokens = LexerTestHelper.TokenizeWithoutEof("VAR123");

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.Identifier);
            tokens[0].Value.Should().Be("VAR123");
        }

        #endregion

        #region Keywords

        [Theory]
        [InlineData("AND", TokenType.And)]
        [InlineData("and", TokenType.And)]
        [InlineData("And", TokenType.And)]
        [InlineData("OR", TokenType.Or)]
        [InlineData("NOT", TokenType.Not)]
        [InlineData("IF", TokenType.If)]
        [InlineData("IN", TokenType.In)]
        [InlineData("SPECIFIED", TokenType.Specified)]
        [InlineData("TRUE", TokenType.True)]
        [InlineData("FALSE", TokenType.False)]
        [InlineData("EQ", TokenType.EQ)]
        [InlineData("NE", TokenType.NE)]
        [InlineData("LT", TokenType.LT)]
        [InlineData("LE", TokenType.LE)]
        [InlineData("GT", TokenType.GT)]
        [InlineData("GE", TokenType.GE)]
        public void Lexer_Keywords_ShouldBeCaseInsensitive(string keyword, TokenType expectedType)
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof(keyword);

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(expectedType);
        }

        [Theory]
        [InlineData("OBJECTS", TokenType.Objects)]
        [InlineData("CONDITION", TokenType.Condition)]
        [InlineData("RESTRICTIONS", TokenType.Restrictions)]
        [InlineData("INFERENCES", TokenType.Inferences)]
        public void Lexer_ConstraintSections_ShouldTokenizeCorrectly(string keyword, TokenType expectedType)
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof(keyword);

            // Assert
            tokens[0].Type.Should().Be(expectedType);
        }

        [Theory]
        [InlineData("IS_A", TokenType.IsA)]
        [InlineData("IS_OBJECT", TokenType.IsObject)]
        [InlineData("WHERE", TokenType.Where)]
        [InlineData("TYPE_OF", TokenType.TypeOf)]
        [InlineData("PART_OF", TokenType.PartOf)]
        [InlineData("SUBPART_OF", TokenType.SubpartOf)]
        [InlineData("MDATA", TokenType.MData)]
        [InlineData("TABLE", TokenType.Table)]
        public void Lexer_SpecialKeywords_ShouldTokenizeCorrectly(string keyword, TokenType expectedType)
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof(keyword);

            // Assert
            tokens[0].Type.Should().Be(expectedType);
        }

        #endregion

        #region Special Variables

        [Theory]
        [InlineData("$SELF", TokenType.Self)]
        [InlineData("$PARENT", TokenType.Parent)]
        [InlineData("$ROOT", TokenType.Root)]
        [InlineData("$SET_DEFAULT", TokenType.SetDefault)]
        [InlineData("$DEL_DEFAULT", TokenType.DelDefault)]
        [InlineData("$SUM_PARTS", TokenType.SumParts)]
        [InlineData("$COUNT_PARTS", TokenType.CountParts)]
        [InlineData("$SET_PRICING_FACTOR", TokenType.SetPricingFactor)]
        public void Lexer_SpecialVariables_ShouldTokenizeCorrectly(string variable, TokenType expectedType)
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof(variable);

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(expectedType);
            tokens[0].Value.Should().Be(variable.ToUpper());
        }

        [Fact]
        public void Lexer_UnknownSpecialVariable_ShouldThrowException()
        {
            // Act
            Action act = () => LexerTestHelper.Tokenize("$UNKNOWN");

            // Assert
            act.Should().Throw<LexerException>()
                .WithMessage("*Unknown special variable*");
        }

        #endregion

        #region Mathematical Functions

        [Theory]
        [InlineData("SIN", TokenType.Sin)]
        [InlineData("COS", TokenType.Cos)]
        [InlineData("TAN", TokenType.Tan)]
        [InlineData("EXP", TokenType.Exp)]
        [InlineData("LN", TokenType.Ln)]
        [InlineData("LOG10", TokenType.Log10)]
        [InlineData("ABS", TokenType.Abs)]
        [InlineData("SQRT", TokenType.Sqrt)]
        [InlineData("ARCSIN", TokenType.ArcSin)]
        [InlineData("ARCCOS", TokenType.ArcCos)]
        [InlineData("ARCTAN", TokenType.ArcTan)]
        [InlineData("SIGN", TokenType.Sign)]
        [InlineData("FRAC", TokenType.Frac)]
        [InlineData("CEIL", TokenType.Ceil)]
        [InlineData("TRUNC", TokenType.Trunc)]
        [InlineData("FLOOR", TokenType.Floor)]
        public void Lexer_MathFunctions_ShouldTokenizeCorrectly(string function, TokenType expectedType)
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof(function);

            // Assert
            tokens[0].Type.Should().Be(expectedType);
        }

        [Theory]
        [InlineData("LC", TokenType.LC)]
        [InlineData("UC", TokenType.UC)]
        public void Lexer_StringFunctions_ShouldTokenizeCorrectly(string function, TokenType expectedType)
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof(function);

            // Assert
            tokens[0].Type.Should().Be(expectedType);
        }

        #endregion

        #region String Literals

        [Fact]
        public void Lexer_SingleQuotedString_ShouldTokenizeCorrectly()
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof("'Hello World'");

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.String);
            tokens[0].Value.Should().Be("Hello World");
        }

        [Fact]
        public void Lexer_DoubleQuotedString_ShouldTokenizeCorrectly()
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof("\"Hello World\"");

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.String);
            tokens[0].Value.Should().Be("Hello World");
        }

        [Fact]
        public void Lexer_EmptyString_ShouldTokenizeCorrectly()
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof("''");

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.String);
            tokens[0].Value.Should().BeEmpty();
        }

        [Fact]
        public void Lexer_StringWithEscapedQuote_ShouldTokenizeCorrectly()
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof("'It\\'s working'");

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.String);
            tokens[0].Value.Should().Be("It's working");
        }

        [Fact]
        public void Lexer_UnterminatedString_ShouldThrowException()
        {
            // Act
            Action act = () => LexerTestHelper.Tokenize("'unterminated");

            // Assert
            act.Should().Throw<LexerException>()
                .WithMessage("*Unterminated string literal*");
        }

        #endregion

        #region Numbers

        [Theory]
        [InlineData("123", "123")]
        [InlineData("0", "0")]
        [InlineData("42", "42")]
        public void Lexer_Integer_ShouldTokenizeCorrectly(string source, string expectedValue)
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof(source);

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.Number);
            tokens[0].Value.Should().Be(expectedValue);
        }

        [Theory]
        [InlineData("123.45", "123.45")]
        [InlineData("0.5", "0.5")]
        [InlineData("3.14159", "3.14159")]
        public void Lexer_Decimal_ShouldTokenizeCorrectly(string source, string expectedValue)
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof(source);

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.Number);
            tokens[0].Value.Should().Be(expectedValue);
        }

        [Theory]
        [InlineData("-123", "-123")]
        [InlineData("-0.5", "-0.5")]
        public void Lexer_NegativeNumber_ShouldTokenizeCorrectly(string source, string expectedValue)
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof(source);

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.Number);
            tokens[0].Value.Should().Be(expectedValue);
        }

        [Theory]
        [InlineData("314E-2", "314E-2")]
        [InlineData("1.5E+10", "1.5E+10")]
        [InlineData("2e3", "2e3")]
        public void Lexer_ExponentialNotation_ShouldTokenizeCorrectly(string source, string expectedValue)
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof(source);

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.Number);
            tokens[0].Value.Should().Be(expectedValue);
        }

        #endregion

        #region Operators

        [Theory]
        [InlineData("=", TokenType.Equal)]
        [InlineData("<", TokenType.Less)]
        [InlineData(">", TokenType.Greater)]
        [InlineData("<=", TokenType.LessEqual)]
        [InlineData("=<", TokenType.LessEqual)]
        [InlineData(">=", TokenType.GreaterEqual)]
        [InlineData("=>", TokenType.GreaterEqual)]
        [InlineData("<>", TokenType.NotEqual)]
        [InlineData("><", TokenType.NotEqual)]
        public void Lexer_ComparisonOperators_ShouldTokenizeCorrectly(string op, TokenType expectedType)
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof(op);

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(expectedType);
        }

        [Fact]
        public void Lexer_PlusOperator_ShouldTokenizeCorrectly()
        {
            var tokens = LexerTestHelper.TokenizeWithoutEof("+");
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.Plus);
        }

        [Fact]
        public void Lexer_MinusOperator_ShouldTokenizeCorrectly()
        {
            var tokens = LexerTestHelper.TokenizeWithoutEof("-");
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.Minus);
        }

        [Fact]
        public void Lexer_DivideOperator_ShouldTokenizeCorrectly()
        {
            var tokens = LexerTestHelper.TokenizeWithoutEof("/");
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.Divide);
        }

        [Fact]
        public void Lexer_StringConcatenation_ShouldTokenizeCorrectly()
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof("||");

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.Concat);
        }

        #endregion

        #region Punctuation

        [Theory]
        [InlineData(":", TokenType.Colon)]
        [InlineData(",", TokenType.Comma)]
        [InlineData(";", TokenType.Semicolon)]
        [InlineData(".", TokenType.Dot)]
        [InlineData("(", TokenType.OpenParen)]
        [InlineData(")", TokenType.CloseParen)]
        [InlineData("[", TokenType.OpenBracket)]
        [InlineData("]", TokenType.CloseBracket)]
        [InlineData("?", TokenType.QuestionMark)]
        public void Lexer_Punctuation_ShouldTokenizeCorrectly(string punct, TokenType expectedType)
        {
            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof(punct);

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(expectedType);
        }

        #endregion

        #region Comments

        [Fact]
        public void Lexer_CommentLine_ShouldBeIgnored()
        {
            // Arrange
            var source = @"* This is a comment
COLOR";

            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof(source);

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.Identifier);
            tokens[0].Value.Should().Be("COLOR");
        }

        [Fact]
        public void Lexer_MultipleCommentLines_ShouldBeIgnored()
        {
            // Arrange
            var source = @"* Comment 1
* Comment 2
* Comment 3
COLOR";

            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof(source);

            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Value.Should().Be("COLOR");
        }

        [Fact]
        public void Lexer_AsteriskNotAtStartOfLine_ShouldBeTokenized()
        {
            // Arrange (asterisk in middle of line should be multiply operator)
            var source = "5 * 3";

            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof(source);

            // Assert
            tokens.Should().HaveCount(3);
            tokens[1].Type.Should().Be(TokenType.Multiply);
        }

        [Fact]
        public void Lexer_MultiplyInExpression_ShouldWork()
        {
            var tokens = LexerTestHelper.TokenizeWithoutEof("5 * 3");
            tokens.Should().HaveCount(3);
            tokens[0].Type.Should().Be(TokenType.Number);
            tokens[1].Type.Should().Be(TokenType.Multiply);
            tokens[2].Type.Should().Be(TokenType.Number);
        }

        #endregion

        #region Complex Expressions

        [Fact]
        public void Lexer_SimpleProcedure_ShouldTokenizeCorrectly()
        {
            // Arrange
            var source = "$SELF.COLOR = 'Red' IF MODEL = 'Racing'";

            // Act & Assert
            LexerTestHelper.AssertTokenSequence(source,
                (TokenType.Self, "$SELF"),
                (TokenType.Dot, "."),
                (TokenType.Identifier, "COLOR"),
                (TokenType.Equal, "="),
                (TokenType.String, "Red"),
                (TokenType.If, "IF"),
                (TokenType.Identifier, "MODEL"),
                (TokenType.Equal, "="),
                (TokenType.String, "Racing")
            );
        }

        [Fact]
        public void Lexer_ArithmeticExpression_ShouldTokenizeCorrectly()
        {
            // Arrange
            var source = "$SELF.WIDTH = (LENGTH + 10) * 2.5";

            // Act & Assert
            LexerTestHelper.AssertTokenSequence(source,
                (TokenType.Self, "$SELF"),
                (TokenType.Dot, "."),
                (TokenType.Identifier, "WIDTH"),
                (TokenType.Equal, "="),
                (TokenType.OpenParen, "("),
                (TokenType.Identifier, "LENGTH"),
                (TokenType.Plus, "+"),
                (TokenType.Number, "10"),
                (TokenType.CloseParen, ")"),
                (TokenType.Multiply, "*"),
                (TokenType.Number, "2.5")
            );
        }

        [Fact]
        public void Lexer_LogicalExpression_ShouldTokenizeCorrectly()
        {
            // Arrange
            var source = "COLOR = 'Red' AND MODEL = 'Racing'";

            // Act & Assert
            LexerTestHelper.AssertTokenSequence(source,
                (TokenType.Identifier, "COLOR"),
                (TokenType.Equal, "="),
                (TokenType.String, "Red"),
                (TokenType.And, "AND"),
                (TokenType.Identifier, "MODEL"),
                (TokenType.Equal, "="),
                (TokenType.String, "Racing")
            );
        }

        [Fact]
        public void Lexer_TableCall_ShouldTokenizeCorrectly()
        {
            // Arrange
            var source = "TABLE T_BIKE (MODEL = MODEL, COLOR = COLOR)";

            // Act
            var tokens = LexerTestHelper.TokenizeWithoutEof(source);

            // Assert
            tokens[0].Type.Should().Be(TokenType.Table);
            tokens[1].Type.Should().Be(TokenType.Identifier);
            tokens[1].Value.Should().Be("T_BIKE");
            tokens[2].Type.Should().Be(TokenType.OpenParen);
        }

        [Fact]
        public void Lexer_ConstraintDeclaration_ShouldTokenizeCorrectly()
        {
            // Arrange
            var source = "PC IS_A (300) BIKE WHERE MOD = MODEL";

            // Act & Assert
            LexerTestHelper.AssertTokenSequence(source,
                (TokenType.Identifier, "PC"),
                (TokenType.IsA, "IS_A"),
                (TokenType.OpenParen, "("),
                (TokenType.Number, "300"),
                (TokenType.CloseParen, ")"),
                (TokenType.Identifier, "BIKE"),
                (TokenType.Where, "WHERE"),
                (TokenType.Identifier, "MOD"),
                (TokenType.Equal, "="),
                (TokenType.Identifier, "MODEL")
            );
        }

        [Fact]
        public void Lexer_FunctionCall_ShouldTokenizeCorrectly()
        {
            // Arrange
            var source = "SQRT(ABS(-25))";

            // Act & Assert
            LexerTestHelper.AssertTokenSequence(source,
                (TokenType.Sqrt, "SQRT"),
                (TokenType.OpenParen, "("),
                (TokenType.Abs, "ABS"),
                (TokenType.OpenParen, "("),
                (TokenType.Number, "-25"),
                (TokenType.CloseParen, ")"),
                (TokenType.CloseParen, ")")
            );
        }

        [Fact]
        public void Lexer_SubtractionExpression_ShouldTokenizeCorrectly()
        {
            // 5-3 should be three tokens: 5, -, 3
            var tokens = LexerTestHelper.TokenizeWithoutEof("5-3");

            tokens.Should().HaveCount(3);
            tokens[0].Type.Should().Be(TokenType.Number);
            tokens[0].Value.Should().Be("5");
            tokens[1].Type.Should().Be(TokenType.Minus);
            tokens[2].Type.Should().Be(TokenType.Number);
            tokens[2].Value.Should().Be("3");
        }

        [Fact]
        public void Lexer_NegativeNumberAfterEquals_ShouldBeOneToken()
        {
            // =-123 should be two tokens: =, -123
            var tokens = LexerTestHelper.TokenizeWithoutEof("=-123");

            tokens.Should().HaveCount(2);
            tokens[0].Type.Should().Be(TokenType.Equal);
            tokens[1].Type.Should().Be(TokenType.Number);
            tokens[1].Value.Should().Be("-123");
        }

        #endregion

        #region Position Tracking

        [Fact]
        public void Lexer_ShouldTrackLineAndColumnCorrectly()
        {
            // Arrange
            var source = @"COLOR
MODEL";

            // Act
            var tokens = LexerTestHelper.Tokenize(source);

            // Assert
            tokens[0].Position.Line.Should().Be(1);
            tokens[0].Position.Column.Should().Be(1);

            tokens[1].Position.Line.Should().Be(2);
            tokens[1].Position.Column.Should().Be(1);
        }

        [Fact]
        public void Lexer_ShouldTrackColumnWithinLine()
        {
            // Arrange
            var source = "A = B";

            // Act
            var tokens = LexerTestHelper.Tokenize(source);

            // Assert
            tokens[0].Position.Column.Should().Be(1); // A
            tokens[1].Position.Column.Should().Be(3); // =
            tokens[2].Position.Column.Should().Be(5); // B
        }

        #endregion

        #region Error Cases

        [Fact]
        public void Lexer_InvalidCharacter_ShouldThrowException()
        {
            // Act
            Action act = () => LexerTestHelper.Tokenize("COLOR @ MODEL");

            // Assert
            act.Should().Throw<LexerException>()
                .WithMessage("*Unexpected character*@*");
        }

        [Fact]
        public void Lexer_InvalidCharacter_ShouldIncludePosition()
        {
            // Act
            Action act = () => LexerTestHelper.Tokenize("COLOR @ MODEL");

            // Assert
            act.Should().Throw<LexerException>()
                .Which.Position.Column.Should().Be(7);
        }

        #endregion
    }
}