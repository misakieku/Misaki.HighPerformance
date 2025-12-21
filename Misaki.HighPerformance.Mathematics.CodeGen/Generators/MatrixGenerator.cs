using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Misaki.HighPerformance.Mathematics.CodeGen.Generators
{
    internal class MatrixGenerator : GeneratorBase
    {
        private readonly List<(string signature, List<string> assignment)> _constructorSignatures = new();

        private string GetConversionFromTemplate(string template, int componentIndex)
        {
            return template.Replace("{v}", "v")
                .Replace("{c}", s_matrixComponents[componentIndex]);
        }

        protected override bool Validation(out string? message)
        {
            if (typeInfo.ElementTypeSymbol == null)
            {
                message = "You must specify 'elementType' in NumericTypeAttribute for matrix types.";
                return false;
            }

            message = null;
            return true;
        }

        protected override void GenerateBody()
        {
            GenerateField();

            if (typeInfo.Arithmetic)
            {
                GenerateUnitMatrix();
            }

            GenerateConstructors();
            GenerateOverrideMethod();

            if (typeInfo.Arithmetic)
            {
                GenerateArithmeticOperators();
            }
        }

        protected override void GenerateTypeEnd()
        {
            base.GenerateTypeEnd();
            sourceBuilder.AppendLine();
            GenerateMathMethod();
        }

        private void GenerateField()
        {
            for (var i = 0; i < typeInfo.Column; i++)
            {
                sourceBuilder.Append($@"
        public {typeInfo.ComponentTypeFullName} {s_matrixComponents[i]};");
            }

            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($@"
        public unsafe ref {typeInfo.ComponentTypeFullName} this[int index]
        {{
            {INLINE_METHOD_ATTRIBUTE}
            get 
            {{
                RangeCheck(index);
                return ref (({typeInfo.ComponentTypeFullName}*)global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref this))[index];
            }}
        }}");

            sourceBuilder.AppendLine($@"
        [global::System.Diagnostics.Conditional(""ENABLE_COLLECTION_CHECKS"")]
        private void RangeCheck(int index)
        {{
            if (index < 0 || index >= {typeInfo.Column})
            {{
                throw new global::System.ArgumentOutOfRangeException(nameof(index), $""Index {{index}} is out of range of '{typeInfo.TypeName}'"");
            }}
        }}");
        }

        private void GenerateUnitMatrix()
        {
            if (typeInfo.Column == typeInfo.Row)
            {
                var tempSB = new StringBuilder();
                for (var i = 0; i < typeInfo.Column; i++)
                {
                    tempSB.Append($"new {typeInfo.ComponentTypeFullName}(");
                    for (var j = 0; j < typeInfo.Row; j++)
                    {
                        if (i == j)
                        {
                            tempSB.Append("1");
                        }
                        else
                        {
                            tempSB.Append("0");
                        }
                        if (j < typeInfo.Row - 1)
                        {
                            tempSB.Append(", ");
                        }
                    }
                    tempSB.Append(")");
                    if (i < typeInfo.Column - 1)
                    {
                        tempSB.Append(", ");
                    }
                }

                sourceBuilder.Append($@"
        public static {typeInfo.TypeFullName} identity => new {typeInfo.TypeFullName}({string.Join(", ", tempSB.ToString())});");
            }

            sourceBuilder.Append($@"
        public static {typeInfo.TypeFullName} zero => default;");
            sourceBuilder.AppendLine();
        }

        private void GenerateConstructors()
        {
            sourceBuilder.AppendLine($@"
        public unsafe {typeInfo.TypeName}(in global::System.ReadOnlySpan<{typeInfo.ComponentTypeFullName}> values)
        {{
            if (values.Length < {typeInfo.Column})
            {{
                throw new global::System.ArgumentException($""Expected at least {typeInfo.Column} values, but got {{values.Length}}"", nameof(values));
            }}

            fixed ({typeInfo.TypeName}* pThis = &this)
            fixed ({typeInfo.ComponentTypeFullName}* pValues = values)
            {{
                *pThis = *({typeInfo.TypeName}*)pValues;
            }}
        }}");

            if (typeInfo.ElementTypeSymbol != null)
            {
                sourceBuilder.AppendLine($@"
        public unsafe {typeInfo.TypeName}(in global::System.ReadOnlySpan<{typeInfo.ElementTypeFullName}> values)
        {{
            if (values.Length < {typeInfo.Column * typeInfo.Row})
            {{
                throw new global::System.ArgumentException($""Expected at least {typeInfo.Column * typeInfo.Row} values, but got {{values.Length}}"", nameof(values));
            }}

            fixed ({typeInfo.TypeName}* pThis = &this)
            fixed ({typeInfo.ElementTypeFullName}* pValues = values)
            {{
                *pThis = *({typeInfo.TypeName}*)pValues;
            }}
        }}");

                _constructorSignatures.Add((
                    signature: $"{typeInfo.ElementTypeFullName} value",
                    assignment: Enumerable.Range(0, typeInfo.Column).Select(_ => $"new {typeInfo.ComponentTypeFullName}(value)").ToList()));

                var tempSB = new StringBuilder();
                for (var r = 0; r < typeInfo.Row; r++)
                {
                    for (var c = 0; c < typeInfo.Column; c++)
                    {
                        tempSB.Append($"{typeInfo.ElementTypeFullName} m{r}{c}");
                        if (!(r == typeInfo.Row - 1 && c == typeInfo.Column - 1))
                        {
                            tempSB.Append(", ");
                        }
                    }
                }

                _constructorSignatures.Add((
                    signature: tempSB.ToString(),
                    assignment: Enumerable.Range(0, typeInfo.Column).Select(c => $"new {typeInfo.ComponentTypeFullName}({string.Join(", ", Enumerable.Range(0, typeInfo.Row).Select(r => $"m{r}{c}"))})").ToList()));
            }

            _constructorSignatures.Add((
                signature: $"{typeInfo.ComponentTypeFullName} value",
                assignment: Enumerable.Range(0, typeInfo.Column).Select(i => "value").ToList()));

            _constructorSignatures.Add((
                signature: string.Join(", ", Enumerable.Range(0, typeInfo.Column).Select(i => $"{typeInfo.ComponentTypeFullName} c{i}")),
                assignment: Enumerable.Range(0, typeInfo.Column).Select(i => $"c{i}").ToList()));

            if (typeInfo.ConvertableTypes != null)
            {
                foreach (var kv in typeInfo.ConvertableTypes)
                {
                    var targetTemplate = kv.Key;
                    var targetTypes = kv.Value;

                    foreach (var type in targetTypes)
                    {
                        var assignments = new List<string>();
                        for (var i = 0; i < typeInfo.Column; i++)
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
        public {typeInfo.TypeName}({signature})
        {{");
                for (var i = 0; i < typeInfo.Column; i++)
                {
                    sourceBuilder.Append($@"
            this.{s_matrixComponents[i]} = {assignment[i]};");
                }
                sourceBuilder.AppendLine($@"
        }}");
            }
        }

        private void GenerateOverrideMethod()
        {
            var components = s_matrixComponents.Take(typeInfo.Column).ToArray();
            sourceBuilder.AppendLine($@"
        public override readonly string ToString()
        {{
            return $""({string.Join(", ", components.Select(c => $"{c}: {{this.{c}}}"))})"";
        }}

        public override readonly int GetHashCode()
        {{
            return global::System.HashCode.Combine({string.Join(", ", components.Select(c => $"this.{c}"))});
        }}

        public override readonly bool Equals(object? obj)
        {{
            return obj is {typeInfo.TypeFullName} value && Equals(value);
        }}

        public readonly bool Equals({typeInfo.TypeFullName} other)
        {{
            return {string.Join(" && ", components.Select(c => $"this.{c}.Equals(other.{c})"))};
        }}

        public static global::Misaki.HighPerformance.Mathematics.bool{typeInfo.Row}x{typeInfo.Column} operator ==({typeInfo.TypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", components.Select(c => $"lhs.{c} == rhs.{c}"))});
        }}

        public static global::Misaki.HighPerformance.Mathematics.bool{typeInfo.Row}x{typeInfo.Column} operator !=({typeInfo.TypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", components.Select(c => $"lhs.{c} != rhs.{c}"))});
        }}
        
        public static implicit operator {typeInfo.TypeFullName}(global::System.ReadOnlySpan<{typeInfo.ComponentTypeFullName}> value)
        {{
            return new {typeInfo.TypeFullName}(value);
        }}

        public static implicit operator {typeInfo.TypeFullName}({typeInfo.ComponentTypeFullName} value)
        {{
            return new(value);
        }}");
        }

        private void GenerateArithmeticOperators()
        {
            // Add
            sourceBuilder.AppendLine($@"
        public static {typeInfo.TypeFullName} operator +({typeInfo.TypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} + rhs.{c}"))});
        }}

        public static {typeInfo.TypeFullName} operator +({typeInfo.TypeFullName} lhs, {typeInfo.ComponentTypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} + rhs"))});
        }}

        public static {typeInfo.TypeFullName} operator +({typeInfo.ComponentTypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs + rhs.{c}"))});
        }}");

            // Subtract
            sourceBuilder.AppendLine($@"
        public static {typeInfo.TypeFullName} operator -({typeInfo.TypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} - rhs.{c}"))});
        }}

        public static {typeInfo.TypeFullName} operator -({typeInfo.TypeFullName} lhs, {typeInfo.ComponentTypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} - rhs"))});
        }}

        public static {typeInfo.TypeFullName} operator -({typeInfo.ComponentTypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs - rhs.{c}"))});
        }}");

            // Multiply
            sourceBuilder.AppendLine($@"
        public static {typeInfo.TypeFullName} operator *({typeInfo.TypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} * rhs.{c}"))});
        }}

        public static {typeInfo.TypeFullName} operator *({typeInfo.TypeFullName} lhs, {typeInfo.ComponentTypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} * rhs"))});
        }}

        public static {typeInfo.TypeFullName} operator *({typeInfo.ComponentTypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs * rhs.{c}"))});
        }}");

            // Divide
            sourceBuilder.AppendLine($@"
        public static {typeInfo.TypeFullName} operator /({typeInfo.ComponentTypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs / rhs.{c}"))});
        }}

        public static {typeInfo.TypeFullName} operator /({typeInfo.TypeFullName} lhs, {typeInfo.ComponentTypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} / rhs"))});
        }}

        public static {typeInfo.TypeFullName} operator /({typeInfo.TypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} / rhs.{c}"))});
        }}");

            // Modulus
            sourceBuilder.AppendLine($@"
        public static {typeInfo.TypeFullName} operator %({typeInfo.TypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} % rhs.{c}"))});
        }}

        public static {typeInfo.TypeFullName} operator %({typeInfo.TypeFullName} lhs, {typeInfo.ComponentTypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} % rhs"))});
        }}

        public static {typeInfo.TypeFullName} operator %({typeInfo.ComponentTypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs % rhs.{c}"))});
        }}");

            // Unary operators
            sourceBuilder.AppendLine($@"
        public static {typeInfo.TypeFullName} operator +({typeInfo.TypeFullName} value)
        {{
            return value;
        }}

        public static {typeInfo.TypeFullName} operator -({typeInfo.TypeFullName} value)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"-value.{c}"))});
        }}

        public static {typeInfo.TypeFullName} operator ++({typeInfo.TypeFullName} value)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"value.{c} + 1"))});
        }}

        public static {typeInfo.TypeFullName} operator --({typeInfo.TypeFullName} value)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"value.{c} - 1"))});
        }}");

            // Comparison operators
            sourceBuilder.AppendLine($@"
        public static global::Misaki.HighPerformance.Mathematics.bool{typeInfo.Row}x{typeInfo.Column} operator <({typeInfo.TypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} < rhs.{c}"))});
        }}

        public static global::Misaki.HighPerformance.Mathematics.bool{typeInfo.Row}x{typeInfo.Column} operator <=({typeInfo.TypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} <= rhs.{c}"))});
        }}

        public static global::Misaki.HighPerformance.Mathematics.bool{typeInfo.Row}x{typeInfo.Column} operator >({typeInfo.TypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} > rhs.{c}"))});
        }}

        public static global::Misaki.HighPerformance.Mathematics.bool{typeInfo.Row}x{typeInfo.Column} operator >=({typeInfo.TypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} >= rhs.{c}"))});
        }}");

            // Bitwise operators
            sourceBuilder.AppendLine($@"
        public static {typeInfo.TypeFullName} operator <<({typeInfo.TypeFullName} lhs, int shift)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} << shift"))});
        }}

        public static {typeInfo.TypeFullName} operator >>({typeInfo.TypeFullName} lhs, int shift)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} >> shift"))});
        }}

        public static {typeInfo.TypeFullName} operator &({typeInfo.TypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} & rhs.{c}"))});
        }}

        public static {typeInfo.TypeFullName} operator |({typeInfo.TypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} | rhs.{c}"))});
        }}

        public static {typeInfo.TypeFullName} operator ^({typeInfo.TypeFullName} lhs, {typeInfo.TypeFullName} rhs)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"lhs.{c} ^ rhs.{c}"))});
        }}

        public static {typeInfo.TypeFullName} operator ~({typeInfo.TypeFullName} value)
        {{
            return new({string.Join(", ", s_matrixComponents.Take(typeInfo.Column).Select(c => $"~value.{c}"))});
        }}");
        }

        private void GenerateMathMethod()
        {
            if (typeInfo.ElementTypeSymbol == null)
            {
                return;
            }

            sourceBuilder.Append($@"
    public static partial class math
    {{");
            foreach (var (signature, assignment) in _constructorSignatures)
            {
                sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeInfo.TypeFullName} {typeInfo.TypeName}({signature})
        {{
            return new {typeInfo.TypeFullName}({string.Join(", ", assignment)});
        }}");
            }

            sourceBuilder.Append($@"
        public static {typeInfo.TypePrefix}{typeInfo.Column}x{typeInfo.Row} transpose({typeInfo.TypeFullName} value)
        {{
            return new {typeInfo.TypePrefix}{typeInfo.Column}x{typeInfo.Row}(");
            for (var i = 0; i < typeInfo.Column; i++)
            {
                sourceBuilder.Append($@"
                {string.Join(", ", s_matrixComponents.Take(typeInfo.Row).Select((c, j) => $"value.{s_matrixComponents[i]}.{s_vectorComponents[j]}"))}");
                if (i < typeInfo.Column - 1)
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

            if (typeInfo.Arithmetic)
            {
                if (typeInfo.Row == typeInfo.Column)
                {
                    GenerateDeterminantMethod();

                    if (typeInfo.CanInverse)
                    {
                        if (typeInfo.Row == 2)
                        {
                            GenerateInverse2x2Method();
                        }
                        else if (typeInfo.Row == 3)
                        {
                            GenerateInverse3x3Method();
                        }
                        else if (typeInfo.Row == 4)
                        {
                            GenerateInverse4x4Method();
                        }
                    }
                }

                GenerateMulMethod();
            }

            sourceBuilder.Append($@"
    }}");
        }

        private void GenerateInverse2x2Method()
        {
            sourceBuilder.AppendLine($@"
        public static {typeInfo.TypeFullName} inverse({typeInfo.TypeFullName} value)
        {{
            var c0 = value.{s_matrixComponents[0]};
            var c1 = value.{s_matrixComponents[1]};

            // elements
            var m00 = c0.x;
            var m01 = c1.x;
            var m10 = c0.y;
            var m11 = c1.y;

            var det = m00 * m11 - m01 * m10;
            if (det == 0.0f)
            {{
                throw new System.InvalidOperationException(""Matrix is singular"");
            }}

            var invDet = 1.0f / det;

            // adjugate: [ d -b; -c a ]
            return new(new(m11 * invDet, -m10 * invDet), new(-m01 * invDet, m00 * invDet));
        }}");
        }

        private void GenerateInverse3x3Method()
        {
            sourceBuilder.AppendLine($@"
        public static {typeInfo.TypeFullName} inverse({typeInfo.TypeFullName} value)
        {{
            var c0 = value.{s_matrixComponents[0]};
            var c1 = value.{s_matrixComponents[1]};
            var c2 = value.{s_matrixComponents[2]};

            var a00 = c0.x;
            var a01 = c1.x;
            var a02 = c2.x;
            var a10 = c0.y;
            var a11 = c1.y;
            var a12 = c2.y;
            var a20 = c0.z;
            var a21 = c1.z;
            var a22 = c2.z;

            // cofactors (adjugate transposed)
            var c00 =  a11 * a22 - a12 * a21;
            var c01 = - (a10 * a22 - a12 * a20);
            var c02 =  a10 * a21 - a11 * a20;

            var c10 = - (a01 * a22 - a02 * a21);
            var c11 =   a00 * a22 - a02 * a20;
            var c12 = - (a00 * a21 - a01 * a20);

            var c20 =  a01 * a12 - a02 * a11;
            var c21 = - (a00 * a12 - a02 * a10);
            var c22 =  a00 * a11 - a01 * a10;

            var det = a00 * c00 + a01 * c01 + a02 * c02;
            if (det == 0.0f)
            {{
                throw new System.InvalidOperationException(""Matrix is singular"");
            }}

            var invDet = 1.0f / det;

            // adjugate is transpose of cofactor matrix (we computed cofactors already)
            return new(new(c00 * invDet, c10 * invDet, c20 * invDet), new(c01 * invDet, c11 * invDet, c21 * invDet), new(c02 * invDet, c12 * invDet, c22 * invDet));
        }}");
        }

        private void GenerateInverse4x4Method()
        {
            sourceBuilder.AppendLine($@"
        public static {typeInfo.TypeFullName} inverse({typeInfo.TypeFullName} value)
        {{
            var c0 = value.{s_matrixComponents[0]};
            var c1 = value.{s_matrixComponents[1]};
            var c2 = value.{s_matrixComponents[2]};
            var c3 = value.{s_matrixComponents[3]};

            // movelh
            var r0y_r1y_r0x_r1x = shuffle(c1, c0, ShuffleComponent.LeftX, ShuffleComponent.LeftY, ShuffleComponent.RightX, ShuffleComponent.RightY);
            var r0z_r1z_r0w_r1w = shuffle(c2, c3, ShuffleComponent.LeftX, ShuffleComponent.LeftY, ShuffleComponent.RightX, ShuffleComponent.RightY);
            // movehl
            var r2y_r3y_r2x_r3x = shuffle(c1, c0, ShuffleComponent.LeftZ, ShuffleComponent.LeftW, ShuffleComponent.RightZ, ShuffleComponent.RightW);
            var r2z_r3z_r2w_r3w = shuffle(c2, c3, ShuffleComponent.LeftZ, ShuffleComponent.LeftW, ShuffleComponent.RightZ, ShuffleComponent.RightW);

            var r1y_r2y_r1x_r2x = shuffle(c1, c0, ShuffleComponent.LeftY, ShuffleComponent.LeftZ, ShuffleComponent.RightY, ShuffleComponent.RightZ);
            var r1z_r2z_r1w_r2w = shuffle(c2, c3, ShuffleComponent.LeftY, ShuffleComponent.LeftZ, ShuffleComponent.RightY, ShuffleComponent.RightZ);
            var r3y_r0y_r3x_r0x = shuffle(c1, c0, ShuffleComponent.LeftW, ShuffleComponent.LeftX, ShuffleComponent.RightW, ShuffleComponent.RightX);
            var r3z_r0z_r3w_r0w = shuffle(c2, c3, ShuffleComponent.LeftW, ShuffleComponent.LeftX, ShuffleComponent.RightW, ShuffleComponent.RightX);

            var r0_wzyx = shuffle(r0z_r1z_r0w_r1w, r0y_r1y_r0x_r1x, ShuffleComponent.LeftZ, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.RightZ);
            var r1_wzyx = shuffle(r0z_r1z_r0w_r1w, r0y_r1y_r0x_r1x, ShuffleComponent.LeftW, ShuffleComponent.LeftY, ShuffleComponent.RightY, ShuffleComponent.RightW);
            var r2_wzyx = shuffle(r2z_r3z_r2w_r3w, r2y_r3y_r2x_r3x, ShuffleComponent.LeftZ, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.RightZ);
            var r3_wzyx = shuffle(r2z_r3z_r2w_r3w, r2y_r3y_r2x_r3x, ShuffleComponent.LeftW, ShuffleComponent.LeftY, ShuffleComponent.RightY, ShuffleComponent.RightW);
            var r0_xyzw = shuffle(r0y_r1y_r0x_r1x, r0z_r1z_r0w_r1w, ShuffleComponent.LeftZ, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.RightZ);

            // Calculate remaining inner term pairs. inner terms have zw=-xy, so we only have to calculate xy and can pack two pairs per vector.
            var inner12_23 = r1y_r2y_r1x_r2x * r2z_r3z_r2w_r3w - r1z_r2z_r1w_r2w * r2y_r3y_r2x_r3x;
            var inner02_13 = r0y_r1y_r0x_r1x * r2z_r3z_r2w_r3w - r0z_r1z_r0w_r1w * r2y_r3y_r2x_r3x;
            var inner30_01 = r3z_r0z_r3w_r0w * r0y_r1y_r0x_r1x - r3y_r0y_r3x_r0x * r0z_r1z_r0w_r1w;

            // Expand inner terms back to 4 components. zw signs still need to be flipped
            var inner12 = shuffle(inner12_23, inner12_23, ShuffleComponent.LeftX, ShuffleComponent.LeftZ, ShuffleComponent.RightZ, ShuffleComponent.RightX);
            var inner23 = shuffle(inner12_23, inner12_23, ShuffleComponent.LeftY, ShuffleComponent.LeftW, ShuffleComponent.RightW, ShuffleComponent.RightY);

            var inner02 = shuffle(inner02_13, inner02_13, ShuffleComponent.LeftX, ShuffleComponent.LeftZ, ShuffleComponent.RightZ, ShuffleComponent.RightX);
            var inner13 = shuffle(inner02_13, inner02_13, ShuffleComponent.LeftY, ShuffleComponent.LeftW, ShuffleComponent.RightW, ShuffleComponent.RightY);

            // Calculate minors
            var minors0 = r3_wzyx * inner12 - r2_wzyx * inner13 + r1_wzyx * inner23;

            var denom = r0_xyzw * minors0;

            // Horizontal sum of denominator. Free sign flip of z and w compensates for missing flip in inner terms.
            denom = denom + shuffle(denom, denom, ShuffleComponent.LeftY, ShuffleComponent.LeftX, ShuffleComponent.RightW, ShuffleComponent.RightZ);   // x+y        x+y            z+w            z+w
            denom = denom - shuffle(denom, denom, ShuffleComponent.LeftZ, ShuffleComponent.LeftZ, ShuffleComponent.RightX, ShuffleComponent.RightX);   // x+y-z-w  x+y-z-w        z+w-x-y        z+w-x-y

            var rcp_denom_ppnn = 1 / denom;
            {typeInfo.TypeFullName} res;
            res.{s_matrixComponents[0]} = minors0 * rcp_denom_ppnn;

            var inner30 = shuffle(inner30_01, inner30_01, ShuffleComponent.LeftX, ShuffleComponent.LeftZ, ShuffleComponent.RightZ, ShuffleComponent.RightX);
            var inner01 = shuffle(inner30_01, inner30_01, ShuffleComponent.LeftY, ShuffleComponent.LeftW, ShuffleComponent.RightW, ShuffleComponent.RightY);

            var minors1 = r2_wzyx * inner30 - r0_wzyx * inner23 - r3_wzyx * inner02;
            res.{s_matrixComponents[1]} = minors1 * rcp_denom_ppnn;

            var minors2 = r0_wzyx * inner13 - r1_wzyx * inner30 - r3_wzyx * inner01;
            res.{s_matrixComponents[2]} = minors2 * rcp_denom_ppnn;

            var minors3 = r1_wzyx * inner02 - r0_wzyx * inner12 + r2_wzyx * inner01;
            res.{s_matrixComponents[3]} = minors3 * rcp_denom_ppnn;
            return res;
        }}

        public static {typeInfo.TypeFullName} fastinverse({typeInfo.TypeFullName} m)
        {{
            var c0 = m.c0;
            var c1 = m.c1;
            var c2 = m.c2;
            var pos = m.c3;

            var zero = default({typeInfo.ComponentTypeFullName});

            // unpacklo
            var t0 = shuffle(c0, c2, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.LeftY, ShuffleComponent.RightY);
            var t1 = shuffle(c1, zero, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.LeftY, ShuffleComponent.RightY);
            // unpackhi
            var t2 = shuffle(c0, c2, ShuffleComponent.LeftZ, ShuffleComponent.RightZ, ShuffleComponent.LeftW, ShuffleComponent.RightW);
            var t3 = shuffle(c1, zero, ShuffleComponent.LeftZ, ShuffleComponent.RightZ, ShuffleComponent.LeftW, ShuffleComponent.RightW);

            var r0 = shuffle(t0, t1, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.LeftY, ShuffleComponent.RightY);
            var r1 = shuffle(t0, t1, ShuffleComponent.LeftZ, ShuffleComponent.RightZ, ShuffleComponent.LeftW, ShuffleComponent.RightW);
            var r2 = shuffle(t2, t3, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.LeftY, ShuffleComponent.RightY);

            pos = -(r0 * pos.x + r1 * pos.y + r2 * pos.z);
            pos.w = 1.0f;

            return new(r0, r1, r2, pos);
        }}");
        }

        private void GenerateDeterminantMethod()
        {
            sourceBuilder.Append($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {typeInfo.ElementTypeFullName} determinant({typeInfo.TypeFullName} value)
        {{");
            for (var i = 0; i < typeInfo.Column; i++)
            {
                sourceBuilder.Append($@"
            var {s_matrixComponents[i]} = value.{s_matrixComponents[i]};");
            }

            sourceBuilder.AppendLine();

            string Elem(int r, int c) => $"{s_matrixComponents[c]}.{s_vectorComponents[r]}";

            // recursive function that returns a string for determinant of submatrix defined by rowIndices and colIndices
            string DetExpr(List<int> rows, List<int> cols)
            {
                var m = rows.Count;
                if (m == 1)
                {
                    return Elem(rows[0], cols[0]);
                }
                if (m == 2)
                {
                    // a b
                    // c d  -> a*d - b*c
                    var a = Elem(rows[0], cols[0]);
                    var b = Elem(rows[0], cols[1]);
                    var c = Elem(rows[1], cols[0]);
                    var d = Elem(rows[1], cols[1]);
                    return $"({a} * {d} - {b} * {c})";
                }

                // expand along the first row (rows[0])
                var sb = new StringBuilder();
                for (var j = 0; j < m; ++j)
                {
                    var col = cols[j];
                    var aij = Elem(rows[0], col);

                    // build minor indices
                    var subRows = new List<int>(rows.Skip(1));
                    var subCols = new List<int>(cols.Where((_, idx) => idx != j));

                    var subDet = DetExpr(subRows, subCols);
                    var sign = ((0 + j) % 2 == 0) ? 1 : -1;
                    if (sign == 1)
                        sb.Append($"{aij} * {subDet}");
                    else
                        sb.Append($"-({aij} * {subDet})");

                    if (j != m - 1)
                        sb.Append(" + ");
                }
                return $"({sb})";
            }

            var rowsList = Enumerable.Range(0, typeInfo.Row).ToList();
            var colsList = Enumerable.Range(0, typeInfo.Row).ToList();
            var expr = DetExpr(rowsList, colsList);

            sourceBuilder.AppendLine($@"
            return ({typeInfo.ElementTypeFullName}){expr};
        }}");
        }

        private void GenerateMulMethod()
        {
            var lhsType = typeInfo.TypeFullName;
            var lhsRows = typeInfo.Row;
            var lhsCols = typeInfo.Column;
            var typePrefix = typeInfo.TypePrefix;

            // Matrix-Vector Multiplication: RxC * C-element vector = R-element vector
            var rhsVectorType = $"{typePrefix}{lhsCols}";
            var resultVectorType = $"{typePrefix}{lhsRows}";

            var columnSizeBytes = lhsRows * typeInfo.ComponentSize;
            var vectorBits = columnSizeBytes > 16 ? 256 : 128;
            var isFloatingPoint = typeInfo.ElementTypeSymbol!.SpecialType == SpecialType.System_Single ||
                                   typeInfo.ElementTypeSymbol!.SpecialType == SpecialType.System_Double;

            sourceBuilder.Append($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {resultVectorType} mul({lhsType} m, {rhsVectorType} v)
        {{");

            for (var i = 0; i < lhsCols; i++)
            {
                var component = s_vectorComponents[i];
                sourceBuilder.Append($@"
            var v{component} = global::System.Runtime.Intrinsics.Vector{vectorBits}.Create(v.{component});");
            }

            sourceBuilder.Append($@"

            var sum = global::System.Runtime.Intrinsics.Vector{vectorBits}.Multiply(m.c0.AsVector{vectorBits}(), vx);");

            for (var i = 1; i < lhsCols; i++)
            {
                var component = s_vectorComponents[i];
                var col = s_matrixComponents[i];

                if (isFloatingPoint)
                {
                    sourceBuilder.Append($@"
            sum = global::System.Runtime.Intrinsics.Vector{vectorBits}.FusedMultiplyAdd(m.{col}.AsVector{vectorBits}(), v{component}, sum);");
                }
                else
                {
                    sourceBuilder.Append($@"
            sum = global::System.Runtime.Intrinsics.Vector{vectorBits}.Add(sum, 
                  global::System.Runtime.Intrinsics.Vector{vectorBits}.Multiply(m.{col}.AsVector{vectorBits}(), v{component}));");
                }
            }

            sourceBuilder.AppendLine($@"
            return sum.As{typeInfo.ComponentTypeName}(); 
        }}");

            // Vector-Matrix Multiplication: R-element vector * RxC = C-element vector
            var lhsVectorType = $"{typePrefix}{lhsRows}";
            var resultVectorTypeT = $"{typePrefix}{lhsCols}";
            sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {resultVectorTypeT} mul({lhsVectorType} v, {lhsType} m)
        {{
            return new {resultVectorTypeT}({string.Join(", ", Enumerable.Range(0, lhsCols).Select(c => $"dot(v, m.{s_matrixComponents[c]})"))});
        }}");

            // Matrix-Matrix Multiplication
            for (var rhsRows = 2; rhsRows <= 4; rhsRows++)
            {
                if (lhsCols != rhsRows)
                {
                    continue;
                }

                for (var rhsCols = 2; rhsCols <= 4; rhsCols++)
                {
                    var rhsType = $"{typePrefix}{rhsRows}x{rhsCols}";
                    var resultType = $"{typePrefix}{lhsRows}x{rhsCols}";

                    sourceBuilder.AppendLine($@"
        {INLINE_METHOD_ATTRIBUTE}
        public static {resultType} mul({lhsType} lhs, {rhsType} rhs)
        {{
            return new {resultType}({string.Join(", ", Enumerable.Range(0, rhsCols).Select(c => $"mul(lhs, rhs.{s_matrixComponents[c]})"))});
        }}");
                }
            }
        }
    }
}