using NUnit.Framework;
using SICore.Clients.Game;
using SIData;
using System.Collections.Generic;
using System.Linq;

namespace SICore.Tests;

[TestFixture]
public sealed class GameTests
{
    [Test]
    [TestCase(new int[] { 100, 200, 200, -60, 700 }, 2, new int[] { 4, 0, -1, -1 })]
    [TestCase(new int[] { 100, 200, 200, -60, 700 }, 3, new int[] { -1, 4, 0, -1 })]
    [TestCase(new int[] { 100, 200, 200, -60, 700 }, 4, new int[] { -1, -1, 4, 0 })]
    [TestCase(new int[] { 100, 200, 200, -60, 700 }, 5, new int[] { 0, -1, -1, 4 })]
    [TestCase(new int[] { 100, 200, 200, -60, 700 }, 6, new int[] { 4, 0, -1, -1 })]
    public void FinalStakersTest(int[] sums, int themesCount, int[] order)
    {
        var players = new List<GamePlayerAccount>();

        for (int i = 0; i < sums.Length; i++)
        {
            players.Add(new GamePlayerAccount(new Account()) { Sum = sums[i], InGame = sums[i] > 0 });
        }

        var enumerator = new ThemeDeletersEnumerator(players, themesCount);
        enumerator.Reset(false);

        var playerSets = new List<HashSet<int>>();

        for (int i = 0; i < order.Length; i++)
        {
            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current.PlayerIndex, Is.EqualTo(order[i]));

            if (enumerator.Current.PlayerIndex == -1)
            {
                var found = false;
                for (int j = 0; j < playerSets.Count; j++)
                {
                    if (playerSets[j].Intersect(enumerator.Current.PossibleIndicies).Any())
                    {
                        found = true;
                        Assert.That(enumerator.Current.PossibleIndicies, Is.EqualTo(playerSets[j]));
                    }
                }

                if (!found)
                {
                    playerSets.Add(enumerator.Current.PossibleIndicies);
                }
            }
        }

        Assert.That(enumerator.MoveNext(), Is.False);

        enumerator.Reset(false);
        while (enumerator.MoveNext())
        {
            if (enumerator.Current.PlayerIndex == -1)
            {
                var list = enumerator.Current.PossibleIndicies;
                var count = list.Count;
                var newIndex = list.First();

                enumerator.Current.SetIndex(newIndex);

                Assert.That(enumerator.Current.PlayerIndex, Is.EqualTo(newIndex));
                Assert.That(list.Count, Is.EqualTo(count - 1));
                Assert.That(list.Contains(newIndex), Is.False);
            }
        }
    }
}
