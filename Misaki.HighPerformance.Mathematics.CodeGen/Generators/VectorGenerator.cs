using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Misaki.HighPerformance.Mathematics.CodeGen.Generators
{
    internal class VectorGenerator : GeneratorBase
    {
        private readonly string[] _vectorComponents = new[] { "x", "y", "z", "w" };
        private int _vectorBitsSize;
        private int _missingComponents;

        private readonly List<(string signature, string assignment)> _constructorSignatures = new();

        protected override void Initialize()
        {
            var componentSize = typeInfo.ComponentSize;
            var typeSize = componentSize * typeInfo.Row * typeInfo.Column;
            var vectorBytesSize = typeSize switch
            {
                <= 16 => 16,
                <= 32 => 32,
                _ => 64,
            };

            _vectorBitsSize = vectorBytesSize * 8;
            _missingComponents = (vectorBytesSize - typeSize) / componentSize;
        }

        protected override void GenerateBody()
        {
            GenerateField();
            if (typeInfo.Arithmetic)
            {
                GenerateUnitVector();
            }
            GenerateConstructors();
            GenerateUnsafeMethod();
            GenerateOverrideMethod();
            if (typeInfo.Arithmetic)
            {
                GenerateArithmeticOperators();
            }
            if (!string.IsNullOrEmpty(typeInfo.TypePrefix))
            {
                GenerateSwizzleProperties();
            }
        }

        protected override void GenerateTypeEnd()
        {
            base.GenerateTypeEnd();

            if (typeInfo.Arithmetic)
            {
                sourceBuilder.AppendLine();
                GenerateVectorExtension();
                GenerateMathMethod();
            }
        }

        private void GenerateField()
        {
            for (var i = 0; i < typeInfo.Row; i++)
            {
                sourceBuilder.Append($@"
        public {typeInfo.ComponentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {_vectorComponents[i]};");
            }

            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($@"
        public unsafe ref {typeInfo.ComponentTypeFullName} this[int index]
        {{
            {INLINE_METHOD_ATTRIBUTE}
            get => ref (({typeInfo.ComponentTypeFullName}*)global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref this))[index];
        }}");
        }

        private static List<List<int>> GetPartitions(int target)
        {
            var result = new List<List<int>>();
            void Recurse(List<int> current, int sum)
            {
                if (sum == target)
                {
                    result.Add(new List<int>(current));
                    return;
                }
                for (var i = 1; i <= 3; i++)
                {
                    if (sum + i <= target)
                    {
                        current.Add(i);
                        Recurse(current, sum + i);
                        current.RemoveAt(current.Count - 1);
                    }
                }
            }
            Recurse(new(), 0);
            return result;
        }

        private static IEnumerable<List<int>> GetPermutations(List<int> list)
        {
            if (list.Count == 1)
                yield return new List<int>(list);
            else
            {
                var seen = new HashSet<string>();
                for (var i = 0; i < list.Count; i++)
                {
                    var head = list[i];
                    var tail = new List<int>(list);
                    tail.RemoveAt(i);
                    foreach (var perm in GetPermutations(tail))
                    {
                        perm.Insert(0, head);
                        var key = string.Join(",", perm);
                        if (seen.Add(key))
                            yield return perm;
                    }
                }
            }
        }

        private void GenerateUnitVector()
        {
            var typeFullName = typeInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            sourceBuilder.Append($@"
        public static {typeFullName} one => new {typeFullName}({string.Join(", ", Enumerable.Repeat("1", typeInfo.Row))});");
            sourceBuilder.Append($@"
        public static {typeFullName} zero => default;");
            sourceBuilder.Append($@"
        public static {typeFullName} unitX => new {typeFullName}({string.Join(", ", Enumerable.Repeat("0", typeInfo.Row - 1).Prepend("1"))});");
            sourceBuilder.Append($@"
        public static {typeFullName} unitY => new {typeFullName}({string.Join(", ", Enumerable.Repeat("0", typeInfo.Row - 2).Prepend("0, 1"))});");

            if (typeInfo.Row > 2)
            {
                sourceBuilder.Append($@"
        public static {typeFullName} unitZ => new {typeFullName}({string.Join(", ", Enumerable.Repeat("0", typeInfo.Row - 3).Prepend("0, 0, 1"))});");
            }

            if (typeInfo.Row > 3)
            {
                sourceBuilder.Append($@"
        public static {typeFullName} unitW => new {typeFullName}({string.Join(", ", Enumerable.Repeat("0", typeInfo.Row - 4).Prepend("0, 0, 0, 1"))});");
            }

            sourceBuilder.AppendLine();
        }

        private void GenerateConstructors()
        {
            var typeName = typeInfo.TypeName;
            var componentType = typeInfo.ComponentTypeFullName;

            sourceBuilder.AppendLine($@"
        public unsafe {typeName}(global::System.ReadOnlySpan<{componentType}> values)
        {{
            if (values.Length < {typeInfo.Row})
            {{
                throw new global::System.ArgumentException($""Expected at least {typeInfo.Row} values, but got {{values.Length}}"", nameof(values));
            }}

            fixed ({typeName}* pThis = &this)
            fixed ({componentType}* pValues = values)
            {{
                *pThis = *({typeName}*)pValues;
            }}
        }}");

            sourceBuilder.AppendLine($@"
        public {typeName}({componentType} value)
        {{
            this = Create({string.Join(", ", Enumerable.Range(0, typeInfo.Row).Select(_ => "value"))});
        }}");

            if (string.IsNullOrEmpty(typeInfo.TypePrefix))
            {
                return;
            }

            var partitions = GetPartitions(typeInfo.Row);
            var seenSignatures = new HashSet<string>();

            foreach (var partition in partitions)
            {
                foreach (var perm in GetPermutations(partition))
                {
                    var paramNames = new List<string>();
                    var paramList = new List<string>();
                    var assignments = new List<string>();
                    var fieldOffset = 0;
                    for (var i = 0; i < perm.Count; i++)
                    {
                        var size = perm[i];
                        var type = size == 1 ? componentType : $"{typeInfo.TypePrefix}{size}";
                        var name = string.Empty;
                        for (var j = 0; j < size; j++)
                        {
                            name += $"{_vectorComponents[fieldOffset + j]}";
                        }
                        paramNames.Add(name);
                        paramList.Add($"{type} {name}");
                        for (var j = 0; j < size; j++)
                        {
                            var source = size == 1 ? name : $"{name}.{_vectorComponents[j]}";
                            assignments.Add(source);
                        }
                        fieldOffset += size;
                    }

                    var signature = string.Join(", ", paramList);
                    if (!seenSignatures.Add(signature))
                        continue;

                    var assignment = string.Join(", ", assignments);
                    _constructorSignatures.Add((signature, assignment));
                }
            }

            foreach (var (signature, assignment) in _constructorSignatures)
            {
                sourceBuilder.AppendLine($@"
        public {typeName}({signature})
        {{
            this = Create({assignment});
        }}");
            }

            sourceBuilder.Append($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} Create({string.Join(", ", Enumerable.Range(0, typeInfo.Row).Select((_, i) => $"{typeInfo.ComponentTypeFullName} {_vectorComponents[i]}"))})
        {{");

            if (typeInfo.VectorType != null)
            {
                sourceBuilder.Append($@"
            global::System.Span<{typeInfo.ComponentTypeFullName}> span = [{string.Join(", ", Enumerable.Range(0, typeInfo.Row + _missingComponents).Select(i => i < typeInfo.Row ? $"{_vectorComponents[i]}" : "default"))}];
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Create(global::System.Runtime.CompilerServices.Unsafe.As<global::System.Span<{typeInfo.ComponentTypeFullName}>, global::System.ReadOnlySpan<{typeInfo.VectorTypeFullName}>>(ref span));");
            }
            else
            {
                sourceBuilder.Append($@"
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Create({string.Join(", ", Enumerable.Range(0, typeInfo.Row + _missingComponents).Select(i => i < typeInfo.Row ? $"{_vectorComponents[i]}" : "default"))});");
            }

            sourceBuilder.AppendLine($@"
            ref var address = ref global::System.Runtime.CompilerServices.Unsafe.As<global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}<{typeInfo.VectorTypeFullName}>, byte>(ref vector);
            return global::System.Runtime.CompilerServices.Unsafe.ReadUnaligned<{typeInfo.TypeFullName}>(ref address);
        }}");
        }

        private void GenerateUnsafeMethod()
        {
            var componentType = typeInfo.ComponentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public unsafe {componentType}* AsPointer()
        {{
            return ({componentType}*)global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref this);
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public unsafe global::System.Span<{componentType}> AsSpan()
        {{
            return new global::System.Span<{componentType}>(AsPointer(), {typeInfo.Row});
        }}");
        }

        private void GenerateOverrideMethod()
        {
            var typeName = typeInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var componentType = typeInfo.ComponentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var components = _vectorComponents.Take(typeInfo.Row).ToArray();
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public override readonly string ToString()
        {{
            return $""({string.Join(", ", components.Select(c => $"{c}: {{this.{c}}}"))})"";
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public override readonly int GetHashCode()
        {{
            return global::System.HashCode.Combine({string.Join(", ", components.Select(c => $"this.{c}"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public override readonly bool Equals(object? obj)
        {{
            return obj is {typeName} value && Equals(value);
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public readonly bool Equals({typeName} other)
        {{
            return {string.Join(" && ", components.Select(c => $"this.{c}.Equals(other.{c})"))};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static global::Misaki.HighPerformance.Mathematics.bool{typeInfo.Row} operator ==({typeName} lhs, {typeName} rhs)
        {{
            return new({string.Join(", ", components.Select(c => $"lhs.{c} == rhs.{c}"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static global::Misaki.HighPerformance.Mathematics.bool{typeInfo.Row} operator !=({typeName} lhs, {typeName} rhs)
        {{
            return new({string.Join(", ", components.Select(c => $"lhs.{c} != rhs.{c}"))});
        }}
        
        {INLINE_METHOD_ATTRIBUTE}
        public static implicit operator {typeName}(global::System.ReadOnlySpan<{componentType}> value)
        {{
            return new {typeName}(value);
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static implicit operator {typeName}({componentType} value)
        {{
            return new(value);
        }}");
        }

        private void GenerateArithmeticOperators()
        {
            var typeName = typeInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var typeSimpleName = typeInfo.TypeSymbol.Name;
            var componentType = typeInfo.ComponentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var asResult = $"As{typeSimpleName}()";

            // Add
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator +({typeName} lhs, {typeName} rhs)
        {{
            var vector = lhs.AsVector{_vectorBitsSize}() + rhs.AsVector{_vectorBitsSize}();
            return vector.{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator +({typeName} lhs, {componentType} rhs)
        {{
            return lhs + new {typeName}(rhs);
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator +({componentType} lhs, {typeName} rhs)
        {{
            return new {typeName}(lhs) + rhs;
        }}");

            // Subtract
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator -({typeName} lhs, {typeName} rhs)
        {{
            var vector = lhs.AsVector{_vectorBitsSize}() - rhs.AsVector{_vectorBitsSize}();
            return vector.{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator -({typeName} lhs, {componentType} rhs)
        {{
            return lhs - new {typeName}(rhs);
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator -({componentType} lhs, {typeName} rhs)
        {{
            return new {typeName}(lhs) - rhs;
        }}");

            // Multiply
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator *({typeName} lhs, {typeName} rhs)
        {{
            var vector = lhs.AsVector{_vectorBitsSize}() * rhs.AsVector{_vectorBitsSize}();
            return vector.{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator *({typeName} lhs, {componentType} rhs)
        {{
            var vector = lhs.AsVector{_vectorBitsSize}() * rhs;
            return vector.{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator *({componentType} lhs, {typeName} rhs)
        {{
            return new {typeName}(lhs) * rhs;
        }}");

            // Divide
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator /({typeName} lhs, {typeName} rhs)
        {{
            var vector = lhs.AsVector{_vectorBitsSize}() / rhs.AsVector{_vectorBitsSize}();
            return vector.{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator /({typeName} lhs, {componentType} rhs)
        {{
            var vector = lhs.AsVector{_vectorBitsSize}() / rhs;
            return vector.{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator /({componentType} lhs, {typeName} rhs)
        {{
            return new {typeName}(lhs) / rhs;
        }}");

            // Modulus
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator %({typeName} lhs, {typeName} rhs)
        {{
            return new {typeName}({string.Join(", ", _vectorComponents.Take(typeInfo.Row).Select(c => $"lhs.{c} % rhs.{c}"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator %({typeName} lhs, {componentType} rhs)
        {{
            return lhs % new {typeName}(rhs);
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator %({componentType} lhs, {typeName} rhs)
        {{
            return new {typeName}(lhs) % rhs;
        }}");

            // Unary operators
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator +({typeName} value)
        {{
            return value;
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator -({typeName} value)
        {{
            var vector = -value.AsVector{_vectorBitsSize}();
            return vector.{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator ++({typeName} value)
        {{
            var vector = value.AsVector{_vectorBitsSize}() + global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Create<{componentType}>(1);
            return vector.{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator --({typeName} value)
        {{
            var vector = value.AsVector{_vectorBitsSize}() - global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Create<{componentType}>(1);
            return vector.{asResult};
        }}");

            // Comparison operators
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static bool{typeInfo.Row} operator <({typeName} lhs, {typeName} rhs)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.LessThan(lhs.AsVector{_vectorBitsSize}(), rhs.AsVector{_vectorBitsSize}());
            return new({string.Join(", ", _vectorComponents.Take(typeInfo.Row).Select((_, i) => $"vector[{i}] != 0"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static bool{typeInfo.Row} operator <=({typeName} lhs, {typeName} rhs)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.LessThanOrEqual(lhs.AsVector{_vectorBitsSize}(), rhs.AsVector{_vectorBitsSize}());
            return new({string.Join(", ", _vectorComponents.Take(typeInfo.Row).Select((_, i) => $"vector[{i}] != 0"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static bool{typeInfo.Row} operator >({typeName} lhs, {typeName} rhs)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.GreaterThan(lhs.AsVector{_vectorBitsSize}(), rhs.AsVector{_vectorBitsSize}());
            return new({string.Join(", ", _vectorComponents.Take(typeInfo.Row).Select((_, i) => $"vector[{i}] != 0"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static bool{typeInfo.Row} operator >=({typeName} lhs, {typeName} rhs)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.GreaterThanOrEqual(lhs.AsVector{_vectorBitsSize}(), rhs.AsVector{_vectorBitsSize}());
            return new({string.Join(", ", _vectorComponents.Take(typeInfo.Row).Select((_, i) => $"vector[{i}] != 0"))});
        }}");

            // Bitwise operators
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator <<({typeName} x, int n)
        {{
            var vector = x.AsVector{_vectorBitsSize}() << n;
            return vector.{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator >>({typeName} x, int n)
        {{
            var vector = x.AsVector{_vectorBitsSize}() >> n;
            return vector.{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator &({typeName} lhs, {typeName} rhs)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.BitwiseAnd(lhs.AsVector{_vectorBitsSize}(), rhs.AsVector{_vectorBitsSize}());
            return vector.{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator |({typeName} lhs, {typeName} rhs)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.BitwiseOr(lhs.AsVector{_vectorBitsSize}(), rhs.AsVector{_vectorBitsSize}());
            return vector.{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator ^({typeName} lhs, {typeName} rhs)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Xor(lhs.AsVector{_vectorBitsSize}(), rhs.AsVector{_vectorBitsSize}());
            return vector.{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator ~({typeName} value)
        {{
            unchecked
            {{
                return value ^ new {typeName}(({componentType})-1);
            }}
        }}");
        }

        private IEnumerable<string> GenerateSwizzles(IEnumerable<string> pool, int maxLen)
        {
            IEnumerable<string> Recurse(string prefix, int depth)
            {
                if (depth == 0)
                {
                    yield return prefix;
                }
                else
                {
                    foreach (var c in pool)
                    {
                        foreach (var s in Recurse(prefix + c, depth - 1))
                        {
                            yield return s;
                        }
                    }
                }
            }
            return Enumerable.Range(2, maxLen - 1).SelectMany(len => Recurse(string.Empty, len));
        }

        private void GenerateSwizzleProperties()
        {
            var validComponents = _vectorComponents.Take(typeInfo.Row).ToArray();
            var swizzles = GenerateSwizzles(validComponents, _vectorComponents.Length);
            foreach (var swizzle in swizzles)
            {
                var targetDim = swizzle.Length;
                var targetStruct = $"{typeInfo.TypePrefix}{targetDim}";
                sourceBuilder.AppendLine($@"
        public readonly {targetStruct} {swizzle} 
        {{
            {INLINE_METHOD_ATTRIBUTE}
            get => new({string.Join(", ", swizzle.Select(c => $"this.{c}"))});
        }}");
            }
        }

        private void GenerateVectorExtension()
        {
            var typeName = typeInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var typeSimpleName = typeInfo.TypeSymbol.Name;
            var componentType = typeInfo.ComponentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            sourceBuilder.Append($@"
    public static partial class VectorInterop
    {{
        {INLINE_METHOD_ATTRIBUTE}
        public static global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}<{componentType}> AsVector{_vectorBitsSize}(this {typeName} value)
        {{");
            var fullTypeName = $"{typeInfo.TypePrefix}{typeInfo.Row + _missingComponents}";
            sourceBuilder.Append($@"
            return global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Create({string.Join(", ", Enumerable.Range(0, typeInfo.Row + _missingComponents).Select(i => i < typeInfo.Row ? $"value.{_vectorComponents[i]}" : "1"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} As{typeSimpleName}(this global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}<{componentType}> value)
        {{");
            sourceBuilder.AppendLine($@"
            ref var address = ref global::System.Runtime.CompilerServices.Unsafe.As<global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}<{componentType}>, byte>(ref value);
            return global::System.Runtime.CompilerServices.Unsafe.ReadUnaligned<{typeName}>(ref address);
        }}
    }}");
        }

        private void GenerateMathMethod()
        {
            var typeFullName = typeInfo.TypeFullName;
            var typePrefix = typeInfo.TypePrefix;
            var componentTypeFullName = typeInfo.ComponentTypeFullName;

            sourceBuilder.Append($@"
    public static partial class math
    {{");

            sourceBuilder.AppendLine($@"
        public static {typeFullName} {typeInfo.TypeName}({componentTypeFullName} value)
        {{
            return {typeFullName}.Create({string.Join(", ", Enumerable.Range(0, typeInfo.Row).Select(_ => "value"))});
        }}");

            foreach (var (signature, assignment) in _constructorSignatures)
            {
                sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} {typeInfo.TypeName}({signature})
        {{
            return {typeFullName}.Create({assignment});
        }}");
            }

            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {componentTypeFullName} shuffle({typeFullName} left, {typeFullName} right, ShuffleComponent x)
        {{
            return select_shuffle_component(left, right, x);
        }}");

            for (var i = 1; i < typeInfo.Row; i++)
            {
                var dimension = i + 1;
                sourceBuilder.Append($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typePrefix}{dimension} shuffle({typeFullName} left, {typeFullName} right, {string.Join(", ", Enumerable.Range(0, dimension).Select(x => $"ShuffleComponent {_vectorComponents[x]}"))})
        {{
            return new {typePrefix}{dimension}(");
                for (var j = 0; j < dimension; j++)
                {
                    sourceBuilder.Append($@"
                select_shuffle_component(left, right, {_vectorComponents[j]})");
                    if (j < dimension - 1)
                    {
                        sourceBuilder.Append(",");
                    }
                    else
                    {
                        sourceBuilder.Append(");");
                    }
                }
                sourceBuilder.AppendLine($@"
        }}");
            }

            sourceBuilder.Append($@"
        {INLINE_METHOD_ATTRIBUTE}
        internal static {componentTypeFullName} select_shuffle_component({typeFullName} a, {typeFullName} b, ShuffleComponent component)
        {{
            switch(component)
            {{");

            for (var i = 0; i < typeInfo.Row; i++)
            {
                sourceBuilder.Append($@"
                case ShuffleComponent.Left{_vectorComponents[i].ToUpper()}:
                    return a.{_vectorComponents[i]};
                case ShuffleComponent.Right{_vectorComponents[i].ToUpper()}:
                    return b.{_vectorComponents[i]};");
            }

            sourceBuilder.Append($@"
                default:
                    throw new System.ArgumentException(""Invalid shuffle component: "" + component);
            }}
        }}
    }}");
        }
    }
}