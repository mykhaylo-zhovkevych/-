using System;
using System.Collections.Generic;
using System.Linq;

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

        static void PrettyPrint(SyntaxNode node, string indent = "", bool isLast = true)
        {
            var marker = isLast ? "└──" : "├──";

            // Print the current node type
            Console.Write(indent);
            Console.Write(marker);
            Console.Write(node.Kind);

            // If it is a token, display its value
            if (node is SyntaxToken tokenNode)
            {
                if (tokenNode.Value != null)
                {
                    Console.Write($" {tokenNode.Value}");
                }
            }

            Console.WriteLine();

            indent += isLast ? "    " : "│   ";

            var children = node.GetChildren().ToList(); // Ensure enumeration happens only once
            var lastChild = children.LastOrDefault();

            foreach (var child in children)
            {
                PrettyPrint(child, indent, child == lastChild);
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
        /// Represents a single token produced by the lexer.
        /// A token has a type (e.g., number, operator), its position in the source text, its textual representation, and optionally its value.
        /// </summary>
        class SyntaxToken : SyntaxNode
        {
            public SyntaxToken(SyntaxKind kind, int position, string text, object value)
            {
                Kind = kind;
                Position = position;
                Text = text;
                Value = value;
            }

            public override SyntaxKind Kind { get; }
            public int Position { get; }
            public string Text { get; }
            public object Value { get; }

            public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();
        }

        /// <summary>
        /// Represents a node in the syntax tree. 
        /// All expressions and tokens derive from this base class.
        /// </summary>
        abstract class SyntaxNode
        {
            public abstract SyntaxKind Kind { get; }
            public abstract IEnumerable<SyntaxNode> GetChildren();
        }

        /// <summary>
        /// Base class for syntax nodes that represent expressions.
        /// Derived classes represent specific types of expressions, e.g., binary operations or literals.
        /// </summary>
        abstract class ExpressionSyntax : SyntaxNode { }

        /// <summary>
        /// Represents a numeric literal in the syntax tree, e.g., "123".
        /// </summary>
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
                yield return NumberToken;
            }
        }

        /// <summary>
        /// Represents a binary operation in the syntax tree, such as "1 + 2".
        /// This class includes the left operand, the operator token, and the right operand.
        /// </summary>
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
                yield return OperatorToken;
                yield return Right;
            }
        }

        /// <summary>
        /// The Lexer class is responsible for breaking an input string into tokens.
        /// It iterates through the input character by character and identifies tokens such as numbers,
        /// operators, parentheses, and whitespace. This is the first step in parsing a mathematical expression.
        /// </summary>
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

        /// <summary>
        /// The Parser class processes a sequence of tokens and constructs an abstract syntax tree (AST).
        /// This AST represents the hierarchical structure of a mathematical expression.
        /// </summary>
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