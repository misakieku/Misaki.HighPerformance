using Misaki.HighPerformance.HPC.Generator.VectorAPI;

namespace Misaki.HighPerformance.HPC.Generator
{
    internal static class UtilityTemplate
    {
        public static Method SinFloat_Standard(IVectorAPIContext api)
        {
            var body = api.Return(api.Call("Sin", "value"));
            return new Method(
                modifier: "public static",
                returnType: api.GetVectorType<float>(),
                name: $"SinFloat_Standard",
                parameters: new[] { $"{api.GetVectorType<float>()} value" },
                body: body);
        }

        public static Method SinFloat_Fast(IVectorAPIContext api)
        {
            var invPi = api.Create("0.318309886f").Assign();

            var x_sin = new Expression(api, "value").Assign();
            var y_sin = api.Multiply(x_sin, invPi).Assign();
            var k_sin = api.Round(y_sin).Assign();
            var z_sin = api.Subtract(y_sin, k_sin).Assign();

            var half = api.Create("0.5f").Assign();
            var two = api.Create("2.0f").Assign();

            var k_even_sin = (api.Round(k_sin * half) * two).Assign();
            var sign_sin = (api.One<float>() - two * api.Abs(k_sin - k_even_sin)).Assign();

            var c1 = api.Create("3.14159265f").Assign();
            var c3 = api.Create("-5.16771278f").Assign();
            var c5 = api.Create("2.55016404f").Assign();
            var c7 = api.Create("-0.59926453f").Assign();
            var c9 = api.Create("0.08214589f").Assign();

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
                returnType: api.GetVectorType<float>(),
                name: $"SinFloat_Fast",
                parameters: new[] { $"{api.GetVectorType<float>()} value" },
                body: body);
        }
    }
}
