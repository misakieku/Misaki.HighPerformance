using System.Collections.Generic;

namespace Misaki.HighPerformance.HPC.Generator.APIContext
{
    internal class Avx2APIContext : IVectorAPIContext
    {
        private readonly List<string> _statements = new();
        private int _varCount = 0;
        private string? _lastAssignedVariable;

        public string? LastAssignedVariable => _lastAssignedVariable;

        public string GetVectorType()
        {
            return "Vector256";
        }

        public string GetVectorType<T>()
        {
            return $"Vector256<{VectorAPIContext.GetTypeName<T>()}>";
        }

        public Expression Call(string methodName, params string[] args)
        {
            return new Expression(this, $"{methodName}({string.Join(", ", args)})");
        }

        public Expression Assign(Expression expr, string? varName = null, bool isNew = true)
        {
            varName ??= $"v{_varCount++}";

            var statement = isNew ? $"var {varName} = {expr.Code};" : $"{varName} = {expr.Code};";
            _statements.Add(statement);
            _lastAssignedVariable = varName;

            expr.Clear();
            return new Expression(this, varName);
        }

        public Code Return(Expression? expr)
        {
            if (expr != null)
            {
                var statement = $"return {expr.Code};";
                _statements.Add(statement);
                expr.Clear();
            }

            var fullCode = new Code(_statements);
            Reset();

            return fullCode;
        }


        public Expression Create(string value)
        {
            return new Expression(this, $"{GetVectorType()}.Create({value})");
        }

        public Expression Zero<T>()
        {
            return new Expression(this, $"{GetVectorType<T>()}.Zero");
        }

        public Expression One<T>()
        {
            return new Expression(this, $"{GetVectorType<T>()}.One");
        }

        public Expression Count<T>()
        {
            return new Expression(this, $"{GetVectorType<T>()}.Count");
        }


        public Expression Add(Expression a, Expression b)
        {
            return new Expression(this, $"Avx2.Add({a}, {b})");
        }

        public Expression Multiply(Expression a, Expression b)
        {
            return new Expression(this, $"Avx2.Multiply({a}, {b})");
        }

        public Expression Subtract(Expression a, Expression b)
        {
            return new Expression(this, $"Avx2.Subtract({a}, {b})");
        }

        public Expression Divide(Expression a, Expression b)
        {
            return new Expression(this, $"Avx2.Divide({a}, {b})");
        }

        public Expression MultiplyAdd(Expression left, Expression right, Expression addend)
        {
            return new Expression(this, $"Fma.MultiplyAdd({left}, {right}, {addend})");
        }


        public Expression Round(Expression value)
        {
            return new Expression(this, $"Avx2.RoundToNearestInteger({value})");
        }

        public Expression Abs(Expression value)
        {
            return new Expression(this, $"{GetVectorType()}.Abs({value})");
        }

        public void Reset()
        {
            _statements.Clear();
            _varCount = 0;
        }
    }
}
