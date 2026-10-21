using Misaki.Highperformance.TypeGenerator.Emitters.Common;
using Misaki.Highperformance.TypeGenerator.Formatting;
using Misaki.Highperformance.TypeGenerator.Models;

namespace Misaki.Highperformance.TypeGenerator.Emitters.Matrices;

public static class MatrixLayoutEmitter
{
    public static void EmitFieldsAndIndexers(CodeWriter writer, MatrixType mat)
    {
        using (writer.Region("Fields & Indexers"))
        {
            var colVecName = mat.ColumnVector.Name;
            var rowVecName = mat.RowVector.Name;

            foreach (var col in mat.ColumnNames)
            {
                writer.WriteLine("[global::System.Text.Json.Serialization.JsonInclude]");
                writer.WriteLine($"public {colVecName} {col};");
            }
            writer.WriteLine();

            // Column indexer
            writer.WriteLine("[global::System.Diagnostics.CodeAnalysis.UnscopedRef]");
            writer.WriteLine($"public unsafe ref {colVecName} this[int col]");
            using (writer.Scope(""))
            {
                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"get => ref global::System.Runtime.CompilerServices.Unsafe.Add(ref {mat.ColumnNames[0]}, col);");
            }
            writer.WriteLine();

            // Element indexer (col, row)
            writer.WriteLine("[global::System.Diagnostics.CodeAnalysis.UnscopedRef]");
            writer.WriteLine($"public unsafe ref {mat.Element.StorageTypeName} this[int col, int row]");
            using (writer.Scope(""))
            {
                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine("get => ref this[col][row];");
            }
            writer.WriteLine();

            // GetRow and SetRow
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public {rowVecName} GetRow(int row) =>");
            writer.WriteLine($"    new({string.Join(", ", mat.ColumnNames.Select(c => $"this.{c}[row]"))});");
            writer.WriteLine();

            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public void SetRow(int row, {rowVecName} value)");
            using (writer.Scope(""))
            {
                for (var i = 0; i < mat.Cols; i++)
                {
                    writer.WriteLine($"this.{mat.ColumnNames[i]}[row] = value[{i}];");
                }
            }
            writer.WriteLine();
        }
    }
}
