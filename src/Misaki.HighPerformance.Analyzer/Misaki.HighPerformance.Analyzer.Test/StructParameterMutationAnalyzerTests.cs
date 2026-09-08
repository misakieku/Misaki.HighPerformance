using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Misaki.HighPerformance.Analyzer.Test.CSharpAnalyzerVerifier<
    Misaki.HighPerformance.Analyzer.StructParameterMutationAnalyzer>;

namespace Misaki.HighPerformance.Analyzer.Test
{
    [TestClass]
    public class StructParameterMutationAnalyzerTests
    {
        private const string AttributeDefinition = @"
using System;

namespace Misaki.HighPerformance.LowLevel
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class AllowCopyAttribute : Attribute { }
}
";

        [TestMethod]
        public async Task FieldModified_NoEscape_ReportsWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

class C
{
    void Func(MyStruct s)
    {
        {|#0:s.value = 10|};
    }
}";
            var expected = VerifyCS.Diagnostic("MHP003").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task FieldModified_ReturnedStruct_NoWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

class C
{
    MyStruct Func(MyStruct s)
    {
        s.value = 10;
        return s;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task FieldModified_AllowCopyAttribute_NoWarning()
        {
            var test = AttributeDefinition + @"
struct MyStruct
{
    public int value;
}

class C
{
    void Func([Misaki.HighPerformance.LowLevel.AllowCopy] MyStruct s)
    {
        s.value = 10;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task FieldModified_RefParameter_NoWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

class C
{
    void Func(ref MyStruct s)
    {
        s.value = 10;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task FieldModified_InParameter_NoWarning()
        {
            var test = @"
struct MyStruct
{
    public readonly int value;
}

class C
{
    void Func(in MyStruct s)
    {
        var x = s.value;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task NonReadonlyMethod_ReportsWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
    public void Increment()
    {
        value++;
    }
}

class C
{
    void Func(MyStruct s)
    {
        {|#0:s.Increment()|};
    }
}";
            var expected = VerifyCS.Diagnostic("MHP003").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task ReadonlyMethod_NoWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
    public readonly int GetData() => value;
}

class C
{
    void Func(MyStruct s)
    {
        var d = s.GetData();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task EscapesViaRefParameter_NoWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

class C
{
    void Func(MyStruct s, ref MyStruct target)
    {
        s.value = 10;
        target = s;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task EscapesViaOutParameter_NoWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

class C
{
    void Func(MyStruct s, out MyStruct target)
    {
        s.value = 10;
        target = s;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task EscapesViaField_NoWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

class C
{
    MyStruct _field;
    void Func(MyStruct s)
    {
        s.value = 10;
        _field = s;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task EscapesViaArray_NoWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

class C
{
    void Func(MyStruct s, MyStruct[] arr)
    {
        s.value = 10;
        arr[0] = s;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task CompoundAssignment_ReportsWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

class C
{
    void Func(MyStruct s)
    {
        {|#0:s.value += 5|};
    }
}";
            var expected = VerifyCS.Diagnostic("MHP003").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task IncrementDecrement_ReportsWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

class C
{
    void Func(MyStruct s)
    {
        {|#0:s.value++|};
    }
}";
            var expected = VerifyCS.Diagnostic("MHP003").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task PassedAsRefArgument_ReportsWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

class C
{
    void Mutate(ref MyStruct x)
    {
        x.value = 1;
    }

    void Func(MyStruct s)
    {
        Mutate({|#0:ref s|});
    }
}";
            var expected = VerifyCS.Diagnostic("MHP003").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task PrimitiveType_Modified_NoWarning()
        {
            var test = @"
class C
{
    void Func(int x)
    {
        x = 10;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task ReadOnlyStruct_NoWarning()
        {
            var test = @"
readonly struct MyReadOnlyStruct
{
    public readonly int value;
}

class C
{
    void Func(MyReadOnlyStruct s)
    {
        var x = s.value;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task MultipleMutations_ReportsEach()
        {
            var test = @"
struct MyStruct
{
    public int a;
    public int b;
}

class C
{
    void Func(MyStruct s)
    {
        {|#0:s.a = 1|};
        {|#1:s.b = 2|};
    }
}";
            var expected0 = VerifyCS.Diagnostic("MHP003").WithLocation(0);
            var expected1 = VerifyCS.Diagnostic("MHP003").WithLocation(1);
            await VerifyCS.VerifyAnalyzerAsync(test, expected0, expected1);
        }

        [TestMethod]
        public async Task ReturnedIntNotStruct_ReportsWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

class C
{
    int Func(MyStruct s)
    {
        {|#0:s.value = 10|};
        return s.value;
    }
}";
            var expected = VerifyCS.Diagnostic("MHP003").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task EscapesViaTupleReturn_NoWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

class C
{
    (MyStruct, int) Func(MyStruct s)
    {
        s.value = 10;
        return (s, 42);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task EscapesViaLocalReturn_NoWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

class C
{
    MyStruct Func(MyStruct s)
    {
        s.value = 10;
        var copy = s;
        return copy;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task PropertySetter_ReportsWarning()
        {
            var test = @"
struct MyStruct
{
    public int Value { get; set; }
}

class C
{
    void Func(MyStruct s)
    {
        {|#0:s.Value = 10|};
    }
}";
            var expected = VerifyCS.Diagnostic("MHP003").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task MutatingExtensionMethod_ReportsWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

static class Extensions
{
    public static void Mutate(this ref MyStruct s)
    {
        s.value = 1;
    }
}

class C
{
    void Func(MyStruct s)
    {
        {|#0:s.Mutate()|};
    }
}";
            var expected = VerifyCS.Diagnostic("MHP003").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task NonMutatingExtensionMethod_NoWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

static class Extensions
{
    public static void Inspect(this MyStruct s)
    {
    }
}

class C
{
    void Func(MyStruct s)
    {
        s.Inspect();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task EscapesViaConditionalReturn_NoWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

class C
{
    MyStruct Func(MyStruct s, bool condition)
    {
        s.value = 10;
        return condition ? s : default;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task LocalFunction_MutatesParameter_ReportsWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

class C
{
    void Func()
    {
        void Local(MyStruct s)
        {
            {|#0:s.value = 10|};
        }
    }
}";
            var expected = VerifyCS.Diagnostic("MHP003").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task LocalFunction_ReturnsStruct_NoWarning()
        {
            var test = @"
struct MyStruct
{
    public int value;
}

class C
{
    void Func()
    {
        MyStruct Local(MyStruct s)
        {
            s.value = 10;
            return s;
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task Lambda_MutatesParameter_ReportsWarning()
        {
            var test = @"
using System;

struct MyStruct
{
    public int value;
}

class C
{
    void Func()
    {
        Action<MyStruct> a = (s) =>
        {
            {|#0:s.value = 10|};
        };
    }
}";
            var expected = VerifyCS.Diagnostic("MHP003").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task Lambda_ReturnsStruct_NoWarning()
        {
            var test = @"
using System;

struct MyStruct
{
    public int value;
}

class C
{
    void Func()
    {
        Func<MyStruct, MyStruct> f = (s) =>
        {
            s.value = 10;
            return s;
        };
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}


