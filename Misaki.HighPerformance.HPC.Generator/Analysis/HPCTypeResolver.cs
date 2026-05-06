using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Misaki.HighPerformance.HPC.Generator.IR;
using System.Collections.Generic;

namespace Misaki.HighPerformance.HPC.Generator.Analysis
{
    /// <summary>
    /// Resolves HPC-specific types from Roslyn's semantic model.
    /// Centralises all "what is the scalar element type of this expression?"
    /// logic that was previously duplicated across <c>HPCRewriter</c> and
    /// <c>HPCOptimizerRewriter</c>.
    /// </summary>
    internal sealed class HPCTypeResolver
    {
        private readonly SemanticModel _semanticModel;

        /// <summary>
        /// Maps generic type-parameter names (e.g. <c>"TLane0"</c>) to their
        /// resolved scalar element types (e.g. <c>"float"</c>).
        /// Populated by <see cref="RegisterConstraints"/>.
        /// </summary>
        private readonly Dictionary<string, string> _typeParamToPrimitive = new();

        public HPCTypeResolver(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        // ── Type-parameter registration ───────────────────────────────────────

        /// <summary>
        /// Scans the generic constraints on <paramref name="method"/> and
        /// registers every type parameter constrained to
        /// <c>ISPMDLane&lt;TSelf, TNumber&gt;</c>.
        /// Must be called before <see cref="Resolve"/> is used on the method body.
        /// </summary>
        public void RegisterConstraints(MethodDeclarationSyntax method)
        {
            _typeParamToPrimitive.Clear();

            foreach (var clause in method.ConstraintClauses)
            {
                var typeParamName = clause.Name.Identifier.Text;

                foreach (var constraint in clause.Constraints)
                {
                    if (constraint is TypeConstraintSyntax typeConstraint &&
                        typeConstraint.Type is GenericNameSyntax generic &&
                        generic.Identifier.Text == "ISPMDLane" &&
                        generic.TypeArgumentList.Arguments.Count == 2)
                    {
                        // ISPMDLane<TSelf, TNumber> — TNumber is the scalar element type
                        var primitiveTypeName = generic.TypeArgumentList.Arguments[1].ToString();
                        _typeParamToPrimitive[typeParamName] = primitiveTypeName;
                    }
                }
            }
        }

        // ── Expression type resolution ────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="HPCType"/> for <paramref name="node"/>, or
        /// <c>null</c> if the node is not an HPC-typed expression (i.e. not a
        /// <c>WideLane&lt;T&gt;</c>, a constrained SPMD type-parameter, or a
        /// plain scalar float/double).
        /// </summary>
        public HPCType? Resolve(SyntaxNode node)
        {
            var typeInfo = _semanticModel.GetTypeInfo(node);
            var type = typeInfo.Type;
            if (type is null) return null;

            // WideLane<float>, WideLane<double>, …
            if (type.Name == "WideLane" &&
                type is INamedTypeSymbol wideLane &&
                wideLane.IsGenericType)
            {
                return HPCType.FromElementName(
                    wideLane.TypeArguments[0].ToDisplayString());
            }

            // Generic type parameter constrained to ISPMDLane<TSelf, TNumber>
            if (type is ITypeParameterSymbol typeParam)
            {
                foreach (var constraint in typeParam.ConstraintTypes)
                {
                    if (constraint.Name == "ISPMDLane" &&
                        constraint is INamedTypeSymbol namedConstraint &&
                        namedConstraint.IsGenericType)
                    {
                        return HPCType.FromElementName(
                            namedConstraint.TypeArguments[1].ToDisplayString());
                    }
                }

                // Registered from method constraints
                if (_typeParamToPrimitive.TryGetValue(typeParam.Name, out var prim))
                    return HPCType.FromElementName(prim);
            }

            // Bare scalar (used when a scalar literal/variable is involved in a
            // mixed scalar-vector expression)
            if (type.SpecialType == SpecialType.System_Single)  return HPCType.Float;
            if (type.SpecialType == SpecialType.System_Double) return HPCType.Double;
            if (type.SpecialType == SpecialType.System_Int32)  return HPCType.Int;
            if (type.SpecialType == SpecialType.System_UInt32) return HPCType.UInt;
            if (type.SpecialType == SpecialType.System_Int64)  return HPCType.Long;

            return null;
        }

        /// <summary>
        /// Resolves the type of the expression using the registered type-parameter
        /// map as a fallback (for simple identifier references where the semantic
        /// model yields a type-parameter symbol rather than a concrete type).
        /// </summary>
        public HPCType? ResolveByName(string typeName)
        {
            if (_typeParamToPrimitive.TryGetValue(typeName, out var prim))
                return HPCType.FromElementName(prim);
            return null;
        }

        /// <summary>Returns true if <paramref name="typeName"/> is a known SPMD type-param.</summary>
        public bool IsSPMDTypeParam(string typeName) =>
            _typeParamToPrimitive.ContainsKey(typeName);

        /// <summary>Snapshot of current type-param → primitive mappings (for reference).</summary>
        public IReadOnlyDictionary<string, string> TypeParamMap => _typeParamToPrimitive;
    }
}
