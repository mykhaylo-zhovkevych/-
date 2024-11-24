using System;
using System.Collections.Generic;
using System.Linq;
using Compiler.CodeAnalysis;

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
    }
}