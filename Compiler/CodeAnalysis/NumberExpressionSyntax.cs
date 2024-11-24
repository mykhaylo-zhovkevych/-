using System.Collections.Generic;
using System.Linq;
using System;

namespace Compiler.CodeAnalysis
{

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
}