using System;
using System.Collections.Immutable;

namespace VisibilityConditions
{
    // TOKENS

    public enum TokenType
    {
        LeftParen,
        RightParen,
        Symbol,
        BooleanLiteral,
        IntLiteral,
        StringLiteral,
        EndOfInput
    }

    public record Token(TokenType Type, int StartOfWhitespace, int WhitespaceLength, string Text)
    {
        public int Start => StartOfWhitespace + WhitespaceLength;
        public int Length => Text.Length;
        public int LengthWithWhitespace => WhitespaceLength + Text.Length;
    }

    public class ExpressionLexer
    {
        public static ImmutableArray<Token> Lex(string text)
        {
            var tokens = ImmutableArray.CreateBuilder<Token>();

            int index = 0;
            Token? token;
            bool done = false;
            while ((token = LexNextToken(text, index)) != null
                && !done)
            {
                if (token.Type is TokenType.EndOfInput)
                {
                    done = true;
                }

                tokens.Add(token);
                index = index + token.LengthWithWhitespace;
            }

            return tokens.ToImmutable();
        }

        private static Token? LexNextToken(string text, int start)
        {
            return LexSimpleToken(text, start, "(", TokenType.LeftParen)
                ?? LexSimpleToken(text, start, ")", TokenType.RightParen)
                ?? LexSimpleToken(text, start, "true", TokenType.BooleanLiteral)
                ?? LexSimpleToken(text, start, "false", TokenType.BooleanLiteral)
                ?? LexVariableLengthToken(text, start, SymbolLength, TokenType.Symbol)
                ?? LexVariableLengthToken(text, start, IntLiteralLength, TokenType.IntLiteral)
                ?? LexVariableLengthToken(text, start, StringLiteralLength, TokenType.StringLiteral)
                ?? LexEndOfInput(text, start);
        }

        private static Token? LexSimpleToken(string text, int start, string tokenText, TokenType tokenType)
        {
            int whitespaceLength = LexWhitespace(text, start);
            var tokenStart = start + whitespaceLength;

            if (tokenStart + tokenText.Length > text.Length)
            {
                return null;
            }

            for (int i = 0; i < tokenText.Length; i++)
            {
                if (text[tokenStart + i] != tokenText[i])
                {
                    return null;
                }
            }

            return new Token(tokenType, start, whitespaceLength, tokenText);
        }

        private static Token? LexVariableLengthToken(string text, int start, Func<string, int, int> getTokenLength, TokenType tokenType)
        {
            int whitespaceLength = LexWhitespace(text, start);
            var tokenStart = start + whitespaceLength;

            int tokenLength = getTokenLength(text, tokenStart);
            if (tokenLength == 0)
            {
                return null;
            }

            return new Token(tokenType, start, whitespaceLength, text.Substring(tokenStart, tokenLength));
        }

        private static int SymbolLength(string text, int start)
        {
            int index = start;
            if (index < text.Length
                && char.IsLetter(text[index]))
            {
                index++;
            }
            else
            {
                return 0;
            }

            while (index < text.Length
                && char.IsLetterOrDigit(text[index]))
            {
                index++;
            }

            return index - start;
        }

        private static int IntLiteralLength(string text, int start)
        {
            int index = start;
            while (index < text.Length
                && char.IsDigit(text[index]))
            {
                index++;
            }

            return index - start;
        }

        private static int StringLiteralLength(string text, int start)
        {
            int index = start;
            if (index >= text.Length
                || text[index] != '"')
            {
                return 0;
            }

            index++;

            while (index < text.Length
                && text[index] != '"')
            {
                index++;
            }

            if (index >= text.Length
                && text[index] != '"')
            {
                return 0;
            }

            index++;

            return index - start;
        }

        private static Token? LexEndOfInput(string text, int start)
        {
            var whitespaceLength = LexWhitespace(text, start);
            if (start + whitespaceLength != text.Length)
            {
                return null;
            }

            return new Token(TokenType.EndOfInput, start, whitespaceLength, string.Empty);
        }


        private static int LexWhitespace(string text, int start)
        {
            int index = start;

            while (index < text.Length
                && char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            return index - start;
        }
    }

    // EXPRESSIONS

    public enum BinaryOperation
    {
        And,
        Or,
        Equal,
        NotEqual,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual
    }

    public enum UnaryOperation
    {
        Not
    }

    public abstract record ExpressionType();

    public abstract record Expression();

    public record Variable(string Name) : Expression;

    public record IntConstant(int Value) : Expression;
    public record StringConstant(string Value) : Expression;
    public record BooleanConstant(bool Value) : Expression;

    public record UnaryExpression(UnaryOperation Operation, Expression Child) : Expression;

    public record BinaryExpression(BinaryOperation Operation, Expression Left, Expression Right) : Expression;

    public abstract record ParseResult(int NextTokenIndex);
    public record ParseSuccess(Expression Expression, int NextTokenIndex) : ParseResult(NextTokenIndex);
    public record ParseFailure(string Message, int NextTokenIndex) : ParseResult(NextTokenIndex);

    public class ExpressionParser
    {
        public static ParseResult Parse(ImmutableArray<Token> input)
        {
            var result = ParseExpression(input, start: 0);

            return result switch
            {
                ParseFailure => result,
                ParseSuccess success when success.NextTokenIndex == input.Length - 1 => result,
                ParseSuccess success => new ParseFailure($"Unexpected token '{input[success.NextTokenIndex].Text}'.", success.NextTokenIndex),
                _ => throw new InvalidOperationException($"Unhandled parse result type: {result.GetType()}.")
            };
        }

        private static ParseResult ParseExpression(ImmutableArray<Token> tokens, int start)
        {
            return tokens[start].Type switch
            {
                TokenType.LeftParen => ParseSExpression(tokens, start),
                TokenType.Symbol => ParseVariable(tokens, start),
                TokenType.IntLiteral => ParseIntLiteral(tokens, start),
                TokenType.BooleanLiteral => ParseBooleanLiteral(tokens, start),
                TokenType.StringLiteral => ParseStringLiteral(tokens, start),
                TokenType.EndOfInput => new ParseFailure("Unexpected end of input.", start),
                _ => new ParseFailure($"Unexpected token '{tokens[start].Text}'.", start)
            };
        }

        private static ParseResult ParseVariable(ImmutableArray<Token> tokens, int start)
        {
            return new ParseSuccess(new Variable(tokens[start].Text), start + 1);
        }

        private static ParseResult ParseIntLiteral(ImmutableArray<Token> tokens, int start)
        {
            return new ParseSuccess(new IntConstant(int.Parse(tokens[start].Text)), start + 1);
        }

        private static ParseResult ParseBooleanLiteral(ImmutableArray<Token> tokens, int start)
        {
            return tokens[start].Text switch
            {
                "true" => new ParseSuccess(new BooleanConstant(true), start + 1),
                "false" => new ParseSuccess(new BooleanConstant(false), start + 1),
                _ => throw new InvalidOperationException($"Unexpected boolean literal '{tokens[start].Text}'.")
            };
        }

        private static ParseResult ParseStringLiteral(ImmutableArray<Token> tokens, int start)
        {
            string tokenText = tokens[start].Text;
            return new ParseSuccess(new StringConstant(tokenText.Substring(1, tokenText.Length - 2)), start + 1);
        }

        private static ParseResult ParseSExpression(ImmutableArray<Token> tokens, int start)
        {
            // We already know we have a ( so we can skip right to the next token.
            var index = start + 1;
            if (index >= tokens.Length)
            {
                return new ParseFailure("Unexpected end of input.", index);
            }

            var token = tokens[index];
            return token.Type switch
            {
                TokenType.Symbol when IsBinaryOperation(token.Text) => ParseBinaryOperation(token.Text, tokens, index + 1),
                TokenType.Symbol when IsUnaryOperation(token.Text) => ParseUnaryOperation(token.Text, tokens, index + 1),
                TokenType.Symbol => new ParseFailure($"Unknown operation '{token.Text}'.", index),
                _ => new ParseFailure($"Expected an operation name; found '{token.Text}' instead.", index)
            };
        }

        private static ParseResult ParseUnaryOperation(string unopName, ImmutableArray<Token> tokens, int start)
        {
            // "start" puts us past the operation, so we can start parsing the arguments immediately.
            var childResult = ParseExpression(tokens, start);
            Expression? childExpression;
            switch (childResult)
            {
                case ParseSuccess success:
                    childExpression = success.Expression;
                    break;

                case ParseFailure:
                    return new ParseFailure("Expression expected.", start);

                default:
                    throw new InvalidOperationException($"Unhandled parse result type: {childResult.GetType()}.");
            }

            start = childResult.NextTokenIndex;

            if (tokens[start].Type != TokenType.RightParen)
            {
                return new ParseFailure($"Operation '{unopName}' takes one operand; ')' expected.", start);
            }

            var nextTokenIndex = start + 1;
            Expression expression = unopName switch
            {
                "not" => new UnaryExpression(UnaryOperation.Not, childExpression),
                _ => throw new InvalidOperationException($"Unhandled unary operation '{unopName}'.")
            };

            return new ParseSuccess(expression, nextTokenIndex);
        }

        private static ParseResult ParseBinaryOperation(string binopName, ImmutableArray<Token> tokens, int start)
        {
            // "start" puts us past the operation, so we can start parsing the arguments immediately.
            var leftResult = ParseExpression(tokens, start);
            Expression? leftExpression;
            switch (leftResult)
            {
                case ParseSuccess success:
                    leftExpression = success.Expression;
                    break;

                case ParseFailure:
                    return new ParseFailure("Expression expected.", start);

                default:
                    throw new InvalidOperationException($"Unhandled parse result type: {leftResult.GetType()}.");
            }

            start = leftResult.NextTokenIndex;

            var rightResult = ParseExpression(tokens, start);
            Expression? rightExpression;
            switch (rightResult)
            {
                case ParseSuccess success:
                    rightExpression = success.Expression;
                    break;

                case ParseFailure:
                    return new ParseFailure("Expression expected.", start);

                default:
                    throw new InvalidOperationException($"Unhandled parse result type: {rightResult.GetType()}.");
            }

            start = leftResult.NextTokenIndex;

            if (tokens[start].Type != TokenType.RightParen)
            {
                return new ParseFailure($"Operation '{binopName}' takes two operands; ')' expected.", start);
            }

            var nextTokenIndex = start + 1;
            BinaryOperation operation = binopName switch
            {
                "and" => BinaryOperation.And,
                "or" => BinaryOperation.Or,
                "eq" => BinaryOperation.Equal,
                "neq" => BinaryOperation.NotEqual,
                "gt" => BinaryOperation.GreaterThan,
                "lt" => BinaryOperation.LessThan,
                "gte" => BinaryOperation.GreaterThanOrEqual,
                "lte" => BinaryOperation.LessThanOrEqual,
                _ => throw new InvalidOperationException($"Unhandled binary operation '{binopName}'.")
            };

            var expression = new BinaryExpression(operation, leftExpression, rightExpression);

            return new ParseSuccess(expression, nextTokenIndex);
        }

        private static bool IsBinaryOperation(string text)
        {
            return text switch
            {
                "and" or "or" => true,
                "eq" or "neq" => true,
                "gt" or "lt" or "gte" or "lte" => true,
                _ => false
            };
        }

        private static bool IsUnaryOperation(string text)
        {
            return text switch
            {
                "not" => true,
                _ => false
            };
        }

        private static ParseResult ParseConstant(ImmutableArray<Token> tokens, int start)
        {
            throw new NotImplementedException();
        }
    }

    public abstract record InterpretationResult;
    public record InterpretationSuccess(object Value) : InterpretationResult;
    public record InterpretationFailure(string Message) : InterpretationResult;

    public class ExpressionInterpreter
    {
        private readonly Func<string, object?> getVariableValue;

        public ExpressionInterpreter(Func<string, object?> getVariableValue)
        {
            this.getVariableValue = getVariableValue;
        }

        public InterpretationResult Interpret(Expression expression)
        {
            return expression switch
            {
                Variable variableExpression => Interpret(variableExpression),
                IntConstant intExpression => new InterpretationSuccess(intExpression.Value),
                StringConstant stringExpression => new InterpretationSuccess(stringExpression.Value),
                BooleanConstant booleanExpression => new InterpretationSuccess(booleanExpression.Value),
                BinaryExpression binaryExpression => Interpret(binaryExpression),
                UnaryExpression unaryOperation => Interpret(unaryOperation),
                _ => throw new InvalidOperationException($"Unknown expression type {expression.GetType()}.")
            };
        }

        private InterpretationResult Interpret(Variable variableExpression)
        {
            if (getVariableValue(variableExpression.Name) is object value)
            {
                return new InterpretationSuccess(value);
            }
            else
            {
                return new InterpretationFailure($"Variable '{variableExpression.Name}' is not defined.");
            }
        }

        private InterpretationResult Interpret(BinaryExpression binaryExpression)
        {
            var leftResult = Interpret(binaryExpression.Left);
            if (leftResult is not InterpretationSuccess leftSuccess)
            {
                return leftResult;
            }

            var rightResult = Interpret(binaryExpression.Right);
            if (rightResult is not InterpretationSuccess rightSuccess)
            {
                return rightResult;
            }

            return (binaryExpression.Operation, leftSuccess.Value, rightSuccess.Value) switch
            {
                (BinaryOperation.And, bool left, bool right) => new InterpretationSuccess(left && right),
                (BinaryOperation.And, object left, object right) => new InterpretationFailure($"Attempting to 'and' a {left.GetType()} and a {right.GetType()}."),

                (BinaryOperation.Or, bool left, bool right) => new InterpretationSuccess(left || right),
                (BinaryOperation.Or, object left, object right) => new InterpretationFailure($"Attempting to 'or' a {left.GetType()} and a {right.GetType()}."),

                (BinaryOperation.Equal, bool left, bool right) => new InterpretationSuccess(left == right),
                (BinaryOperation.Equal, int left, int right) => new InterpretationSuccess(left == right),
                (BinaryOperation.Equal, string left, string right) => new InterpretationSuccess(left == right),
                (BinaryOperation.Equal, object left, object right) => new InterpretationFailure($"Attempting to compare a {left.GetType()} and a {right.GetType()}."),

                (BinaryOperation.NotEqual, bool left, bool right) => new InterpretationSuccess(left != right),
                (BinaryOperation.NotEqual, int left, int right) => new InterpretationSuccess(left != right),
                (BinaryOperation.NotEqual, string left, string right) => new InterpretationSuccess(left != right),
                (BinaryOperation.NotEqual, object left, object right) => new InterpretationFailure($"Attempting to compare a {left.GetType()} and a {right.GetType()}."),

                (BinaryOperation.GreaterThan, int left, int right) => new InterpretationSuccess(left > right),
                (BinaryOperation.GreaterThan, object left, object right) => new InterpretationFailure($"Attempting to compare a {left.GetType()} and a {right.GetType()}."),

                (BinaryOperation.GreaterThanOrEqual, int left, int right) => new InterpretationSuccess(left >= right),
                (BinaryOperation.GreaterThanOrEqual, object left, object right) => new InterpretationFailure($"Attempting to compare a {left.GetType()} and a {right.GetType()}."),

                (BinaryOperation.LessThan, int left, int right) => new InterpretationSuccess(left < right),
                (BinaryOperation.LessThan, object left, object right) => new InterpretationFailure($"Attempting to compare a {left.GetType()} and a {right.GetType()}."),

                (BinaryOperation.LessThanOrEqual, int left, int right) => new InterpretationSuccess(left <= right),
                (BinaryOperation.LessThanOrEqual, object left, object right) => new InterpretationFailure($"Attempting to compare a {left.GetType()} and a {right.GetType()}."),

                (BinaryOperation unknownOperation, _, _) => throw new InvalidOperationException($"Unknown binary operation '{unknownOperation}'.")
            };
        }

        private InterpretationResult Interpret(UnaryExpression unaryExpression)
        {
            var childResult = Interpret(unaryExpression.Child);
            if (childResult is not InterpretationSuccess childSuccess)
            {
                return childResult;
            }

            return (unaryExpression.Operation, childSuccess.Value) switch
            {
                (UnaryOperation.Not, bool child) => new InterpretationSuccess(!child),
                (UnaryOperation.Not, object child) => new InterpretationFailure($"Attempting to negate type {child.GetType()}"),

                (UnaryOperation unknownOperation, _) => throw new InvalidOperationException($"Unknown unary operation '{unknownOperation}'.")
            };
        }
    }

}
