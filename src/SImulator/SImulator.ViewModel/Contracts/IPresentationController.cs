using SImulator.ViewModel.Model;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;

namespace SImulator.ViewModel.Contracts;

/// <summary>
/// Represents a game presentation controller.
/// </summary>
public interface IPresentationController
{
    /// <summary>
    /// Starts new game.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the game.
    /// </summary>
    void StopGame();

    void SetGameThemes(string[] themes);

    void SetRoundThemes(ThemeInfoViewModel[] themes, bool isFinal);

    void SetMedia(MediaSource media, bool background);

    void SetStage(TableStage stage);

    void SetText(string text);

    void SetQuestionContentType(QuestionContentType questionContentType);

    void SetQuestionStyle(QuestionStyle questionStyle);

    void SetQuestionSound(bool sound);

    void AddPlayer();

    void RemovePlayer(string playerName);

    void ClearPlayers();

    void UpdatePlayerInfo(int index, PlayerInfo player);

    void UpdateSettings(Settings settings);

    void SetSound(string sound = "");

    void PlaySimpleSelection(int theme, int quest);

    void PlayComplexSelection(int theme, int quest, bool setActive);

    void PlaySelection(int theme);

    void SetPlayer(int playerIndex);

    void AddLostButtonPlayer(string name);

    void ClearLostButtonPlayers();

    void SeekMedia(int position);

    void RunMedia();

    void StopMedia();

    void RestoreQuestion(int themeIndex, int questionIndex, int price);

    void SetCaption(string caption);

    void SetLeftTime(double leftTime);
}
