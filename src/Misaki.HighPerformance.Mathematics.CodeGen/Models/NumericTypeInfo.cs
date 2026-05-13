using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Misaki.HighPerformance.Mathematics.CodeGen.Models
{
    internal record NumericTypeInfo
    {
        public INamedTypeSymbol TypeSymbol
        {
            get;
        }

        public INamedTypeSymbol ComponentTypeSymbol
        {
            get;
        }

        public int ComponentSize
        {
            get;
        }

        public int Row
        {
            get;
        }

        public int Column
        {
            get;
        }

        public string TypePrefix
        {
            get;
        }

        public bool Arithmetic
        {
            get;
        }

        public bool CanInverse
        {
            get;
        }

        public INamedTypeSymbol? ElementTypeSymbol
        {
            get;
        }

        public INamedTypeSymbol? VectorType
        {
            get;
        }

        public Dictionary<string, INamedTypeSymbol[]>? ConvertableTypes
        {
            get;
            set;
        }

        public string TypeName => TypeSymbol.Name;
        public string ComponentTypeName => ComponentTypeSymbol.Name;
        public string TypeFullName => TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        public string ComponentTypeFullName => ComponentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        public string? ElementTypeName => ElementTypeSymbol?.Name;
        public string? ElementTypeFullName => ElementTypeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        public string VectorTypeFullName => VectorType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? ComponentTypeFullName;

        public NumericTypeInfo(INamedTypeSymbol typeSymbol, INamedTypeSymbol componentType, int componentSize, int row, int column, string typePrefix,
            bool arithmetic, bool canInverse, INamedTypeSymbol? elementType, INamedTypeSymbol? vectorType)
        {
            TypeSymbol = typeSymbol;
            ComponentTypeSymbol = componentType;
            ComponentSize = componentSize;
            Row = row;
            Column = column;
            TypePrefix = typePrefix;
            Arithmetic = arithmetic;
            CanInverse = canInverse;
            ElementTypeSymbol = elementType;
            VectorType = vectorType;
        }
    }
}
