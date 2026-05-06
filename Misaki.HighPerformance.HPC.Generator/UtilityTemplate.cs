using Misaki.HighPerformance.HPC.Generator.APIContext;
using System.Text;

namespace Misaki.HighPerformance.HPC.Generator
{
    internal static class UtilityTemplate
    {
        public static Method Sin_Standard<T>(IVectorAPIContext api)
        {
            var body = api.Return(api.Call($"{api.GetVectorType()}.Sin", "value"));
            return new Method(
                modifier: "public static",
                returnType: api.GetVectorType<T>(),
                name: $"Sin_{typeof(T).Name}_Standard",
                parameters: new[] { $"{api.GetVectorType<T>()} value" },
                body: body);
        }

        public static Method Sin_Fast<T>(IVectorAPIContext api)
        {
            var isFloat = typeof(T) == typeof(float);
            var typePrefix = isFloat ? "f" : "d";

            var input = new Expression(api, "value");

            var invPi = api.Create($"0.318309886{typePrefix}").Assign();

            var x_sin = input;
            var y_sin = api.Multiply(x_sin, invPi).Assign();
            var k_sin = api.Round(y_sin).Assign();
            var z_sin = api.Subtract(y_sin, k_sin).Assign();

            var half = api.Create($"0.5{typePrefix}").Assign();
            var two = api.Create($"2.0{typePrefix}").Assign();

            var k_even_sin = (api.Round(k_sin * half) * two).Assign();
            var sign_sin = (api.One<T>() - two * api.Abs(k_sin - k_even_sin)).Assign();

            var c1 = api.Create($"3.14159265{typePrefix}").Assign();
            var c3 = api.Create($"-5.16771278{typePrefix}").Assign();
            var c5 = api.Create($"2.55016404{typePrefix}").Assign();
            var c7 = api.Create($"-0.59926453{typePrefix}").Assign();
            var c9 = api.Create($"0.08214589{typePrefix}").Assign();

            var z2_sin = (z_sin * z_sin).Assign();
            var poly_sin = api.MultiplyAdd(z2_sin, c9, c7).Assign();

            var poly_sin_name = api.LastAssignedVariable;
            poly_sin = api.MultiplyAdd(z2_sin, poly_sin, c5).Assign(poly_sin_name, false);
            poly_sin = api.MultiplyAdd(z2_sin, poly_sin, c3).Assign(poly_sin_name, false);
            poly_sin = api.MultiplyAdd(z2_sin, poly_sin, c1).Assign(poly_sin_name, false);
            poly_sin = api.Multiply(z_sin, poly_sin).Assign(poly_sin_name, false);

            var body = api.Return(poly_sin * sign_sin);

            return new Method(
                modifier: "public static",
                returnType: api.GetVectorType<T>(),
                name: $"Sin_{typeof(T).Name}_Fast",
                parameters: new[] { $"{api.GetVectorType<T>()} {input.Code}" },
                body: body);
        }

        public static Method Cos_Standard<T>(IVectorAPIContext api)
        {
            var body = api.Return(api.Call($"{api.GetVectorType()}.Cos", "value"));
            return new Method(
                modifier: "public static",
                returnType: api.GetVectorType<T>(),
                name: $"Cos_{typeof(T).Name}_Standard",
                parameters: new[] { $"{api.GetVectorType<T>()} value" },
                body: body);
        }

        public static Method Cos_Fast<T>(IVectorAPIContext api)
        {
            var isFloat = typeof(T) == typeof(float);
            var typePrefix = isFloat ? "f" : "d";

            var input = new Expression(api, "value");

            var halfPi = api.Create($"1.570796327{typePrefix}").Assign();
            var invPi = api.Create($"0.318309886{typePrefix}").Assign();

            var x_cos = api.Add(input, halfPi).Assign();
            var y_cos = api.Multiply(x_cos, invPi).Assign();
            var k_cos = api.Round(y_cos).Assign();
            var z_cos = api.Subtract(y_cos, k_cos).Assign();

            var half = api.Create($"0.5{typePrefix}").Assign();
            var two = api.Create($"2.0{typePrefix}").Assign();

            var k_even_cos = api.Multiply(api.Round(api.Multiply(k_cos, half)), two).Assign();
            var sign_cos = api.Subtract(api.One<T>(), api.Multiply(two, api.Abs(api.Subtract(k_cos, k_even_cos)))).Assign();

            var c1 = api.Create($"3.14159265{typePrefix}").Assign();
            var c3 = api.Create($"-5.16771278{typePrefix}").Assign();
            var c5 = api.Create($"2.55016404{typePrefix}").Assign();
            var c7 = api.Create($"-0.59926453{typePrefix}").Assign();
            var c9 = api.Create($"0.08214589{typePrefix}").Assign();

            var z2_cos = api.Multiply(z_cos, z_cos).Assign();
            var poly_cos = api.MultiplyAdd(z2_cos, c9, c7).Assign();

            var poly_cos_name = api.LastAssignedVariable;
            poly_cos = api.MultiplyAdd(z2_cos, poly_cos, c5).Assign(poly_cos_name, false);
            poly_cos = api.MultiplyAdd(z2_cos, poly_cos, c3).Assign(poly_cos_name, false);
            poly_cos = api.MultiplyAdd(z2_cos, poly_cos, c1).Assign(poly_cos_name, false);
            poly_cos = api.Multiply(z_cos, poly_cos).Assign(poly_cos_name, false);

            var body = api.Return(poly_cos * sign_cos);

            return new Method(
                modifier: "public static",
                returnType: api.GetVectorType<T>(),
                name: $"Cos_{typeof(T).Name}_Fast",
                parameters: new[] { $"{api.GetVectorType<T>()} {input.Code}" },
                body: body);
        }

        public static Method SinCos_Standard<T>(IVectorAPIContext api)
        {
            var sin_cos = api.Return(api.Call($"{api.GetVectorType()}.SinCos", "value"));
            return new Method(
                modifier: "public static",
                returnType: "void",
                name: $"SinCos_{typeof(T).Name}_Standard",
                parameters: new[] { $"{api.GetVectorType<T>()} value", $"out {api.GetVectorType<T>()} sin", $"out {api.GetVectorType<T>()} cos" },
                body: sin_cos);
        }

        public static Method SinCos_Fast<T>(IVectorAPIContext api)
        {
            var isFloat = typeof(T) == typeof(float);
            var typePrefix = isFloat ? "f" : "d";

            var input = new Expression(api, "value");
            var sinOut = new Expression(api, "sin");
            var cosOut = new Expression(api, "cos");

            var halfPi = api.Create($"1.570796327{typePrefix}").Assign();
            var invPi = api.Create($"0.318309886{typePrefix}").Assign();

            var x_sin = input;
            var x_cos = api.Add(x_sin, halfPi).Assign();

            var y_sin = api.Multiply(x_sin, invPi).Assign();
            var y_cos = api.Multiply(x_cos, invPi).Assign();

            var k_sin = api.Round(y_sin).Assign();
            var k_cos = api.Round(y_cos).Assign();

            var z_sin = api.Subtract(y_sin, k_sin).Assign();
            var z_cos = api.Subtract(y_cos, k_cos).Assign();

            var half = api.Create($"0.5{typePrefix}").Assign();
            var two = api.Create($"2.0{typePrefix}").Assign();
            var one = api.One<T>();

            var k_even_sin = api.Multiply(api.Round(api.Multiply(k_sin, half)), two).Assign();
            var sign_sin = api.Subtract(one, api.Multiply(two, api.Abs(api.Subtract(k_sin, k_even_sin)))).Assign();

            var k_even_cos = api.Multiply(api.Round(api.Multiply(k_cos, half)), two).Assign();
            var sign_cos = api.Subtract(one, api.Multiply(two, api.Abs(api.Subtract(k_cos, k_even_cos)))).Assign();

            var c1 = api.Create($"3.14159265{typePrefix}").Assign();
            var c3 = api.Create($"-5.16771278{typePrefix}").Assign();
            var c5 = api.Create($"2.55016404{typePrefix}").Assign();
            var c7 = api.Create($"-0.59926453{typePrefix}").Assign();
            var c9 = api.Create($"0.08214589{typePrefix}").Assign();

            var z2_sin = api.Multiply(z_sin, z_sin).Assign();
            var poly_sin = api.MultiplyAdd(z2_sin, c9, c7).Assign();

            var poly_sin_name = api.LastAssignedVariable;
            poly_sin = api.MultiplyAdd(z2_sin, poly_sin, c5).Assign(poly_sin_name, false);
            poly_sin = api.MultiplyAdd(z2_sin, poly_sin, c3).Assign(poly_sin_name, false);
            poly_sin = api.MultiplyAdd(z2_sin, poly_sin, c1).Assign(poly_sin_name, false);
            poly_sin = api.Multiply(z_sin, poly_sin).Assign(poly_sin_name, false);

            var z2_cos = api.Multiply(z_cos, z_cos).Assign();
            var poly_cos = api.MultiplyAdd(z2_cos, c9, c7).Assign();

            var poly_cos_name = api.LastAssignedVariable;
            poly_cos = api.MultiplyAdd(z2_cos, poly_cos, c5).Assign(poly_cos_name, false);
            poly_cos = api.MultiplyAdd(z2_cos, poly_cos, c3).Assign(poly_cos_name, false);
            poly_cos = api.MultiplyAdd(z2_cos, poly_cos, c1).Assign(poly_cos_name, false);
            poly_cos = api.Multiply(z_cos, poly_cos).Assign(poly_cos_name, false);

            sinOut = api.Multiply(poly_sin, sign_sin).Assign(sinOut.Code, false);
            cosOut = api.Multiply(poly_cos, sign_cos).Assign(cosOut.Code, false);

            var body = api.Return(api.Create(""));

            return new Method(
                modifier: "public static",
                returnType: "void",
                name: $"SinCos_{typeof(T).Name}_Fast",
                parameters: new[] { $"{api.GetVectorType<T>()} {input.Code}", $"out {api.GetVectorType<T>()} {sinOut.Code}", $"out {api.GetVectorType<T>()} {cosOut.Code}" },
                body: body);
        }

        public static string GenerateSinCosUtilityMethods(IVectorAPIContext api, string identation)
        {
            var methods = new Method[]
            {
                Sin_Standard<float>(api),
                Sin_Fast<float>(api),
                Cos_Standard<float>(api),
                Cos_Fast<float>(api),
                SinCos_Standard<float>(api),
                SinCos_Fast<float>(api),
                Sin_Standard<double>(api),
                Sin_Fast<double>(api),
                Cos_Standard<double>(api),
                Cos_Fast<double>(api),
                SinCos_Standard<double>(api),
                SinCos_Fast<double>(api)
            };

            var sb = new StringBuilder();
            var inlineAttr = identation + "[MethodImpl(MethodImplOptions.AggressiveInlining)]";

            foreach (var method in methods)
            {
                sb.AppendLine(inlineAttr);
                sb.AppendLine(method.GetFullCode(identation));
            }

            return sb.ToString();
        }
    }
}
