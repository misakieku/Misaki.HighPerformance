//Temprorarily disable this analyzer until we have a more robust implementation that can handle more complex scenarios without false positives.
//The current implementation is too naive and may not cover all edge cases effectively.

#if false
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

using System.Linq;

namespace Misaki.HighPerformance.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class OwnershipTransferAnalyzer : DiagnosticAnalyzer
    {
        public const string DIAGNOSTIC_ID = "MHP003";
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            DIAGNOSTIC_ID,
            "Ownership transfer detected",
            "Variable '{0}' is used after its ownership has been transferred.",
            "Safety",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            // Register an action to intercept method and function calls
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            if (!(symbolInfo.Symbol is IMethodSymbol methodSymbol))
            {
                return;
            }

            for (var i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
            {
                var argument = invocation.ArgumentList.Arguments[i];
                IParameterSymbol parameter = null;

                // Handle named arguments if present
                if (argument.NameColon != null)
                {
                    parameter = methodSymbol.Parameters.FirstOrDefault(p => p.Name == argument.NameColon.Name.Identifier.ValueText);
                }
                else if (i < methodSymbol.Parameters.Length)
                {
                    parameter = methodSymbol.Parameters[i];
                    // Handle params arrays
                    if (parameter.IsParams && i >= methodSymbol.Parameters.Length - 1)
                    {
                        parameter = methodSymbol.Parameters[methodSymbol.Parameters.Length - 1];
                    }
                }

                if (parameter == null)
                {
                    continue;
                }

                // Check if the parameter requires an ownership transfer
                var hasOwnershipAttribute = parameter.GetAttributes()
                    .Any(attr => attr.AttributeClass?.Name == "OwnershipTransferAttribute" || attr.AttributeClass?.Name == "OwnershipTransfer");

                if (hasOwnershipAttribute)
                {
                    var argSymbolInfo = semanticModel.GetSymbolInfo(argument.Expression);
                    var transferredSymbol = argSymbolInfo.Symbol;

                    // Only track local variables and parameters as they represent single runtime instances within the scope
                    if (transferredSymbol is ILocalSymbol || transferredSymbol is IParameterSymbol)
                    {
                        CheckForSubsequentUsage(context, invocation, transferredSymbol);
                    }
                }
            }
        }

        private void CheckForSubsequentUsage(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, ISymbol transferredSymbol)
        {
            // Find the lexical block to scan for subsequent accesses (supports standard methods and top-level statements)
            var block = invocation.Ancestors().OfType<BlockSyntax>().FirstOrDefault() 
                            ?? (SyntaxNode)invocation.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();

            if (block == null)
            {
                return;
            }

            // Find all identifiers inside this method implementation
            var identifiers = block.DescendantNodes().OfType<IdentifierNameSyntax>();

            foreach (var identifier in identifiers)
            {
                // Trigger a warning if the code accesses the data textually after the invocation ends
                if (identifier.SpanStart > invocation.Span.End)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;

                    if (symbol != null && symbol.Equals(transferredSymbol, SymbolEqualityComparer.Default))
                    {
                        var diagnostic = Diagnostic.Create(s_rule, identifier.GetLocation(), transferredSymbol.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
#endif