using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Misaki.HighPerformance.HPC.Generator
{
    internal enum SIMDInstruction
    {
        Add,
        Subtract,
        Multiply,
        MultiplyAdd,

        Asin,
        Atan2,
    }

    internal abstract class HPCRewriter : CSharpSyntaxRewriter
    {
        protected struct MathExpression
        {
            public string Expression
            {
                get; set;
            }

            public string Name
            {
                get; set;
            }
        }

        public static IReadOnlyCollection<HPCRewriter> GetRewriter(TargetInstructionSet instructionSet)
        {
            var rewriters = new List<HPCRewriter>();

            // TODO: Add more rewriters for different instruction sets
            if (instructionSet.HasFlag(TargetInstructionSet.AVX2))
            {
                rewriters.Add(new AVX2Rewriter());
            }

            return rewriters;
        }

        private static readonly Dictionary<string, string> s_remapProperties = new()
        {
            ["LaneWidth"] = "Count",
        };

        private static readonly Dictionary<string, SIMDInstruction> s_remapMath = new()
        {
            ["Add"] = SIMDInstruction.Add,
            ["Subtract"] = SIMDInstruction.Subtract,
            ["Multiply"] = SIMDInstruction.Multiply,
            ["MultiplyAdd"] = SIMDInstruction.MultiplyAdd,
            ["Asin"] = SIMDInstruction.Asin,
            ["Atan2"] = SIMDInstruction.Atan2,
        };

        protected readonly Dictionary<string, string> spmdTypes = new();

        public abstract string Name
        {
            get;
        }

        public virtual string GetNesessaryUsing()
        {
            return string.Empty;
        }

        public override SyntaxNode? VisitAttributeList(AttributeListSyntax node)
        {
            var filteredAttributes = SyntaxFactory.SeparatedList(
                node.Attributes.Where(a => !a.Name.ToString().Contains("HPCompute"))
            );

            if (filteredAttributes.Count == 0)
            {
                return null;
            }

            return node.WithAttributes(filteredAttributes).WithTriviaFrom(node);
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var typesToRemove = new HashSet<string>();

            // 1. Analyze constraints to identify ISPMDLane generics
            foreach (var clause in node.ConstraintClauses)
            {
                var typeNameStr = clause.Name.Identifier.Text;
                foreach (var constraint in clause.Constraints.OfType<TypeConstraintSyntax>())
                {
                    if (constraint.Type is GenericNameSyntax genericType &&
                        genericType.Identifier.Text == "ISPMDLane" &&
                        genericType.TypeArgumentList.Arguments.Count == 2)
                    {
                        var primType = genericType.TypeArgumentList.Arguments[1].ToString();
                        spmdTypes[typeNameStr] = primType;
                        typesToRemove.Add(typeNameStr);
                    }
                }
            }

            var methodToVisit = node;

            // 2. Strip type parameter and constraints BEFORE visiting so VisitIdentifierName doesn't touch them
            if (typesToRemove.Count > 0)
            {
                // Remove from <TLane0, ...> generics list
                if (methodToVisit.TypeParameterList != null)
                {
                    var newParams = methodToVisit.TypeParameterList.Parameters
                        .Where(p => !typesToRemove.Contains(p.Identifier.Text))
                        .ToList();

                    if (newParams.Any())
                    {
                        methodToVisit = methodToVisit.WithTypeParameterList(
                            SyntaxFactory.TypeParameterList(SyntaxFactory.SeparatedList(newParams))
                        );
                    }
                    else
                    {
                        methodToVisit = methodToVisit.WithTypeParameterList(null); // Removes angle brackets entirely
                    }
                }

                // Remove the matching `where TLane0 : ...` clause
                var newConstraints = methodToVisit.ConstraintClauses
                    .Where(c => !typesToRemove.Contains(c.Name.Identifier.Text))
                    .ToList();

                methodToVisit = methodToVisit.WithConstraintClauses(
                    SyntaxFactory.List(newConstraints)
                );
            }

            // 3. Fallback to base to rewrite method arguments, return types, and body via our updated visitors
            return base.VisitMethodDeclaration(methodToVisit);
        }

        public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
        {
            if (node.Identifier.Text == "WideLane" &&
                node.TypeArgumentList.Arguments.Count == 1)
            {
                return SyntaxFactory.GenericName("Vector256")
                    .WithTypeArgumentList(node.TypeArgumentList)
                    .WithTriviaFrom(node);
            }

            return base.VisitGenericName(node);
        }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            // Rewrites signature types and generic types from `TLane0` to `Vector256<float>`
            if (spmdTypes.TryGetValue(node.Identifier.Text, out var primType))
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

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            var isSpmdOrWideLane = false;
            var isFloatingPoint = false;

            // 1. Check if the left-side expression is WideLane<...> or a tracked generic SPMD type
            if (node.Expression is GenericNameSyntax genericName &&
                genericName.Identifier.Text == "WideLane" &&
                genericName.TypeArgumentList.Arguments.Count == 1)
            {
                isSpmdOrWideLane = true;

                var argTypeStr = genericName.TypeArgumentList.Arguments[0].ToString();
                isFloatingPoint = argTypeStr == "float" || argTypeStr == "double";
            }
            else if (node.Expression is IdentifierNameSyntax idName &&
                     spmdTypes.TryGetValue(idName.Identifier.Text, out var mappedPrimType))
            {
                isSpmdOrWideLane = true;
                isFloatingPoint = mappedPrimType == "float" || mappedPrimType == "double";
            }

            if (isSpmdOrWideLane)
            {
                if (s_remapProperties.TryGetValue(node.Name.Identifier.Text, out var remappedName))
                {
                    // Keep the evaluated left-hand side (TLane0 -> Vector256<float>) but change the property
                    var rewrittenExpression = (ExpressionSyntax)Visit(node.Expression);

                    return SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        rewrittenExpression,
                        SyntaxFactory.IdentifierName(remappedName)
                    ).WithTriviaFrom(node);
                }

                if (s_remapMath.TryGetValue(node.Name.Identifier.Text, out var instruction))
                {
                    var rewritResult = RewriteMathExpression(instruction, isFloatingPoint);
                    return SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(rewritResult.Expression),
                        SyntaxFactory.IdentifierName(rewritResult.Name)
                    ).WithTriviaFrom(node);
                }
            }

            return base.VisitMemberAccessExpression(node);
        }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                bool isSpmdOrWideLane = false;

                if (memberAccess.Expression is GenericNameSyntax genericName
                    && genericName.Identifier.Text == "WideLane"
                    && genericName.TypeArgumentList.Arguments.Count == 1)
                {
                    isSpmdOrWideLane = true;
                }
                else if (memberAccess.Expression is IdentifierNameSyntax idName
                         && spmdTypes.TryGetValue(idName.Identifier.Text, out var mappedPrimType))
                {
                    isSpmdOrWideLane = true;
                }

                if (isSpmdOrWideLane)
                {
                    var args = node.ArgumentList.Arguments;
                    var argList = new ArgumentSyntax[args.Count];

                    for (var i = 0; i < args.Count; i++)
                    {
                        argList[i] = (ArgumentSyntax)Visit(args[i]);
                    }

                    if (s_remapMath.TryGetValue(memberAccess.Name.Identifier.Text, out var instruction))
                    {
                        RewriteMathArguments(instruction, argList);
                        var arguments = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(argList));

                        var newExpression = (ExpressionSyntax)Visit(memberAccess);
                        return SyntaxFactory.InvocationExpression(newExpression, arguments)
                            .WithTriviaFrom(node);
                    }
                }
            }

            return base.VisitInvocationExpression(node);
        }

        protected abstract MathExpression RewriteMathExpression(SIMDInstruction instruction, bool isFloatingPoint);
        protected abstract void RewriteMathArguments(SIMDInstruction instruction, Span<ArgumentSyntax> originalArgs);
    }
}
