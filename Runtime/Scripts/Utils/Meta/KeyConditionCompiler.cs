using System;
using System.Collections.Generic;
using Unity.Collections;

namespace Dalichrome.RandomGenerator.Utils
{
    /// <summary>
    /// Compiles key-condition expressions into reusable AST nodes.
    /// Grammar: identifiers, !, &, |, ().
    /// Example: "humidity&temperature", "!(water|height)".
    /// </summary>
    public static class KeyConditionCompiler
    {
        private sealed class KeyNode : KeyConditionNode
        {
            public readonly string Key;
            public KeyNode(string key) => Key = key;
            internal override bool Evaluate(Func<string, bool> keySatisfied)
                => keySatisfied(Key);
        }

        private sealed class NotNode : KeyConditionNode
        {
            public readonly KeyConditionNode Child;
            public NotNode(KeyConditionNode child) => Child = child;
            internal override bool Evaluate(Func<string, bool> keySatisfied)
                => !Child.Evaluate(keySatisfied);
        }

        private sealed class AndNode : KeyConditionNode
        {
            public readonly KeyConditionNode Left;
            public readonly KeyConditionNode Right;
            public AndNode(KeyConditionNode left, KeyConditionNode right) { Left = left; Right = right; }
            internal override bool Evaluate(Func<string, bool> keySatisfied)
                => Left.Evaluate(keySatisfied) && Right.Evaluate(keySatisfied);
        }

        private sealed class OrNode : KeyConditionNode
        {
            public readonly KeyConditionNode Left;
            public readonly KeyConditionNode Right;
            public OrNode(KeyConditionNode left, KeyConditionNode right) { Left = left; Right = right; }
            internal override bool Evaluate(Func<string, bool> keySatisfied)
                => Left.Evaluate(keySatisfied) || Right.Evaluate(keySatisfied);
        }

        internal static KeyConditionNode Compile(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr))
                return null;

            var parser = new Parser(expr.AsSpan());
            KeyConditionNode root = parser.ParseOr();
            parser.SkipWhitespace();
            if (!parser.End)
                throw new FormatException($"Unexpected trailing characters in key expression: '{expr}'");
            return root;
        }

        /// <summary>
        /// Returns all key identifiers referenced by a key-condition expression.
        /// Example: "!(water|height)&humidity" => ["water","height","humidity"]
        /// </summary>
        public static List<FixedString64Bytes> ExtractKeys(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr))
                return new List<FixedString64Bytes>(0);

            KeyConditionNode root = Compile(expr);
            if (root == null)
                return new List<FixedString64Bytes>(0);

            var set = new HashSet<FixedString64Bytes>();
            CollectKeys(root, set);

            return new List<FixedString64Bytes>(set);
        }

        private static void CollectKeys(KeyConditionNode node, HashSet<FixedString64Bytes> outKeys)
        {
            switch (node)
            {
                case null:
                    return;

                case KeyNode k:
                    outKeys.Add(new FixedString64Bytes(k.Key));
                    return;

                case NotNode n:
                    CollectKeys(n.Child, outKeys);
                    return;

                case AndNode a:
                    CollectKeys(a.Left, outKeys);
                    CollectKeys(a.Right, outKeys);
                    return;

                case OrNode o:
                    CollectKeys(o.Left, outKeys);
                    CollectKeys(o.Right, outKeys);
                    return;

                default:
                    // If you add node types later, youÅfll want to handle them here.
                    throw new Exception($"Unknown KeyConditionNode type: {node.GetType().Name}");
            }
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

            public KeyConditionNode ParseOr()
            {
                KeyConditionNode left = ParseAnd();
                while (true)
                {
                    SkipWhitespace();
                    if (Match('|'))
                    {
                        KeyConditionNode right = ParseAnd();
                        left = new OrNode(left, right);
                    }
                    else break;
                }
                return left;
            }

            private KeyConditionNode ParseAnd()
            {
                KeyConditionNode left = ParseUnary();
                while (true)
                {
                    SkipWhitespace();
                    if (Match('&'))
                    {
                        KeyConditionNode right = ParseUnary();
                        left = new AndNode(left, right);
                    }
                    else break;
                }
                return left;
            }

            private KeyConditionNode ParseUnary()
            {
                SkipWhitespace();
                if (Match('!'))
                {
                    KeyConditionNode child = ParseUnary();
                    return new NotNode(child);
                }
                return ParsePrimary();
            }

            private KeyConditionNode ParsePrimary()
            {
                SkipWhitespace();
                if (Match('('))
                {
                    KeyConditionNode inner = ParseOr();
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
