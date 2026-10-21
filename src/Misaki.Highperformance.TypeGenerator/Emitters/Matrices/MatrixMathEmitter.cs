using System.Text;
using Misaki.Highperformance.TypeGenerator.Emitters.Common;
using Misaki.Highperformance.TypeGenerator.Formatting;
using Misaki.Highperformance.TypeGenerator.Models;

namespace Misaki.Highperformance.TypeGenerator.Emitters.Matrices;

public static class MatrixMathEmitter
{
    private static readonly string[] s_compPool = ["x", "y", "z", "w"];

    public static void EmitMathMethods(CodeWriter writer, MatrixType mat)
    {
        writer.WriteLine("#pragma warning disable CS8981");
        using (writer.Scope("public static partial class math"))
        {
            // 1. Math factory constructor
            var colParams = string.Join(", ", mat.ColumnNames.Select(c => $"{mat.ColumnVector.Name} {c}"));
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {mat.Name} {mat.Name}({colParams}) => new({string.Join(", ", mat.ColumnNames)});");
            writer.WriteLine();

            foreach (var other in TypeRegistry.AllScalars.Where(s => s.Kind != mat.Element.Kind))
            {
                var otherMatName = $"{other.Name}{mat.Rows}x{mat.Cols}";
                writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
                writer.WriteLine($"public static {mat.Name} {mat.Name}({otherMatName} m) => new(m);");
                writer.WriteLine();
            }

            // 2. Transpose (works for all types including bool!)
            var transposedName = $"{mat.Element.Name}{mat.Cols}x{mat.Rows}";
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {transposedName} transpose({mat.Name} m) =>");
            writer.WriteLine($"    new({string.Join(", ", Enumerable.Range(0, mat.Rows).Select(r => $"m.GetRow({r})"))});");
            writer.WriteLine();

            // 3. Multiplications (only for arithmetic types)
            if (mat.Element.Capabilities.HasFlag(TypeCapabilities.Arithmetic))
            {
                EmitMultiplications(writer, mat);

                if (mat.IsSquare)
                {
                    EmitDeterminant(writer, mat);
                    if (mat.Element.IsFloatingPoint)
                    {
                        EmitInverse(writer, mat);
                    }
                }
            }
        }
        writer.WriteLine("#pragma warning restore CS8981");
    }

    private static void EmitMultiplications(CodeWriter writer, MatrixType mat)
    {
        var colVec = mat.ColumnVector.Name;
        var rowVec = mat.RowVector.Name;

        // Matrix * Vector (RxC * C-dim vector = R-dim vector)
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {colVec} mul({mat.Name} m, {rowVec} v) =>");
        var mTerms = mat.ColumnNames.Select((c, i) => $"m.{c} * v.{s_compPool[i]}");
        writer.WriteLine($"    {string.Join(" + ", mTerms)};");
        writer.WriteLine();

        // Vector * Matrix (R-dim vector * RxC matrix = C-dim vector)
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {rowVec} mul({colVec} v, {mat.Name} m) =>");
        var vTerms = mat.ColumnNames.Select(c => $"dot(v, m.{c})");
        writer.WriteLine($"    new({string.Join(", ", vTerms)});");
        writer.WriteLine();

        // Matrix * Matrix (same dimension RxR)
        if (mat.IsSquare)
        {
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {mat.Name} mul({mat.Name} a, {mat.Name} b) =>");
            var mmTerms = mat.ColumnNames.Select(c => $"mul(a, b.{c})");
            writer.WriteLine($"    new({string.Join(", ", mmTerms)});");
            writer.WriteLine();
        }
    }

    private static void EmitDeterminant(CodeWriter writer, MatrixType mat)
    {
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {mat.Element.LogicalTypeName} determinant({mat.Name} m)");
        using (writer.Scope(""))
        {
            foreach (var col in mat.ColumnNames)
            {
                writer.WriteLine($"var {col} = m.{col};");
            }
            writer.WriteLine();

            string Elem(int r, int c) => $"{mat.ColumnNames[c]}.{s_compPool[r]}";

            string DetExpr(List<int> rows, List<int> cols)
            {
                var n = rows.Count;
                if (n == 1) return Elem(rows[0], cols[0]);
                if (n == 2)
                {
                    return $"({Elem(rows[0], cols[0])} * {Elem(rows[1], cols[1])} - {Elem(rows[0], cols[1])} * {Elem(rows[1], cols[0])})";
                }

                var sb = new StringBuilder();
                for (var j = 0; j < n; j++)
                {
                    var col = cols[j];
                    var aij = Elem(rows[0], col);
                    var subRows = rows.Skip(1).ToList();
                    var subCols = cols.Where((_, idx) => idx != j).ToList();
                    var subDet = DetExpr(subRows, subCols);

                    if (j > 0) sb.Append(j % 2 == 0 ? " + " : " - ");
                    else if (j % 2 != 0) sb.Append("-");

                    sb.Append($"({aij} * {subDet})");
                }
                return $"({sb})";
            }

            var allRows = Enumerable.Range(0, mat.Rows).ToList();
            var allCols = Enumerable.Range(0, mat.Cols).ToList();
            var expr = DetExpr(allRows, allCols);

            writer.WriteLine($"return ({mat.Element.LogicalTypeName}){expr};");
        }
        writer.WriteLine();
    }

    private static void EmitInverse(CodeWriter writer, MatrixType mat)
    {
        if (mat.Rows == 2)
        {
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {mat.Name} inverse({mat.Name} m)");
            using (writer.Scope(""))
            {
                writer.WriteLine("var det = determinant(m);");
                writer.WriteLine($"var invDet = {mat.Element.OneLiteral} / det;");
                writer.WriteLine($"return new(new(m.c1.y * invDet, -m.c0.y * invDet), new(-m.c1.x * invDet, m.c0.x * invDet));");
            }
            writer.WriteLine();
        }
        else if (mat.Rows == 3)
        {
            writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
            writer.WriteLine($"public static {mat.Name} inverse({mat.Name} m)");
            using (writer.Scope(""))
            {
                writer.WriteLine("var c0 = m.c0; var c1 = m.c1; var c2 = m.c2;");
                writer.WriteLine("var c00 = c1.y * c2.z - c1.z * c2.y;");
                writer.WriteLine("var c01 = -(c0.y * c2.z - c0.z * c2.y);");
                writer.WriteLine("var c02 = c0.y * c1.z - c0.z * c1.y;");
                writer.WriteLine("var c10 = -(c1.x * c2.z - c1.z * c2.x);");
                writer.WriteLine("var c11 = c0.x * c2.z - c0.z * c2.x;");
                writer.WriteLine("var c12 = -(c0.x * c1.z - c0.z * c1.x);");
                writer.WriteLine("var c20 = c1.x * c2.y - c1.y * c2.x;");
                writer.WriteLine("var c21 = -(c0.x * c2.y - c0.y * c2.x);");
                writer.WriteLine("var c22 = c0.x * c1.y - c0.y * c1.x;");
                writer.WriteLine();
                writer.WriteLine("var det = c0.x * c00 + c1.x * c01 + c2.x * c02;");
                writer.WriteLine($"var invDet = {mat.Element.OneLiteral} / det;");
                writer.WriteLine($"return new(new(c00 * invDet, c01 * invDet, c02 * invDet), new(c10 * invDet, c11 * invDet, c12 * invDet), new(c20 * invDet, c21 * invDet, c22 * invDet));");
            }
            writer.WriteLine();
        }
        else if (mat.Rows == 4)
        {
            EmitInverse4x4(writer, mat);
        }
    }

    private static void EmitInverse4x4(CodeWriter writer, MatrixType mat)
    {
        var one = mat.Element.OneLiteral;
        var colVec = mat.ColumnVector.Name;

        // inverse(4x4)
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {mat.Name} inverse({mat.Name} value)");
        using (writer.Scope(""))
        {
            writer.WriteLine("var c0 = value.c0;");
            writer.WriteLine("var c1 = value.c1;");
            writer.WriteLine("var c2 = value.c2;");
            writer.WriteLine("var c3 = value.c3;");
            writer.WriteLine();
            writer.WriteLine("// movelh");
            writer.WriteLine("var r0y_r1y_r0x_r1x = shuffle(c1, c0, ShuffleComponent.LeftX, ShuffleComponent.LeftY, ShuffleComponent.RightX, ShuffleComponent.RightY);");
            writer.WriteLine("var r0z_r1z_r0w_r1w = shuffle(c2, c3, ShuffleComponent.LeftX, ShuffleComponent.LeftY, ShuffleComponent.RightX, ShuffleComponent.RightY);");
            writer.WriteLine("// movehl");
            writer.WriteLine("var r2y_r3y_r2x_r3x = shuffle(c1, c0, ShuffleComponent.LeftZ, ShuffleComponent.LeftW, ShuffleComponent.RightZ, ShuffleComponent.RightW);");
            writer.WriteLine("var r2z_r3z_r2w_r3w = shuffle(c2, c3, ShuffleComponent.LeftZ, ShuffleComponent.LeftW, ShuffleComponent.RightZ, ShuffleComponent.RightW);");
            writer.WriteLine();
            writer.WriteLine("var r1y_r2y_r1x_r2x = shuffle(c1, c0, ShuffleComponent.LeftY, ShuffleComponent.LeftZ, ShuffleComponent.RightY, ShuffleComponent.RightZ);");
            writer.WriteLine("var r1z_r2z_r1w_r2w = shuffle(c2, c3, ShuffleComponent.LeftY, ShuffleComponent.LeftZ, ShuffleComponent.RightY, ShuffleComponent.RightZ);");
            writer.WriteLine("var r3y_r0y_r3x_r0x = shuffle(c1, c0, ShuffleComponent.LeftW, ShuffleComponent.LeftX, ShuffleComponent.RightW, ShuffleComponent.RightX);");
            writer.WriteLine("var r3z_r0z_r3w_r0w = shuffle(c2, c3, ShuffleComponent.LeftW, ShuffleComponent.LeftX, ShuffleComponent.RightW, ShuffleComponent.RightX);");
            writer.WriteLine();
            writer.WriteLine("var r0_wzyx = shuffle(r0z_r1z_r0w_r1w, r0y_r1y_r0x_r1x, ShuffleComponent.LeftZ, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.RightZ);");
            writer.WriteLine("var r1_wzyx = shuffle(r0z_r1z_r0w_r1w, r0y_r1y_r0x_r1x, ShuffleComponent.LeftW, ShuffleComponent.LeftY, ShuffleComponent.RightY, ShuffleComponent.RightW);");
            writer.WriteLine("var r2_wzyx = shuffle(r2z_r3z_r2w_r3w, r2y_r3y_r2x_r3x, ShuffleComponent.LeftZ, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.RightZ);");
            writer.WriteLine("var r3_wzyx = shuffle(r2z_r3z_r2w_r3w, r2y_r3y_r2x_r3x, ShuffleComponent.LeftW, ShuffleComponent.LeftY, ShuffleComponent.RightY, ShuffleComponent.RightW);");
            writer.WriteLine("var r0_xyzw = shuffle(r0y_r1y_r0x_r1x, r0z_r1z_r0w_r1w, ShuffleComponent.LeftZ, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.RightZ);");
            writer.WriteLine();
            writer.WriteLine("// Calculate remaining inner term pairs. inner terms have zw=-xy, so we only have to calculate xy and can pack two pairs per vector.");
            writer.WriteLine("var inner12_23 = r1y_r2y_r1x_r2x * r2z_r3z_r2w_r3w - r1z_r2z_r1w_r2w * r2y_r3y_r2x_r3x;");
            writer.WriteLine("var inner02_13 = r0y_r1y_r0x_r1x * r2z_r3z_r2w_r3w - r0z_r1z_r0w_r1w * r2y_r3y_r2x_r3x;");
            writer.WriteLine("var inner30_01 = r3z_r0z_r3w_r0w * r0y_r1y_r0x_r1x - r3y_r0y_r3x_r0x * r0z_r1z_r0w_r1w;");
            writer.WriteLine();
            writer.WriteLine("// Expand inner terms back to 4 components. zw signs still need to be flipped");
            writer.WriteLine("var inner12 = shuffle(inner12_23, inner12_23, ShuffleComponent.LeftX, ShuffleComponent.LeftZ, ShuffleComponent.RightZ, ShuffleComponent.RightX);");
            writer.WriteLine("var inner23 = shuffle(inner12_23, inner12_23, ShuffleComponent.LeftY, ShuffleComponent.LeftW, ShuffleComponent.RightW, ShuffleComponent.RightY);");
            writer.WriteLine();
            writer.WriteLine("var inner02 = shuffle(inner02_13, inner02_13, ShuffleComponent.LeftX, ShuffleComponent.LeftZ, ShuffleComponent.RightZ, ShuffleComponent.RightX);");
            writer.WriteLine("var inner13 = shuffle(inner02_13, inner02_13, ShuffleComponent.LeftY, ShuffleComponent.LeftW, ShuffleComponent.RightW, ShuffleComponent.RightY);");
            writer.WriteLine();
            writer.WriteLine("// Calculate minors");
            writer.WriteLine("var minors0 = r3_wzyx * inner12 - r2_wzyx * inner13 + r1_wzyx * inner23;");
            writer.WriteLine();
            writer.WriteLine("var denom = r0_xyzw * minors0;");
            writer.WriteLine();
            writer.WriteLine("// Horizontal sum of denominator. Free sign flip of z and w compensates for missing flip in inner terms.");
            writer.WriteLine("denom = denom + shuffle(denom, denom, ShuffleComponent.LeftY, ShuffleComponent.LeftX, ShuffleComponent.RightW, ShuffleComponent.RightZ);");
            writer.WriteLine("denom = denom - shuffle(denom, denom, ShuffleComponent.LeftZ, ShuffleComponent.LeftZ, ShuffleComponent.RightX, ShuffleComponent.RightX);");
            writer.WriteLine();
            writer.WriteLine($"var rcp_denom_ppnn = {one} / denom;");
            writer.WriteLine();
            writer.WriteLine("var inner30 = shuffle(inner30_01, inner30_01, ShuffleComponent.LeftX, ShuffleComponent.LeftZ, ShuffleComponent.RightZ, ShuffleComponent.RightX);");
            writer.WriteLine("var inner01 = shuffle(inner30_01, inner30_01, ShuffleComponent.LeftY, ShuffleComponent.LeftW, ShuffleComponent.RightW, ShuffleComponent.RightY);");
            writer.WriteLine();
            writer.WriteLine("var minors1 = r2_wzyx * inner30 - r0_wzyx * inner23 - r3_wzyx * inner02;");
            writer.WriteLine("var minors2 = r0_wzyx * inner13 - r1_wzyx * inner30 - r3_wzyx * inner01;");
            writer.WriteLine("var minors3 = r1_wzyx * inner02 - r0_wzyx * inner12 + r2_wzyx * inner01;");
            writer.WriteLine();
            writer.WriteLine("return new(minors0 * rcp_denom_ppnn, minors1 * rcp_denom_ppnn, minors2 * rcp_denom_ppnn, minors3 * rcp_denom_ppnn);");
        }
        writer.WriteLine();

        // fastinverse(4x4)
        writer.WriteLine(FileHeaderEmitter.InlineMethodAttribute);
        writer.WriteLine($"public static {mat.Name} fastinverse({mat.Name} m)");
        using (writer.Scope(""))
        {
            writer.WriteLine("var c0 = m.c0;");
            writer.WriteLine("var c1 = m.c1;");
            writer.WriteLine("var c2 = m.c2;");
            writer.WriteLine("var pos = m.c3;");
            writer.WriteLine();
            writer.WriteLine($"var zero = default({colVec});");
            writer.WriteLine();
            writer.WriteLine("// unpacklo");
            writer.WriteLine("var t0 = shuffle(c0, c2, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.LeftY, ShuffleComponent.RightY);");
            writer.WriteLine("var t1 = shuffle(c1, zero, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.LeftY, ShuffleComponent.RightY);");
            writer.WriteLine("// unpackhi");
            writer.WriteLine("var t2 = shuffle(c0, c2, ShuffleComponent.LeftZ, ShuffleComponent.RightZ, ShuffleComponent.LeftW, ShuffleComponent.RightW);");
            writer.WriteLine("var t3 = shuffle(c1, zero, ShuffleComponent.LeftZ, ShuffleComponent.RightZ, ShuffleComponent.LeftW, ShuffleComponent.RightW);");
            writer.WriteLine();
            writer.WriteLine("var r0 = shuffle(t0, t1, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.LeftY, ShuffleComponent.RightY);");
            writer.WriteLine("var r1 = shuffle(t0, t1, ShuffleComponent.LeftZ, ShuffleComponent.RightZ, ShuffleComponent.LeftW, ShuffleComponent.RightW);");
            writer.WriteLine("var r2 = shuffle(t2, t3, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.LeftY, ShuffleComponent.RightY);");
            writer.WriteLine();
            writer.WriteLine("pos = -(r0 * pos.x + r1 * pos.y + r2 * pos.z);");
            writer.WriteLine($"pos.w = {one};");
            writer.WriteLine();
            writer.WriteLine("return new(r0, r1, r2, pos);");
        }
        writer.WriteLine();
    }
}
