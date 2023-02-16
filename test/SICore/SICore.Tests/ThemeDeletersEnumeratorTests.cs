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

        Assert.AreEqual(true, enumerator.IsEmpty());
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
        Assert.AreEqual(3, enumerator.Current.PlayerIndex);
        enumerator.MoveNext();
        Assert.AreEqual(0, enumerator.Current.PlayerIndex);
        enumerator.MoveNext();
        Assert.AreEqual(2, enumerator.Current.PlayerIndex);
        enumerator.MoveNext();
        Assert.AreEqual(-1, enumerator.Current.PlayerIndex);
        Assert.AreEqual(newVariants, enumerator.Current.PossibleIndicies);
        enumerator.MoveNext();
        Assert.AreEqual(-1, enumerator.Current.PlayerIndex);
        Assert.AreEqual(newVariants, enumerator.Current.PossibleIndicies);
        enumerator.MoveNext();
        Assert.AreEqual(-1, enumerator.Current.PlayerIndex);
        Assert.AreEqual(newVariants, enumerator.Current.PossibleIndicies);
        enumerator.MoveNext();
        Assert.AreEqual(-1, enumerator.Current.PlayerIndex);
        Assert.AreEqual(newVariants, enumerator.Current.PossibleIndicies);
        enumerator.MoveNext();
        Assert.AreEqual(4, enumerator.Current.PlayerIndex);

        Assert.IsFalse(enumerator.MoveNext());
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
        Assert.AreEqual(newVariants, enumerator.Current.PossibleIndicies);
    }
}
