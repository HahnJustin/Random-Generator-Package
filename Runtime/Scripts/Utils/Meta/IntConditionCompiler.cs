using System;

namespace Dalichrome.RandomGenerator.Utils
{
    /// <summary>
    /// Compiles int-condition expressions into reusable AST nodes.
    /// Grammar:
    /// - Literals: "7" (equals)
    /// - Ranges: "1-3"
    /// - Prefix compares: ">5", ">=10", "<2", "<=9", "!=4"
    /// - Unary not: "!(...)" or "!7" (negates the following node)
    /// - And/Or: "&", "|"
    /// - Grouping: "()"
    /// Examples: "1-3|7", "!(0-2)", "(3|7)&!10", ">=5&!=9", "<=0|>10"
    /// </summary>
    public static class IntConditionCompiler
    {
        // „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ AST NODES „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ

        private sealed class LiteralNode : IntConditionNode
        {
            public readonly int Value;
            public LiteralNode(int value) => Value = value;
            internal override bool Evaluate(int v) => v == Value;
        }

        private sealed class RangeNode : IntConditionNode
        {
            public readonly int Min;
            public readonly int Max;
            public RangeNode(int min, int max) { Min = min; Max = max; }
            internal override bool Evaluate(int v) => v >= Min && v <= Max;
        }

        private enum CompareOp : byte
        {
            NotEqual,
            Less,
            LessOrEqual,
            Greater,
            GreaterOrEqual
        }

        private sealed class CompareNode : IntConditionNode
        {
            public readonly CompareOp Op;
            public readonly int Value;
            public CompareNode(CompareOp op, int value) { Op = op; Value = value; }

            internal override bool Evaluate(int v)
            {
                return Op switch
                {
                    CompareOp.NotEqual => v != Value,
                    CompareOp.Less => v < Value,
                    CompareOp.LessOrEqual => v <= Value,
                    CompareOp.Greater => v > Value,
                    CompareOp.GreaterOrEqual => v >= Value,
                    _ => false
                };
            }
        }

        private sealed class NotNode : IntConditionNode
        {
            public readonly IntConditionNode Child;
            public NotNode(IntConditionNode child) => Child = child;
            internal override bool Evaluate(int v) => !Child.Evaluate(v);
        }

        private sealed class AndNode : IntConditionNode
        {
            public readonly IntConditionNode Left;
            public readonly IntConditionNode Right;
            public AndNode(IntConditionNode left, IntConditionNode right) { Left = left; Right = right; }
            internal override bool Evaluate(int v) => Left.Evaluate(v) && Right.Evaluate(v);
        }

        private sealed class OrNode : IntConditionNode
        {
            public readonly IntConditionNode Left;
            public readonly IntConditionNode Right;
            public OrNode(IntConditionNode left, IntConditionNode right) { Left = left; Right = right; }
            internal override bool Evaluate(int v) => Left.Evaluate(v) || Right.Evaluate(v);
        }

        // „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ PUBLIC API „Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ„Ÿ

        internal static IntConditionNode Compile(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr))
                return null; // means "presence-only" or handled upstream

            var parser = new Parser(expr.AsSpan());
            IntConditionNode root = parser.ParseOr();
            parser.SkipWhitespace();
            if (!parser.End)
                throw new FormatException($"Unexpected trailing characters in int expression: '{expr}'");
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

            private char Peek() => End ? '\0' : _text[_index];

            private char PeekNext()
            {
                int j = _index + 1;
                return (j >= 0 && j < _text.Length) ? _text[j] : '\0';
            }

            private void Expect(char c, string message)
            {
                if (!Match(c)) throw new FormatException(message);
            }

            public IntConditionNode ParseOr()
            {
                IntConditionNode left = ParseAnd();
                while (true)
                {
                    SkipWhitespace();
                    if (Match('|'))
                    {
                        IntConditionNode right = ParseAnd();
                        left = new OrNode(left, right);
                    }
                    else break;
                }
                return left;
            }

            private IntConditionNode ParseAnd()
            {
                IntConditionNode left = ParseUnary();
                while (true)
                {
                    SkipWhitespace();
                    if (Match('&'))
                    {
                        IntConditionNode right = ParseUnary();
                        left = new AndNode(left, right);
                    }
                    else break;
                }
                return left;
            }

            private IntConditionNode ParseUnary()
            {
                SkipWhitespace();

                // Handle both unary NOT (!) and prefix "!="
                if (Match('!'))
                {
                    // If immediately followed by '=', this is the "!=" operator (prefix)
                    if (Match('='))
                    {
                        int rhs = ParseInt();
                        return new CompareNode(CompareOp.NotEqual, rhs);
                    }

                    IntConditionNode child = ParseUnary();
                    return new NotNode(child);
                }

                return ParsePrimary();
            }

            private IntConditionNode ParsePrimary()
            {
                SkipWhitespace();

                if (Match('('))
                {
                    IntConditionNode inner = ParseOr();
                    Expect(')', "Missing closing ')' in int expression.");
                    return inner;
                }

                // Prefix comparisons: <, <=, >, >=
                char p = Peek();
                if (p == '<' || p == '>')
                {
                    _index++; // consume < or >
                    bool orEqual = Match('=');

                    int rhs = ParseInt();

                    if (p == '<')
                        return new CompareNode(orEqual ? CompareOp.LessOrEqual : CompareOp.Less, rhs);
                    else
                        return new CompareNode(orEqual ? CompareOp.GreaterOrEqual : CompareOp.Greater, rhs);
                }

                // Literal or range (a-b)
                int start = ParseInt();
                SkipWhitespace();

                if (Match('-'))
                {
                    int end = ParseInt();
                    if (end < start)
                        throw new FormatException($"Invalid range {start}-{end} (end < start).");
                    return new RangeNode(start, end);
                }

                return new LiteralNode(start);
            }

            private int ParseInt()
            {
                SkipWhitespace();
                if (End)
                    throw new FormatException("Expected integer literal.");

                int sign = 1;
                if (Peek() == '+') _index++;
                else if (Peek() == '-') { sign = -1; _index++; }

                int result = 0;
                bool any = false;
                while (!End && char.IsDigit(Peek()))
                {
                    any = true;
                    result = result * 10 + (_text[_index] - '0');
                    _index++;
                }

                if (!any)
                    throw new FormatException("Invalid integer literal.");

                return sign * result;
            }
        }
    }
}
