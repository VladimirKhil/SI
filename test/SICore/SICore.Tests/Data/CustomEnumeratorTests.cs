using NUnit.Framework;
using SICore.Extensions;

namespace SICore.Tests.Data;

[TestFixture]
public sealed class CustomEnumeratorTests
{
    [Test]
    public void Enumerate_Ok()
    {
        var enumerator = new CustomEnumerator<int>(new int[] { 1, 3, 2 });
        
        Assert.That(enumerator.MoveNext(), Is.True);
        Assert.That(enumerator.Current, Is.EqualTo(1));
        Assert.That(enumerator.MoveNext(), Is.True);
        Assert.That(enumerator.Current, Is.EqualTo(3));
        Assert.That(enumerator.MoveNext(), Is.True);
        Assert.That(enumerator.Current, Is.EqualTo(2));
        Assert.That(enumerator.MoveNext(), Is.False);
    }

    [Test]
    public void Update_Ok()
    {
        var enumerator = new CustomEnumerator<int>(new int[] { 1, 3, 2 });

        enumerator.MoveNext();
        enumerator.Update(CustomEnumeratorUpdaters.RemoveByIndex(2));
        Assert.That(enumerator.Current, Is.EqualTo(1));

        enumerator.MoveNext();
        Assert.That(enumerator.Current, Is.EqualTo(2));
        Assert.That(enumerator.MoveNext(), Is.False);
    }
}
