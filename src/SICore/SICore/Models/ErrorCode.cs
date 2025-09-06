﻿namespace SICore.Models;

/// <summary>
/// Defines well-known error codes.
/// </summary>
public enum ErrorCode
{
    /// <summary>
    /// File is too large.
    /// </summary>
    OversizedFile = 1,

    /// <summary>
    /// Cannot kick yourself.
    /// </summary>
    CannotKickYourSelf = 2,

    /// <summary>
    /// Cannot kick bots.
    /// </summary>
    CannotKickBots = 3,

    /// <summary>
    /// Cannot set host to yourself.
    /// </summary>
    CannotSetHostToYourself = 4,

    /// <summary>
    /// Cannot set host to bots.
    /// </summary>
    CannotSetHostToBots = 5,

    /// <summary>
    /// Avatar is too big.
    /// </summary>
    AvatarTooBig = 6,

    /// <summary>
    /// Avatar is invalid.
    /// </summary>
    InvalidAvatar = 7,

    /// <summary>
    /// Person with the same name already exists.
    /// </summary>
    PersonAlreadyExists = 8,

    /// <summary>
    /// Appellation failed because of too few players.
    /// </summary>
    AppellationFailedTooFewPlayers = 9,
}
