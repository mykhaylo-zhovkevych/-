using System.Collections.Generic;
using System.Linq;
using System;

namespace Compiler.CodeAnalysis
{

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
}