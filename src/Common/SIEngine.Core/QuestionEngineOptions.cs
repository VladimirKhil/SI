﻿using SIPackages.Core;

namespace SIEngine.Core;

/// <summary>
/// Defines question engine options.
/// </summary>
public sealed class QuestionEngineOptions
{
    /// <summary>
    /// False starts mode.
    /// </summary>
    public FalseStartMode FalseStarts { get; set; } = FalseStartMode.Enabled;

    /// <summary>
    /// Show simple right answers flag. Complex right answers are always shown.
    /// </summary>
    public bool ShowSimpleRightAnswers { get; set; }

    /// <summary>
    /// Default type name.
    /// </summary>
    public string DefaultTypeName { get; set; } = QuestionTypes.Simple;

    /// <summary>
    /// Play special questions.
    /// </summary>
    public bool PlaySpecials { get; set; } = true;
}
