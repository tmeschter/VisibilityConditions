using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace VisibilityConditions.Test
{
    public class ParserTests
    {
        [Fact]
        public void ParseVariable()
        {
            var inputText = "abc";
            var tokens = ExpressionLexer.Lex(inputText);
            var result = ExpressionParser.Parse(tokens);

            Assert.Equal(expected: new ParseSuccess(new Variable("abc"), 1), actual: result);
        }

        [Fact]
        public void ParseIntConstant()
        {
            var inputText = "42";
            var tokens = ExpressionLexer.Lex(inputText);
            var result = ExpressionParser.Parse(tokens);

            Assert.Equal(expected: new ParseSuccess(new IntConstant(42), 1), actual: result);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public void ParseBooleanConstant(string inputText, bool expectedValue)
        {
            var tokens = ExpressionLexer.Lex(inputText);
            var result = ExpressionParser.Parse(tokens);

            Assert.Equal(expected: new ParseSuccess(new BooleanConstant(expectedValue), 1), actual: result);
        }

        [Fact]
        public void ParseStringConstant()
        {
            var inputText = "\"I am the very model of a modern major general\"";
            var tokens = ExpressionLexer.Lex(inputText);
            var result = ExpressionParser.Parse(tokens);

            Assert.Equal(
                expected: new ParseSuccess(new StringConstant("I am the very model of a modern major general"), 1),
                actual: result);
        }

        [Theory]
        [InlineData("and", BinaryOperation.And)]
        [InlineData("or", BinaryOperation.Or)]
        [InlineData("eq", BinaryOperation.Equal)]
        [InlineData("neq", BinaryOperation.NotEqual)]
        [InlineData("gt", BinaryOperation.GreaterThan)]
        [InlineData("lt", BinaryOperation.LessThan)]
        [InlineData("gte", BinaryOperation.GreaterThanOrEqual)]
        [InlineData("lte", BinaryOperation.LessThanOrEqual)]
        public void ParseBinop(string operationName, BinaryOperation expectedOperation)
        {
            var inputText = $"({operationName} true false)";
            var tokens = ExpressionLexer.Lex(inputText);
            var result = ExpressionParser.Parse(tokens);

            Assert.Equal(
                expected: new ParseSuccess(
                    new BinaryExpression(
                        expectedOperation,
                        new BooleanConstant(true),
                        new BooleanConstant(false)),
                    5),
                actual: result);
        }

        [Fact]
        public void ParseCompoundBinop()
        {
            var inputText = $"(and (or true false) (or false true))";
            var tokens = ExpressionLexer.Lex(inputText);
            var result = ExpressionParser.Parse(tokens);

            Assert.Equal(
                expected:
                    new ParseSuccess(
                        new BinaryExpression(
                            BinaryOperation.And,
                            new BinaryExpression(
                                BinaryOperation.Or,
                                new BooleanConstant(true),
                                new BooleanConstant(false)),
                            new BinaryExpression(
                                BinaryOperation.Or,
                                new BooleanConstant(false),
                                new BooleanConstant(true))),
                        13),
                actual: result);
        }

        [Theory]
        [InlineData("not", UnaryOperation.Not)]
        public void ParseUnop(string operationName, UnaryOperation expectedOperation)
        {
            var inputText = $"({operationName} true)";
            var tokens = ExpressionLexer.Lex(inputText);
            var result = ExpressionParser.Parse(tokens);

            Assert.Equal(
                expected:
                    new ParseSuccess(
                        new UnaryExpression(
                            expectedOperation,
                            new BooleanConstant(true)),
                        4),
                actual:
                    result);
        }
    }
}
