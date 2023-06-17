using SICore.Clients.Viewer;
using SIData;
using SIUI.ViewModel;

namespace SICore;

/// <summary>
/// Defines a viewer behavior.
/// </summary>
public interface IViewerLogic : ILogic
{
    IPlayerLogic PlayerLogic { get; }

    IShowmanLogic ShowmanLogic { get; }

    TableInfoViewModel TInfo { get; }

    bool CanSwitchType { get; }

    /// <summary>
    /// Получение сообщений
    /// </summary>
    void ReceiveText(Message m);

    /// <summary>
    /// Печать в протокол формы
    /// </summary>
    /// <param name="text">Текст</param>
    void Print(string text);

    /// <summary>
    /// Новое состояние игры
    /// </summary>
    void Stage();

    /// <summary>
    /// Получены темы игры
    /// </summary>
    void GameThemes();

    /// <summary>
    /// Получены темы раунда
    /// </summary>
    void RoundThemes(bool print);

    /// <summary>
    /// Игрок выбрал вопрос
    /// </summary>
    void Choice();

    /// <summary>
    /// Question fragment received.
    /// </summary>
    void OnScreenContent(string[] mparams);

    /// <summary>
    /// Question background fragment received.
    /// </summary>
    void OnBackgroundContent(string[] mparams);

    void SetRight(string answer);

    void Resume();

    /// <summary>
    /// Можно жать на кнопку
    /// </summary>
    void Try();

    /// <summary>
    /// Жать уже нельзя
    /// </summary>
    void EndTry(string text);

    /// <summary>
    /// Показать табло
    /// </summary>
    void ShowTablo();

    /// <summary>
    /// Игрок получил или потерял деньги
    /// </summary>
    void Person(int playerIndex, bool isRight);

    /// <summary>
    /// Известен тип вопроса
    /// </summary>
    void OnQuestionType();

    /// <summary>
    /// Завершение раунда
    /// </summary>
    void StopRound();

    /// <summary>
    /// Удалена тема
    /// </summary>
    void Out(int themeIndex);

    /// <summary>
    /// Победитель игры
    /// </summary>
    void Winner();

    /// <summary>
    /// Время вышло
    /// </summary>
    void TimeOut();

    /// <summary>
    /// Размышления в финале
    /// </summary>
    void FinalThink();

    /// <summary>
    /// Обновление изображения
    /// </summary>
    /// <param name="i"></param>
    /// <param name="path"></param>
    void UpdatePicture(Account account, string path);

    /// <summary>
    /// Попытка подключения
    /// </summary>
    void TryConnect(IConnector connector);

    void OnTextSpeed(double speed);

    void SetText(string text, TableStage stage = TableStage.Round);

    void OnPauseChanged(bool isPaused);

    void TableLoaded();

    void PrintGreeting();

    void TextShape(string[] mparams);

    void OnTimeChanged();

    void OnTimerChanged(int timerIndex, string timerCommand, string arg, string person);

    void OnPersonFinalStake(int playerIndex);

    void OnPersonFinalAnswer(int playerIndex);

    void OnPackageLogo(string uri);

    void OnPersonApellated(int playerIndex);

    void OnPersonPass(int playerIndex);

    void OnReplic(string personCode, string text);

    void OnRoundContent(string[] mparams) { }

    void OnAtomHint(string hint) { }

    void ReloadMedia() { }

    void OnBannedList(IEnumerable<BannedInfo> banned) { }

    void OnBanned(BannedInfo bannedInfo) { }

    void OnUnbanned(string ip) { }

    void SetCaption(string caption) { }

    void OnGameMetadata(string gameName, string packageName, string contactUri, string voiceChatUri) { }

    void AddPlayer(PlayerAccount account) { }

    void RemovePlayerAt(int index) { }

    void ResetPlayers() { }
}
