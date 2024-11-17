using System;
using System.Collections.Generic;

// 52:00

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    return;

                var parser = new Parser(line);
                var expression = parser.Parse();

                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkGray;
                PrettyPrint(expression);
                Console.ForegroundColor = color;


            }
        }

        static void PrettyPrint(SyntaxNode node, string indent = "")
        {
            // Press the current node type
            Console.Write(indent);
            Console.Write(node.Kind);

            // If it is a token, display its value
            if (node is SyntaxTokenNode tokenNode)
            {
                var token = tokenNode.GetToken();
                if (token.Value != null)
                {
                    Console.Write($" {token.Value}");
                }
            }

            Console.WriteLine();

            // Indentation for children
            indent += "    ";

            // Call PrettyPrint for each child node
            foreach (var child in node.GetChildren())
            {
                PrettyPrint(child, indent);
            }
        }


        /// <summary>
        /// Defines the various types of syntax tokens and expressions.
        /// </summary>
        enum SyntaxKind
        {
            NumberToken,
            PlusToken,
            MinusToken,
            StarToken,
            SlashToken,
            OpenParenthesisToken,
            CloseParenthesisToken,
            WhitespaceToken,
            EndOfFileToken,
            BadToken,
            NumberExpression,
            BinaryExpression
        }

        /// <summary>
        /// Represents a single token in the input.
        /// </summary>
        class SyntaxToken
        {
            /// <summary>
            /// Constructs a new SyntaxToken.
            /// </summary>
            /// <param name="kind">The kind of token (e.g., NumberToken, PlusToken, etc.).</param>
            /// <param name="position">The position of the token in the input string.</param>
            /// <param name="text">The text representation of the token.</param>
            /// <param name="value">The value of the token (e.g., the number for NumberToken).</param>
            public SyntaxToken(SyntaxKind kind, int position, string text, object value)
            {
                Kind = kind;
                Position = position;
                Text = text;
                Value = value;
            }

            /// <summary>
            /// The kind of token.
            /// </summary>
            public SyntaxKind Kind { get; }

            /// <summary>
            /// The position of the token in the input string.
            /// </summary>
            public int Position { get; }

            /// <summary>
            /// The text representation of the token.
            /// </summary>
            public string Text { get; }

            /// <summary>
            /// The value of the token (e.g., the number for NumberToken).
            /// </summary>
            public object Value { get; }
        }

        abstract class SyntaxNode
        {
            public abstract SyntaxKind Kind { get; }

            public abstract IEnumerable<SyntaxNode> GetChildren();
        }

        abstract class ExpressionSyntax : SyntaxNode { }

        //________________________________________---------------------------------------

        class SyntaxTokenNode : SyntaxNode
        {
            private SyntaxToken _token;

            public SyntaxTokenNode(SyntaxToken token)
            {
                _token = token;
            }

            public override SyntaxKind Kind => _token.Kind;

            public SyntaxToken GetToken() => _token;

            public override IEnumerable<SyntaxNode> GetChildren()
            {
                return Enumerable.Empty<SyntaxNode>();
            }
        }

        //________________________________________---------------------------------------

        sealed class NumberExpressionSyntax : ExpressionSyntax
        {
            public NumberExpressionSyntax(SyntaxToken numberToken)
            {
                NumberToken = numberToken;
            }

            public SyntaxToken NumberToken { get; }

            public override SyntaxKind Kind => SyntaxKind.NumberExpression;

            public override IEnumerable<SyntaxNode> GetChildren()
            {
                yield return new SyntaxTokenNode(NumberToken);
            }
        }

        sealed class BinaryExpressionSyntax : ExpressionSyntax
        {
            public BinaryExpressionSyntax(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right)
            {
                Left = left;
                OperatorToken = operatorToken;
                Right = right;
            }

            public ExpressionSyntax Left { get; }
            public SyntaxToken OperatorToken { get; }
            public ExpressionSyntax Right { get; }

            public override SyntaxKind Kind => SyntaxKind.BinaryExpression;

            public override IEnumerable<SyntaxNode> GetChildren()
            {
                yield return Left;
                yield return new SyntaxTokenNode(OperatorToken);
                yield return Right;
            }
        }

        class Lexer
        {
            private readonly string _text;
            private int _position;

            public Lexer(string text)
            {
                _text = text;
            }

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
                    return new SyntaxToken(SyntaxKind.NumberToken, start, text, int.Parse(text));
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
                        return new SyntaxToken(SyntaxKind.BadToken, _position++, _text.Substring(_position - 1, 1), null);
                }
            }
        }

        class Parser
        {
            private readonly SyntaxToken[] _tokens;
            private int _position;

            public Parser(IEnumerable<SyntaxToken> tokens)
            {
                _tokens = tokens.ToArray();
            }

            public Parser(string line)
            {
                var lexer = new Lexer(line);
                var tokens = new List<SyntaxToken>();
                SyntaxToken token;

                do
                {
                    token = lexer.NextToken();
                    if (token.Kind != SyntaxKind.WhitespaceToken && token.Kind != SyntaxKind.BadToken)
                    {
                        tokens.Add(token);
                    }
                } while (token.Kind != SyntaxKind.EndOfFileToken);

                _tokens = tokens.ToArray();
                _position = 0; // Ensure position starts at 0
            }

            private SyntaxToken Peek(int offset)
            {
                var index = _position + offset;
                return index >= _tokens.Length ? _tokens[^1] : _tokens[index];
            }

            private SyntaxToken Current => Peek(0);

            public string Line { get; }

            private SyntaxToken NextToken()
            {
                var current = Current;
                _position++;
                return current;
            }

            public ExpressionSyntax Parse()
            {
                return ParseExpression();
            }

            private ExpressionSyntax ParseExpression()
            {
                var left = ParsePrimaryExpression();

                while (Current.Kind == SyntaxKind.PlusToken || Current.Kind == SyntaxKind.MinusToken)
                {
                    var operatorToken = NextToken();
                    var right = ParsePrimaryExpression();
                    left = new BinaryExpressionSyntax(left, operatorToken, right);
                }

                return left;
            }

            private ExpressionSyntax ParsePrimaryExpression()
            {
                var numberToken = Match(SyntaxKind.NumberToken);
                return new NumberExpressionSyntax(numberToken);
            }

            private SyntaxToken Match(SyntaxKind kind)
            {
                if (Current.Kind == kind)
                    return NextToken();

                return new SyntaxToken(SyntaxKind.BadToken, Current.Position, null, null);
            }
        }
    }
}