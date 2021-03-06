#pragma warning disable GU0008 // Avoid relay properties.
#pragma warning disable GU0072 // Avoid internal check
namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Text;

    internal class DocumentEditorCodeFixContext
    {
        private readonly CodeFixContext context;

        public DocumentEditorCodeFixContext(CodeFixContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets the document corresponding to the <see cref="P:Microsoft.CodeAnalysis.CodeFixes.CodeFixContext.Span" /> to fix.
        /// </summary>
        public Document Document => this.context.Document;

        public CancellationToken CancellationToken => this.context.CancellationToken;

        /// <summary>
        /// Gets the text span within the <see cref="P:Microsoft.CodeAnalysis.CodeFixes.CodeFixContext.Document" /> to fix.
        /// </summary>
        public TextSpan Span => this.context.Span;

        /// <summary>
        /// Gets the diagnostics to fix.
        /// NOTE: All the diagnostics in this collection have the same <see cref="P:Microsoft.CodeAnalysis.CodeFixes.CodeFixContext.Span" />.
        /// </summary>
        public ImmutableArray<Diagnostic> Diagnostics => this.context.Diagnostics;

        public void RegisterCodeFix(
            string title,
            Action<DocumentEditor, CancellationToken> action,
            Type equivalenceKey,
            Diagnostic diagnostic)
        {
            // ReSharper disable once ImpureMethodCallOnReadonlyValueField
            this.context.RegisterCodeFix(
                new DocumentEditorAction(title, this.context.Document, action, equivalenceKey.FullName),
                diagnostic);
        }

        public void RegisterCodeFix(
            string title,
            Action<DocumentEditor, CancellationToken> action,
            string equivalenceKey,
            Diagnostic diagnostic)
        {
            // ReSharper disable once ImpureMethodCallOnReadonlyValueField
            this.context.RegisterCodeFix(
                new DocumentEditorAction(title, this.context.Document, action, equivalenceKey),
                diagnostic);
        }
    }
}
