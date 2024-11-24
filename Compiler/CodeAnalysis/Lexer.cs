using System.Collections.Generic;
using System.Linq;
using System;

namespace Compiler.CodeAnalysis
{

    /// <summary>
    /// The Lexer class is responsible for breaking an input string into tokens.
    /// It iterates through the input character by character and identifies tokens such as numbers,
    /// operators, parentheses, and whitespace. This is the first step in parsing a mathematical expression.
    /// </summary>
    class Lexer
    {
        private readonly string _text;
        private int _position;
        private List<string> _diagnostics = new List<string>();

        public Lexer(string text)
        {
            _text = text;
        }

        // This method allows access to the private _diagnostics list but in a read-only way.
        public IEnumerable<string> Diagnostics => _diagnostics;


        private char Current => _position >= _text.Length ? '\0' : _text[_position];

        private void Next() => _position++;

        public SyntaxToken NextToken()
        {
            if (_position >= _text.Length)
                return new SyntaxToken(SyntaxKind.EndOfFileToken, _position, "\0", null);

            if (char.IsDigit(Current))
            {
                var start = _position;
                while (char.IsDigit(Current))
                    Next();

                var text = _text.Substring(start, _position - start);

                if (!int.TryParse(text, out var value))
                    _diagnostics.Add($"The number {_text} is not valid Int32.");


                return new SyntaxToken(SyntaxKind.NumberToken, start, text, value);
            }

            if (char.IsWhiteSpace(Current))
            {
                var start = _position;
                while (char.IsWhiteSpace(Current))
                    Next();

                var text = _text.Substring(start, _position - start);
                return new SyntaxToken(SyntaxKind.WhitespaceToken, start, text, null);
            }

            switch (Current)
            {
                case '+': return new SyntaxToken(SyntaxKind.PlusToken, _position++, "+", null);
                case '-': return new SyntaxToken(SyntaxKind.MinusToken, _position++, "-", null);
                case '*': return new SyntaxToken(SyntaxKind.StarToken, _position++, "*", null);
                case '/': return new SyntaxToken(SyntaxKind.SlashToken, _position++, "/", null);
                case '(': return new SyntaxToken(SyntaxKind.OpenParenthesisToken, _position++, "(", null);
                case ')': return new SyntaxToken(SyntaxKind.CloseParenthesisToken, _position++, ")", null);
                default:
                    _diagnostics.Add($"ERROR: bad character input: '{Current}'");
                    return new SyntaxToken(SyntaxKind.BadToken, _position++, _text.Substring(_position - 1, 1), null);
            }
        }
    }
}
