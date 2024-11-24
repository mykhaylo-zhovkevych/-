using System.Collections.Generic;
using System.Linq;
using System;

namespace Compiler.CodeAnalysis
{

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
}