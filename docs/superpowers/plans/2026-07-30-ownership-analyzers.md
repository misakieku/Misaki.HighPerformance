# Ownership Analyzers (MHP004 + MHP005) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add two Roslyn analyzers that enforce simplified Rust-style aliasing-XOR-ownership: one variable cannot appear as the argument to an `[Owner]` parameter alongside any other position in the same call (MHP004), and a variable passed to an `[Owner]` parameter cannot be used later in the method body (MHP005).

**Architecture:** MHP004 uses `RegisterOperationAction(OperationKind.Invocation)` with `IInvocationOperation` to inspect argument symbols against their parameters' `[Owner]` attribute — a same-call check with no state tracking. MHP005 uses `RegisterSyntaxNodeAction(SyntaxKind.InvocationExpression)` and walks the containing block's descendant identifiers after the call span to detect post-transfer usage of consumed symbols; it is deliberately simple, does not handle reassignment or branching, and reports at `Warning` severity to acknowledge these limitations.

**Tech Stack:** Roslyn 3.3.1 (netstandard2.0), C# 10+, MSTest with `CSharpAnalyzerVerifier<T>` from `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.MSTest` 1.1.0.

---

## File Structure

| File | Action | Responsibility |
|---|---|---|
| `src/.../LowLevel/Attributes.cs` | Modify | Remove `bool mutable` from `OwnerAttribute` |
| `src/.../Analyzer/.../OwnershipAliasingAnalyzer.cs` | Create | MHP004: same-call ownership aliasing |
| `src/.../Analyzer/.../OwnershipUseAfterTransferAnalyzer.cs` | Create | MHP005: use-after-ownership-transfer |
| `src/.../Analyzer.Test/OwnershipAliasingAnalyzerTests.cs` | Create | MHP004 tests |
| `src/.../Analyzer.Test/OwnershipUseAfterTransferAnalyzerTests.cs` | Create | MHP005 tests |
| `src/.../Analyzer.Test/MisakiHighPerformanceAnalyzerUnitTests.cs` | Delete | Remove placeholder boilerplate tests |

All analyzer files follow the existing pattern: one `DiagnosticAnalyzer` class per diagnostic ID, `s_rule` field, `SupportedDiagnostics`, `Initialize` with `ConfigureGeneratedCodeAnalysis` + `EnableConcurrentExecution`.

All test files follow the existing verifier pattern: `using VerifyCS = CSharpAnalyzerVerifier<TAnalyzer>` and `VerifyAnalyzerAsync(testSource)`.

---

### Task 1: Simplify `OwnerAttribute`

**Files:**
- Modify: `src/Misaki.HighPerformance.LowLevel/Attributes.cs`

- [ ] **Step 1: Remove the constructor parameter**

Replace the `OwnerAttribute` class:

```csharp
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.ReturnValue)]
public class OwnerAttribute : Attribute
{
}
```

The implicit parameterless constructor replaces `OwnerAttribute(bool mutable = true)`. No existing code uses `[Owner(false)]`, so this is safe.

- [ ] **Step 2: Verify compilation**

```bash
dotnet build src/Misaki.HighPerformance.LowLevel/Misaki.HighPerformance.LowLevel.csproj
```

Expected: Build succeeds. The three `[Owner]` usages in `MemoryUtility.cs` (lines 269, 293, 502) use no-arg form and remain valid.

- [ ] **Step 3: Commit**

```bash
git add src/Misaki.HighPerformance.LowLevel/Attributes.cs
git commit -m "refactor: remove mutable parameter from OwnerAttribute"
```

---

### Task 2: Create MHP004 `OwnershipAliasingAnalyzer`

**Files:**
- Create: `src/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer/OwnershipAliasingAnalyzer.cs`

- [ ] **Step 1: Write the analyzer**

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Misaki.HighPerformance.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class OwnershipAliasingAnalyzer : DiagnosticAnalyzer
    {
        public const string DIAGNOSTIC_ID = "MHP004";
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            DIAGNOSTIC_ID,
            "Ownership aliasing violation",
            "Variable '{0}' is passed to an [Owner] parameter and also appears in another argument position. An [Owner] parameter requires exclusive reference.",
            "Safety",
            DiagnosticSeverity.Error,
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

            // Build: symbol -> list of (isOwner, argument)
            var usages = new Dictionary<ISymbol, List<(bool IsOwner, IArgumentOperation Argument)>>(SymbolEqualityComparer.Default);

            foreach (var arg in invocation.Arguments)
            {
                var sym = GetLocalOrParamSymbol(arg.Value);
                if (sym == null)
                {
                    continue;
                }

                var isOwner = HasOwnerAttribute(arg.Parameter);

                if (!usages.ContainsKey(sym))
                {
                    usages[sym] = new List<(bool, IArgumentOperation)>();
                }

                usages[sym].Add((isOwner, arg));
            }

            // Report: any symbol appears as [Owner] AND in at least one other position
            foreach (var (sym, symUsages) in usages)
            {
                if (symUsages.Count <= 1)
                {
                    continue;
                }

                if (symUsages.Any(u => u.IsOwner))
                {
                    var diagnostic = Diagnostic.Create(
                        s_rule,
                        symUsages[0].Argument.Syntax.GetLocation(),
                        sym.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static ISymbol? GetLocalOrParamSymbol(IOperation operation)
        {
            return operation switch
            {
                ILocalReferenceOperation local => local.Local,
                IParameterReferenceOperation param => param.Parameter,
                // Unwrap implicit conversions that wrap a local/param reference
                IConversionOperation conv => GetLocalOrParamSymbol(conv.Operand),
                _ => null,
            };
        }

        private static bool HasOwnerAttribute(IParameterSymbol? parameter)
        {
            if (parameter == null)
            {
                return false;
            }

            return parameter.GetAttributes().Any(a =>
                a.AttributeClass != null &&
                a.AttributeClass.Name == "OwnerAttribute");
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

```bash
dotnet build src/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer.csproj
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer/OwnershipAliasingAnalyzer.cs
git commit -m "feat: add MHP004 OwnershipAliasingAnalyzer"
```

---

### Task 3: Create MHP004 tests

**Files:**
- Create: `src/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer.Test/OwnershipAliasingAnalyzerTests.cs`

- [ ] **Step 1: Write the test file**

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Misaki.HighPerformance.Analyzer.Test.CSharpAnalyzerVerifier<
    Misaki.HighPerformance.Analyzer.OwnershipAliasingAnalyzer>;

namespace Misaki.HighPerformance.Analyzer.Test
{
    [TestClass]
    public class OwnershipAliasingAnalyzerTests
    {
        // Shared source prefix: defines OwnerAttribute and test helper methods
        private const string Prefix = @"
using System;

namespace Misaki.HighPerformance.LowLevel
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.ReturnValue)]
    public class OwnerAttribute : Attribute { }
}

public static class Native
{
    public static void Free([Misaki.HighPerformance.LowLevel.Owner] IntPtr ptr) { }
    public static void Move([Misaki.HighPerformance.LowLevel.Owner] IntPtr a,
                            [Misaki.HighPerformance.LowLevel.Owner] IntPtr b) { }
    public static void Mix([Misaki.HighPerformance.LowLevel.Owner] IntPtr a, IntPtr b) { }
    public static void Inspect(IntPtr a, IntPtr b) { }
}
";

        [TestMethod]
        public async Task DifferentVariables_NoOwnerParams_NoDiagnostic()
        {
            var test = Prefix + @"
class C {
    void M() {
        var a = IntPtr.Zero;
        var b = IntPtr.Zero;
        Native.Inspect(a, b);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task DifferentVariables_BothOwnerParams_NoDiagnostic()
        {
            var test = Prefix + @"
class C {
    void M() {
        var a = IntPtr.Zero;
        var b = IntPtr.Zero;
        Native.Move(a, b);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SameVariable_MultipleUnmarkedParams_NoDiagnostic()
        {
            var test = Prefix + @"
class C {
    void M() {
        var a = IntPtr.Zero;
        Native.Inspect(a, a);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SameVariable_TwoOwnerParams_ReportsDiagnostic()
        {
            var test = Prefix + @"
class C {
    void M() {
        var p = IntPtr.Zero;
        Native.Move(p, p);
    }
}";
            var expected = VerifyCS.Diagnostic("MHP004")
                .WithArguments("p");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SameVariable_OwnerAndUnmarked_ReportsDiagnostic()
        {
            var test = Prefix + @"
class C {
    void M() {
        var p = IntPtr.Zero;
        Native.Mix(p, p);
    }
}";
            var expected = VerifyCS.Diagnostic("MHP004")
                .WithArguments("p");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task SameVariable_UnmarkedAndOwner_ReportsDiagnostic()
        {
            // Same as above but argument order swapped: unmarked param first
            var test = Prefix + @"
public static class Native2 {
    public static void Mix2(IntPtr a, [Misaki.HighPerformance.LowLevel.Owner] IntPtr b) { }
}

class C {
    void M() {
        var p = IntPtr.Zero;
        Native2.Mix2(p, p);
    }
}";
            var expected = VerifyCS.Diagnostic("MHP004")
                .WithArguments("p");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task ParameterVariable_OwnerAliasing_ReportsDiagnostic()
        {
            var test = Prefix + @"
class C {
    void M(IntPtr p) {
        Native.Move(p, p);
    }
}";
            var expected = VerifyCS.Diagnostic("MHP004")
                .WithArguments("p");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
```

- [ ] **Step 2: Run MHP004 tests**

```bash
dotnet test src/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer.Test/Misaki.HighPerformance.Analyzer.Test.csproj --filter "FullyQualifiedName~OwnershipAliasingAnalyzerTests"
```

Expected: All 7 tests pass.

- [ ] **Step 3: Commit**

```bash
git add src/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer.Test/OwnershipAliasingAnalyzerTests.cs
git commit -m "test: add MHP004 OwnershipAliasingAnalyzer tests"
```

---

### Task 4: Create MHP005 `OwnershipUseAfterTransferAnalyzer`

**Files:**
- Create: `src/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer/OwnershipUseAfterTransferAnalyzer.cs`

- [ ] **Step 1: Write the analyzer**

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Misaki.HighPerformance.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class OwnershipUseAfterTransferAnalyzer : DiagnosticAnalyzer
    {
        public const string DIAGNOSTIC_ID = "MHP005";
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            DIAGNOSTIC_ID,
            "Possible use after ownership transfer",
            "Variable '{0}' may be used after its ownership has been transferred to an [Owner] parameter. Ensure the variable is not accessed after transferring ownership.",
            "Safety",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            if (!(semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol))
            {
                return;
            }

            // Phase 1: Identify consumed symbols (arguments passed to [Owner] params)
            foreach (var consumedSymbol in GetConsumedSymbols(invocation, methodSymbol, semanticModel))
            {
                // Phase 2: Scan the containing block for later uses of the consumed symbol
                var block = invocation.Ancestors().OfType<BlockSyntax>().FirstOrDefault()
                    ?? (SyntaxNode)invocation.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();

                if (block == null)
                {
                    continue;
                }

                foreach (var identifier in block.DescendantNodes().OfType<IdentifierNameSyntax>())
                {
                    // Only consider identifiers that appear textually after this invocation
                    if (identifier.SpanStart <= invocation.Span.End)
                    {
                        continue;
                    }

                    var symbol = semanticModel.GetSymbolInfo(identifier).Symbol;
                    if (symbol != null && SymbolEqualityComparer.Default.Equals(symbol, consumedSymbol))
                    {
                        var diagnostic = Diagnostic.Create(
                            s_rule,
                            identifier.GetLocation(),
                            consumedSymbol.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static ImmutableArray<ISymbol> GetConsumedSymbols(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel)
        {
            if (invocation.ArgumentList == null)
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            var builder = ImmutableArray.CreateBuilder<ISymbol>();

            for (var i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
            {
                var argument = invocation.ArgumentList.Arguments[i];
                var parameter = GetParameter(methodSymbol, argument, i);
                if (parameter == null || !HasOwnerAttribute(parameter))
                {
                    continue;
                }

                var symbol = semanticModel.GetSymbolInfo(argument.Expression).Symbol;
                if (symbol is ILocalSymbol || symbol is IParameterSymbol)
                {
                    builder.Add(symbol);
                }
            }

            return builder.ToImmutable();
        }

        private static IParameterSymbol? GetParameter(IMethodSymbol method, ArgumentSyntax argument, int index)
        {
            if (argument.NameColon != null)
            {
                return method.Parameters.FirstOrDefault(p => p.Name == argument.NameColon.Name.Identifier.ValueText);
            }

            if (index < method.Parameters.Length)
            {
                var param = method.Parameters[index];
                if (param.IsParams && index >= method.Parameters.Length - 1)
                {
                    return method.Parameters[method.Parameters.Length - 1];
                }

                return param;
            }

            return null;
        }

        private static bool HasOwnerAttribute(IParameterSymbol parameter)
        {
            return parameter.GetAttributes().Any(a =>
                a.AttributeClass != null &&
                a.AttributeClass.Name == "OwnerAttribute");
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

```bash
dotnet build src/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer.csproj
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer/OwnershipUseAfterTransferAnalyzer.cs
git commit -m "feat: add MHP005 OwnershipUseAfterTransferAnalyzer"
```

---

### Task 5: Create MHP005 tests

**Files:**
- Create: `src/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer.Test/OwnershipUseAfterTransferAnalyzerTests.cs`

- [ ] **Step 1: Write the test file**

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Misaki.HighPerformance.Analyzer.Test.CSharpAnalyzerVerifier<
    Misaki.HighPerformance.Analyzer.OwnershipUseAfterTransferAnalyzer>;

namespace Misaki.HighPerformance.Analyzer.Test
{
    [TestClass]
    public class OwnershipUseAfterTransferAnalyzerTests
    {
        private const string Prefix = @"
using System;

namespace Misaki.HighPerformance.LowLevel
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.ReturnValue)]
    public class OwnerAttribute : Attribute { }
}

public static class Native
{
    public static void Free([Misaki.HighPerformance.LowLevel.Owner] IntPtr ptr) { }
    public static void Inspect(IntPtr ptr) { }
    public static void Read([Misaki.HighPerformance.LowLevel.Owner] IntPtr a, IntPtr b) { }
}
";

        [TestMethod]
        public async Task VariableUsedBeforeTransfer_NoDiagnostic()
        {
            var test = Prefix + @"
class C {
    void M() {
        var p = IntPtr.Zero;
        Native.Inspect(p);   // Use before transfer — OK
        Native.Free(p);      // Transfer ownership
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task VariableTransferred_NotUsedAfter_NoDiagnostic()
        {
            var test = Prefix + @"
class C {
    void M() {
        var p = IntPtr.Zero;
        Native.Free(p);      // Transfer — last use, OK
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task VariableTransferred_UsedAfter_ReportsDiagnostic()
        {
            var test = Prefix + @"
class C {
    void M() {
        var p = IntPtr.Zero;
        Native.Free(p);      // Transfer ownership
        Native.Inspect(p);   // MHP005: use after transfer
    }
}";
            var expected = VerifyCS.Diagnostic("MHP005")
                .WithArguments("p");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task DifferentVariableUsedAfter_NoDiagnostic()
        {
            var test = Prefix + @"
class C {
    void M() {
        var p = IntPtr.Zero;
        var q = IntPtr.Zero;
        Native.Free(p);      // Transfer p
        Native.Inspect(q);   // Use q — unrelated, OK
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task VariablePassedAsNonOwner_UsedAfter_NoDiagnostic()
        {
            // Passing as non-owner (unmarked) does NOT transfer ownership
            var test = Prefix + @"
class C {
    void M() {
        var p = IntPtr.Zero;
        Native.Inspect(p);   // Non-owner borrow
        Native.Inspect(p);   // Still fine — no transfer occurred
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task ParameterTransferred_UsedAfter_ReportsDiagnostic()
        {
            var test = Prefix + @"
class C {
    void M(IntPtr p) {
        Native.Free(p);      // Transfer ownership of parameter
        Native.Inspect(p);   // MHP005: use after transfer
    }
}";
            var expected = VerifyCS.Diagnostic("MHP005")
                .WithArguments("p");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TopLevelStatement_TransferredUsedAfter_ReportsDiagnostic()
        {
            var test = Prefix + @"
var p = IntPtr.Zero;
Native.Free(p);      // Transfer
Native.Inspect(p);   // MHP005
";
            var expected = VerifyCS.Diagnostic("MHP005")
                .WithArguments("p");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
```

- [ ] **Step 2: Run MHP005 tests**

```bash
dotnet test src/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer.Test/Misaki.HighPerformance.Analyzer.Test.csproj --filter "FullyQualifiedName~OwnershipUseAfterTransferAnalyzerTests"
```

Expected: All 7 tests pass.

- [ ] **Step 3: Commit**

```bash
git add src/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer.Test/OwnershipUseAfterTransferAnalyzerTests.cs
git commit -m "test: add MHP005 OwnershipUseAfterTransferAnalyzer tests"
```

---

### Task 6: Remove placeholder test file and run full suite

**Files:**
- Delete: `src/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer.Test/MisakiHighPerformanceAnalyzerUnitTests.cs`

- [ ] **Step 1: Delete the placeholder test file**

```bash
Remove-Item -LiteralPath "src/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer.Test/MisakiHighPerformanceAnalyzerUnitTests.cs"
```

- [ ] **Step 2: Run full analyzer test suite**

```bash
dotnet test src/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer.Test/Misaki.HighPerformance.Analyzer.Test.csproj
```

Expected: All 14 new tests pass, no other failures.

- [ ] **Step 3: Verify no regressions in full solution build**

```bash
dotnet build src/Misaki.HighPerformance.slnx
```

Expected: Full solution builds without errors.

- [ ] **Step 4: Commit**

```bash
git rm src/Misaki.HighPerformance.Analyzer/Misaki.HighPerformance.Analyzer.Test/MisakiHighPerformanceAnalyzerUnitTests.cs
git commit -m "chore: remove placeholder test file, finalize MHP004/MHP005"
```

---

## Comparison: What to Expect from Each Diagnostic

| Scenario | MHP004 | MHP005 |
|---|---|---|
| `Free(p, p)` — same var to two `[Owner]` params | ❌ Error | — |
| `Mix(p, p)` — same var to `[Owner]` + unmarked | ❌ Error | — |
| `Free(a, b)` — different vars to `[Owner]` params | ✅ | — |
| `Inspect(p, p)` — same var to two unmarked params | ✅ | — |
| `Free(p); Inspect(p);` — use after transfer | — | ⚠ Warning |
| `Inspect(p); Free(p);` — use before transfer | — | ✅ |
| `Free(p);` (with no later use) — last use | — | ✅ |
| `Inspect(p); Inspect(p);` — no transfer at all | — | ✅ |

## Acknowledged Limitations

- **MHP005 is scope-naive.** It walks the entire containing block's text, not the live control-flow graph. Reassignments (`p = Alloc()`) between the transfer and the later use are not detected as "safe," so the warning may fire on code that is actually correct.
- **MHP005 is branch-unaware.** If the transfer happens inside one branch and the later use happens in another branch (impossible at runtime), the warning still fires.
- **MHP004 tracks only locals and parameters.** Complex expressions (method returns, property accesses) are not resolved for identity. If the same method call returns a value used in two `[Owner]` positions, the analyzer won't catch it.
- These trade-offs are intentional to keep analyzer complexity low. MHP005's `Warning` severity reflects the possibility of false positives.
