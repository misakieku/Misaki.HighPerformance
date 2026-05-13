using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

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

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

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
                return;   // Readonly structs are safe (compiler enforced)
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
    }
}
