using Misaki.HighPerformance.LowLevel.Collections;

namespace Misaki.HighPerformance.Test.UnitTest.Collections;

[TestClass]
public class TestFixedString
{
    [TestMethod]
    public void TestFixedString32()
    {
        var fs = new FixedString32("Hello");
        Assert.AreEqual(5, fs.Length);
        Assert.AreEqual("Hello", fs.ToString());
        Assert.AreEqual("Hello", fs.Value);

        fs.Value = "World";
        Assert.AreEqual(5, fs.Length);
        Assert.AreEqual("World", fs.Value);

        fs.Value = "";
        Assert.AreEqual(0, fs.Length);
        Assert.AreEqual("", fs.Value);
    }

    [TestMethod]
    public void TestFixedString32TooLong()
    {
        // MAX_LENGTH is 15
        try
        {
            new FixedString32("This string is definitely longer than 15 characters");
            Assert.Fail("Should have thrown ArgumentException");
        }
        catch (ArgumentException)
        {
            // Success
        }
    }

    [TestMethod]
    public void TestFixedString64()
    {
        var fs = new FixedString64("A bit longer string");
        Assert.AreEqual("A bit longer string", fs.Value);
    }

    [TestMethod]
    public void TestFixedString128()
    {
        var fs = new FixedString128("Even longer string for 128 bytes buffer");
        Assert.AreEqual("Even longer string for 128 bytes buffer", fs.Value);
    }
}
