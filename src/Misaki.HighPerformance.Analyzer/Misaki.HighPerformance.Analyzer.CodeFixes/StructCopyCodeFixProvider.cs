using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Misaki.HighPerformance.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StructCopyCodeFixProvider)), Shared]
    public class StructCopyCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(StructCopyCodeAnalyzer.DIAGNOSTIC_ID);
            }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the expression identified by the diagnostic.
            // This will be the Right-Hand Side (RHS) of the assignment or declaration
            var expressionSyntax = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                .OfType<ExpressionSyntax>()
                .First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Share memory",
                    c => ShareAsync(context.Document, expressionSyntax, c),
                    "StructCopyCodeFixProvider_ShareMemory"),
                diagnostic);

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Transfer ownership",
                    c => TransferOwnershipAsync(context.Document, expressionSyntax, c),
                    "StructCopyCodeFixProvider_TransferOwnership"),
                diagnostic);
        }

        private async Task<Document> ShareAsync(Document document, ExpressionSyntax expressionToFix, CancellationToken cancellationToken)
        {
            // 1. Get the semantic model to figure out exactly what type we are dealing with
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetTypeInfo(expressionToFix).Type;

            // 2. Generate the type name string (e.g., "UniquePtr<ID3D12Device>")
            // We use ToMinimalDisplayString so Roslyn handles namespaces/using directives automatically.
            var typeName = typeSymbol.ToMinimalDisplayString(semanticModel, expressionToFix.SpanStart);
            var typeSyntax = SyntaxFactory.ParseTypeName(typeName);

            // 3. Create the "Share()" invocation: expression.Share()
            var detachMethod = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                expressionToFix, // The 'a' in 'b = a'
                SyntaxFactory.IdentifierName("Share")
            );

            var detachInvocation = SyntaxFactory.InvocationExpression(detachMethod);

            // 4. Replace the old node with the new node in the Syntax Tree
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(expressionToFix, detachInvocation);

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> TransferOwnershipAsync(Document document, ExpressionSyntax expressionToFix, CancellationToken cancellationToken)
        {
            // 1. Get the semantic model to figure out exactly what type we are dealing with
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetTypeInfo(expressionToFix).Type;

            // 2. Generate the type name string (e.g., "UniquePtr<ID3D12Device>")
            // We use ToMinimalDisplayString so Roslyn handles namespaces/using directives automatically.
            var typeName = typeSymbol.ToMinimalDisplayString(semanticModel, expressionToFix.SpanStart);
            var typeSyntax = SyntaxFactory.ParseTypeName(typeName);

            // 3. Create the "Detach()" invocation: expression.Detach()
            var detachMethod = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                expressionToFix, // The 'a' in 'b = a'
                SyntaxFactory.IdentifierName("Detach")
            );

            var detachInvocation = SyntaxFactory.InvocationExpression(detachMethod);

            // 4. Create the "new UniquePtr<T>(...)" expression
            var newObjectCreation = SyntaxFactory.ObjectCreationExpression(typeSyntax)
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(detachInvocation)
                        )
                    )
                )
                .WithAdditionalAnnotations(Formatter.Annotation); // Auto-format whitespace

            // 5. Replace the old node with the new node in the Syntax Tree
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(expressionToFix, newObjectCreation);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
