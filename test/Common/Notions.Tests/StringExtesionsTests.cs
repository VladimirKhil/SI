using NUnit.Framework;

namespace Notions.Tests
{
    public sealed class Tests
    {
        [Test]
        [TestCase("test", "Test")]
        [TestCase("@test", "@Test")]
        [TestCase("http://ya.ru", "http://ya.ru")]
        [TestCase("https://ya.ru", "https://ya.ru")]
        [TestCase(null, null)]
        [TestCase("", "")]
        public void GrowFirstLetter_Ok(string input, string expectedOutput)
        {
            var result = input.GrowFirstLetter();
            Assert.AreEqual(expectedOutput, result);
        }
    }
}