using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Misaki.HighPerformance.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StructParameterMutationAnalyzer : DiagnosticAnalyzer
    {
        public const string DIAGNOSTIC_ID = "MHP003";

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            DIAGNOSTIC_ID,
            "Non-ref struct parameter state modified",
            "A non-ref struct parameter must either be provably read-only, explicitly marked [AllowCopy], or have its modified value escape via return or reference.",
            "Safety",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterOperationBlockAction(AnalyzeOperationBlock);
        }

        private void AnalyzeOperationBlock(OperationBlockAnalysisContext context)
        {
            if (!(context.OwningSymbol is IMethodSymbol method))
            {
                return;
            }

            // 1. Analyze the owning method
            AnalyzeMethodSymbol(context, method, context.OperationBlocks);

            // 2. Also analyze any local functions or lambdas declared within the block
            foreach (var block in context.OperationBlocks)
            {
                foreach (var op in block.DescendantsAndSelf())
                {
                    if (op is ILocalFunctionOperation localFunc && localFunc.Body != null)
                    {
                        AnalyzeMethodSymbol(context, localFunc.Symbol, ImmutableArray.Create<IOperation>(localFunc.Body));
                    }
                    else if (op is IAnonymousFunctionOperation anonFunc && anonFunc.Body != null)
                    {
                        AnalyzeMethodSymbol(context, anonFunc.Symbol, ImmutableArray.Create<IOperation>(anonFunc.Body));
                    }
                }
            }
        }

        private void AnalyzeMethodSymbol(
            OperationBlockAnalysisContext context,
            IMethodSymbol method,
            ImmutableArray<IOperation> blocks)
        {
            if (method.Parameters.IsEmpty)
            {
                return;
            }

            List<IParameterSymbol> candidateParameters = null;
            foreach (var param in method.Parameters)
            {
                if (IsCandidateStructParameter(param))
                {
                    candidateParameters = candidateParameters ?? new List<IParameterSymbol>();
                    candidateParameters.Add(param);
                }
            }

            if (candidateParameters == null)
            {
                return;
            }

            foreach (var param in candidateParameters)
            {
                AnalyzeParameter(context, method, param, blocks);
            }
        }

        private void AnalyzeParameter(
            OperationBlockAnalysisContext context,
            IMethodSymbol method,
            IParameterSymbol parameter,
            ImmutableArray<IOperation> blocks)
        {
            var mutations = new List<Location>();
            var seenSpans = new HashSet<TextSpan>();
            bool escapesViaReturn = false;
            bool escapesViaReference = false;

            foreach (var block in blocks)
            {
                foreach (var op in block.DescendantsAndSelf())
                {
                    // Check if the parameter escapes via return
                    if (!escapesViaReturn && op is IReturnOperation returnOp && IsDirectReturnOf(returnOp, method))
                    {
                        if (IsReturnedAsStruct(returnOp.ReturnedValue, parameter, block))
                        {
                            escapesViaReturn = true;
                        }
                    }

                    // Check if the parameter escapes via reference
                    if (!escapesViaReference && op is IAssignmentOperation escapeAssign &&
                        AssignsParameterToExternalLocation(escapeAssign, parameter))
                    {
                        escapesViaReference = true;
                    }

                    // Collect mutations
                    CheckOperationForMutation(op, parameter, mutations, seenSpans);
                }
            }

            // If the modified value escapes via return or reference, no diagnostic
            if (escapesViaReturn || escapesViaReference)
            {
                return;
            }

            // Report diagnostic on each mutation location
            foreach (var location in mutations)
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, location));
            }
        }

        private static bool IsCandidateStructParameter(IParameterSymbol parameter)
        {
            if (parameter.RefKind != RefKind.None)
            {
                return false;
            }

            var type = parameter.Type;
            if (type == null ||
                !type.IsValueType ||
                type.TypeKind != TypeKind.Struct ||
                type.SpecialType != SpecialType.None ||
                type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T ||
                type.IsReadOnly ||
                type.IsTupleType ||
                type.IsRefLikeType)
            {
                return false;
            }

            if (HasAllowCopyAttribute(parameter))
            {
                return false;
            }

            return true;
        }

        private static bool HasAllowCopyAttribute(IParameterSymbol parameter)
        {
            return parameter.GetAttributes().Any(a =>
                a.AttributeClass != null &&
                (a.AttributeClass.Name == "AllowCopyAttribute" || a.AttributeClass.Name == "AllowCopy"));
        }

        private static void CheckOperationForMutation(
            IOperation op,
            IParameterSymbol parameter,
            List<Location> mutations,
            HashSet<TextSpan> seenSpans)
        {
            // 1. Assignment (simple or compound, e.g. s.value = 10, s.value += 1, s = other)
            if (op is IAssignmentOperation assignment)
            {
                if (IsRootedInParameter(assignment.Target, parameter))
                {
                    AddMutation(assignment.Syntax.GetLocation(), mutations, seenSpans);
                }
            }
            // 2. Increment or decrement (e.g. s.value++, --s.value)
            else if (op is IIncrementOrDecrementOperation incDec)
            {
                if (IsRootedInParameter(incDec.Target, parameter))
                {
                    AddMutation(incDec.Syntax.GetLocation(), mutations, seenSpans);
                }
            }
            // 3. Invocation
            else if (op is IInvocationOperation invocation)
            {
                var targetMethod = invocation.TargetMethod;

                // 3a. Instance method on the struct that is not readonly (e.g. s.Increment())
                if (!targetMethod.IsStatic && IsRootedInParameter(invocation.Instance, parameter))
                {
                    if (!targetMethod.IsReadOnly)
                    {
                        AddMutation(invocation.Syntax.GetLocation(), mutations, seenSpans);
                    }
                }
                // 3b. Extension method with 'this ref' receiver (e.g. s.Mutate())
                else if (targetMethod.IsExtensionMethod && invocation.Arguments.Length > 0 &&
                         targetMethod.Parameters[0].RefKind == RefKind.Ref &&
                         IsRootedInParameter(invocation.Arguments[0].Value, parameter))
                {
                    AddMutation(invocation.Syntax.GetLocation(), mutations, seenSpans);
                }

                // 3c. Arguments passed by ref or out (e.g. Mutate(ref s), Mutate(out s), Interlocked.Increment(ref s.value))
                for (int i = 0; i < invocation.Arguments.Length; i++)
                {
                    var arg = invocation.Arguments[i];
                    if (targetMethod.IsExtensionMethod && i == 0)
                    {
                        continue;
                    }

                    if (arg.Parameter != null &&
                        (arg.Parameter.RefKind == RefKind.Ref || arg.Parameter.RefKind == RefKind.Out) &&
                        IsRootedInParameter(arg.Value, parameter))
                    {
                        AddMutation(arg.Syntax.GetLocation(), mutations, seenSpans);
                    }
                }
            }
        }

        private static void AddMutation(
            Location location,
            List<Location> mutations,
            HashSet<TextSpan> seenSpans)
        {
            if (location != null && seenSpans.Add(location.SourceSpan))
            {
                mutations.Add(location);
            }
        }

        private static bool IsRootedInParameter(IOperation operation, IParameterSymbol parameter)
        {
            var current = operation;
            while (current != null)
            {
                if (current is IParameterReferenceOperation paramRef)
                {
                    return SymbolEqualityComparer.Default.Equals(paramRef.Parameter, parameter);
                }

                if (current is IFieldReferenceOperation fieldRef)
                {
                    current = fieldRef.Instance;
                }
                else if (current is IPropertyReferenceOperation propRef)
                {
                    current = propRef.Instance;
                }
                else if (current is IConversionOperation conv)
                {
                    current = conv.Operand;
                }
                else
                {
                    break;
                }
            }

            return false;
        }

        private static bool IsDirectReturnOf(IReturnOperation returnOp, IMethodSymbol targetMethod)
        {
            for (var parent = returnOp.Parent; parent != null; parent = parent.Parent)
            {
                if (parent is ILocalFunctionOperation localFunc)
                {
                    return SymbolEqualityComparer.Default.Equals(localFunc.Symbol, targetMethod);
                }
                if (parent is IAnonymousFunctionOperation anonFunc)
                {
                    return SymbolEqualityComparer.Default.Equals(anonFunc.Symbol, targetMethod);
                }
            }

            return true;
        }

        private static bool IsReturnedAsStruct(IOperation operation, IParameterSymbol parameter, IOperation root)
        {
            if (operation == null)
            {
                return false;
            }

            while (operation is IConversionOperation conv)
            {
                operation = conv.Operand;
            }

            if (operation is IParameterReferenceOperation paramRef)
            {
                return SymbolEqualityComparer.Default.Equals(paramRef.Parameter, parameter);
            }

            if (operation is ITupleOperation tupleOp)
            {
                foreach (var element in tupleOp.Elements)
                {
                    if (IsReturnedAsStruct(element, parameter, root))
                    {
                        return true;
                    }
                }
            }

            if (operation is IConditionalOperation condOp)
            {
                if (IsReturnedAsStruct(condOp.WhenTrue, parameter, root) ||
                    IsReturnedAsStruct(condOp.WhenFalse, parameter, root))
                {
                    return true;
                }
            }

            if (operation is ILocalReferenceOperation localRef)
            {
                if (IsLocalAssignedFromParameter(localRef.Local, parameter, root))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLocalAssignedFromParameter(ILocalSymbol local, IParameterSymbol parameter, IOperation root)
        {
            foreach (var op in root.DescendantsAndSelf())
            {
                if (op is IVariableDeclaratorOperation decl &&
                    SymbolEqualityComparer.Default.Equals(decl.Symbol, local) &&
                    IsParameterReference(decl.Initializer?.Value, parameter))
                {
                    return true;
                }

                if (op is IAssignmentOperation assign &&
                    assign.Target is ILocalReferenceOperation targetLocal &&
                    SymbolEqualityComparer.Default.Equals(targetLocal.Local, local) &&
                    IsParameterReference(assign.Value, parameter))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsParameterReference(IOperation operation, IParameterSymbol parameter)
        {
            if (operation == null)
            {
                return false;
            }

            while (operation is IConversionOperation conv)
            {
                operation = conv.Operand;
            }

            return operation is IParameterReferenceOperation paramRef &&
                   SymbolEqualityComparer.Default.Equals(paramRef.Parameter, parameter);
        }

        private static bool AssignsParameterToExternalLocation(IAssignmentOperation assignment, IParameterSymbol parameter)
        {
            var value = assignment.Value;
            while (value is IConversionOperation conv)
            {
                value = conv.Operand;
            }

            if (!ContainsParameter(value, parameter))
            {
                return false;
            }

            var target = assignment.Target;
            while (target is IConversionOperation convTarget)
            {
                target = convTarget.Operand;
            }

            // 1. Ref or out parameter of the method
            if (target is IParameterReferenceOperation targetParam &&
                targetParam.Parameter.RefKind != RefKind.None &&
                !SymbolEqualityComparer.Default.Equals(targetParam.Parameter, parameter))
            {
                return true;
            }

            // 2. Ref local (points to an external reference)
            if (target is ILocalReferenceOperation targetLocal &&
                targetLocal.Local.RefKind != RefKind.None)
            {
                return true;
            }

            // 3. Field of an object, this, or static
            if (target is IFieldReferenceOperation fieldRef &&
                !IsRootedInParameter(fieldRef.Instance, parameter))
            {
                return true;
            }

            // 4. Property of an object, this, or static
            if (target is IPropertyReferenceOperation propRef &&
                !IsRootedInParameter(propRef.Instance, parameter))
            {
                return true;
            }

            // 5. Array element: arr[i] = s
            if (target is IArrayElementReferenceOperation)
            {
                return true;
            }

            // 6. Pointer indirection: *ptr = s
            if (target.Syntax != null && target.Syntax.IsKind(SyntaxKind.PointerIndirectionExpression))
            {
                return true;
            }

            return false;
        }

        private static bool ContainsParameter(IOperation op, IParameterSymbol parameter)
        {
            if (op == null)
            {
                return false;
            }

            while (op is IConversionOperation conv)
            {
                op = conv.Operand;
            }

            if (op is IParameterReferenceOperation paramRef)
            {
                return SymbolEqualityComparer.Default.Equals(paramRef.Parameter, parameter);
            }

            if (op is ITupleOperation tuple)
            {
                return tuple.Elements.Any(e => ContainsParameter(e, parameter));
            }

            return false;
        }
    }
}
