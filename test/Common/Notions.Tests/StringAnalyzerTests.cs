using NUnit.Framework;

namespace Notions.Tests;

[TestFixture]
internal sealed class StringAnalyzerTests
{
    [Test]
    public void LongestCommonSubstring_Ok()
    {
        var match = StringAnalyzer.LongestCommonSubstring("abc", "dabe", "panb0");

        Assert.That(match, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(match!.Value.Substring, Is.EqualTo("ab"));
            Assert.That(match!.Value.PositionsHistory, Has.Length.EqualTo(2));
        });

        Assert.Multiple(() =>
        {
            Assert.That(match!.Value.PositionsHistory[0], Is.EqualTo(new int[] { 0, 1, 1 }));
            Assert.That(match!.Value.PositionsHistory[1], Is.EqualTo(new int[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void LongestCommonSubstring2_Ok()
    {
        var match = StringAnalyzer.LongestCommonSubstring(
            "10. Some question text. Answer: some answer",
            "20. Another interresting text. Answer: here it is",
            "30. More data here. Answer: something",
            "40. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Answer: Duis dictum",
            "50. Pellentesque nibh urna, tincidunt a risus eget. Answer: porttitor volutpat est");

        Assert.That(match, Is.Not.Null);

        Assert.That(match!.Value.Substring, Is.EqualTo("0. te. Answer: s"));
    }
}
