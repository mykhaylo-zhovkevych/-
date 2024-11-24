using System.Collections.Generic;
using System.Linq;
using System;

namespace Compiler.CodeAnalysis
{

    /// <summary>
    /// Base class for syntax nodes that represent expressions.
    /// Derived classes represent specific types of expressions, e.g., binary operations or literals.
    /// </summary>
    abstract class ExpressionSyntax : SyntaxNode { }
}