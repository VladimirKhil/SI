using SIPackages;
using SIPackages.Core;

namespace SIEngine.Core;

/// <summary>
/// Handles question play stages.
/// </summary>
public interface IQuestionPlayHandler
{
    /// <summary>
    /// Shows text.
    /// </summary>
    /// <param name="text">Text to show.</param>
    /// <param name="backgroundSound">Background audio.</param>
    void OnText(string text, IMedia? backgroundSound);

    /// <summary>
    /// Displays text to read aloud.
    /// </summary>
    /// <param name="oralText">Text to read.</param>
    void OnOral(string oralText);

    /// <summary>
    /// Shows image.
    /// </summary>
    /// <param name="image">Image to show.</param>
    /// <param name="backgroundSound">Background audio.</param>
    void OnImage(IMedia image, IMedia? backgroundSound);

    /// <summary>
    /// Plays audio.
    /// </summary>
    /// <param name="sound">Audio to play.</param>
    void OnSound(IMedia sound);

    /// <summary>
    /// Plays video.
    /// </summary>
    /// <param name="video">Video to play.</param>
    void OnVideo(IMedia video);

    /// <summary>
    /// Processes unsupported content.
    /// </summary>
    /// <param name="atom">Unsupported content.</param>
    void OnUnsupportedAtom(Atom atom);

    /// <summary>
    /// Asks for the answer.
    /// </summary>
    void AskAnswer();
}
