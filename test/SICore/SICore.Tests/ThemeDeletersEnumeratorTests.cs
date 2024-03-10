using NUnit.Framework;
using SICore.Clients.Game;
using System.Collections.Generic;

namespace SICore.Tests;

[TestFixture]
public sealed class ThemeDeletersEnumeratorTests
{
    [Test]
    public void RemoveLast()
    {
        var enumerator = new ThemeDeletersEnumerator(new ThemeDeletersEnumerator.IndexInfo[] { new ThemeDeletersEnumerator.IndexInfo(new HashSet<int>(new int[] { 0 })) });
        enumerator.RemoveAt(0);

        Assert.That(enumerator.IsEmpty(), Is.True);
    }

    [Test]
    public void Remove()
    {
        var variants = new HashSet<int>(new int[] { 1, 2, 6, 7, 8 });

        var enumerator = new ThemeDeletersEnumerator(new ThemeDeletersEnumerator.IndexInfo[]
        {
            new ThemeDeletersEnumerator.IndexInfo(4),
            new ThemeDeletersEnumerator.IndexInfo(0),
            new ThemeDeletersEnumerator.IndexInfo(3),
            new ThemeDeletersEnumerator.IndexInfo(variants),
            new ThemeDeletersEnumerator.IndexInfo(variants),
            new ThemeDeletersEnumerator.IndexInfo(variants),
            new ThemeDeletersEnumerator.IndexInfo(variants),
            new ThemeDeletersEnumerator.IndexInfo(variants),
            new ThemeDeletersEnumerator.IndexInfo(5)

        });

        enumerator.RemoveAt(1);

        var newVariants = new HashSet<int>(new int[] { 1, 5, 6, 7 });

        enumerator.Reset(false);
        enumerator.MoveNext();
        Assert.That(enumerator.Current.PlayerIndex, Is.EqualTo(3));
        enumerator.MoveNext();
        Assert.That(enumerator.Current.PlayerIndex, Is.EqualTo(0));
        enumerator.MoveNext();
        Assert.That(enumerator.Current.PlayerIndex, Is.EqualTo(2));
        enumerator.MoveNext();
        Assert.That(enumerator.Current.PlayerIndex, Is.EqualTo(-1));
        Assert.That(enumerator.Current.PossibleIndicies, Is.EqualTo(newVariants));
        enumerator.MoveNext();
        Assert.That(enumerator.Current.PlayerIndex, Is.EqualTo(-1));
        Assert.That(enumerator.Current.PossibleIndicies, Is.EqualTo(newVariants));
        enumerator.MoveNext();
        Assert.That(enumerator.Current.PlayerIndex, Is.EqualTo(-1));
        Assert.That(enumerator.Current.PossibleIndicies, Is.EqualTo(newVariants));
        enumerator.MoveNext();
        Assert.That(enumerator.Current.PlayerIndex, Is.EqualTo(-1));
        Assert.That(enumerator.Current.PossibleIndicies, Is.EqualTo(newVariants));
        enumerator.MoveNext();
        Assert.That(enumerator.Current.PlayerIndex, Is.EqualTo(4));

        Assert.That(enumerator.MoveNext(), Is.False);
    }

    [Test]
    public void RemoveNew()
    {
        var variants = new HashSet<int>(new int[] { 4 });
        var newVariants = new HashSet<int>(new int[] { 3 });

        var enumerator = new ThemeDeletersEnumerator(new []
        {
            new ThemeDeletersEnumerator.IndexInfo(1),
            new ThemeDeletersEnumerator.IndexInfo(0),
            new ThemeDeletersEnumerator.IndexInfo(2),
            new ThemeDeletersEnumerator.IndexInfo(variants)
        });

        enumerator.RemoveAt(3);

        enumerator.Reset(false);
        enumerator.MoveNext();
        enumerator.MoveNext();
        enumerator.MoveNext();
        enumerator.MoveNext();
        Assert.That(enumerator.Current.PossibleIndicies, Is.EqualTo(newVariants));
    }
}
