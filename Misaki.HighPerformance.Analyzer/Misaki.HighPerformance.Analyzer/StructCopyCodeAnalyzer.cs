using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Misaki.HighPerformance.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StructCopyCodeAnalyzer : DiagnosticAnalyzer
    {
        public const string DIAGNOSTIC_ID = "MHP001";
        private const string _TITLE = "Struct marked as NonCopyable was copied";
        private const string _MESSAGE_FORMAT = "The struct '{0}' is designed for unique ownership and cannot be copied. Use .Detach(), .Get(), .Share() or pass by reference.";
        private const string _CATEGORY = "Safety";

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            DIAGNOSTIC_ID, _TITLE, _MESSAGE_FORMAT, _CATEGORY, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // We want to catch:
            // 1. var a = b; (Variable Declaration)
            // 2. a = b;     (Assignment Expression)
            context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
            context.RegisterSyntaxNodeAction(AnalyzeDeclaration, SyntaxKind.VariableDeclarator);
        }

        private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;
            var rightHandSide = assignment.Right;

            AnalyzePossibleCopy(context, assignment.Left, rightHandSide);
        }

        private void AnalyzeDeclaration(SyntaxNodeAnalysisContext context)
        {
            var declarator = (VariableDeclaratorSyntax)context.Node;

            // Handle: var a = b;
            if (declarator.Initializer == null)
            {
                return;
            }

            var variableType = context.SemanticModel.GetTypeInfo(declarator.Initializer.Value).Type;
            if (variableType == null)
            {
                return;
            }

            // Check if this is a NonCopyable type
            if (!IsNonCopyable(variableType))
            {
                return;
            }

            AnalyzePossibleCopy(context, declarator, declarator.Initializer.Value);
        }

        private void AnalyzePossibleCopy(SyntaxNodeAnalysisContext context, SyntaxNode targetNode, ExpressionSyntax rightHandSide)
        {
            // 1. Get type of the RHS
            var typeInfo = context.SemanticModel.GetTypeInfo(rightHandSide);
            var type = typeInfo.Type;

            if (type == null || !IsNonCopyable(type))
            {
                return;
            }

            // 2. Determine if the RHS is a "Storage Location" (Variable, Field, Parameter)
            // If it is, this is a copy operation.
            // If the RHS is a Method Call (Factory) or 'new' keyword, we allow it (Creation/Transfer).

            var isCopy = false;

            switch (rightHandSide)
            {
                case AssignmentExpressionSyntax _: // e.g. = a = b;
                case IdentifierNameSyntax _: // e.g. = myVar;
                case MemberAccessExpressionSyntax _: // e.g. = obj.myField;
                case ElementAccessExpressionSyntax _: // e.g. = arr[0];
                    isCopy = true;
                    break;
                    // We explicitly allow InvocationExpression (methods) and ObjectCreationExpression (new)
                    // because those typically represent creating a new owner, not copying an existing one.
            }

            if (isCopy)
            {
                // 3. Double check that we are not just referencing a constant or static readonly
                var symbol = context.SemanticModel.GetSymbolInfo(rightHandSide).Symbol;

                // If it's a local, parameter, field, or property, it's a copy of an existing value.
                if (symbol != null && (
                    symbol.Kind == SymbolKind.Local ||
                    symbol.Kind == SymbolKind.Parameter ||
                    symbol.Kind == SymbolKind.Field ||
                    symbol.Kind == SymbolKind.Property))
                {
                    var diagnostic = Diagnostic.Create(s_rule, rightHandSide.GetLocation(), type.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private bool IsNonCopyable(ITypeSymbol type)
        {
            // Check for [NonCopyable] attribute on the struct
            return type.GetAttributes().Any(ad =>
                ad.AttributeClass != null &&
                ad.AttributeClass.Name == "NonCopyableAttribute");
        }
    }
}
