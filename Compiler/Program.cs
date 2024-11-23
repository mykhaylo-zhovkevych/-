using System;
using System.Collections.Generic;
using System.Linq;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            bool showTree = false;

            while (true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    return;

                if (line == "#showTree")
                {   
                    showTree = !showTree;
                    Console.WriteLine(showTree ? "Showing parse trees. " : "Not showing parse trees");
                    continue;
                }
                else if (line == "#cls")
                {
                    Console.Clear();
                    continue;
                }

                var syntaxTree = SyntaxTree.Parse(line);

                if (showTree)
                {
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    PrettyPrint(syntaxTree.Root);
                    Console.ForegroundColor = color;
                } 

                // Any() returns true, wehn there any errors
                if (!syntaxTree.Diagnostics.Any())
                {
                    var e = new Evaluator(syntaxTree.Root);
                    var result = e.Evaluate();
                    Console.WriteLine(result);
                }
                // (If there are syntax errors)
                else
                {
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkRed;

                    foreach (var diagnostic in syntaxTree.Diagnostics)
                        Console.WriteLine(diagnostic);

                    Console.ForegroundColor = color;
                }
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
            BinaryExpression,
            ParenthesizedExpression
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

        sealed class ParenthesizedExpressionSyntax : ExpressionSyntax
        {

            public ParenthesizedExpressionSyntax (SyntaxToken openParenthesisToken, ExpressionSyntax expression, SyntaxToken closeParenthesisToken)
            {
                OpenParenthesisToken = openParenthesisToken;
                Expression = expression;
                CloseParenthesisToken = closeParenthesisToken;
            }


            public SyntaxToken OpenParenthesisToken { get; }
            public ExpressionSyntax Expression { get; }
            public SyntaxToken CloseParenthesisToken { get; }

            public override SyntaxKind Kind => SyntaxKind.ParenthesizedExpression;

            public override IEnumerable<SyntaxNode> GetChildren()
            {
                yield return OpenParenthesisToken;
                yield return Expression;    
                yield return CloseParenthesisToken; 
            }
        }

        /// <summary>
        /// Represents the result of parsing a text input into a structured format (a syntax tree).
        /// </summary>
        sealed class SyntaxTree
        {
            public SyntaxTree (IEnumerable<string> diagnostics, ExpressionSyntax root, SyntaxToken endOfFileToken)
            {
                Diagnostics = diagnostics.ToArray();
                Root = root;
                EndOfFileToken = endOfFileToken;
            }

            public IReadOnlyList<string> Diagnostics { get; }
            public ExpressionSyntax Root { get; }
            public SyntaxToken EndOfFileToken { get; }

            public static SyntaxTree Parse(string text)
            {
                var parser = new Parser(text);
                return parser.Parse();
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


        /// <summary>
        /// The Parser class processes a sequence of tokens and constructs an abstract syntax tree (AST).
        /// This AST represents the hierarchical structure of a mathematical expression.
        /// </summary>
        class Parser
        {
            private readonly SyntaxToken[] _tokens;
            private List<string> _diagnostics = new List<string>();
            private int _position;
            private SyntaxToken endOFfileToken;

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
                // This line adds any error messages collected by the Lexer into the parser _diagnostics list.
                _diagnostics.AddRange(lexer.Diagnostics);
                _position = 0; // Ensure position starts at 0
            }

            // This method allows access to the private _diagnostics list but in a read-only way.
            public IEnumerable<string> Diagnostics => _diagnostics;

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

            private SyntaxToken Match(SyntaxKind kind)
            {
                if (Current.Kind == kind)
                    return NextToken();
                _diagnostics.Add($"ERROR: Unexpected token <{Current.Kind}>, expected <{kind}>");
                return new SyntaxToken(SyntaxKind.BadToken, Current.Position, null, null);
            }


            // help method 
            private ExpressionSyntax ParseExpression()
            {
                return ParseTerm();
            }

            public SyntaxTree Parse()
            {
                // Creates the main structure for the input, such as a math equation or command.
                var expression = ParseTerm();
                // Ensures the input ends with the correct end-of-file token. If it doesn’t, it may log an error.
                var endOFFileToken = Match(SyntaxKind.EndOfFileToken);
                // Combines the diagnostics, parsed expression, and end-of-file token into a SyntaxTree.
                return new SyntaxTree(_diagnostics, expression, endOFfileToken);

            }

            /// <summary>
            /// Parses a term in an arithmetic expression. A term can consist of factors 
            /// connected by addition (+) or subtraction (-) operators.
            /// </summary>
            private ExpressionSyntax ParseTerm()
            {
                // Start by parsing the first factor
                var left = ParseFactor();

                // Continue parsing for addition or subtraction operators
                while (Current.Kind == SyntaxKind.PlusToken ||
                       Current.Kind == SyntaxKind.MinusToken)
                {
                    // Get the operator (+ or -)
                    var operatorToken = NextToken();

                    // Parse the next factor after the operator
                    var right = ParseFactor();

                    // Create a binary expression with the operator applied to the left and right factors
                    left = new BinaryExpressionSyntax(left, operatorToken, right);
                }

                // Return the final parsed expression (either a single factor or a complex binary expression)
                return left;
            }



            /// <summary>
            /// Parses a factor in an arithmetic expression. A factor can be a primary expression 
            /// connected by multiplication (*) or division (/) operators.
            /// </summary>
            private ExpressionSyntax ParseFactor()
            {
                // Start by parsing the primary expression (like numbers or parenthesized expressions)
                var left = ParsePrimaryExpression();

                // Continue parsing for multiplication or division operators
                while (Current.Kind == SyntaxKind.StarToken ||
                       Current.Kind == SyntaxKind.SlashToken)
                {
                    // Get the operator (* or /)
                    var operatorToken = NextToken();

                    // Parse the next primary expression after the operator
                    var right = ParsePrimaryExpression();

                    // Create a binary expression with the operator applied to the left and right primary expressions
                    left = new BinaryExpressionSyntax(left, operatorToken, right);
                }

                // Return the final parsed expression (either a single primary expression or a complex binary expression)
                return left;
            }


            /// <summary>
            /// Parses the primary building blocks of an expression, which could be a number or a 
            /// parenthesized sub-expression.
            /// </summary>
            private ExpressionSyntax ParsePrimaryExpression()
            {
                // Check if the current token is an opening parenthesis, indicating a sub-expression
                if (Current.Kind == SyntaxKind.OpenParenthesisToken)
                {
                    // Consume the opening parenthesis token
                    var left = NextToken();

                    // Parse the expression inside the parentheses
                    var expression = ParseExpression();

                    // Match the closing parenthesis token
                    var right = Match(SyntaxKind.CloseParenthesisToken);

                    // Return a parenthesized expression
                    return new ParenthesizedExpressionSyntax(left, expression, right);
                }

                // If not a parenthesis, expect a number token (for numeric literals)
                var numberToken = Match(SyntaxKind.NumberToken);

                // Return a number expression
                return new NumberExpressionSyntax(numberToken);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        class Evaluator
        {
            private readonly ExpressionSyntax _root;

            public Evaluator(ExpressionSyntax root)
            {
                this._root = root;
            }

            public int Evaluate()
            {
                return EvaluateExpression(_root);
            }

            private int EvaluateExpression(ExpressionSyntax node)
            {
                // NumberExpression

                if (node is NumberExpressionSyntax n)
                    return (int) n.NumberToken.Value;

                // BinaryExpression

                if (node is BinaryExpressionSyntax b)
                {
                    var left = EvaluateExpression(b.Left);
                    var right = EvaluateExpression(b.Right);

                    if (b.OperatorToken.Kind == SyntaxKind.PlusToken)
                        return left + right;
                    else if (b.OperatorToken.Kind == SyntaxKind.MinusToken)
                        return left - right;
                    else if (b.OperatorToken.Kind == SyntaxKind.StarToken)
                        return left * right;
                    else if (b.OperatorToken.Kind == SyntaxKind.SlashToken)
                        return left / right;
                    else
                        throw new Exception($"Unexpected binary operator {b.OperatorToken.Kind}");
                }
                if (node is ParenthesizedExpressionSyntax p)
                    return EvaluateExpression(p.Expression);

                throw new Exception($"Unexpected node {node.Kind}");
            }
        }
    }
}