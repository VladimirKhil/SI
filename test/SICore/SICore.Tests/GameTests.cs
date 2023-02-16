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
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(enumerator.Current.PlayerIndex, order[i]);

            if (enumerator.Current.PlayerIndex == -1)
            {
                var found = false;
                for (int j = 0; j < playerSets.Count; j++)
                {
                    if (playerSets[j].Intersect(enumerator.Current.PossibleIndicies).Any())
                    {
                        found = true;
                        Assert.AreEqual(playerSets[j], enumerator.Current.PossibleIndicies);
                    }
                }

                if (!found)
                {
                    playerSets.Add(enumerator.Current.PossibleIndicies);
                }
            }
        }

        Assert.IsFalse(enumerator.MoveNext());

        enumerator.Reset(false);
        while (enumerator.MoveNext())
        {
            if (enumerator.Current.PlayerIndex == -1)
            {
                var list = enumerator.Current.PossibleIndicies;
                var count = list.Count;
                var newIndex = list.First();

                enumerator.Current.SetIndex(newIndex);

                Assert.AreEqual(enumerator.Current.PlayerIndex, newIndex);
                Assert.IsTrue(list.Count == count - 1);
                Assert.IsTrue(!list.Contains(newIndex));
            }
        }
    }
}
