using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Misaki.HighPerformance.Analyzer.Test.CSharpAnalyzerVerifier<
    Misaki.HighPerformance.Analyzer.DefensiveCopyAnalyzer>;

namespace Misaki.HighPerformance.Analyzer.Test
{
    [TestClass]
    public class DefensiveCopyAnalyzerTests
    {
        private const string StructWithMethods = @"
struct MyStruct
{
    public int count;

    public void Mutate()
    {
        count++;
    }

    public readonly int ReadOnlyGet()
    {
        return count;
    }
}

class RefContainer
{
    private MyStruct[] _items = new MyStruct[4];

    public ref MyStruct this[int index] => ref _items[index];

    public ref MyStruct GetRef(int index) => ref _items[index];
}
";

        [TestMethod]
        public async Task ReadonlyField_MutatingMethod_ReportsMHP002()
        {
            var test = StructWithMethods + @"
class C
{
    private readonly MyStruct _field = default;

    void Test()
    {
        {|#0:_field.Mutate()|};
    }
}";
            var expected = VerifyCS.Diagnostic("MHP002").WithLocation(0).WithArguments("Mutate", "_field");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task ReadonlyField_ReadOnlyMethod_NoWarning()
        {
            var test = StructWithMethods + @"
class C
{
    private readonly MyStruct _field = default;

    void Test()
    {
        _field.ReadOnlyGet();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task LocalCopiedFromRefIndexer_MutatingMethod_ReportsMHP004()
        {
            var test = StructWithMethods + @"
class C
{
    void Test(RefContainer container)
    {
        var copy = container[0];
        {|#0:copy.Mutate()|};
    }
}";
            var expected = VerifyCS.Diagnostic("MHP004").WithLocation(0).WithArguments("Mutate", "copy");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task LocalWithRefVar_MutatingMethod_NoWarning()
        {
            var test = StructWithMethods + @"
class C
{
    void Test(RefContainer container)
    {
        ref var actualRef = ref container[0];
        actualRef.Mutate();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task LocalCopiedFromRefIndexer_ReadOnlyMethod_NoWarning()
        {
            var test = StructWithMethods + @"
class C
{
    void Test(RefContainer container)
    {
        var copy = container[0];
        copy.ReadOnlyGet();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task LocalCopiedFromArray_MutatingMethod_ReportsMHP004()
        {
            var test = StructWithMethods + @"
class C
{
    void Test(MyStruct[] arr)
    {
        var copy = arr[0];
        {|#0:copy.Mutate()|};
    }
}";
            var expected = VerifyCS.Diagnostic("MHP004").WithLocation(0).WithArguments("Mutate", "copy");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task LocalCopiedFromRefMethod_MutatingMethod_ReportsMHP004()
        {
            var test = StructWithMethods + @"
class C
{
    void Test(RefContainer container)
    {
        var copy = container.GetRef(0);
        {|#0:copy.Mutate()|};
    }
}";
            var expected = VerifyCS.Diagnostic("MHP004").WithLocation(0).WithArguments("Mutate", "copy");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task LocalCopiedFromRef_Returned_NoWarning()
        {
            var test = StructWithMethods + @"
class C
{
    MyStruct Test(RefContainer container)
    {
        var copy = container[0];
        copy.Mutate();
        return copy;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task LocalAssignedFromRefIndexer_MutatingMethod_ReportsMHP004()
        {
            var test = StructWithMethods + @"
class C
{
    void Test(RefContainer container)
    {
        MyStruct copy;
        copy = container[0];
        {|#0:copy.Mutate()|};
    }
}";
            var expected = VerifyCS.Diagnostic("MHP004").WithLocation(0).WithArguments("Mutate", "copy");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
