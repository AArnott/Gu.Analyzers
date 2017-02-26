﻿namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class AssignmentWalker : CSharpSyntaxWalker
    {
        private static readonly Pool<AssignmentWalker> Cache = new Pool<AssignmentWalker>(
            () => new AssignmentWalker(),
            x => x.assignments.Clear());

        private readonly List<AssignmentExpressionSyntax> assignments = new List<AssignmentExpressionSyntax>();

        private AssignmentWalker()
        {
        }

        /// <summary>
        /// Gets a list with all <see cref="AssignmentExpressionSyntax"/> in the scope.
        /// </summary>
        public IReadOnlyList<AssignmentExpressionSyntax> Assignments => this.assignments;

        public static Pool<AssignmentWalker>.Pooled Create(SyntaxNode node)
        {
            var pooled = Cache.GetOrCreate();
            pooled.Item.Visit(node);
            return pooled;
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            this.assignments.Add(node);
            base.VisitAssignmentExpression(node);
        }

        internal static bool Assigns(ISymbol symbol, SyntaxNode scope, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (symbol == null ||
                scope == null)
            {
                return false;
            }

            using (var pooledAssignments = Create(scope))
            {
                foreach (var assignment in pooledAssignments.Item.Assignments)
                {
                    var assignedSymbol = semanticModel.GetSymbolSafe(assignment.Left, cancellationToken);
                    if (SymbolComparer.Equals(symbol, assignedSymbol))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}