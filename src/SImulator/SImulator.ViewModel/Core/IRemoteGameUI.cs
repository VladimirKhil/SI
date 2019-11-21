using System.ServiceModel;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;
using SImulator.Model;

namespace SImulator.ViewModel.Core
{
    /// <summary>
    /// Удалённое проведение игры
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IGameHost))]
    public interface IRemoteGameUI
    {
        /// <summary>
        /// Начать игру
        /// </summary>
        [OperationContract]
        void Start();

        /// <summary>
        /// Остановить игру
        /// </summary>
        [OperationContract]
        void StopGame();

        [OperationContract]
        void SetGameThemes(string[] themes);

        [OperationContract]
        void SetRoundThemes(ThemeInfoViewModel[] themes, bool isFinal);

        [OperationContract]
        void ClearBuffer();

        [OperationContract]
        void AppendToBuffer(byte[] data);

        [OperationContract]
        void SetMediaFromBuffer(string uri, bool background);

        [OperationContract]
        void SetMedia(MediaSource media, bool background);

        [OperationContract]
        void SetStage(TableStage stage);

        [OperationContract]
        void SetText(string text);

        [OperationContract]
        void SetQuestionContentType(QuestionContentType questionContentType);

        [OperationContract]
        void SetQuestionStyle(QuestionStyle questionStyle);

        [OperationContract]
        void SetQuestionSound(bool sound);

        [OperationContract]
        void AddPlayer();

        [OperationContract]
        void RemovePlayer(string playerName);

        [OperationContract]
        void ClearPlayers();

        [OperationContract]
        void UpdatePlayerInfo(int index, PlayerInfo player);

        [OperationContract]
        void UpdateSettings(Settings settings);

        [OperationContract]
        void SetSound(string sound);

        [OperationContract]
        void PlaySimpleSelection(int theme, int quest);

        [OperationContract]
        void PlayComplexSelection(int theme, int quest, bool setActive);

        [OperationContract]
        void PlaySelection(int theme);

        [OperationContract]
        void SetPlayer(int playerIndex);

        [OperationContract]
        void AddLostButtonPlayer(string name);

        [OperationContract]
        void ClearLostButtonPlayers();

        [OperationContract]
        void SeekMedia(int position);

        [OperationContract]
        void RunMedia();

        [OperationContract]
        void StopMedia();

        [OperationContract]
        void RestoreQuestion(int themeIndex, int questionIndex, int price);
    }
}
