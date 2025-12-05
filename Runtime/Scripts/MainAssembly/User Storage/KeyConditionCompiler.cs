using System;

namespace Dalichrome.RandomGenerator.UserData
{
    /// <summary>
    /// Compiles key-condition expressions into reusable AST nodes.
    /// Grammar: identifiers, !, &, |, ().
    /// Example: "humidity&temperature", "!(water|height)".
    /// </summary>
    public static class KeyConditionCompiler
    {
        public abstract class Node
        {
            public abstract bool Evaluate(Func<string, bool> keySatisfied);
        }

        private sealed class KeyNode : Node
        {
            public readonly string Key;
            public KeyNode(string key) => Key = key;
            public override bool Evaluate(Func<string, bool> keySatisfied)
                => keySatisfied(Key);
        }

        private sealed class NotNode : Node
        {
            public readonly Node Child;
            public NotNode(Node child) => Child = child;
            public override bool Evaluate(Func<string, bool> keySatisfied)
                => !Child.Evaluate(keySatisfied);
        }

        private sealed class AndNode : Node
        {
            public readonly Node Left;
            public readonly Node Right;
            public AndNode(Node left, Node right) { Left = left; Right = right; }
            public override bool Evaluate(Func<string, bool> keySatisfied)
                => Left.Evaluate(keySatisfied) && Right.Evaluate(keySatisfied);
        }

        private sealed class OrNode : Node
        {
            public readonly Node Left;
            public readonly Node Right;
            public OrNode(Node left, Node right) { Left = left; Right = right; }
            public override bool Evaluate(Func<string, bool> keySatisfied)
                => Left.Evaluate(keySatisfied) || Right.Evaluate(keySatisfied);
        }

        public static Node Compile(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr))
                return null;

            var parser = new Parser(expr.AsSpan());
            Node root = parser.ParseOr();
            parser.SkipWhitespace();
            if (!parser.End)
                throw new FormatException($"Unexpected trailing characters in key expression: '{expr}'");
            return root;
        }

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

            public Node ParseOr()
            {
                Node left = ParseAnd();
                while (true)
                {
                    SkipWhitespace();
                    if (Match('|'))
                    {
                        Node right = ParseAnd();
                        left = new OrNode(left, right);
                    }
                    else break;
                }
                return left;
            }

            private Node ParseAnd()
            {
                Node left = ParseUnary();
                while (true)
                {
                    SkipWhitespace();
                    if (Match('&'))
                    {
                        Node right = ParseUnary();
                        left = new AndNode(left, right);
                    }
                    else break;
                }
                return left;
            }

            private Node ParseUnary()
            {
                SkipWhitespace();
                if (Match('!'))
                {
                    Node child = ParseUnary();
                    return new NotNode(child);
                }
                return ParsePrimary();
            }

            private Node ParsePrimary()
            {
                SkipWhitespace();
                if (Match('('))
                {
                    Node inner = ParseOr();
                    if (!Match(')'))
                        throw new FormatException("Missing closing ')' in key expression.");
                    return inner;
                }

                string id = ParseIdentifier();
                if (string.IsNullOrEmpty(id))
                    throw new FormatException("Expected key identifier in key expression.");
                return new KeyNode(id);
            }

            private string ParseIdentifier()
            {
                SkipWhitespace();
                int start = _index;
                while (!End)
                {
                    char c = _text[_index];
                    if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                        _index++;
                    else
                        break;
                }
                if (_index == start) return string.Empty;
                return _text.Slice(start, _index - start).ToString();
            }
        }
    }
}
