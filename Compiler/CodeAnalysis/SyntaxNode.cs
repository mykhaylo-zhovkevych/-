using System.Collections.Generic;
using System.Linq;
using System;

namespace Compiler.CodeAnalysis
{

    /// <summary>
    /// Represents a node in the syntax tree. 
    /// All expressions and tokens derive from this base class.
    /// </summary>
    abstract class SyntaxNode
    {
        public abstract SyntaxKind Kind { get; }
        public abstract IEnumerable<SyntaxNode> GetChildren();
    }
}