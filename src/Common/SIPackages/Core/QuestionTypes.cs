﻿namespace SIPackages.Core;

/// <summary>
/// Provides well-known question types.
/// </summary>
public static class QuestionTypes
{
    /// <summary>
    /// Default (for current round) question type.
    /// </summary>
    public const string Default = "";

    /// <summary>
    /// Simple question type.
    /// </summary>
    public const string Simple = "simple";

    /// <summary>
    /// Stake question type.
    /// </summary>
    [Obsolete("Use Stake question type")]
    internal const string Auction = "auction";

    /// <summary>
    /// Stake question type.
    /// </summary>
    public const string Stake = "stake";

    /// <summary>
    /// Stake for all question type.
    /// </summary>
    public const string StakeAll = "stakeAll";

    /// <summary>
    /// Secret question type.
    /// </summary>
    [Obsolete("Use Secret question type")]
    internal const string Cat = "cat";

    /// <summary>
    /// Secret question type.
    /// </summary>
    public const string Secret = "secret";

    /// <summary>
    /// Secret question type with price known before question given.
    /// </summary>
    public const string SecretPublicPrice = "secretPublicPrice";

    /// <summary>
    /// Secret question type with no question (gives money immediately).
    /// </summary>
    public const string SecretNoQuestion = "secretNoQuestion";

    /// <summary>
    /// Extended secret question type.
    /// </summary>
    [Obsolete("Use Secret question type")]
    internal const string BagCat = "bagcat";

    /// <summary>
    /// No-risk question.
    /// </summary>
    [Obsolete("Use NoRisk question type")]
    internal const string Sponsored = "sponsored";

    /// <summary>
    /// No-risk question.
    /// </summary>
    public const string NoRisk = "noRisk";

    /// <summary>
    /// Question for yourself.
    /// </summary>
    /// <remarks>Reserved for <see cref="NoRisk" /> question replacement.</remarks>
    public const string ForYourself = "forYourself";

    /// <summary>
    /// Question for all players.
    /// </summary>
    public const string ForAll = "forAll";
}
