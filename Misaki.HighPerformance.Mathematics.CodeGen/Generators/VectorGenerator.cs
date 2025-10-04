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

        private readonly List<(string signature, string assignment)> _constructorSignatures = new();

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
                assignment: string.Join(", ", Enumerable.Range(0, typeInfo.Row).Select(_ => "value"))));

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

                    var assignment = string.Join(", ", assignments);
                    _constructorSignatures.Add((signature, assignment));
                }
            }

            if (typeInfo.ConvertableTypes != null)
            {
                foreach (var kv in typeInfo.ConvertableTypes)
                {
                    var targetTemplate = kv.Key;
                    var targetTypes = kv.Value;

                    var tempSB = new StringBuilder();
                    foreach (var type in targetTypes)
                    {
                        for (var i = 0; i < typeInfo.Row; i++)
                        {
                            tempSB.Append(GetConversionFromTemplate(targetTemplate, i));
                            if (i < typeInfo.Row - 1)
                            {
                                tempSB.Append(", ");
                            }
                        }

                        _constructorSignatures.Add((
                            signature: $"{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} v",
                            assignment: tempSB.ToString()));

                        tempSB.Clear();
                    }
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
        public static {typeName} Create({string.Join(", ", Enumerable.Range(0, typeInfo.Row).Select((_, i) => $"{typeInfo.ComponentTypeFullName} {s_vectorComponents[i]}"))})
        {{");

            if (typeInfo.VectorType != null)
            {
                sourceBuilder.Append($@"
            global::System.Span<{typeInfo.ComponentTypeFullName}> span = [{string.Join(", ", Enumerable.Range(0, typeInfo.Row + _missingComponents).Select(i => i < typeInfo.Row ? $"{s_vectorComponents[i]}" : "default"))}];
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Create(global::System.Runtime.CompilerServices.Unsafe.As<global::System.Span<{typeInfo.ComponentTypeFullName}>, global::System.ReadOnlySpan<{typeInfo.VectorTypeFullName}>>(ref span));");
            }
            else
            {
                sourceBuilder.Append($@"
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Create({string.Join(", ", Enumerable.Range(0, typeInfo.Row + _missingComponents).Select(i => i < typeInfo.Row ? $"{s_vectorComponents[i]}" : "default"))});");
            }

            sourceBuilder.AppendLine($@"
            ref var address = ref global::System.Runtime.CompilerServices.Unsafe.As<global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}<{typeInfo.VectorTypeFullName}>, byte>(ref vector);
            return global::System.Runtime.CompilerServices.Unsafe.ReadUnaligned<{typeInfo.TypeFullName}>(ref address);
        }}");

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
            return (lhs.AsVector{_vectorBitsSize}() + rhs.AsVector{_vectorBitsSize}()).{asResult};
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
        }}

#if NET10_0_OR_GREATER
        // Use scaler here to let JIT handle the simd optimization since we can not do a in-place vectorlization manually.
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
            return (lhs.AsVector{_vectorBitsSize}() - rhs.AsVector{_vectorBitsSize}()).{asResult};
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
        }}

#if NET10_0_OR_GREATER
        // Use scaler here to let JIT handle the simd optimization since we can not do a in-place vectorlization manually.
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
            return (lhs.AsVector{_vectorBitsSize}() * rhs.AsVector{_vectorBitsSize}()).{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator *({typeName} lhs, {componentType} rhs)
        {{
            return (lhs.AsVector{_vectorBitsSize}() * rhs).{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator *({componentType} lhs, {typeName} rhs)
        {{
            return new {typeName}(lhs) * rhs;
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
            return (lhs.AsVector{_vectorBitsSize}() / rhs.AsVector{_vectorBitsSize}()).{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator /({typeName} lhs, {componentType} rhs)
        {{
            return (lhs.AsVector{_vectorBitsSize}() / rhs).{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator /({componentType} lhs, {typeName} rhs)
        {{
            return new {typeName}(lhs) / rhs;
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
            return lhs % new {typeName}(rhs);
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator %({componentType} lhs, {typeName} rhs)
        {{
            return new {typeName}(lhs) % rhs;
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
            return (-value.AsVector{_vectorBitsSize}()).{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator ++({typeName} value)
        {{
            return (value.AsVector{_vectorBitsSize}() + global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.CreateScalar(1{_componentTypePrefix})).{asResult};
        }}

        {INLINE_METHOD_ATTRIBUTE}
        public static {typeName} operator --({typeName} value)
        {{
            return (value.AsVector{_vectorBitsSize}() - global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.CreateScalar(1{_componentTypePrefix})).{asResult};
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
            return global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Create({string.Join(", ", Enumerable.Range(0, typeInfo.Row + _missingComponents).Select(i => i < typeInfo.Row ? $"value.{s_vectorComponents[i]}" : "0"))});");
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

        private string ComponentwiseAssignments(Func<string, string> func, string separator = ", ")
        {
            return string.Join(separator, s_vectorComponents.Take(typeInfo.Row).Select(func));
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

            if (typeInfo.ComponentTypeSymbol.SpecialType == SpecialType.System_Boolean)
            {
                sourceBuilder.AppendLine($@"
        /// <summary>Returns true if any component of the input {typeName} vector is true, false otherwise.</summary>
        /// <param name=""v"">Vector of values to compare.</param>
        /// <returns>True if any the components of v are true, false otherwise.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static bool any({typeFullName} v)
        {{
            return {ComponentwiseAssignments(c => $"v.{c}", " ||")};
        }}");

                sourceBuilder.AppendLine($@"
        /// <summary>Returns true if all component of the input {typeName} vector is true, false otherwise.</summary>
        /// <param name=""v"">Vector of values to compare.</param>
        /// <returns>True if all the components of v are true, false otherwise.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static bool all({typeFullName} v)
        {{
            return {ComponentwiseAssignments(c => $"v.{c}", " && ")};
        }}");
            }

            if (typeInfo.Arithmetic)
            {
                StartRegion("Math Methods");

                sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise minimum of two {typeName} vectors.</summary>
        /// <param name=""x"">The first vector to compare.</param>
        /// <param name=""y"">The second vector to compare.</param>
        /// <returns>The componentwise minimum of the two vectors.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} min({typeFullName} x, {typeFullName} y)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Min(x.AsVector{_vectorBitsSize}(), y.AsVector{_vectorBitsSize}());
            return vector.As{typeName}();
        }}");

                sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise maximum of two {typeName} vectors.</summary>
        /// <param name=""x"">The first vector to compare.</param>
        /// <param name=""y"">The second vector to compare.</param>
        /// <returns>The componentwise maximum of the two vectors.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} max({typeFullName} x, {typeFullName} y)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Max(x.AsVector{_vectorBitsSize}(), y.AsVector{_vectorBitsSize}());
            return vector.As{typeName}();
        }}");

                sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise absolute value of a {typeName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise absolute value of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} abs({typeFullName} v)
        {{
            return max(v, -v);
        }}");

                sourceBuilder.AppendLine($@"
        /// <summary>Returns the result of a componentwise clamping of the value v into the interval (inclusive) [min, max], where v, min and max are {typeName} vectors.</summary>
        /// <param name=""v"">Input value to be clamped.</param>
        /// <param name=""min"">Lower bound of the interval.</param>
        /// <param name=""max"">Upper bound of the interval.</param>
        /// <returns>The componentwise clamping of the input valueToClamp into the interval (inclusive) [min, max].</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} clamp({typeFullName} v, {typeFullName} min, {typeFullName} max)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Clamp(v.AsVector{_vectorBitsSize}(), min.AsVector{_vectorBitsSize}(), max.AsVector{_vectorBitsSize}());
            return vector.As{typeName}();
        }}");

                sourceBuilder.AppendLine($@"
        /// <summary>Returns the dot product of two {typeName} vectors.</summary>
        /// <param name=""x"">The first vector to dot.</param>
        /// <param name=""y"">The second vector to dot.</param>
        /// <returns>The dot product of the two vectors.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {componentTypeFullName} dot({typeFullName} x, {typeFullName} y)
        {{
            return global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Dot(x.AsVector{_vectorBitsSize}(), y.AsVector{_vectorBitsSize}());
        }}");

                sourceBuilder.AppendLine($@"
        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name=""falseValue"">Value to use if test is false.</param>
        /// <param name=""trueValue"">Value to use if test is true.</param>
        /// <param name=""test"">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} select({typeFullName} falseValue, {typeFullName} trueValue, bool test)
        {{
            return test ? trueValue : falseValue;
        }}");

                sourceBuilder.AppendLine($@"
        /// <summary>
        /// Returns a componentwise selection between two {typeName} vectors falseValue and trueValue based on a bool{typeInfo.Row} selection mask test.
        /// Per component, the component from trueValue is selected when test is true, otherwise the component from falseValue is selected.
        /// </summary>
        /// <param name=""falseValue"">Values to use if test is false.</param>
        /// <param name=""trueValue"">Values to use if test is true.</param>
        /// <param name=""test"">Selection mask to choose between falseValue and trueValue.</param>
        /// <returns>The componentwise selection between falseValue and trueValue according to selection mask test.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} select({typeFullName} falseValue, {typeFullName} trueValue, global::Misaki.HighPerformance.Mathematics.bool{typeInfo.Row} test)
        {{
            return new {typeFullName}({ComponentwiseAssignments(c => $"test.{c} ? trueValue.{c} : falseValue.{c}")});
        }}");

                if (typeInfo.ComponentTypeSymbol.SpecialType != SpecialType.System_UInt16
                    && typeInfo.ComponentTypeSymbol.SpecialType != SpecialType.System_UInt32
                    && typeInfo.ComponentTypeSymbol.SpecialType != SpecialType.System_UInt64)
                {
                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise sign of a {typeName} vector. 1 for positive components, 0 for zero components and -1 for negative components.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise sign of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} sign({typeFullName} v)
        {{
            return new {typeFullName}({ComponentwiseAssignments(c => $"sign(v.{c})")});
        }}");
                }

                sourceBuilder.AppendLine($@"
        /// <summary>Returns true if any component of the input {typeName} vector is true, false otherwise.</summary>
        /// <param name=""v"">Vector of values to compare.</param>
        /// <returns>True if any the components of v are true, false otherwise.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static bool any({typeFullName} v)
        {{
            return {ComponentwiseAssignments(c => $"v.{c} != 0", " || ")};
        }}");

                sourceBuilder.AppendLine($@"
        /// <summary>Returns true if all component of the input {typeName} vector is true, false otherwise.</summary>
        /// <param name=""v"">Vector of values to compare.</param>
        /// <returns>True if all the components of v are true, false otherwise.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static bool all({typeFullName} v)
        {{
            return {ComponentwiseAssignments(c => $"v.{c} != 0", " && ")};
        }}");

                // Only floating point types support these methods
                if (typeInfo.ComponentTypeSymbol.SpecialType == SpecialType.System_Single
                    || typeInfo.ComponentTypeSymbol.SpecialType == SpecialType.System_Double)
                {
                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the result of a componentwise clamping of the {typeName} vector v into the interval [0, 1].</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise clamping of the input into the interval [0, 1].</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} saturate({typeFullName} v)
        {{
            return clamp(v, new {typeFullName}(0{_componentTypePrefix}), new {typeFullName}(1{_componentTypePrefix}));
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the linear interpolation between two {typeName} vectors x and y by the interpolant t.</summary>
        /// <param name=""x"">The vector when t is 0.</param>
        /// <param name=""y"">The vector when t is 1.</param>
        /// <param name=""t"">The interpolation factor.</param>
        /// <returns>The linearly interpolated vector.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} lerp({typeFullName} x, {typeFullName} y, {componentTypeFullName} t)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Lerp(x.AsVector{_vectorBitsSize}(), y.AsVector{_vectorBitsSize}(), global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Create(t));
            return vector.As{typeName}();
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the result of a componentwise linear interpolation between two {typeName} vectors x and y using a {typeName} vector t.</summary>
        /// <param name=""x"">The value to interpolate from.</param>
        /// <param name=""y"">The value to interpolate to.</param>
        /// <param name=""t"">The interpolation factor.</param>
        /// <returns>The linearly interpolated vector.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} lerp({typeFullName} x, {typeFullName} y, {typeFullName} t)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Lerp(x.AsVector{_vectorBitsSize}(), y.AsVector{_vectorBitsSize}(), t.AsVector{_vectorBitsSize}());
            return vector.As{typeName}();
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise result of normalizing a value x to a range [a, b]. The opposite of lerp. Equivalent to (v - a) / (b - a).</summary>
        /// <param name=""start"">The start point of the range.</param>
        /// <param name=""end"">The end point of the range.</param>
        /// <param name=""v""> The value to normalize to the range.</param>
        /// <returns>The componentwise interpolation parameter of x with respect to the input range [a, b].</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} unlerp({typeFullName} start, {typeFullName} end, {typeFullName} v)
        {{
            return (v - start) / (end - start);
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise result of a non-clamping linear remapping of a value x from source range [srcStart, srcEnd] to the destination range [dstStart, dstEnd].</summary>
        /// <param name=""srcStart"">The start point of the source range [srcStart, srcEnd].</param>
        /// <param name=""srcEnd"">The end point of the source range [srcStart, srcEnd].</param>
        /// <param name=""dstStart"">The start point of the destination range [dstStart, dstEnd].</param>
        /// <param name=""dstEnd"">The end point of the destination range [dstStart, dstEnd].</param>
        /// <param name=""v"">The value to remap from the source to destination range.</param>
        /// <returns>The componentwise remap of input x from the source range to the destination range.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} remap({typeFullName} srcStart, {typeFullName} srcEnd, {typeFullName} dstStart, {typeFullName} dstEnd, {typeFullName} v)
        {{
            return lerp(dstStart, dstEnd, unlerp(srcStart, srcEnd, v));
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the result of a componentwise multiply-add operation (a * b + c) on 3 {typeName} vectors.</summary>
        /// <remarks>
        /// When fast math enabled on some architectures, this could be converted to a fused multiply add (FMA).
        /// FMA is more accurate due to rounding once at the end of the computation rather than twice that is required when
        /// this computation is not fused.
        /// </remarks>
        /// <param name=""mulA"">First value to multiply.</param>
        /// <param name=""mulB"">Second value to multiply.</param>
        /// <param name=""addC"">Third value to add to the product of a and b.</param>
        /// <returns>The componentwise multiply-add of the inputs.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} mad({typeFullName} mulA, {typeFullName} mulB, {typeFullName} addC)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.FusedMultiplyAdd(mulA.AsVector{_vectorBitsSize}(), mulB.AsVector{_vectorBitsSize}(), addC.AsVector{_vectorBitsSize}());
            return vector.As{typeName}();
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise tangent of a {typeFullName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise tangent of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} tan({typeFullName} v)
        {{
            return new {typeFullName}({ComponentwiseAssignments(c => $"tan(v.{c})")});
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise hyperbolic tangent of a {typeFullName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise hyperbolic tangent of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} tanh({typeFullName} v)
        {{
            return new {typeFullName}({ComponentwiseAssignments(c => $"tanh(v.{c})")});
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise arctangent of a {typeFullName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise arctangent of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} atan({typeFullName} v)
        {{
            return new {typeFullName}({ComponentwiseAssignments(c => $"atan(v.{c})")});
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise 2-argument arctangent of a {typeFullName} vector.</summary>
        /// <param name=""x"">Input value x.</param>
        /// <param name=""y"">Input value y.</param>
        /// <returns>The componentwise 2-argument arctangent of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} atan2({typeFullName} x, {typeFullName} y)
        {{
            return new {typeFullName}({ComponentwiseAssignments(c => $"atan2(x.{c}, y.{c})")});
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise cosine of a {typeName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise cosine of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} cos({typeFullName} v)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Cos(v.AsVector{_vectorBitsSize}());
            return vector.As{typeName}();
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise hyperbolic cosine of a {typeFullName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise hyperbolic cosine of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} cosh({typeFullName} v)
        {{
            return new {typeFullName}({ComponentwiseAssignments(c => $"cosh(v.{c})")});
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise arccosine of a {typeFullName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise arccosine of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} acos({typeFullName} v)
        {{
            return new {typeFullName}({ComponentwiseAssignments(c => $"acos(v.{c})")});
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise sine of a {typeName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise sine of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} sin({typeFullName} v)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Sin(v.AsVector{_vectorBitsSize}());
            return vector.As{typeName}();
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise hyperbolic sine of a {typeFullName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise hyperbolic sine of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} sinh({typeFullName} v)
        {{
            return new {typeFullName}({ComponentwiseAssignments(c => $"sinh(v.{c})")});
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise arcsine of a {typeFullName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise arcsine of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} asin({typeFullName} v)
        {{
            return new {typeFullName}({ComponentwiseAssignments(c => $"asin(v.{c})")});
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise sine and cosine of a {typeName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise sine and cosine of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static ({typeFullName} sin, {typeFullName} cos) sincos({typeFullName} v)
        {{
            var vectors = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.SinCos(v.AsVector{_vectorBitsSize}());
            return (vectors.Sin.As{typeName}(), vectors.Cos.As{typeName}());
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise floor of a {typeName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise floor of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} floor({typeFullName} v)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Floor(v.AsVector{_vectorBitsSize}());
            return vector.As{typeName}();
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise ceiling of a {typeName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise ceiling of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} ceil({typeFullName} v)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Ceiling(v.AsVector{_vectorBitsSize}());
            return vector.As{typeName}();
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the result of rounding each component of a {typeName} vector value to the nearest integral value.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise round to nearest integral value of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} round({typeFullName} v)
        {{
            return new {typeFullName}({ComponentwiseAssignments(c => $"round(v.{c})")});
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise base-e exponential of a {typeName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise base-e exponential of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} exp({typeFullName} v)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Exp(v.AsVector{_vectorBitsSize}());
            return vector.As{typeName}();
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise base-2 exponential of a {typeName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise base-2 exponential of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} exp2({typeFullName} v)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Exp(v.AsVector{_vectorBitsSize}() * 0.693147180559945309{_componentTypePrefix});
            return vector.As{typeName}();
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise base-10 exponential of a {typeName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise base-10 exponential of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} exp10({typeFullName} v)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Exp(v.AsVector{_vectorBitsSize}() * 2.302585092994045684{_componentTypePrefix});
            return vector.As{typeName}();
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise natural logarithm (base-e) of a {typeName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise natural logarithm of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} log({typeFullName} v)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Log(v.AsVector{_vectorBitsSize}());
            return vector.As{typeName}();
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise base-2 logarithm of a {typeName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise base-2 logarithm of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} log2({typeFullName} v)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Log2(v.AsVector{_vectorBitsSize}());
            return vector.As{typeName}();
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise base-10 logarithm of a {typeName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise base-10 logarithm of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} log10({typeFullName} v)
        {{
            return new {typeFullName}({ComponentwiseAssignments(c => $"log10(v.{c})")});
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise quadratic root of a {typeName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise quadratic root of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} sqrt({typeFullName} v)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Sqrt(v.AsVector{_vectorBitsSize}());
            return vector.As{typeName}();
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the reciprocal square root of a {typeName} vector.</summary>
        /// <param name=""v"">Value to use when computing reciprocal square root.</param>
        /// <returns>The reciprocal square root.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} rsqrt({typeFullName} v)
        {{
            return 1{_componentTypePrefix} / sqrt(v);
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the length of a {typeName} vector.</summary>
        /// <param name=""v"">Vector to use when computing length.</param>
        /// <returns>Length of vector v.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {componentTypeFullName} length({typeFullName} v)
        {{
            return sqrt(dot(v, v));
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the squared length of a {typeName} vector.</summary>
        /// <param name=""v"">Vector to use when computing squared length.</param>
        /// <returns>Squared length of vector v.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {componentTypeFullName} lengthsq({typeFullName} v)
        {{
            return dot(v, v);
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the distance between two {typeName} vectors.</summary>
        /// <param name=""x"">First vector to use in distance computation.</param>
        /// <param name=""y"">Second vector to use in distance computation.</param>
        /// <returns>The distance between x and y.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {componentTypeFullName} distance({typeFullName} x, {typeFullName} y)
        {{
            return length(y - x);
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the squared distance between two {typeName} vectors.</summary>
        /// <param name=""x"">First vector to use in distance computation.</param>
        /// <param name=""y"">Second vector to use in distance computation.</param>
        /// <returns>The squared distance between x and y.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {componentTypeFullName} distancesq({typeFullName} x, {typeFullName} y)
        {{
            return lengthsq(y - x);
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise truncate of a {typeName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise truncate of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} trunc({typeFullName} v)
        {{
            var vector = global::System.Runtime.Intrinsics.Vector{_vectorBitsSize}.Truncate(v.AsVector{_vectorBitsSize}());
            return vector.As{typeName}();
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise fractional parts of a {typeName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The componentwise fractional part of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} frac({typeFullName} v)
        {{
            return v - floor(v);
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the reciprocal a {typeName} vector.</summary>
        /// <param name=""v"">Input value.</param>
        /// <returns>The reciprocal of the input.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} rcp({typeFullName} v)
        {{
            return 1{_componentTypePrefix} / v;
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise result of raising v to the power p.</summary>
        /// <param name=""x"">The exponent base.</param>
        /// <param name=""y"">The exponent power.</param>
        /// <returns>The componentwise result of raising x to the power y.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} pow({typeFullName} x, {typeFullName} y)
        {{
            return new {typeFullName}({ComponentwiseAssignments(c => $"pow(x.{c}, y.{c})")});
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the componentwise floating point remainder of x/y.</summary>
        /// <param name=""x"">The dividend in x/y.</param>
        /// <param name=""y"">The divisor in x/y.</param>
        /// <returns>The componentwise remainder of x/y.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} fmod({typeFullName} x, {typeFullName} y)
        {{
            return new {typeFullName}({ComponentwiseAssignments(c => $"x.{c} % y.{c}")});
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>
        /// Performs a componentwise split of a {typeName} vector into an integral part i and a fractional part that gets returned.
        /// Both parts take the sign of the corresponding input component.
        /// </summary>
        /// <param name=""x"">Value to split into integral and fractional part.</param>
        /// <param name=""i"">Output value containing integral part of x.</param>
        /// <returns>The componentwise fractional part of x.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} modf({typeFullName} x, out {typeFullName} i)
        {{
            i = trunc(x);
            return x - i;
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns a normalized version of the {typeName} vector v by scaling it by 1 / length(v).</summary>
        /// <param name=""v"">Vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} normalize({typeFullName} v)
        {{
            return rsqrt(dot(v, v)) * v;
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>
        /// Returns a safe normalized version of the {typeName} vector v by scaling it by 1 / length(v).
        /// Returns the given default value when 1 / length(v) does not produce a finite number.
        /// </summary>
        /// <param name=""v"">Vector to normalize.</param>
        /// <param name=""defaultvalue"">Vector to return if normalized vector is not finite.</param>
        /// <returns>The normalized vector or the default value if the normalized vector is not finite.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        static public {typeFullName} normalizesafe({typeFullName} v, {typeFullName} defaultvalue = default)
        {{
            var len = dot(v, v);
            return select(defaultvalue, v * rsqrt(len), len > FLT_MIN_NORMAL);
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns a componentwise smooth Hermite interpolation between 0.0f and 1.0f when v is in the interval (inclusive) [vMin, vMax].</summary>
        /// <param name=""vMin"">The minimum range of the v parameter.</param>
        /// <param name=""vMax"">The maximum range of the v parameter.</param>
        /// <param name=""v"">The value to be interpolated.</param>
        /// <returns>Returns component values camped to the range [0, 1].</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} smoothstep({typeFullName} vMin, {typeFullName} vMax, {typeFullName} v)
        {{
            var t = saturate((v - vMin) / (vMax - vMin));
            return t * t * (3{_componentTypePrefix} - (2{_componentTypePrefix} * t));
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the result of a componentwise step function where each component is 1.0f when v = threshold and 0.0f otherwise.</summary>
        /// <param name=""threshold"">Vector of values to be used as a threshold for returning 1.</param>
        /// <param name=""v"">Vector of values to compare against threshold.</param>
        /// <returns>1 if the componentwise comparison v = threshold is true, otherwise 0.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} step({typeFullName} threshold, {typeFullName} v)
        {{
            return select(new {typeFullName}(0{_componentTypePrefix}), new {typeFullName}(1{_componentTypePrefix}), v == threshold);
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Given an incident vector i and a normal vector n, returns the reflection vector r = i - 2.0 * dot(i, n) * n.</summary>
        /// <param name=""i"">Incident vector.</param>
        /// <param name=""n"">Normal vector.</param>
        /// <returns>Reflection vector.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} reflect({typeFullName} i, {typeFullName} n)
        {{
            return i - 2{_componentTypePrefix} * n * dot(i, n);
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the refraction vector given the incident vector i, the normal vector n and the refraction index.</summary>
        /// <param name=""i"">Incident vector.</param>
        /// <param name=""n"">Normal vector.</param>
        /// <param name=""indexOfRefraction"">Index of refraction.</param>
        /// <returns>Refraction vector.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} refract({typeFullName} i, {typeFullName} n, {componentTypeFullName} indexOfRefraction)
        {{
            var ni = dot(n, i);
            var k = 1.0f - indexOfRefraction * indexOfRefraction * (1{_componentTypePrefix} - ni * ni);
            return select(new(0{_componentTypePrefix}), indexOfRefraction * i - (indexOfRefraction * ni + sqrt(k)) * n, k >= 0);
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>
        /// Compute vector projection of a onto b.
        /// </summary>
        /// <remarks>
        /// Some finite vectors a and b could generate a non-finite result. This is most likely when a's components
        /// are very large (close to Single.MaxValue) or when b's components are very small (close to FLT_MIN_NORMAL).
        /// In these cases, you can call <see cref=""projectsafe({typeFullName},{typeFullName},{typeFullName})""/>
        /// which will use a given default value if the result is not finite.
        /// </remarks>
        /// <param name=""a"">Vector to project.</param>
        /// <param name=""ontoB"">Non-zero vector to project onto.</param>
        /// <returns>Vector projection of a onto b.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} project({typeFullName} a, {typeFullName} ontoB)
        {{
            return (dot(a, ontoB) / dot(ontoB, ontoB)) * ontoB;
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>
        /// Compute vector projection of a onto b. If result is not finite, then return the default value instead.
        /// </summary>
        /// <remarks>
        /// This function performs extra checks to see if the result of projecting a onto b is finite. If you know that
        /// your inputs will generate a finite result or you don't care if the result is finite, then you can call
        /// <see cref=""project({typeFullName},{typeFullName})""/> instead which is faster than this function.
        /// </remarks>
        /// <param name=""a"">Vector to project.</param>
        /// <param name=""ontoB"">Non-zero vector to project onto.</param>
        /// <param name=""defaultValue"">Default value to return if projection is not finite.</param>
        /// <returns>Vector projection of a onto b or the default value.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} projectsafe({typeFullName} a, {typeFullName} ontoB, {typeFullName} defaultValue = default)
        {{
            var proj = project(a, ontoB);

            return select(defaultValue, proj, all(isfinite(proj)));
        }}");

                    sourceBuilder.AppendLine($@"
        /// <summary>Conditionally flips a vector n if two vectors i and ng are pointing in the same direction. Returns n if dot(i, ng) &lt; 0, -n otherwise.</summary>
        /// <param name=""n"">Vector to conditionally flip.</param>
        /// <param name=""i"">First vector in direction comparison.</param>
        /// <param name=""ng"">Second vector in direction comparison.</param>
        /// <returns>-n if i and ng point in the same direction; otherwise return n unchanged.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} faceforward({typeFullName} n, {typeFullName} i, {typeFullName} ng)
        {{
            return select(n, -n, dot(ng, i) >= 0.0f);
        }}");
                }
                else
                {
                    sourceBuilder.AppendLine($@"
        /// <summary>Returns the result of a componentwise multiply-add operation (a * b + c) on 3 {typeName} vectors.</summary>
        /// <param name=""mulA"">First value to multiply.</param>
        /// <param name=""mulB"">Second value to multiply.</param>
        /// <param name=""addC"">Third value to add to the product of a and b.</param>
        /// <returns>The componentwise multiply-add of the inputs.</returns>
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeFullName} mad({typeFullName} mulA, {typeFullName} mulB, {typeFullName} addC)
        {{
            return mulA * mulB + addC;
        }}");
                }

                EndRegion();
            }

            sourceBuilder.Append($@"
    }}");
        }
    }
}