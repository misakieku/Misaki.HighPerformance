using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Misaki.HighPerformance.Mathematics.CodeGen.Generators
{
    internal class VectorGenerator : GeneratorBase
    {
        private int _vectorBitsSize;
        private int _missingComponents;
        private string _componentTypePrefix = null!;

        private readonly List<(string signature, List<string> assignment)> _constructorSignatures = new();

        private string GetConversionFromTemplate(string template, int componentIndex)
        {
            return template.Replace("{v}", "v")
                .Replace("{c}", s_vectorComponents[componentIndex]);
        }

        protected override void Initialize()
        {
            var componentSize = typeInfo.ComponentSize;
            var typeSize = componentSize * typeInfo.Row * typeInfo.Column;
            var vectorBytesSize = typeSize switch
            {
                //<= 8 => 8,
                <= 16 => 16,
                <= 32 => 32,
                _ => 64,
            };

            _vectorBitsSize = vectorBytesSize * 8;
            _missingComponents = (vectorBytesSize - typeSize) / componentSize;

            _componentTypePrefix = typeInfo.ComponentTypeSymbol.SpecialType switch
            {
                SpecialType.System_UInt16 or SpecialType.System_UInt32 or SpecialType.System_UInt64 => "u",
                SpecialType.System_Single => "f",
                SpecialType.System_Double => "d",
                _ => string.Empty
            };
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
            GenerateConvertionMethod();

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

            sourceBuilder.AppendLine();
            if (typeInfo.Arithmetic)
            {
                GenerateVectorExtension();
            }

            GenerateMathMethod();
        }

        private void GenerateField()
        {
            for (var i = 0; i < typeInfo.Row; i++)
            {
                sourceBuilder.Append($@"
        public {typeInfo.ComponentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {s_vectorComponents[i]};");
            }

            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($@"
        public unsafe ref {typeInfo.ComponentTypeFullName} this[int index]
        {{
            {INLINE_METHOD_ATTRIBUTE}
            get 
            {{
#if ENABLE_COLLECTION_CHECKS
                if (index < 0 || index >= {typeInfo.Row})
                {{
                    throw new global::System.ArgumentOutOfRangeException(nameof(index), $""Index {{index}} is out of range of '{typeInfo.TypeName}'"");
                }}
#endif
                return ref (({typeInfo.ComponentTypeFullName}*)global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref this))[index];
            }}
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

            StartRegion("Constructors");

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

            _constructorSignatures.Add((
                signature: $"{componentType} value",
                assignment: Enumerable.Range(0, typeInfo.Row).Select(_ => "value").ToList()));

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
                            name += $"{s_vectorComponents[fieldOffset + j]}";
                        }
                        paramNames.Add(name);
                        paramList.Add($"{type} {name}");
                        for (var j = 0; j < size; j++)
                        {
                            var source = size == 1 ? name : $"{name}.{s_vectorComponents[j]}";
                            assignments.Add(source);
                        }
                        fieldOffset += size;
                    }

                    var signature = string.Join(", ", paramList);
                    if (!seenSignatures.Add(signature))
                    {
                        continue;
                    }

                    _constructorSignatures.Add((signature, assignments));
                }
            }

            if (typeInfo.ConvertableTypes != null)
            {
                foreach (var kv in typeInfo.ConvertableTypes)
                {
                    var targetTemplate = kv.Key;
                    var targetTypes = kv.Value;

                    foreach (var type in targetTypes)
                    {
                        var assignments = new List<string>();
                        for (var i = 0; i < typeInfo.Row; i++)
                        {
                            assignments.Add(GetConversionFromTemplate(targetTemplate, i));
                        }

                        _constructorSignatures.Add((
                            signature: $"{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} v",
                            assignment: assignments));
                    }
                }
            }

            foreach (var (signature, assignment) in _constructorSignatures)
            {
                sourceBuilder.Append($@"
        public {typeName}({signature})
        {{");
                for (var i = 0; i < typeInfo.Row; i++)
                {
                    sourceBuilder.Append($@"
            this.{s_vectorComponents[i]} = {assignment[i]};");
                }
                sourceBuilder.AppendLine($@"
        }}");
            }

            EndRegion();
        }

        private void GenerateUnsafeMethod()
        {
            var componentType = typeInfo.ComponentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            StartRegion("Unsafe Methods");

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
            var components = s_vectorComponents.Take(typeInfo.Row).ToArray();
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

            EndRegion();
        }

        private void GenerateConvertionMethod()
        {
            if (typeInfo.ConvertableTypes == null)
            {
                return;
            }

            StartRegion("Conversion Methods");

            foreach (var kv in typeInfo.ConvertableTypes)
            {
                foreach (var type in kv.Value)
                {
                    // We can use constructor directly
                    sourceBuilder.AppendLine($@"
        public static implicit operator {typeInfo.TypeName}({type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} v) => new {typeInfo.TypeName}(v);");
                }
            }

            EndRegion();
        }

        private void GenerateArithmeticOperators()
        {
            var typeName = typeInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var typeSimpleName = typeInfo.TypeSymbol.Name;
            var componentType = typeInfo.ComponentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var asResult = $"As{typeSimpleName}()";

            StartRegion("Arithmetic Operators");

            // Add
            sourceBuilder.Append($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator +({typeName} lhs, {typeName} rhs)
        {{
            return new {typeName}({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select(c => $"lhs.{c} + rhs.{c}"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator +({typeName} lhs, {componentType} rhs)
        {{
            return new {typeName}({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select(c => $"lhs.{c} + rhs"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator +({componentType} lhs, {typeName} rhs)
        {{
            return new {typeName}({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select(c => $"lhs + rhs.{c}"))});
        }}

#if false //NET10_0_OR_GREATER
        {INLINE_METHOD_ATTRIBUTE}
        public void operator +=({typeName} other)
        {{");

            for (var i = 0; i < typeInfo.Row; i++)
            {
                sourceBuilder.Append($@"
            this.{s_vectorComponents[i]} += other.{s_vectorComponents[i]};");
            }
            sourceBuilder.AppendLine($@"
        }}
#endif");

            // Subtract
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator -({typeName} lhs, {typeName} rhs)
        {{
            return new {typeName}({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select(c => $"lhs.{c} - rhs.{c}"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator -({typeName} lhs, {componentType} rhs)
        {{
            return new {typeName}({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select(c => $"lhs.{c} - rhs"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator -({componentType} lhs, {typeName} rhs)
        {{
            return new {typeName}({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select(c => $"lhs - rhs.{c}"))});
        }}

#if NET10_0_OR_GREATER
        {INLINE_METHOD_ATTRIBUTE}
        public void operator -=({typeName} other)
        {{");

            for (var i = 0; i < typeInfo.Row; i++)
            {
                sourceBuilder.Append($@"
            this.{s_vectorComponents[i]} -= other.{s_vectorComponents[i]};");
            }
            sourceBuilder.AppendLine($@"
        }}
#endif");

            // Multiply
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator *({typeName} lhs, {typeName} rhs)
        {{
            return new {typeName}({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select(c => $"lhs.{c} * rhs.{c}"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator *({typeName} lhs, {componentType} rhs)
        {{
            return new {typeName}({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select(c => $"lhs.{c} * rhs"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator *({componentType} lhs, {typeName} rhs)
        {{
            return new {typeName}({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select(c => $"lhs * rhs.{c}"))});
        }}

#if NET10_0_OR_GREATER
        // Use scaler here to let JIT handle the simd optimization since we can not do a in-place vectorlization manually.
        {INLINE_METHOD_ATTRIBUTE}
        public void operator *=({typeName} other)
        {{");

            for (var i = 0; i < typeInfo.Row; i++)
            {
                sourceBuilder.Append($@"
            this.{s_vectorComponents[i]} *= other.{s_vectorComponents[i]};");
            }
            sourceBuilder.AppendLine($@"
        }}
#endif");

            // Divide
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator /({typeName} lhs, {typeName} rhs)
        {{
            return new {typeName}({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select(c => $"lhs.{c} / rhs.{c}"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator /({typeName} lhs, {componentType} rhs)
        {{
            return new {typeName}({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select(c => $"lhs.{c} / rhs"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator /({componentType} lhs, {typeName} rhs)
        {{
            return new {typeName}({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select(c => $"lhs / rhs.{c}"))});
        }}

#if NET10_0_OR_GREATER
        // Use scaler here to let JIT handle the simd optimization since we can not do a in-place vectorlization manually.
        {INLINE_METHOD_ATTRIBUTE}
        public void operator /=({typeName} other)
        {{");

            for (var i = 0; i < typeInfo.Row; i++)
            {
                sourceBuilder.Append($@"
            this.{s_vectorComponents[i]} /= other.{s_vectorComponents[i]};");
            }
            sourceBuilder.AppendLine($@"
        }}
#endif");

            // Modulus
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator %({typeName} lhs, {typeName} rhs)
        {{
            return new {typeName}({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select(c => $"lhs.{c} % rhs.{c}"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator %({typeName} lhs, {componentType} rhs)
        {{
            return new {typeName}({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select(c => $"lhs.{c} % rhs"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator %({componentType} lhs, {typeName} rhs)
        {{
            return new {typeName}({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select(c => $"lhs % rhs.{c}"))});
        }}

#if NET10_0_OR_GREATER
        // Use scaler here to let JIT handle the simd optimization since we can not do a in-place vectorlization manually.
        {INLINE_METHOD_ATTRIBUTE}
        public void operator %=({typeName} other)
        {{");

            for (var i = 0; i < typeInfo.Row; i++)
            {
                sourceBuilder.Append($@"
            this.{s_vectorComponents[i]} %= other.{s_vectorComponents[i]};");
            }
            sourceBuilder.AppendLine($@"
        }}
#endif");

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
            return new {typeName}(0{_componentTypePrefix}) - value;
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator ++({typeName} value)
        {{
            return value + new {typeName}(1{_componentTypePrefix});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator --({typeName} value)
        {{
            return value - new {typeName}(1{_componentTypePrefix});
        }}");

            // Comparison operators
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static bool{typeInfo.Row} operator <({typeName} lhs, {typeName} rhs)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.LessThan(lhs.AsVector{_vectorBitsSize}(), rhs.AsVector{_vectorBitsSize}());
            return new({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select((_, i) => $"vector[{i}] != 0"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static bool{typeInfo.Row} operator <=({typeName} lhs, {typeName} rhs)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.LessThanOrEqual(lhs.AsVector{_vectorBitsSize}(), rhs.AsVector{_vectorBitsSize}());
            return new({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select((_, i) => $"vector[{i}] != 0"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static bool{typeInfo.Row} operator >({typeName} lhs, {typeName} rhs)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.GreaterThan(lhs.AsVector{_vectorBitsSize}(), rhs.AsVector{_vectorBitsSize}());
            return new({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select((_, i) => $"vector[{i}] != 0"))});
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static bool{typeInfo.Row} operator >=({typeName} lhs, {typeName} rhs)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.GreaterThanOrEqual(lhs.AsVector{_vectorBitsSize}(), rhs.AsVector{_vectorBitsSize}());
            return new({string.Join(", ", s_vectorComponents.Take(typeInfo.Row).Select((_, i) => $"vector[{i}] != 0"))});
        }}");

            // Bitwise operators
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator <<({typeName} x, int n)
        {{
            return (x.AsVector{_vectorBitsSize}() << n).{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator >>({typeName} x, int n)
        {{
            return (x.AsVector{_vectorBitsSize}() >> n).{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator &({typeName} lhs, {typeName} rhs)
        {{
            return (global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.BitwiseAnd(lhs.AsVector{_vectorBitsSize}(), rhs.AsVector{_vectorBitsSize}())).{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator |({typeName} lhs, {typeName} rhs)
        {{
            return (global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.BitwiseOr(lhs.AsVector{_vectorBitsSize}(), rhs.AsVector{_vectorBitsSize}())).{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator ^({typeName} lhs, {typeName} rhs)
        {{
            return (global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Xor(lhs.AsVector{_vectorBitsSize}(), rhs.AsVector{_vectorBitsSize}())).{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator ~({typeName} value)
        {{
            unchecked
            {{
                return value ^ new {typeName}(({componentType})-1);
            }}
        }}");

            EndRegion();
        }

        // Generate swizzle properties like .xy, .xyz, .zyxw, etc.
        // If the component is repeated, the property is read-only.
        // For example, .xx is read-only, but .yx is read-write.
        private IEnumerable<(string property, bool canSet)> GenerateSwizzles(IEnumerable<string> pool, int maxLen)
        {
            IEnumerable<(string property, bool canSet)> Recurse(string prefix, int depth)
            {
                if (depth == 0)
                {
                    yield return (prefix, prefix.Distinct().Count() == prefix.Length);
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
            var validComponents = s_vectorComponents.Take(typeInfo.Row).ToArray();
            var swizzles = GenerateSwizzles(validComponents, s_vectorComponents.Length);

            StartRegion("Swizzle Properties");

            foreach (var (property, canSet) in swizzles)
            {
                var targetDim = property.Length;
                var targetStruct = $"{typeInfo.TypePrefix}{targetDim}";
                var modifier = canSet ? "public" : "public readonly";

                sourceBuilder.Append($@"
        {modifier} {targetStruct} {property} 
        {{
            {INLINE_METHOD_ATTRIBUTE}
            get {{ return new({string.Join(", ", property.Select(c => $"this.{c}"))});}}");
                if (canSet)
                {
                    var assignments = string.Empty;
                    for (var i = 0; i < property.Length; i++)
                    {
                        assignments += $"this.{property[i]} = value.{s_vectorComponents[i]}; ";
                    }

                    sourceBuilder.Append($@"
            {INLINE_METHOD_ATTRIBUTE}
            set {{ {assignments} }}");
                }
                sourceBuilder.AppendLine($@"
        }}");
            }

            EndRegion();
        }

        private void GenerateVectorExtension()
        {
            var typeName = typeInfo.TypeFullName;
            var typeSimpleName = typeInfo.TypeName;
            var componentType = typeInfo.ComponentTypeFullName;

            sourceBuilder.Append($@"
    public static partial class VectorInterop
    {{
        {INLINE_METHOD_ATTRIBUTE}
        public unsafe static global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}<{componentType}> AsVector{_vectorBitsSize}(this {typeName} value)
        {{");

            if (typeInfo.Row == 4)
            {
                sourceBuilder.Append($@"
            return global::System.Runtime.CompilerServices.Unsafe.BitCast<{typeName}, global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}<{componentType}>>(value);");
            }
            else
            {
                sourceBuilder.Append($@"
            return global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Create({string.Join(", ", Enumerable.Range(0, typeInfo.Row + _missingComponents).Select(i => i < typeInfo.Row ? $"value.{s_vectorComponents[i]}" : "default"))});");
            }
            sourceBuilder.Append($@"
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public unsafe static {typeName} As{typeSimpleName}(this global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}<{componentType}> value)
        {{");
            sourceBuilder.AppendLine($@"
            ref var address = ref global::System.Runtime.CompilerServices.Unsafe.As<global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}<{componentType}>, byte>(ref value);
            return global::System.Runtime.CompilerServices.Unsafe.ReadUnaligned<{typeName}>(ref address);
        }}
    }}");
        }

        private void GenerateMathMethod()
        {
            var typeName = typeInfo.TypeName;
            var typeFullName = typeInfo.TypeFullName;
            var typePrefix = typeInfo.TypePrefix;
            var componentTypeFullName = typeInfo.ComponentTypeFullName;

            sourceBuilder.Append($@"
    public static partial class math
    {{");
            foreach (var (signature, assignment) in _constructorSignatures)
            {
                sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} {typeName}({signature})
        {{
            return new {typeFullName}({string.Join(", ", assignment)});
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
        public static {typePrefix}{dimension} shuffle({typeFullName} left, {typeFullName} right, {string.Join(", ", Enumerable.Range(0, dimension).Select(x => $"ShuffleComponent {s_vectorComponents[x]}"))})
        {{
            return new {typePrefix}{dimension}(");
                for (var j = 0; j < dimension; j++)
                {
                    sourceBuilder.Append($@"
                select_shuffle_component(left, right, {s_vectorComponents[j]})");
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
                case ShuffleComponent.Left{s_vectorComponents[i].ToUpper()}:
                    return a.{s_vectorComponents[i]};
                case ShuffleComponent.Right{s_vectorComponents[i].ToUpper()}:
                    return b.{s_vectorComponents[i]};");
            }

            sourceBuilder.AppendLine($@"
                default:
                    throw new System.ArgumentException(""Invalid shuffle component: "" + component);
            }}
        }}");
            sourceBuilder.Append($@"
    }}");
        }
    }
}