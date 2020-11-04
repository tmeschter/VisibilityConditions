using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace VisibilityConditions.Test
{
    public class InterpreterTests
    {
        [Fact]
        public void InterpretVariable()
        {
            var inputText = "abc";
            var tokens = ExpressionLexer.Lex(inputText);
            var parseResult = (ParseSuccess)ExpressionParser.Parse(tokens);
            var interpreter = new ExpressionInterpreter(getVariable);
            var interpretationResult = interpreter.Interpret(parseResult.Expression);

            Assert.Equal(expected: new InterpretationSuccess(123), actual: interpretationResult);

            object getVariable(string variableName)
            {
                if (variableName == "abc")
                {
                    return 123;
                }

                throw new InvalidOperationException();
            }
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("42", 42)]
        [InlineData("042", 42)]
        [InlineData("\"Alpha\"", "Alpha")]
        [InlineData("(not true)", false)]
        [InlineData("(not false)", true)]
        [InlineData("(not (not false))", false)]
        [InlineData("(eq 42 42)", true)]
        [InlineData("(eq 42 43)", false)]
        [InlineData("(neq 1 2)", true)]
        [InlineData("(and true false)", false)]
        [InlineData("(or true false)", true)]
        [InlineData("(and (lte 5 6) (not (eq 2 3)))", true)]
        [InlineData("(or (eq \"Foo\" \"Bar\") true)", true)]
        public void Interpret(string inputText, object expectedValue)
        {
            var tokens = ExpressionLexer.Lex(inputText);
            var parseResult = (ParseSuccess)ExpressionParser.Parse(tokens);
            var interpreter = new ExpressionInterpreter(varName => throw new InvalidOperationException());
            var interpretationResult = interpreter.Interpret(parseResult.Expression);

            Assert.Equal(
                expected:
                    new InterpretationSuccess(expectedValue),
                actual:
                    interpretationResult);
        }
    }
}
