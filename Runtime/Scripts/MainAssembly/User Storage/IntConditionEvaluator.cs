using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class IntConditionEvaluator
{
    public static bool Evaluate(string expr, int value)
    {
        if (string.IsNullOrWhiteSpace(expr))
            return true; // no condition == always true

        var parser = new Parser(expr.AsSpan(), value);
        bool result = parser.ParseOr();
        parser.SkipWhitespace();

        if (!parser.End)
            throw new FormatException($"Unexpected trailing characters in int expression: '{expr}'");

        return result;
    }

    private ref struct Parser
    {
        private ReadOnlySpan<char> _text;
        private int _index;
        private readonly int _value;

        public Parser(ReadOnlySpan<char> text, int value)
        {
            _text = text;
            _index = 0;
            _value = value;
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
                    throw new FormatException("Missing closing ')' in int expression.");
                return value;
            }

            int start = ParseInt();
            SkipWhitespace();
            if (Match('-'))
            {
                int end = ParseInt();
                if (end < start)
                    throw new FormatException($"Invalid range {start}-{end} (end < start).");
                return _value >= start && _value <= end;
            }

            return _value == start;
        }

        private int ParseInt()
        {
            SkipWhitespace();
            if (End)
                throw new FormatException("Expected integer literal.");

            int sign = 1;
            if (Peek() == '+')
            {
                _index++;
            }
            else if (Peek() == '-')
            {
                sign = -1;
                _index++;
            }

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
