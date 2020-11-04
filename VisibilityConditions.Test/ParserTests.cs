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
    }
}
