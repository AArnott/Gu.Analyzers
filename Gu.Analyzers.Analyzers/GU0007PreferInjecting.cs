﻿namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0007PreferInjecting : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0007";
        private const string Title = "Prefer injecting.";
        private const string MessageFormat = "Prefer injecting.";
        private const string Description = "Prefer injecting.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Title,
            messageFormat: MessageFormat,
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.DisabledByDefault,
            description: Description,
            helpLinkUri: HelpLink);

        internal enum Injectable
        {
            No,
            Safe,
            Unsafe
        }

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleObjectCreation, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(HandleMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        }

        internal static Injectable CanInject(ObjectCreationExpressionSyntax objectCreation, ConstructorDeclarationSyntax ctor)
        {
            if (objectCreation?.ArgumentList?.Arguments.Any() != true)
            {
                return Injectable.Safe;
            }

            var injectable = Injectable.Safe;
            foreach (var argument in objectCreation.ArgumentList.Arguments)
            {
                var temp = IsInjectable(argument.Expression, ctor);
                switch (temp)
                {
                    case Injectable.No:
                        return Injectable.No;
                    case Injectable.Safe:
                        break;
                    case Injectable.Unsafe:
                        injectable = Injectable.Unsafe;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return injectable;
        }

        internal static Injectable IsInjectable(ExpressionSyntax expression, ConstructorDeclarationSyntax ctor)
        {
            using (var pooled = SetPool<ExpressionSyntax>.Create())
            {
                return IsInjectable(expression, ctor, pooled.Item);
            }
        }

        internal static ITypeSymbol MemberType(ISymbol symbol) => (symbol as IPropertySymbol)?.Type;

        private static void HandleObjectCreation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            if (objectCreation.FirstAncestorOrSelf<ConstructorDeclarationSyntax>()?.Modifiers.Any(SyntaxKind.StaticKeyword) != false)
            {
                return;
            }

            var ctor = context.SemanticModel.GetSymbolSafe(objectCreation, context.CancellationToken) as IMethodSymbol;
            if (ctor == null || !IsInjectionType(ctor.ContainingType))
            {
                return;
            }

            if (CanInject(objectCreation, objectCreation.FirstAncestorOrSelf<ConstructorDeclarationSyntax>()) == Injectable.Safe)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, objectCreation.GetLocation()));
            }
        }

        private static void HandleMemberAccess(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            var ctor = memberAccess.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            if (ctor?.Modifiers.Any(SyntaxKind.StaticKeyword) != false)
            {
                return;
            }

            var symbol = context.SemanticModel.GetSymbolSafe(memberAccess, context.CancellationToken);
            var memberType = MemberType(symbol);
            if (memberType == null || !IsInjectionType(memberType))
            {
                return;
            }

            if (IsInjectable(memberAccess, ctor) != Injectable.No)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, memberAccess.GetLocation()));
            }
        }

        private static Injectable IsInjectable(ExpressionSyntax expression, ConstructorDeclarationSyntax ctor, HashSet<ExpressionSyntax> @checked)
        {
            if (!@checked.Add(expression))
            {
                return Injectable.No;
            }

            var identifierName = expression as IdentifierNameSyntax;
            if (identifierName?.Identifier != null)
            {
                var identifier = identifierName.Identifier.ValueText;
                if (identifier == null)
                {
                    return Injectable.No;
                }

                // ReSharper disable once UnusedVariable
                if (ctor.ParameterList?.Parameters.TryGetSingle(x => x.Identifier.ValueText == identifier, out ParameterSyntax parameter) == false)
                {
                    return Injectable.No;
                }

                return Injectable.Safe;
            }

            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Parent is MemberAccessExpressionSyntax ||
                    memberAccess.Expression is MemberAccessExpressionSyntax)
                {
                    // only handling simple servicelocator
                    return Injectable.No;
                }

                return IsInjectable(memberAccess.Expression, ctor, @checked);
            }

            if (expression is ObjectCreationExpressionSyntax nestedObjectCreation)
            {
                if (CanInject(nestedObjectCreation, ctor) == Injectable.No)
                {
                    return Injectable.No;
                }

                return Injectable.Safe;
            }

            return Injectable.No;
        }

        private static bool IsInjectionType(ITypeSymbol type)
        {
            if (type?.ContainingNamespace == null ||
                type.IsValueType ||
                type.IsStatic)
            {
                return false;
            }

            foreach (var namespaceSymbol in type.ContainingNamespace.ConstituentNamespaces)
            {
                if (namespaceSymbol.Name == "System")
                {
                    return false;
                }
            }

            return true;
        }
    }
}