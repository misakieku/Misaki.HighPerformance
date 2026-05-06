using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Misaki.HighPerformance.HPC.Generator.VectorAPI
{
    internal class Expression
    {
        public IVectorAPIContext API
        {
            get;
        }

        public string Code
        {
            get; private set;
        }

        public Expression(IVectorAPIContext api, string code)
        {
            API = api;
            Code = code;
        }

        public Expression Assign(string? varName = null, bool isNew = true)
        {
            return API.Assign(this, varName, isNew);
        }

        public void Clear()
        {
            Code = string.Empty;
        }

        public override string ToString()
        {
            return Code;
        }

        public static Expression operator +(Expression a, Expression b)
        {
            return a.API.Add(a, b);
        }

        public static Expression operator -(Expression a, Expression b)
        {
            return a.API.Subtract(a, b);
        }

        public static Expression operator *(Expression a, Expression b)
        {
            return a.API.Multiply(a, b);
        }

        public static Expression operator /(Expression a, Expression b)
        {
            return a.API.Divide(a, b);
        }
    }

    internal record Code
    {
        private readonly string[] _statements;

        public Code(IEnumerable<string> statements)
        {
            _statements = statements.ToArray();
        }

        public string GetFullCode(string lineIndentation)
        {
            var sb = new StringBuilder();
            foreach (var stmt in _statements)
            {
                sb.AppendLine(lineIndentation + stmt);
            }

            return sb.ToString();
        }
    }

    internal record Method
    {
        public string Modifier
        {
            get;
        }

        public string ReturnType
        {
            get;
        }

        public string Name
        {
            get;
        }

        public string[] Parameters
        {
            get;
        }

        public Code Body
        {
            get;
        }

        public Method(string modifier, string returnType, string name, string[] parameters, Code body)
        {
            Modifier = modifier;
            ReturnType = returnType;
            Name = name;
            Parameters = parameters;
            Body = body;
        }

        public string GetFullCode(string lineIndentation)
        {
            var sb = new StringBuilder();
            sb.AppendLine(lineIndentation + $"{Modifier} {ReturnType} {Name}({string.Join(", ", Parameters)})");
            sb.AppendLine(lineIndentation + "{");
            sb.Append(Body.GetFullCode(lineIndentation + "    "));
            sb.AppendLine(lineIndentation + "}");
            return sb.ToString();
        }
    }

    internal interface IVectorAPIContext
    {
        string? LastAssignedVariable
        {
            get;
        }

        string GetVectorType();
        string GetVectorType<T>();

        Expression Call(string methodName, params string[] args);
        Expression Assign(Expression expr, string? varName = null, bool isNew = true);
        Code Return(Expression expr);

        Expression Create(string value);
        Expression Zero<T>();
        Expression One<T>();
        Expression Count<T>();

        Expression Add(Expression a, Expression b);
        Expression Subtract(Expression a, Expression b);
        Expression Multiply(Expression a, Expression b);
        Expression Divide(Expression a, Expression b);
        Expression MultiplyAdd(Expression left, Expression right, Expression addend);

        Expression Round(Expression value);
        Expression Abs(Expression value);

        void Reset();
    }

    internal static class VectorAPIContext
    {
        public static string GetTypeName<T>()
        {
            return typeof(T) switch
            {
                _ when typeof(T) == typeof(float) => "float",
                _ when typeof(T) == typeof(double) => "double",
                _ when typeof(T) == typeof(byte) => "byte",
                _ when typeof(T) == typeof(short) => "short",
                _ when typeof(T) == typeof(int) => "int",
                _ when typeof(T) == typeof(uint) => "uint",
                _ when typeof(T) == typeof(long) => "long",
                _ when typeof(T) == typeof(ulong) => "ulong",
                _ => throw new NotSupportedException($"Type {typeof(T)} is not supported in vector operations.")
            };
        }
    }
}
