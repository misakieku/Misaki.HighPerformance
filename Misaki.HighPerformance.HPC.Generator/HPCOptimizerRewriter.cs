using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace Misaki.HighPerformance.HPC.Generator
{
    internal class HPCOptimizerRewriter : CSharpSyntaxRewriter
    {
        private readonly Dictionary<string, string> _spmdTypes = new();
        private readonly SemanticModel _semanticModel;

        public HPCOptimizerRewriter(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        private bool IsKnownHpcType(ITypeSymbol? type)
        {
            if (type == null)
            {
                return false;
            }

            // Check if it's WideLane, or one of the mapped TLane0 constraints
            if (type.Name == "WideLane")
            {
                return true;
            }

            if (_spmdTypes.ContainsKey(type.Name))
            {
                return true;
            }

            return false;
        }

        protected string? GetHpcPrimitiveType(SyntaxNode originalNode)
        {
            var typeInfo = semanticModel.GetTypeInfo(originalNode);
            var type = typeInfo.Type;

            if (type == null)
            {
                return null;
            }

            if (string.Equals(type.Name, "WideLane") && type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                // Returns "Single" (float) or "Double" (double)
                return namedType.TypeArguments[0].ToDisplayString();
            }

            if (type is ITypeParameterSymbol typeParam)
            {
                // Inspect the `where TLane0 : ISPMDLane<TLane0, float>` constraints!
                foreach (var constraint in typeParam.ConstraintTypes)
                {
                    if (constraint.Name == "ISPMDLane" && constraint is INamedTypeSymbol constraintNamed && constraintNamed.IsGenericType)
                    {
                        // The second generic argument is the primitive format (float/double)
                        return constraintNamed.TypeArguments[1].ToDisplayString();
                    }
                }
            }

            if (type.SpecialType == SpecialType.System_Single)
            {
                return "float";
            }

            if (type.SpecialType == SpecialType.System_Double)
            {
                return "double";
            }

            return null;
        }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            // Rewrites signature types and generic types from `TLane0` to `Vector256<float>`
            if (_spmdTypes.TryGetValue(node.Identifier.Text, out var primType))
            {
                return SyntaxFactory.GenericName("Vector256")
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                SyntaxFactory.IdentifierName(primType))))
                    .WithTriviaFrom(node);
            }

            return base.VisitIdentifierName(node);
        }
    }
}
