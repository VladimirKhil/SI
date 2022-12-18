using NUnit.Framework;

namespace Notions.Tests;

public sealed class StringExtensionTests
{
    [Test]
    [TestCase("test", "Test")]
    [TestCase("@test", "@Test")]
    [TestCase("http://ya.ru", "http://ya.ru")]
    [TestCase("https://ya.ru", "https://ya.ru")]
    [TestCase("", "")]
    public void GrowFirstLetter_Ok(string input, string expectedOutput)
    {
        var result = input.GrowFirstLetter();
        Assert.AreEqual(expectedOutput, result);
    }

    [TestCase("a sample text", 8, "a sample")]
    [TestCase("😎😎😎", 5, "😎😎")]
    [TestCase("abc", 0, "")]
    [TestCase("", 10, "")]
    public void Shorten_Ok(string input, int maxLength, string expectedOutput)
    {
        var result = input.Shorten(maxLength);
        Assert.AreEqual(expectedOutput, result);
    }
}