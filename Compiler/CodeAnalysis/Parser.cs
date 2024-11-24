using System.Collections.Generic;
using System.Linq;
using System;

namespace Compiler.CodeAnalysis
{

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
}