using System;
using Xunit;

namespace VisibilityConditions.Test
{
    public class LexerTests
    {
        [Theory]
        [InlineData("(", TokenType.LeftParen)]
        [InlineData(")", TokenType.RightParen)]
        [InlineData("abc", TokenType.Symbol)]
        [InlineData("a1234", TokenType.Symbol)]
        [InlineData("true", TokenType.BooleanLiteral)]
        [InlineData("false", TokenType.BooleanLiteral)]
        [InlineData("0", TokenType.IntLiteral)]
        [InlineData("10", TokenType.IntLiteral)]
        [InlineData("0123", TokenType.IntLiteral)]
        [InlineData("\"Lorem ipsum dolor sit\"", TokenType.StringLiteral)]
        public void IndividualTokensLexCorrectly(string inputText, TokenType expectedTokenType)
        {
            var result = ExpressionLexer.Lex(inputText);

            Assert.Collection(result, new Action<Token>[]
            {
                token => Assert.Equal(new Token(expectedTokenType, 0, 0, inputText), token),
                token => Assert.Equal(new Token(TokenType.EndOfInput, inputText.Length, 0, string.Empty), token)
            });

            const string leadingWhitespace = "  ";
            string inputWithLeadingWhitespace = leadingWhitespace + inputText;

            result = ExpressionLexer.Lex(inputWithLeadingWhitespace);

            Assert.Collection(result, new Action<Token>[]
            {
                token => Assert.Equal(new Token(expectedTokenType, 0, leadingWhitespace.Length, inputText), token),
                token => Assert.Equal(new Token(TokenType.EndOfInput, leadingWhitespace.Length + inputText.Length, 0, string.Empty), token)
            });

            const string trailingWhitespace = "   ";
            string inputWithLeadingAndTrailingWhitespace = leadingWhitespace + inputText + trailingWhitespace;

            result = ExpressionLexer.Lex(inputWithLeadingAndTrailingWhitespace);

            Assert.Collection(result, new Action<Token>[]
            {
                token => Assert.Equal(new Token(expectedTokenType, 0, leadingWhitespace.Length, inputText), token),
                token => Assert.Equal(new Token(TokenType.EndOfInput, leadingWhitespace.Length + inputText.Length, trailingWhitespace.Length, string.Empty), token)
            });
        }
    }
}
