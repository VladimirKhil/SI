using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SICore;

namespace SICore.Tests;

// ---------------------------------------------------------------------
// General utility for analyzing the sequence of winners.
// ---------------------------------------------------------------------
internal static class WinnerSequenceMetrics
{
    public static int CountImmediateRepeats(IReadOnlyList<string> winners)
    {
        int count = 0;

        for (int i = 1; i < winners.Count; i++)
        {
            if (winners[i] == winners[i - 1])
            {
                count++;
            }
        }

        return count;
    }
}

[TestFixture]
[Category("Deterministic")]
public class EdgeCaseTests
{
    // Cannot choose a winner without candidates.
    [Test]
    public void ChooseAnswerer_EmptyCandidates_ThrowsArgumentException()
    {
        var randomizer = new AnswererRandomizer(2);

        Assert.Throws<ArgumentException>(() => randomizer.ChooseAnswerer(new List<string>()));
    }
}

[TestFixture]
[Category("Deterministic")]
public class DeterministicInvariantTests
{
    // If only one player pressed, they win immediately, as there is no one to share the button with.
    [Test]
    public void SoloCandidate_AlwaysWins()
    {
        var randomizer = new AnswererRandomizer(1);

        for (int i = 0; i < 50; i++)
        {
            var winner = randomizer.ChooseAnswerer(new List<string> { "A" });
            Assert.That(winner, Is.EqualTo("A"));
        }
    }

    // Two participating players must win strictly in alternation.
    [Test]
    public void TwoCandidates_AlternateFromSecondDraw()
    {
        var randomizer = new AnswererRandomizer(2);
        var candidates = new List<string> { "A", "B" };
        string? previous = null;

        for (int draw = 0; draw < 100; draw++)
        {
            var winner = randomizer.ChooseAnswerer(candidates);

            if (previous != null)
            {
                Assert.That(winner, Is.EqualTo(previous == "A" ? "B" : "A"), $"Repeat winner on draw {draw}");
            }

            previous = winner;
        }
    }

    // The repeat ban does not apply if on the next button draw the previous winner was the only one who pressed the button.
    [Test]
    public void Ban_DoesNotApply_WhenBannedPlayerIsOnlyCandidate()
    {
        var randomizer = new AnswererRandomizer(2);

        // Someone won the button
        var prevWinner = randomizer.ChooseAnswerer(new List<string> { "A", "B" });

        // Next time only they pressed — they win
        var winner = randomizer.ChooseAnswerer(new List<string> { prevWinner });

        Assert.That(winner, Is.EqualTo(prevWinner));
    }

    // A player who just won the button solo (there were no competitors) must not
    // win it on the next button draw if at least one competitor has appeared.
    [Test]
    public void SoloWinner_ImmediatelyBanned_WhenPairedNext()
    {
        var randomizer = new AnswererRandomizer(2);

        // "A" won the button uncontested
        var firstWinner = randomizer.ChooseAnswerer(new List<string> { "A" });
        Assert.That(firstWinner, Is.EqualTo("A"));

        // Now their competitor must win
        var secondWinner = randomizer.ChooseAnswerer(new List<string> { "B", "A" });
        Assert.That(secondWinner, Is.EqualTo("B"));
    }

    // The previous winner's ban applies globally, even if a new competitor faces them for the first time in the game.
    [Test]
    public void Ban_TargetsMostRecentWinner_AcrossPlayersWhoNeverMetBefore()
    {
        var randomizer = new AnswererRandomizer(3);

        // X wins solo
        var w1 = randomizer.ChooseAnswerer(new List<string> { "X" });
        Assert.That(w1, Is.EqualTo("X"));

        // B enters: X is banned as the previous winner, so B wins
        var w2 = randomizer.ChooseAnswerer(new List<string> { "B", "X" });
        Assert.That(w2, Is.EqualTo("B"));

        // A enters: now B is banned, so A wins the button
        var w3 = randomizer.ChooseAnswerer(new List<string> { "A", "B" });
        Assert.That(w3, Is.EqualTo("A"));
    }

    // The duration of a player's absence does not matter: the ban is tied only to the previous winner.
    [Test]
    public void Ban_UsesCurrentPrevWinner_NotAnythingRelatedToAbsenceDuration()
    {
        var randomizer = new AnswererRandomizer(3);

        randomizer.ChooseAnswerer(new List<string> { "X" });      // X won
        randomizer.ChooseAnswerer(new List<string> { "B", "X" }); // B won (X is banned)

        // B skips two button draws while X presses the button solo
        randomizer.ChooseAnswerer(new List<string> { "X" });      // X won
        randomizer.ChooseAnswerer(new List<string> { "X" });      // X again (last winner is X)

        // B returned: X was the winner just now, which means X yields to B again
        var winner = randomizer.ChooseAnswerer(new List<string> { "B", "X" });

        Assert.That(
            winner,
            Is.EqualTo("B"),
            "Ban must attach to current prevWinner (X), rather than depend on B having been absent");
    }

    // Adding players mid-match must not break the ban on two consecutive wins.
    [Test]
    public void GrowingPlayerCount_NeverRepeatsWinnerImmediately()
    {
        var randomizer = new AnswererRandomizer(2);
        var winners = new List<string>();

        // Two-player segment of the game
        var pair = new List<string> { "A", "B" };
        for (int draw = 0; draw < 10; draw++)
        {
            winners.Add(randomizer.ChooseAnswerer(pair));
        }

        // A third player was added and, accordingly, the window size changed
        randomizer.SetPlayersCount(3);
        var trio = new List<string> { "A", "B", "C" };
        for (int draw = 0; draw < 200; draw++)
        {
            winners.Add(randomizer.ChooseAnswerer(trio));
        }

        // There must not be a single consecutive win
        Assert.That(WinnerSequenceMetrics.CountImmediateRepeats(winners), Is.EqualTo(0));
    }

    // In a dynamic match with random rosters of pressers, the previous winner never wins two consecutive button draws (unless they are alone).
    [Test]
    public void FullParticipationEveryDraw_NeverRepeatsWinnerImmediately([Range(2, 10)] int playersCount)
    {
        var allPlayers = Enumerable.Range(0, playersCount).Select(i => $"P{i}").ToList();
        var randomizer = new AnswererRandomizer(playersCount);
        var rng = new Random(42);
        var winners = new List<string>();
        string? prevWinner = null;
        const int draws = 300;

        for (int draw = 0; draw < draws; draw++)
        {
            var candidates = new List<string>();

            if (prevWinner != null)
            {
                // The previous winner presses again
                candidates.Add(prevWinner);

                // Assemble a random pool of competitors for them (from 1 to all remaining)
                var otherPlayers = allPlayers.Where(p => p != prevWinner).OrderBy(_ => rng.Next()).ToList();
                int competitorsCount = rng.Next(1, otherPlayers.Count + 1);
                candidates.AddRange(otherPlayers.Take(competitorsCount));
            }
            else
            {
                // For the first button draw, take a random group of at least 2 people
                candidates = allPlayers.OrderBy(_ => rng.Next()).Take(rng.Next(2, allPlayers.Count + 1)).ToList();
            }

            var winner = randomizer.ChooseAnswerer(candidates);
            winners.Add(winner);
            prevWinner = winner;
        }

        Assert.That(WinnerSequenceMetrics.CountImmediateRepeats(winners), Is.EqualTo(0));
    }
}

[TestFixture]
[Category("Statistical")]
public sealed class StatisticalPropertyTests
{
    // If all N players press the button with equal frequency, each of them
    // on average wins the button once every N button draws.
    [Test]
    public void EqualProbability_MeanIntervalConvergesToPlayerCount([Range(2, 10)] int playerCount, [Range(0, 10)] int seed)
    {
        var players = Enumerable.Range(0, playerCount).Select(i => $"P{i}").ToList();
        var randomizer = new AnswererRandomizer(playerCount, seed);
        var winners = new List<string>();
        const int draws = 3000;

        for (int draw = 0; draw < draws; draw++)
        {
            winners.Add(randomizer.ChooseAnswerer(players));
        }

        // Record the number of button draws between wins for each player
        var playerGaps = players.ToDictionary(p => p, _ => new List<int>());
        var lastSeen = new Dictionary<string, int>();

        for (int i = 0; i < winners.Count; i++)
        {
            var w = winners[i];

            if (lastSeen.TryGetValue(w, out var last))
            {
                playerGaps[w].Add(i - last);
            }

            lastSeen[w] = i;
        }

        // Verify that the average win interval of each player falls within the allowed 10% margin of error
        foreach (var p in players)
        {
            Assert.That(playerGaps[p], Is.Not.Empty, $"Player {p} never won repeatedly during the game");

            double playerMean = playerGaps[p].Average();
            Assert.That(
                playerMean,
                Is.EqualTo(playerCount).Within(playerCount * 0.1),
                $"For player {p}, average win interval ({playerMean:F2}) deviates from N={playerCount}");
        }
    }

    // Players with different activity have equal chances of winning in their button draws,
    // but more active players win the button more often overall.
    [Test]
    public void DifferentPressProbabilities_FairRatioAndAbsoluteWinsCorrelateWithActivity([Range(0, 10)] int seed)
    {
        var probabilities = new Dictionary<string, double>
        {
            { "A", 0.75 }, { "B", 0.60 }, { "C", 0.60 }, { "D", 0.45 }, { "E", 0.45 }
        };
        var players = probabilities.Keys.ToList();

        var randomizer = new AnswererRandomizer(players.Count, seed);
        var participationRandom = new Random(seed);

        var wins = players.ToDictionary(p => p, _ => 0);
        var participations = players.ToDictionary(p => p, _ => 0);

        const int draws = 300;

        for (int draw = 0; draw < draws; draw++)
        {
            var candidates = players.Where(p => participationRandom.NextDouble() < probabilities[p]).ToList();

            if (candidates.Count == 0)
            {
                candidates.Add(players[participationRandom.Next(players.Count)]);
            }

            foreach (var c in candidates)
            {
                participations[c]++;
            }

            wins[randomizer.ChooseAnswerer(candidates)]++;
        }

        // The win-to-press ratio should be approximately equal for all
        var ratios = players.ToDictionary(p => p, p => (double)wins[p] / participations[p]);
        double spread = ratios.Values.Max() - ratios.Values.Min();

        Assert.That(
            spread,
            Is.LessThan(0.1),
            $"Ratio spread: {string.Join(", ", ratios.Select(kv => $"{kv.Key}={kv.Value:F2}"))}");

        // A more active player must win more often in total than a passive one
        Assert.That(wins["A"], Is.GreaterThan(wins["B"]));
        Assert.That(wins["B"], Is.GreaterThan(wins["D"]));
    }

    // A new player who joined mid-game must win the button within a reasonable number of button draws.
    [Test]
    public void NewPlayerAddedMidGame_EventuallyWinsWithinReasonableTime()
    {
        // 10% threshold accounts for statistical variance across 200 random seeds
        const double maxAllowedFailureShare = 0.10;
        const int seedsToTry = 200;
        const int maxDrawsToWait = 4;

        var drawsUntilFirstWin = new List<int>();
        int neverWonCount = 0;

        for (int seed = 0; seed < seedsToTry; seed++)
        {
            var randomizer = new AnswererRandomizer(2, seed);
            var pair = new List<string> { "A", "B" };

            for (int step = 0; step < 20; step++)
            {
                randomizer.ChooseAnswerer(pair);
            }

            var trio = new List<string> { "A", "B", "C" };
            randomizer.SetPlayersCount(3);

            bool won = false;

            for (int draw = 1; draw <= maxDrawsToWait; draw++)
            {
                if (randomizer.ChooseAnswerer(trio) == "C")
                {
                    drawsUntilFirstWin.Add(draw);
                    won = true;
                    break;
                }
            }

            if (!won)
            {
                neverWonCount++;
            }
        }

        double avgDraws = drawsUntilFirstWin.Average();

        Assert.That(
            avgDraws,
            Is.LessThanOrEqualTo(3),
            $"On average, a new player waits for their first win too long: {avgDraws:F1} button draws");

        double failureShare = (double)neverWonCount / seedsToTry;

        Assert.That(
            failureShare,
            Is.LessThanOrEqualTo(maxAllowedFailureShare),
            "A new player should almost always win within the expected number of button draws");
    }

    // A player who pauses their button presses does not lose chances in those
    // contests where they participate on equal terms with regular players.
    [Test]
    public void ReturningPlayer_NotSystematicallyDisadvantaged_InDrawsWhereBothPresent([Range(0, 10)] int seed)
    {
        const int totalDraws = 300;
        long winsOfB = 0;
        long winsOfC = 0;

        var randomizer = new AnswererRandomizer(3, seed);

        for (int draw = 0; draw < totalDraws; draw++)
        {
            // Player B participates in blocks of 5 button draws: alternating between pressing the button and skipping
            bool bPresent = (draw / 5) % 2 == 0;
            var candidates = bPresent
                ? new List<string> { "A", "B", "C" }
                : new List<string> { "A", "C" };

            var winner = randomizer.ChooseAnswerer(candidates);

            if (!bPresent)
            {
                continue;
            }

            if (winner == "B")
            {
                winsOfB++;
            }

            if (winner == "C")
            {
                winsOfC++;
            }
        }

        double shareOfB = (double)winsOfB / (winsOfB + winsOfC);

        Assert.That(
            shareOfB,
            Is.EqualTo(0.5).Within(0.1),
            $"In button draws where B and C are both candidates, B's win share should not be underestimated: {shareOfB:F2}");
    }

    // With a large number of participants at the table, when only 2 or 3 people manage to press
    // per button draw, wins are still distributed fairly.
    [Test]
    public void SparseParticipation_TenPlayers_TwoOrThreeRespondPerDraw([Range(0, 10)] int seed)
    {
        var players = Enumerable.Range(0, 10).Select(i => $"P{i}").ToList();
        var randomizer = new AnswererRandomizer(players.Count, seed);
        var pickRandom = new Random(seed);

        var wins = players.ToDictionary(p => p, _ => 0);
        var participations = players.ToDictionary(p => p, _ => 0);
        var weightedWinners = new List<string>();
        const int draws = 1000;

        for (int draw = 0; draw < draws; draw++)
        {
            // Randomly select 2 or 3 contenders per button draw
            int respondersCount = pickRandom.Next(2, 4);
            var candidates = players.OrderBy(_ => pickRandom.Next()).Take(respondersCount).ToList();

            foreach (var c in candidates)
            {
                participations[c]++;
            }

            var winner = randomizer.ChooseAnswerer(candidates);
            wins[winner]++;
            weightedWinners.Add(winner);
        }

        var ratios = players.Select(p => (double)wins[p] / participations[p]).ToList();

        Assert.That(
            ratios.Max() - ratios.Min(),
            Is.LessThan(0.1),
            "With 10 players and 2-3 pressing per button draw, the win/participation ratio should remain close for everyone");
    }

    // When the player count increases, the accumulated losing streak is not lost,
    // but grants more chances to finally win the button.
    [Test]
    public void SetPlayersCount_ExpandingWindow_UnlocksFullAccumulatedLosingStreak()
    {
        int aWins = 0;
        int validTrials = 0;

        // A crowd of bots is needed to force the player to lose several times in a row
        var bots = Enumerable.Range(0, 100).Select(i => $"Bot_{i}").ToList();
        var crowd = new List<string> { "A" };
        crowd.AddRange(bots);

        var duel = new List<string> { "A", "B" };
        const int seedsToTry = 3000;

        for (int i = 0; i < seedsToTry; i++)
        {
            // Set a window of size 1, so the player's maximum weight is capped at 2 (1 + 1)
            var randomizer = new AnswererRandomizer(1);
            bool aAccidentallyWon = false;

            for (int step = 0; step < 6; step++)
            {
                if (randomizer.ChooseAnswerer(crowd) == "A")
                {
                    aAccidentallyWon = true;
                    break;
                }
            }

            // Skip the trial if the player accidentally won among the bots
            if (aAccidentallyWon)
            {
                continue;
            }

            // Increase the window to 6: now all 6 consecutive losses give the player a weight of 7 (1 + 6)
            randomizer.SetPlayersCount(6);

            // Player A's weight is 7, newcomer B's weight is 1.
            if (randomizer.ChooseAnswerer(duel) == "A")
            {
                aWins++;
            }

            validTrials++;
        }

        double winRate = (double)aWins / validTrials;

        // The expected win rate is 7/8 (87.5%). Without expanding the window, the weight would remain 2, and the win rate would be 2/3 (66.7%).
        Assert.That(
            winRate,
            Is.EqualTo(7.0 / 8.0).Within(0.05),
            $"Win rate of the player with unlocked history should be close to 87.5% (7/8), actual: {winRate:P1}");
    }

    // If the number of participants has decreased, the past series of losses
    // should not give an excessive advantage in the new roster.
    [Test]
    public void SetPlayersCount_ShrinkingWindow_CapsAccumulatedWeightToNewWindow()
    {
        int aWins = 0;
        int validTrials = 0;

        // A crowd of bots is needed to force the player to lose several times in a row
        var bots = Enumerable.Range(0, 100).Select(i => $"Bot_{i}").ToList();
        var crowd = new List<string> { "A" };
        crowd.AddRange(bots);

        var duel = new List<string> { "A", "B" };
        const int seedsToTry = 3000;

        for (int i = 0; i < seedsToTry; i++)
        {
            // Set a wide window of 6, where the player's weight can grow up to 7 (1 + 6)
            var randomizer = new AnswererRandomizer(6);
            bool aAccidentallyWon = false;

            for (int step = 0; step < 6; step++)
            {
                if (randomizer.ChooseAnswerer(crowd) == "A")
                {
                    aAccidentallyWon = true;
                    break;
                }
            }

            // Skip the trial if the player accidentally won among the bots
            if (aAccidentallyWon)
            {
                continue;
            }

            // Shrink the window to 1: the losing streak is preserved, but only 1 is taken for weight calculation (weight drops to 2)
            randomizer.SetPlayersCount(1);

            // Player A's weight is 2, newcomer B's weight is 1.
            if (randomizer.ChooseAnswerer(duel) == "A")
            {
                aWins++;
            }

            validTrials++;
        }

        double winRate = (double)aWins / validTrials;

        // The expected win rate is 2/3 (about 66.7%). Without shrinking the window, the weight would remain 7, and the win rate would be 87.5%.
        Assert.That(
            winRate,
            Is.EqualTo(2.0 / 3.0).Within(0.05),
            $"Win rate of the player with truncated window should be close to 66.7% (2/3), actual: {winRate:P1}");
    }

    // Upon temporarily leaving and returning to the game, the player's losing streak is preserved:
    // it is neither reset nor increased during their absence.
    [Test]
    public void DisconnectedAndReconnectedPlayer_PreservesAccumulatedLosingStreak()
    {
        int aWins = 0;
        int validTrials = 0;

        var trio = new List<string> { "A", "B", "C" };
        var pair = new List<string> { "B", "C" };
        const int seedsToTry = 6000;

        for (int seed = 0; seed < seedsToTry; seed++)
        {
            var randomizer = new AnswererRandomizer(3, seed);

            // 1. Player A loses 3 times in a row with a full roster of 3 players.
            // Due to the repeat rule, players B and C strictly alternate wins.
            if (randomizer.ChooseAnswerer(trio) == "A")
            {
                continue;
            }

            if (randomizer.ChooseAnswerer(trio) == "A")
            {
                continue;
            }

            if (randomizer.ChooseAnswerer(trio) == "A")
            {
                continue;
            }

            // 2. Player A leaves, the window shrinks to 2. Players B and C play 10 times between each other.
            randomizer.SetPlayersCount(2);

            for (int step = 0; step < 10; step++)
            {
                randomizer.ChooseAnswerer(pair);
            }

            // 3. Player A returns to the game, the window expands to 3 again.
            randomizer.SetPlayersCount(3);

            // Now all three participants press the button.
            // The previous winner gets a weight of 0 due to the repeat ban.
            // The loser of the duel has 1 loss (weight 1 + 1 = 2).
            // The returning player A preserved 3 losses (weight 1 + 3 = 4). The sum of weights is 6.
            if (randomizer.ChooseAnswerer(trio) == "A")
            {
                aWins++;
            }

            validTrials++;
        }

        double winRate = (double)aWins / validTrials;

        // The expected chance of player A is 4 out of 6 (about 66.7%). If the history were reset, the chance would be 1 out of 3 (33.3%).
        Assert.That(
            winRate,
            Is.EqualTo(4.0 / 6.0).Within(0.05),
            $"Win rate of the returned player should be close to 66.7% (4/6), actual: {winRate:P1}");
    }

    // Verifies the fairness of the algorithm over a long distance with a random number of participants,
    // comparing the actual wins of each player with the accumulated mathematical expectation.
    [Test]
    public void Algorithm_HasNoBias_ActualWinsTightlyMatchExpectation([Range(0, 10)] int seed)
    {
        const int playerCount = 6;
        const int draws = 1000;
        const double tolerance = 0.10; // Allowed tolerance corridor 10%

        var players = Enumerable.Range(0, playerCount).Select(i => $"P{i}").ToArray();
        var randomizer = new AnswererRandomizer(playerCount, seed);
        var pressRng = new Random(seed);

        var actualWins = players.ToDictionary(p => p, _ => 0);
        var expectedWins = players.ToDictionary(p => p, _ => 0.0);
        var streaks = players.ToDictionary(p => p, _ => 0);
        string? prevWinner = null;

        var pool = (string[])players.Clone();

        for (int draw = 0; draw < draws; draw++)
        {
            int roll = pressRng.Next(100);

            // Model the press distribution from real games.
            // Real games have such a distribution on average
            int respondersCount = roll switch
            {
                < 65 => 1,                                // 65% of button draws: solo (one player)
                < 88 => 2,                                // 23% of button draws: duel (two players)
                < 95 => 3,                                // 7% of button draws: three candidates
                < 98 => 4,                                // 3% of button draws: four candidates
                _ => pressRng.Next(5, playerCount + 1)    // 2% of button draws: mass competition (five or more players)
            };

            // Fast selection algorithm for random participants
            for (int i = 0; i < respondersCount; i++)
            {
                int swapIdx = pressRng.Next(i, playerCount);
                (pool[i], pool[swapIdx]) = (pool[swapIdx], pool[i]);
            }

            var candidates = pool.Take(respondersCount).ToList();

            // 1. Theoretical calculation of weights for the current button draw
            var weights = new Dictionary<string, int>(respondersCount);

            foreach (var p in candidates)
            {
                int w = 1 + Math.Min(streaks[p], playerCount);

                // The repeat ban applies only when competitors are present
                if (p == prevWinner && candidates.Count > 1)
                {
                    w = 0;
                }

                weights[p] = w;
            }

            int totalWeight = weights.Values.Sum();

            // 2. Accumulation of theoretical win probability
            foreach (var p in candidates)
            {
                double prob = totalWeight > 0 ? (double)weights[p] / totalWeight : 0.0;
                expectedWins[p] += prob;
            }

            // 3. Button draw
            var winner = randomizer.ChooseAnswerer(candidates);
            actualWins[winner]++;

            // 4. Updating the history of losses
            foreach (var p in candidates)
            {
                streaks[p] = p == winner ? 0 : streaks[p] + 1;
            }

            prevWinner = winner;
        }

        // 5. Check: actual result must fall within 10% margin of error from expectation
        foreach (var p in players)
        {
            double expected = expectedWins[p];
            int actual = actualWins[p];
            double ratio = actual / expected;

            Assert.That(
                ratio,
                Is.EqualTo(1.0).Within(tolerance),
                $"For player {p}: actual={actual}, expected={expected:F1}, ratio={ratio:P1}. " +
                $"Deviation exceeded the allowable threshold {tolerance:P0}!");
        }
    }
}
