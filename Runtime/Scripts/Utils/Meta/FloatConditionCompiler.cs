using System;
using System.Globalization;

namespace Dalichrome.RandomGenerator.Utils
{
    /// <summary>
    /// Compiles float-condition expressions into reusable AST nodes.
    /// Grammar:
    /// - Literals: "7", "0.25" (equals-with-epsilon)
    /// - Ranges: "1-3", "0.1-0.9"
    /// - Prefix compares: ">5", ">=10", "<2", "<=9", "!=4"
    /// - Unary not: "!(...)" or "!7"
    /// - And/Or: "&", "|"
    /// - Grouping: "()"
    /// Examples: "1-3|7", "!(0-2)", "(3|7)&!10", ">=5&!=9", "<=0|>10"
    /// </summary>
    public static class FloatConditionCompiler
    {
        // Equality tolerance (tweak if needed)
        private const float Epsilon = 1e-5f;

        // „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ AST NODES „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ

        private sealed class LiteralNode : FloatConditionNode
        {
            public readonly float Value;
            public LiteralNode(float value) => Value = value;

            internal override bool Evaluate(float v)
                => MathF.Abs(v - Value) <= Epsilon;
        }

        private sealed class RangeNode : FloatConditionNode
        {
            public readonly float Min;
            public readonly float Max;

            public RangeNode(float min, float max) { Min = min; Max = max; }

            internal override bool Evaluate(float v)
                => v >= Min && v <= Max;
        }

        private enum CompareOp : byte
        {
            NotEqual,
            Less,
            LessOrEqual,
            Greater,
            GreaterOrEqual
        }

        private sealed class CompareNode : FloatConditionNode
        {
            public readonly CompareOp Op;
            public readonly float Value;

            public CompareNode(CompareOp op, float value) { Op = op; Value = value; }

            internal override bool Evaluate(float v)
            {
                return Op switch
                {
                    CompareOp.NotEqual => MathF.Abs(v - Value) > Epsilon,
                    CompareOp.Less => v < Value,
                    CompareOp.LessOrEqual => v <= Value,
                    CompareOp.Greater => v > Value,
                    CompareOp.GreaterOrEqual => v >= Value,
                    _ => false
                };
            }
        }

        private sealed class NotNode : FloatConditionNode
        {
            public readonly FloatConditionNode Child;
            public NotNode(FloatConditionNode child) => Child = child;
            internal override bool Evaluate(float v) => !Child.Evaluate(v);
        }

        private sealed class AndNode : FloatConditionNode
        {
            public readonly FloatConditionNode Left;
            public readonly FloatConditionNode Right;
            public AndNode(FloatConditionNode left, FloatConditionNode right) { Left = left; Right = right; }
            internal override bool Evaluate(float v) => Left.Evaluate(v) && Right.Evaluate(v);
        }

        private sealed class OrNode : FloatConditionNode
        {
            public readonly FloatConditionNode Left;
            public readonly FloatConditionNode Right;
            public OrNode(FloatConditionNode left, FloatConditionNode right) { Left = left; Right = right; }
            internal override bool Evaluate(float v) => Left.Evaluate(v) || Right.Evaluate(v);
        }

        // „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ PUBLIC API „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ

        internal static FloatConditionNode Compile(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr))
                return null; // presence-only handled upstream

            var parser = new Parser(expr.AsSpan());
            FloatConditionNode root = parser.ParseOr();
            parser.SkipWhitespace();
            if (!parser.End)
                throw new FormatException($"Unexpected trailing characters in float expression: '{expr}'");
            return root;
        }

        // „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ PARSER „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ

        private ref struct Parser
        {
            private ReadOnlySpan<char> _text;
            private int _index;

            public Parser(ReadOnlySpan<char> text)
            {
                _text = text;
                _index = 0;
            }

            public bool End => _index >= _text.Length;

            public void SkipWhitespace()
            {
                while (!End && char.IsWhiteSpace(_text[_index])) _index++;
            }

            private bool Match(char c)
            {
                SkipWhitespace();
                if (!End && _text[_index] == c)
                {
                    _index++;
                    return true;
                }
                return false;
            }

            private char Peek()
            {
                SkipWhitespace();
                return End ? '\0' : _text[_index];
            }

            private void Expect(char c, string message)
            {
                if (!Match(c)) throw new FormatException(message);
            }

            public FloatConditionNode ParseOr()
            {
                FloatConditionNode left = ParseAnd();
                while (true)
                {
                    SkipWhitespace();
                    if (Match('|'))
                    {
                        FloatConditionNode right = ParseAnd();
                        left = new OrNode(left, right);
                    }
                    else break;
                }
                return left;
            }

            private FloatConditionNode ParseAnd()
            {
                FloatConditionNode left = ParseUnary();
                while (true)
                {
                    SkipWhitespace();
                    if (Match('&'))
                    {
                        FloatConditionNode right = ParseUnary();
                        left = new AndNode(left, right);
                    }
                    else break;
                }
                return left;
            }

            private FloatConditionNode ParseUnary()
            {
                SkipWhitespace();

                // Handle both unary NOT (!) and prefix "!="
                if (Match('!'))
                {
                    if (Match('=')) // "!="
                    {
                        float rhs = ParseFloat();
                        return new CompareNode(CompareOp.NotEqual, rhs);
                    }

                    FloatConditionNode child = ParseUnary();
                    return new NotNode(child);
                }

                return ParsePrimary();
            }

            private FloatConditionNode ParsePrimary()
            {
                SkipWhitespace();

                if (Match('('))
                {
                    FloatConditionNode inner = ParseOr();
                    Expect(')', "Missing closing ')' in float expression.");
                    return inner;
                }

                // Prefix comparisons: <, <=, >, >=
                char p = Peek();
                if (p == '<' || p == '>')
                {
                    _index++; // consume < or >
                    bool orEqual = Match('=');

                    float rhs = ParseFloat();

                    if (p == '<')
                        return new CompareNode(orEqual ? CompareOp.LessOrEqual : CompareOp.Less, rhs);
                    else
                        return new CompareNode(orEqual ? CompareOp.GreaterOrEqual : CompareOp.Greater, rhs);
                }

                // Literal or range (a-b)
                float start = ParseFloat();
                SkipWhitespace();

                if (Match('-'))
                {
                    float end = ParseFloat();
                    if (end < start)
                        throw new FormatException($"Invalid range {start.ToString(CultureInfo.InvariantCulture)}-{end.ToString(CultureInfo.InvariantCulture)} (end < start).");
                    return new RangeNode(start, end);
                }

                return new LiteralNode(start);
            }

            private float ParseFloat()
            {
                SkipWhitespace();
                if (End)
                    throw new FormatException("Expected float literal.");

                int start = _index;

                // optional sign
                if (!End && (Peek() == '+' || Peek() == '-'))
                    _index++;

                bool sawDigit = false;

                while (!End && char.IsDigit(_text[_index]))
                {
                    sawDigit = true;
                    _index++;
                }

                if (!End && _text[_index] == '.')
                {
                    _index++;
                    while (!End && char.IsDigit(_text[_index]))
                    {
                        sawDigit = true;
                        _index++;
                    }
                }

                if (!sawDigit)
                    throw new FormatException("Invalid float literal.");

                ReadOnlySpan<char> span = _text.Slice(start, _index - start);

                if (!float.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                    throw new FormatException($"Invalid float literal '{span.ToString()}'.");

                return f;
            }
        }
    }
}
