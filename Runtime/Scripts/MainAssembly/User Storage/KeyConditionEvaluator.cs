using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class KeyConditionEvaluator
{
    public static bool Evaluate(string expr, Func<string, bool> keySatisfied)
    {
        if (string.IsNullOrWhiteSpace(expr))
            return true;

        var parser = new Parser(expr.AsSpan(), keySatisfied);
        bool result = parser.ParseOr();
        parser.SkipWhitespace();

        if (!parser.End)
            throw new FormatException($"Unexpected trailing characters in key expression: '{expr}'");

        return result;
    }

    private ref struct Parser
    {
        private ReadOnlySpan<char> _text;
        private int _index;
        private readonly Func<string, bool> _keySatisfied;

        public Parser(ReadOnlySpan<char> text, Func<string, bool> keySatisfied)
        {
            _text = text;
            _index = 0;
            _keySatisfied = keySatisfied;
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

        public bool ParseOr()
        {
            bool left = ParseAnd();
            while (true)
            {
                SkipWhitespace();
                if (Match('|'))
                {
                    bool right = ParseAnd();
                    left = left || right;
                }
                else break;
            }
            return left;
        }

        private bool ParseAnd()
        {
            bool left = ParseUnary();
            while (true)
            {
                SkipWhitespace();
                if (Match('&'))
                {
                    bool right = ParseUnary();
                    left = left && right;
                }
                else break;
            }
            return left;
        }

        private bool ParseUnary()
        {
            SkipWhitespace();
            if (Match('!'))
            {
                bool inner = ParseUnary();
                return !inner;
            }
            return ParsePrimary();
        }

        private bool ParsePrimary()
        {
            SkipWhitespace();
            if (Match('('))
            {
                bool value = ParseOr();
                if (!Match(')'))
                    throw new FormatException("Missing closing ')' in key expression.");
                return value;
            }

            string id = ParseIdentifier();
            if (string.IsNullOrEmpty(id))
                throw new FormatException("Expected key identifier in key expression.");

            return _keySatisfied(id);
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