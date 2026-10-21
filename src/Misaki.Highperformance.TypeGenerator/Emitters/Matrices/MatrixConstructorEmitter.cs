using Misaki.Highperformance.TypeGenerator.Emitters.Common;
using Misaki.Highperformance.TypeGenerator.Formatting;
using Misaki.Highperformance.TypeGenerator.Models;

namespace Misaki.Highperformance.TypeGenerator.Emitters.Matrices;

public static class MatrixConstructorEmitter
{
    public static void EmitConstructors(CodeWriter writer, MatrixType mat)
    {
        using (writer.Region("Constructors & Constants"))
        {
            var colVecName = mat.ColumnVector.Name;

            // 1. Column vector constructor
            var colParams = string.Join(", ", mat.ColumnNames.Select(c => $"{colVecName} {c}"));
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public {mat.Name}({colParams})");
            using (writer.Scope(""))
            {
                foreach (var col in mat.ColumnNames)
                {
                    writer.WriteLine($"this.{col} = {col};");
                }
            }
            writer.WriteLine();

            // 2. Scalar splat constructor
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public {mat.Name}({mat.Element.LogicalTypeName} value)");
            using (writer.Scope(""))
            {
                foreach (var col in mat.ColumnNames)
                {
                    writer.WriteLine($"this.{col} = new(value);");
                }
            }
            writer.WriteLine();

            // 3. Element-wise constructor (row-major order)
            var elementParams = new List<string>();
            for (var r = 0; r < mat.Rows; r++)
            {
                for (var c = 0; c < mat.Cols; c++)
                {
                    elementParams.Add($"{mat.Element.LogicalTypeName} m{r}{c}");
                }
            }

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public {mat.Name}({string.Join(", ", elementParams)})");
            using (writer.Scope(""))
            {
                for (var c = 0; c < mat.Cols; c++)
                {
                    var colArgs = string.Join(", ", Enumerable.Range(0, mat.Rows).Select(r => $"m{r}{c}"));
                    writer.WriteLine($"this.{mat.ColumnNames[c]} = new({colArgs});");
                }
            }
            writer.WriteLine();

            // 4. Constants (identity, zero)
            if (mat.Element.Capabilities.HasFlag(TypeCapabilities.Arithmetic))
            {
                writer.WriteLine($"public static readonly {mat.Name} zero = new({mat.Element.ZeroLiteral});");
                if (mat.IsSquare)
                {
                    var identityCols = new List<string>();
                    for (var c = 0; c < mat.Cols; c++)
                    {
                        var colValues = Enumerable.Range(0, mat.Rows)
                            .Select(r => r == c ? mat.Element.OneLiteral : mat.Element.ZeroLiteral);
                        identityCols.Add($"new {colVecName}({string.Join(", ", colValues)})");
                    }
                    writer.WriteLine($"public static readonly {mat.Name} identity = new({string.Join(", ", identityCols)});");
                }
                writer.WriteLine();
            }

            // 5. Implicit scalar operator
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static implicit operator {mat.Name}({mat.Element.LogicalTypeName} value) => new(value);");
            writer.WriteLine();

            // 6. Cross-matrix constructors and operators
            var otherScalars = TypeRegistry.AllScalars.Where(s => s.Kind != mat.Element.Kind);
            foreach (var other in otherScalars)
            {
                var otherMatName = $"{other.Name}{mat.Rows}x{mat.Cols}";
                var colConversions = mat.ColumnNames.Select(c => $"new {colVecName}(m.{c})");

                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public {mat.Name}({otherMatName} m) : this({string.Join(", ", colConversions)}) {{ }}");
                writer.WriteLine();

                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static explicit operator {mat.Name}({otherMatName} m) => new(m);");
                writer.WriteLine();
            }
        }
    }
}
