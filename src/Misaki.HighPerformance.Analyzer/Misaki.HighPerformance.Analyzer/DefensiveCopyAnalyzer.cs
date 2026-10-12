using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;
using System.Linq;

namespace Misaki.HighPerformance.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DefensiveCopyAnalyzer : DiagnosticAnalyzer
    {
        public const string DIAGNOSTIC_ID = "MHP002";
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            DIAGNOSTIC_ID,
            "Defensive copy detected",
            "Calling non-readonly method '{0}' on readonly field or local '{1}' causes a silent defensive copy",
            "Safety",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public const string REF_COPY_DIAGNOSTIC_ID = "MHP004";
        private static readonly DiagnosticDescriptor s_refCopyRule = new DiagnosticDescriptor(
            REF_COPY_DIAGNOSTIC_ID,
            "Mutating copy of ref-returning source",
            "Calling non-readonly method '{0}' on local '{1}' which was copied by value from a ref-returning source; consider using 'ref var {1} = ref ...'",
            "Safety",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule, s_refCopyRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        private void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var method = invocation.TargetMethod;
            var instance = invocation.Instance;

            // 1. Basic Filters: Must be an instance method on a Value Type (Struct)
            if (method.IsStatic || instance == null)
            {
                return;
            }

            if (!instance.Type.IsValueType)
            {
                return; // Classes don't copy
            }

            if (instance.Type.IsReadOnly)
            {
                return; // Readonly structs are safe (compiler enforced)
            }

            // 2. If the method itself is 'readonly', it promises not to mutate, so no copy needed.
            if (method.IsReadOnly)
            {
                return;
            }

            // 3. CHECK THE CONTEXT: Is the variable we are calling on "Read Only"?
            if (IsReadOnlyContext(instance, out var variableName))
            {
                var diagnostic = Diagnostic.Create(
                    s_rule,
                    invocation.Syntax.GetLocation(),
                    method.Name,
                    variableName);

                context.ReportDiagnostic(diagnostic);
            }
            else if (IsCopiedFromRefContext(instance, context.Compilation, invocation.Syntax, out var localName))
            {
                var diagnostic = Diagnostic.Create(
                    s_refCopyRule,
                    invocation.Syntax.GetLocation(),
                    method.Name,
                    localName);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private bool IsReadOnlyContext(IOperation instance, out string name)
        {
            name = "";

            switch (instance)
            {
                // CASE 1: Readonly Field
                case IFieldReferenceOperation fieldRef:
                    if (fieldRef.Field.IsReadOnly)
                    {
                        name = fieldRef.Field.Name;
                        return true;
                    }
                    break;

                // CASE 2: Locals (ref readonly var x)
                case ILocalReferenceOperation localRef:
                    // RefKind.In covers 'ref readonly' locals
                    if (localRef.Local.RefKind == RefKind.In)
                    {
                        name = localRef.Local.Name;
                        return true;
                    }
                    break;

                // CASE 3: Parameters (in MyStruct x)
                case IParameterReferenceOperation paramRef:
                    // RefKind.In covers 'in' parameters
                    if (paramRef.Parameter.RefKind == RefKind.In)
                    {
                        name = paramRef.Parameter.Name;
                        return true;
                    }
                    break;
            }

            return false;
        }

        private static bool IsCopiedFromRefContext(
            IOperation instance,
            Compilation compilation,
            SyntaxNode invocationSyntax,
            out string name)
        {
            name = "";

            if (instance is ILocalReferenceOperation localRef)
            {
                var local = localRef.Local;
                if (local.RefKind != RefKind.None)
                {
                    return false;
                }

                if (IsReturned(local, invocationSyntax))
                {
                    return false;
                }

                foreach (var syntaxRef in local.DeclaringSyntaxReferences)
                {
                    var syntax = syntaxRef.GetSyntax();
                    var semanticModel = compilation.GetSemanticModel(syntaxRef.SyntaxTree);

                    if (syntax is VariableDeclaratorSyntax declarator)
                    {
                        if (declarator.Initializer != null && declarator.Initializer.Value != null)
                        {
                            if (IsRefSource(declarator.Initializer.Value, semanticModel))
                            {
                                name = local.Name;
                                return true;
                            }
                        }
                        else
                        {
                            if (IsAssignedFromRefSource(local, invocationSyntax, semanticModel))
                            {
                                name = local.Name;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsReturned(ILocalSymbol local, SyntaxNode invocationSyntax)
        {
            var enclosingBlock = invocationSyntax.Ancestors().FirstOrDefault(a =>
                a is MethodDeclarationSyntax ||
                a is LocalFunctionStatementSyntax ||
                a is AccessorDeclarationSyntax ||
                a is AnonymousFunctionExpressionSyntax);

            if (enclosingBlock != null)
            {
                foreach (var returnStatement in enclosingBlock.DescendantNodes().OfType<ReturnStatementSyntax>())
                {
                    if (returnStatement.Expression is IdentifierNameSyntax id && id.Identifier.Text == local.Name)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsAssignedFromRefSource(
            ILocalSymbol local,
            SyntaxNode invocationSyntax,
            SemanticModel semanticModel)
        {
            var enclosingBlock = invocationSyntax.Ancestors().FirstOrDefault(a =>
                a is MethodDeclarationSyntax ||
                a is LocalFunctionStatementSyntax ||
                a is AccessorDeclarationSyntax ||
                a is AnonymousFunctionExpressionSyntax);

            if (enclosingBlock != null)
            {
                foreach (var assignment in enclosingBlock.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                {
                    if (assignment.Left is IdentifierNameSyntax id &&
                        id.Identifier.Text == local.Name &&
                        assignment.SpanStart < invocationSyntax.SpanStart)
                    {
                        if (IsRefSource(assignment.Right, semanticModel))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsRefSource(ExpressionSyntax expr, SemanticModel semanticModel)
        {
            while (true)
            {
                if (expr is ParenthesizedExpressionSyntax paren)
                {
                    expr = paren.Expression;
                }
                else if (expr is CastExpressionSyntax cast)
                {
                    expr = cast.Expression;
                }
                else
                {
                    break;
                }
            }

            // 1. ElementAccessExpressionSyntax: e.g. array[i], list[i], span[i]
            if (expr is ElementAccessExpressionSyntax elementAccess)
            {
                var symbol = semanticModel.GetSymbolInfo(elementAccess).Symbol;
                if (symbol is IPropertySymbol prop)
                {
                    if (prop.ReturnsByRef || prop.RefKind != RefKind.None)
                    {
                        return true;
                    }
                }
                else
                {
                    var collectionType = semanticModel.GetTypeInfo(elementAccess.Expression).Type;
                    if (collectionType is IArrayTypeSymbol)
                    {
                        return true;
                    }
                }
            }

            // 2. InvocationExpressionSyntax: e.g. list.GetRef(i), GetRef()
            if (expr is InvocationExpressionSyntax invocation)
            {
                var symbol = semanticModel.GetSymbolInfo(invocation).Symbol;
                if (symbol is IMethodSymbol method)
                {
                    if (method.ReturnsByRef || method.RefKind != RefKind.None)
                    {
                        return true;
                    }
                }
            }

            // 3. Member access or identifier: ref property, ref local, ref parameter, ref field
            var memberSymbol = semanticModel.GetSymbolInfo(expr).Symbol;
            switch (memberSymbol)
            {
                case IPropertySymbol prop when prop.ReturnsByRef || prop.RefKind != RefKind.None:
                    return true;
                case ILocalSymbol loc when loc.RefKind != RefKind.None:
                    return true;
                case IParameterSymbol param when param.RefKind != RefKind.None:
                    return true;
            }

            return false;
        }
    }
}
