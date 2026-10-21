namespace Misaki.Highperformance.TypeGenerator.Models;

public sealed record MatrixType(ScalarType Element, int Rows, int Cols)
{
    private static readonly string[] s_colNames = ["c0", "c1", "c2", "c3"];

    public string Name => $"{Element.Name}{Rows}x{Cols}";
    public bool IsSquare => Rows == Cols;

    public VectorType ColumnVector => new(Element, Rows);
    public VectorType RowVector => new(Element, Cols);

    public IReadOnlyList<string> ColumnNames => s_colNames.Take(Cols).ToArray();
}
