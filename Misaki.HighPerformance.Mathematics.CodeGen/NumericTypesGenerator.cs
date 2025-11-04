using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Misaki.HighPerformance.Mathematics.CodeGen.Generators;
using Misaki.HighPerformance.Mathematics.CodeGen.Models;
using System.Linq;

namespace Misaki.HighPerformance.Mathematics.CodeGen
{
    [Generator]
    internal class NumericTypeGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Create a provider that finds all types with NumericTypeAttribute
            var typesWithAttribute = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    fullyQualifiedMetadataName: typeof(NumericTypeAttribute).FullName,
                    predicate: static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax,
                    transform: static (context, _) => GetTypeInfo(context))
                .Where(static typeInfo => typeInfo is not null);

            // Register the source output
            context.RegisterSourceOutput(typesWithAttribute.Collect(), (spc, types) =>
            {
                foreach (var typeInfo in types)
                {
                    if (typeInfo is null)
                        continue;

                    var generator = GetGenerator(typeInfo.Column);
                    var source = generator.Generate(typeInfo);
                    spc.AddSource($"{typeInfo.TypeSymbol.Name}.g.cs", source);
                }
            });
        }

        private static GeneratorBase GetGenerator(int column)
        {
            return column switch
            {
                1 => new VectorGenerator(),
                _ => new MatrixGenerator(),
            };
        }

        private static NumericTypeInfo? GetTypeInfo(GeneratorAttributeSyntaxContext context)
        {
            if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            {
                return null;
            }

            // Get the attribute data
            var attribute = typeSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == typeof(NumericTypeAttribute).FullName);

            var convertableAttributes = typeSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == typeof(NumericConvertableAttribute).FullName);

            if (attribute == null)
            {
                return null;
            }

            var index = 0;

            var componentType = (INamedTypeSymbol)attribute.ConstructorArguments[index++].Value!;
            var componentSize = (int)attribute.ConstructorArguments[index++].Value!;
            var row = (int)attribute.ConstructorArguments[index++].Value!;
            var column = (int)attribute.ConstructorArguments[index++].Value!;
            var typePrefix = (string)attribute.ConstructorArguments[index++].Value!;
            var arithmetic = (bool)attribute.ConstructorArguments[index++].Value!;
            var canInverse = (bool)attribute.ConstructorArguments[index++].Value!;
            var elementType = (INamedTypeSymbol?)attribute.ConstructorArguments[index++].Value;
            var vectorType = (INamedTypeSymbol?)attribute.ConstructorArguments.ElementAtOrDefault(index++).Value;

            var info = new NumericTypeInfo(typeSymbol, componentType, componentSize, row, column, typePrefix, arithmetic, canInverse, elementType, vectorType);

            if (convertableAttributes != null)
            {
                foreach (var convertableAttribute in convertableAttributes)
                {
                    var template = (string)convertableAttribute.ConstructorArguments[0].Value!;
                    var types = convertableAttribute.ConstructorArguments[1].Values
                        .Select(v => (INamedTypeSymbol)v.Value!)
                        .ToArray();

                    info.ConvertableTypes ??= new();
                    info.ConvertableTypes[template] = types;
                }
            }

            return info;
        }
    }
}